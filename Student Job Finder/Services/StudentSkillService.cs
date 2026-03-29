using Dapper;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using System.Data;

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

        string deleteSql = @"
        DELETE FROM JobFinderSchema.StudentSkills
        WHERE StudentId = @UserId";

        DynamicParameters deleteParams = new DynamicParameters();
        deleteParams.Add("UserId", userId, DbType.String);

        _dapper.ExecuteSqlWithParameters(deleteSql, deleteParams);

        foreach (var skill in skills)
        {
            string sql = @"
            INSERT INTO JobFinderSchema.StudentSkills
            ([StudentId], [SkillName], [SkillScore])
            VALUES (@UserId, @SkillName, @SkillScore)";

            DynamicParameters insertParams = new DynamicParameters();
            insertParams.Add("UserId", userId, DbType.String);
            insertParams.Add("SkillName", skill.SkillName, DbType.String);
            insertParams.Add("SkillScore", skill.SkillScore, DbType.Decimal);

            if (!_dapper.ExecuteSqlWithParameters(sql, insertParams))
                throw new Exception($"Failed to add skill: {skill.SkillName}");
        }
    }
}