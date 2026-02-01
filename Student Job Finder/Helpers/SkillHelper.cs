namespace Student_Job_Finder.Helpers
{
    public enum SkillLevel
    {
        Beginner = 0,
        Intermediate = 1,
        Advanced = 2,
        Expert = 3
    }

    public class SkillHelper
    {
        public static SkillLevel GetSkillLevel(decimal score)
        {
            if (score >= 0.85m) return SkillLevel.Expert;
            if (score >= 0.75m) return SkillLevel.Advanced;
            if (score >= 0.65m) return SkillLevel.Intermediate;
            return SkillLevel.Beginner;
        }

        public static string SkillLevelName(decimal score)
        {
            if (score >= 0.85m) return "Expert";
            if (score >= 0.75m) return "Advanced";
            if (score >= 0.65m) return "Intermediate";
            return "Beginner";
        }

        public static string SkillColorClass(decimal score)
        {
            if (score >= 0.85m) return "skill-expert";
            if (score >= 0.75m) return "skill-advanced";
            if (score >= 0.65m) return "skill-intermediate";
            return "skill-beginner";
        }
    }
}
