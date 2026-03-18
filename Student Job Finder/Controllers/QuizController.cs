using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;
using Student_Job_Finder.Services;

namespace Student_Job_Finder.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class QuizController : Controller
    {
        private readonly DataContextDapper _dapper;

        public QuizController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("AddQuiz/{jobPostId}")]
        public IActionResult AddQuiz(int jobPostId)
        {
            string sql = "SELECT * FROM JobFinderSchema.Posts WHERE PostId = " + jobPostId;
            var post = _dapper.LoadDataSingle<JobPost>(sql);

            string questionsSql = "SELECT * FROM JobFinderSchema.QuizQuestions WHERE JobPostId = " + jobPostId;
            var questions = _dapper.LoadData<QuizQuestion>(questionsSql);

            var model = new JobSkillsViewModel
            {
                PostId = jobPostId,
                PostTitle = post.PostTitle,
                ExistingQuestions = questions.ToList()
            };

            return View(model);
        }

        [HttpPost("AddQuestion")]
        public IActionResult AddQuestion(QuizQuestionToAddDto question)
        {
            string sql = "INSERT INTO JobFinderSchema.QuizQuestions " +
             "(JobPostId, SkillName, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectOption) " +
             "VALUES (" +
             question.JobPostId + ", '" +
             question.SkillName + "', '" +
             question.QuestionText + "', '" +
             question.OptionA + "', '" +
             question.OptionB + "', '" +
             question.OptionC + "', '" +
             question.OptionD + "', '" +
             question.CorrectOption + "')";

            if (_dapper.ExecuteSql(sql))
            {
                return RedirectToAction("AddQuiz", "Quiz", new { jobPostId = question.JobPostId });
            }
            throw new Exception("Failed to add question");
        }

        [HttpGet("GetQuestions/{jobPostId}")]
        public IActionResult GetQuestions(int jobPostId)
        {
            string sql = "SELECT * FROM JobFinderSchema.QuizQuestions WHERE QuizId = " + jobPostId;
            var questions = _dapper.LoadData<QuizQuestion>(sql);
            return Ok(questions);
        }

        [HttpPost("DeleteQuestion/{questionId}")]
        public IActionResult DeleteQuestion(int questionId, int postId)
        {
            string sql = "DELETE FROM JobFinderSchema.QuizQuestions WHERE QuestionId = " + questionId;

            if (_dapper.ExecuteSql(sql))
            {
                return RedirectToAction("AddQuiz", "Quiz", new { jobPostId = postId });
            }

            throw new Exception("Failed to delete question");
        }

        [HttpGet("TakeQuiz/{quizId}")]
        public IActionResult TakeQuiz(int quizId)
        {
            string quizSql = "SELECT * FROM JobFinderSchema.Quizzes WHERE QuizId = " + quizId;
            var quiz = _dapper.LoadDataSingle<Quiz>(quizSql);

            string appIdSql = "SELECT JobPostId FROM JobFinderSchema.JobApplications WHERE JobApplicationId = " + quiz.JobApplicationId;
            int jobPostId = _dapper.LoadDataSingle<int>(appIdSql);

            string questionsSql = "SELECT * FROM JobFinderSchema.QuizQuestions WHERE JobPostId = " + jobPostId;
            var questions = _dapper.LoadData<QuizQuestion>(questionsSql);

            var vm = new TakeQuizViewModel
            {
                QuizId = quizId,
                JobPostId = jobPostId,
                Questions = questions.ToList()
            };

            return View(vm);
        }


        [HttpPost("SubmitQuiz")]
        public IActionResult SubmitQuiz([FromForm] QuizSubmissionDto submission)
        {
            string jobPostSql = @"
                SELECT Apps.JobPostId 
                FROM JobFinderSchema.Quizzes AS Quiz
                JOIN JobFinderSchema.JobApplications AS Apps 
                ON Quiz.JobApplicationId = Apps.JobApplicationId 
                WHERE Quiz.QuizId = " + submission.QuizId;

            int jobPostId = _dapper.LoadDataSingle<int>(jobPostSql);

            string quizQuestionsSql = "SELECT * FROM JobFinderSchema.QuizQuestions WHERE JobPostId = " + jobPostId;
            var quizQuestions = _dapper.LoadData<QuizQuestion>(quizQuestionsSql).ToList();
;
            var calculatedSkills = QuizService.CalculateScores(submission.QuizId, quizQuestions, submission.Answers);

            foreach (var skill in calculatedSkills)
            {
                string insertSql = $@"
                    INSERT INTO JobFinderSchema.QuizSkills (QuizId, SkillName, SkillScore) 
                    VALUES ({skill.QuizId}, '{skill.SkillName}', {skill.SkillScore})";

                _dapper.ExecuteSql(insertSql);
            }

            _dapper.ExecuteSql("UPDATE JobFinderSchema.Quizzes SET CompletedAt = GETDATE() WHERE QuizId = " + submission.QuizId);

            return RedirectToAction("MatchJobs", "JobMatching");
        }


    }
}
