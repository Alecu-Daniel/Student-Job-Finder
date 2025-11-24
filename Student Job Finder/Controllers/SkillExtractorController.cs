using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Dtos;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
namespace Student_Job_Finder.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class SkillExtractorController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly StudentSkillService _skillsService;
        private readonly string _apiKey;

        public SkillExtractorController(IConfiguration config, StudentSkillService skillsService)
        {

            _httpClient = new HttpClient();

            _apiKey = config.GetSection("AppSettings:ApiKey").Value;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _skillsService = skillsService;
        }


        private async Task<string> UploadFileToOpenAI(IFormFile file)
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            var streamContent = new StreamContent(fileStream);

            
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            content.Add(streamContent, "file", file.FileName);
            content.Add(new StringContent("assistants"), "purpose");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/files", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"File upload failed: {response.StatusCode}, {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("id").GetString();
        }


        private async Task<string> ExtractCoursesJsonFromPdf(string fileId)
        {
            string prompt = @"
            You are a reliable JSON data extractor for a university transcript.
            The data provided comes from a single table with the following key data columns at the end of each row:
            ... | Nota (Grade) | Data examinării (Date) | Cred (Credits)

            **GOAL:** Extract the course name, its final grade, and its credits into a JSON array.

            Output ONLY a JSON array like this:
            [
              { ""name"": ""<course name>"", ""grade"": <number or string>, ""credits"": <number> }
            ]

            Parsing rules:
            1. **Grade Isolation (Highest Priority):** The Grade ('Nota') is the **SINGLE numerical or textual value (e.g., 8, 10, or 'Adm') that is IMMEDIATELY followed by a date in the format DD-MM-YYYY (the 'Data examinării' field).**
            2. **Credits Isolation (Positional):** The Credits value ('Cred') is the **ABSOLUTE LAST SINGLE TOKEN on the entire line**, and it always follows the date field.
            3. **Guard against Confusion:** Explicitly ignore any other numbers that precede the Grade/Date/Credits block (these are the 'Total ore/sem' hours, like 42, 28, 14, etc.).
            4. **CRITICAL PROJECT OVERRIDE (Hard Anchor):** For courses beginning with **'Project I' or 'Project II'**, the absolute token sequence near the end of the line is always: `... [Grade] [Date] [Credits]`. If the Credits value is '2', **you MUST ensure the Grade is the number token that IMMEDIATELY precedes the Date token.** The Credit value **MUST NOT** be used as the Grade value under any circumstance.
            5. **Data Type and Credit Conversion (STRICT):**
               - Output numeric grades (1-10) and earned credits (1-5) as **numbers**.
               - Output text grades ('Adm', 'Abs.', 'Rsp.', etc.) as **strings**.
               - **MANDATORY Failed Credit Conversion:** If the Credits value is non-numeric (e.g., '-', 'Abs.', 'Rsp.', or empty, indicating unearned credits), you **MUST** set the 'credits' value in the JSON object to **0** (zero) as a number.
            6. Preserve the course order.
            7. Output perfectly valid JSON..
            ";

            var payload = new
            {
                model = "gpt-4o-mini",
                stream = false,
                input = new[]
                {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "input_text", text = prompt },
                    new { type = "input_file", file_id = fileId }
                }
            }
        }
            };

            var requestJson = JsonSerializer.Serialize(payload);
            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/responses",
                new StringContent(requestJson, Encoding.UTF8, "application/json")
            );

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenAI API error: {response.StatusCode}, {responseContent}");

            using var root = JsonDocument.Parse(responseContent);
            var rootEl = root.RootElement;

            if (rootEl.TryGetProperty("output_text", out var outputText))
                return outputText.GetString()!;

            if (rootEl.TryGetProperty("output", out var outputArr) &&
                outputArr.ValueKind == JsonValueKind.Array &&
                outputArr.GetArrayLength() > 0)
            {
                var first = outputArr[0];
                if (first.TryGetProperty("content", out var contentArr))
                {
                    foreach (var item in contentArr.EnumerateArray())
                        if (item.TryGetProperty("text", out var textNode))
                            return textNode.GetString()!;
                }
            }

            Console.WriteLine("DEBUG RAW RESPONSE:\n" + responseContent);
            throw new Exception("Could not extract JSON from GPT response.");
        }

        
        private async Task<string> CalculateSkillsFromJson(JsonElement coursesJson)
        {
            string prompt = $@"
        You are an expert academic-data extractor and IT skill inference engine.
        You are given a JSON array of courses with numeric grades and credits.

        Rules:
        1. Only assign courses to the following predefined categories exactly as written (case-sensitive):
           [""Software Development"", ""Algorithms and Data Structures"", ""Computer Architecture"",
            ""Artificial Intelligence"", ""Computer Graphics"", ""Operating Systems"", ""Database Management"",
            ""Web Development"", ""Networking"", ""Distributed Systems""].
        2. Do NOT invent new category names or reorder words in the categories.
        3. Infer IT-related skills from course names, but always map them to one of the predefined categories.

        Grade handling:
           - Grades are numeric 0–10.
           - Grade < 5 means failed (low competency contribution).
           - Grade = 0 means ""Abs."" (treat as failed).
           - Ignore no course entirely; all courses count toward a category.
           - When aggregating, treat failed or absent grades (<5) as 0.25 competency

        4. Competency scoring rules:
           • Extract numeric course grades (1–10) related to each skill.
           • Ignore non-numeric grades (Adm, Rsp, etc.).
           • If no numeric grades are found, assume 7.0 as the average grade.

           • If multiple related courses exist:
                - Let each course have:
                     raw_importance in [1–5]
                     credits in (2, 4, 5)
                     credit_weight = 1.0 if credits=5 (exam)
                                     0.8 if credits=4 (colloquium)
                                     0.5 if credits=2 (project)
                - Compute:
                     effective_weight_i = raw_importance_i × credit_weight_i
                     normalized_weight_i = effective_weight_i / Σ(effective_weight)
                     average_grade = Σ(normalized_weight_i × grade_i)
           • Compute competency_score on a 0–1 scale:
             competency_score = average_grade / 10.0
             (for example, a grade of 8.5 becomes 0.85)
           5. Aggregate multiple courses in the same category using weighted average if needed.
           6. Return ONLY valid JSON in this format:
              [
                {{ ""SkillName"": ""<category>"", ""SkillScore"": <float between 0.5 and 1.0> }}
              ]
        Do not include explanations, synonyms, or extra text.

        Input JSON:
{coursesJson.GetRawText()}
";

            var payload = new
            {
                model = "gpt-4o-mini",
                temperature = 0,
                top_p = 1,
                messages = new[]
    {
        new { role = "user", content = prompt }
    }
            };

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            );

            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("OpenAI API error (Step 2): " + responseContent);
                throw new Exception($"OpenAI API returned status {response.StatusCode}");
            }

            
            var root = JsonDocument.Parse(responseContent).RootElement;
            var skillJsonText = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()!;

            return skillJsonText;
        }

        
        private List<StudentSkillToAddDto> MapJsonToDto(string skillJson)
        {
            var skillsToAdd = new List<StudentSkillToAddDto>();
            var root = JsonDocument.Parse(skillJson).RootElement;

            foreach (var skill in root.EnumerateArray())
            {
                skillsToAdd.Add(new StudentSkillToAddDto
                {
                    SkillName = skill.GetProperty("SkillName").GetString() ?? "",
                    SkillScore = Convert.ToDecimal(skill.GetProperty("SkillScore").GetDouble())
                });
            }

            return skillsToAdd;
        }

        [HttpPost("ExtractSkills")]
        public async Task<IActionResult> ExtractSkills(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            string fileId = await UploadFileToOpenAI(file);

            
            string coursesJsonRaw = await ExtractCoursesJsonFromPdf(fileId);

            
            var match = Regex.Match(coursesJsonRaw, @"\[\s*{.*}\s*\]", RegexOptions.Singleline);
            if (!match.Success)
                return BadRequest("Could not find JSON array in GPT response from PDF extraction.");

            string coursesJsonText = match.Value;

            JsonElement coursesJson;
            try
            {
                coursesJson = JsonDocument.Parse(coursesJsonText).RootElement;
            }
            catch (JsonException)
            {
                return BadRequest("Failed to parse courses JSON from GPT response.");
            }

            
            string skillJsonText = await CalculateSkillsFromJson(coursesJson);

            
            var skillMatch = Regex.Match(skillJsonText, @"\[\s*{.*}\s*\]", RegexOptions.Singleline);
            if (!skillMatch.Success)
                return BadRequest("Could not find skills JSON array in GPT response.");

            string finalSkillJson = skillMatch.Value;

            List<StudentSkillToAddDto> skillsToAdd;
            try
            {
                skillsToAdd = MapJsonToDto(finalSkillJson);
            }
            catch (JsonException)
            {
                return BadRequest("Failed to parse skills JSON from GPT response.");
            }

            
            var userId = this.User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            _skillsService.AddSkills(userId, skillsToAdd);

            return RedirectToAction("MySkills", "StudentSkill");
        }

    }

}

