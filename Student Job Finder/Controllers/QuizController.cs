using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;
using Student_Job_Finder.Services;
using System.Data;

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
            string sql = "SELECT * FROM JobFinderSchema.Posts WHERE PostId = @PostId";

            DynamicParameters postParameters = new DynamicParameters();
            postParameters.Add("PostId", jobPostId, DbType.Int32);

            var post = _dapper.LoadDataSingleWithParameters<JobPost>(sql, postParameters);

            string questionsSql = "SELECT * FROM JobFinderSchema.QuizQuestions WHERE JobPostId = @PostId";

            DynamicParameters questionsParameters = new DynamicParameters();
            questionsParameters.Add("PostId", jobPostId, DbType.Int32);

            var questions = _dapper.LoadDataWithParameters<QuizQuestion>(questionsSql, questionsParameters);

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
            string sql = @"
                INSERT INTO JobFinderSchema.QuizQuestions 
                    (JobPostId, SkillName, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectOption) 
                VALUES 
                    (@JobPostId, @SkillName, @QuestionText, @OptionA, @OptionB, @OptionC, @OptionD, @CorrectOption)";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("JobPostId", question.JobPostId, DbType.Int32);
            parameters.Add("SkillName", question.SkillName, DbType.String);
            parameters.Add("QuestionText", question.QuestionText, DbType.String);
            parameters.Add("OptionA", question.OptionA, DbType.String);
            parameters.Add("OptionB", question.OptionB, DbType.String);
            parameters.Add("OptionC", question.OptionC, DbType.String);
            parameters.Add("OptionD", question.OptionD, DbType.String);
            parameters.Add("CorrectOption", question.CorrectOption, DbType.String);

            if (_dapper.ExecuteSqlWithParameters(sql, parameters))
            {
                return RedirectToAction("AddQuiz", "Quiz", new { jobPostId = question.JobPostId });
            }

            throw new Exception("Failed to add question");
        }

        [HttpGet("GetQuestions/{jobPostId}")]
        public IActionResult GetQuestions(int jobPostId)
        {
            string sql = "SELECT * FROM JobFinderSchema.QuizQuestions WHERE QuizId = @PostId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("PostId", jobPostId, DbType.Int32);

            var questions = _dapper.LoadDataWithParameters<QuizQuestion>(sql, parameters);
            return Ok(questions);
        }

        [HttpPost("DeleteQuestion/{questionId}")]
        public IActionResult DeleteQuestion(int questionId, int postId)
        {
            string sql = "DELETE FROM JobFinderSchema.QuizQuestions WHERE QuestionId = @QuestionId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("QuestionId", questionId, DbType.Int32);

            if (_dapper.ExecuteSqlWithParameters(sql, parameters))
            {
                return RedirectToAction("AddQuiz", "Quiz", new { jobPostId = postId });
            }

            throw new Exception("Failed to delete question");
        }

        [HttpGet("TakeQuiz/{quizId}")]
        public IActionResult TakeQuiz(int quizId)
        {
            string quizSql = "SELECT * FROM JobFinderSchema.Quizzes WHERE QuizId = @QuizId";

            DynamicParameters quizParameters = new DynamicParameters();
            quizParameters.Add("QuizId", quizId, DbType.Int32);

            var quiz = _dapper.LoadDataSingleWithParameters<Quiz>(quizSql, quizParameters);

            string appIdSql = "SELECT JobPostId FROM JobFinderSchema.JobApplications WHERE JobApplicationId = @JobApplicationId";

            DynamicParameters appParameters = new DynamicParameters();
            appParameters.Add("JobApplicationId", quiz.JobApplicationId, DbType.Int32);

            int jobPostId = _dapper.LoadDataSingleWithParameters<int>(appIdSql, appParameters);

            string questionsSql = "SELECT * FROM JobFinderSchema.QuizQuestions WHERE JobPostId = @JobPostId";

            DynamicParameters questionsParameters = new DynamicParameters();
            questionsParameters.Add("JobPostId", jobPostId, DbType.Int32);

            var questions = _dapper.LoadDataWithParameters<QuizQuestion>(questionsSql, questionsParameters);

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
                WHERE Quiz.QuizId = @QuizId";

            DynamicParameters jobPostParams = new DynamicParameters();
            jobPostParams.Add("QuizId", submission.QuizId, DbType.Int32);

            int jobPostId = _dapper.LoadDataSingleWithParameters<int>(jobPostSql, jobPostParams);

            string quizQuestionsSql = "SELECT * FROM JobFinderSchema.QuizQuestions WHERE JobPostId = @JobPostId";

            DynamicParameters questionsParams = new DynamicParameters();
            questionsParams.Add("JobPostId", jobPostId, DbType.Int32);

            var quizQuestions = _dapper.LoadDataWithParameters<QuizQuestion>(quizQuestionsSql, questionsParams).ToList();

            var calculatedSkills = QuizService.CalculateScores(submission.QuizId, quizQuestions, submission.Answers);

            foreach (var skill in calculatedSkills)
            {
                string insertSql = @"
            INSERT INTO JobFinderSchema.QuizSkills (QuizId, SkillName, SkillScore) 
            VALUES (@QuizId, @SkillName, @SkillScore)";

                DynamicParameters skillParams = new DynamicParameters();
                skillParams.Add("QuizId", skill.QuizId, DbType.Int32);
                skillParams.Add("SkillName", skill.SkillName, DbType.String);
                skillParams.Add("SkillScore", skill.SkillScore, DbType.Decimal);

                _dapper.ExecuteSqlWithParameters(insertSql, skillParams);
            }

            string updateSql = "UPDATE JobFinderSchema.Quizzes SET CompletedAt = GETDATE() WHERE QuizId = @QuizId";

            DynamicParameters updateParams = new DynamicParameters();
            updateParams.Add("QuizId", submission.QuizId, DbType.Int32);

            _dapper.ExecuteSqlWithParameters(updateSql, updateParams);

            return RedirectToAction("MatchJobs", "JobMatching");
        }


    }
}
