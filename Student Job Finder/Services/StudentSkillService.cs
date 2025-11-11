using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;

public class StudentSkillService
{
    private readonly DataContextDapper _dapper;

    public StudentSkillService(IConfiguration config)
    {
        _dapper = new DataContextDapper(config);
    }

    public void AddSkills(string userId, List<StudentSkillToAddDto> skills)
    {
        if (string.IsNullOrEmpty(userId) || skills == null || skills.Count == 0)
            throw new Exception("Invalid user or no skills provided");

        foreach (var skill in skills)
        {
            string sql = $@"
                INSERT INTO JobFinderSchema.StudentSkills
                ([StudentId], [SkillName], [SkillScore])
                VALUES ({userId}, '{skill.SkillName}', {skill.SkillScore})
            ";

            if (!_dapper.ExecuteSql(sql))
                throw new Exception($"Failed to add skill: {skill.SkillName}");
        }
    }
}