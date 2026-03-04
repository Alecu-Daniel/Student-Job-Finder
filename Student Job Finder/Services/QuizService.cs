using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;

namespace Student_Job_Finder.Services
{
    public class QuizService
    {
        public static List<QuizSkill> CalculateScores(int quizId, List<QuizQuestion> quizQuestions, List<QuestionAnswerDto> studentAnswers)
        {
            var results = new List<QuizSkill>();

            var skillGroups = quizQuestions.GroupBy(q => q.SkillName);

            foreach (var group in skillGroups)
            {
                int totalQuestions = group.Count();
                int correctCount = 0;

                foreach (var question in group)
                {

                    var answer = studentAnswers.FirstOrDefault(a => a.QuestionId == question.QuestionId);

                    if (answer != null && answer.StudentAnswer == question.CorrectOption)
                    {
                        correctCount++;
                    }
                }

                decimal score = (decimal)correctCount / totalQuestions;

                results.Add(new QuizSkill
                {
                    QuizId = quizId,
                    SkillName = group.Key,
                    SkillScore = score
                });
            }

            return results;
        }
    }
}
