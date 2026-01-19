using System;
using System.Collections.Generic;
using System.Linq;

namespace Student_Job_Finder.Services
{
    public class JobMatchingService
    {
        public static decimal ComputeJobMatchScore(
            List<decimal> studentSkills,
            List<decimal> jobRequiredSkills
        )
        {
            if (studentSkills.Count != jobRequiredSkills.Count)
                throw new ArgumentException("Vectors must have the same length");

            decimal totalScore = 0m;
            int requiredSkillCount = 0;

            for (int i = 0; i < jobRequiredSkills.Count; i++)
            {
                decimal required = jobRequiredSkills[i];
                decimal student = studentSkills[i];

                if (required <= 0m)
                    continue;

                requiredSkillCount++;

                if (student >= required)
                {
                    totalScore += 1m;
                }
                else if (student > 0m)
                {
                    totalScore += student / required;
                }

            }

            if (requiredSkillCount == 0)
                return 0m;

            return totalScore / requiredSkillCount;
        }

        public static decimal NormalizePrice(decimal price, string pricePeriod)
        {
            decimal workedHoursPerMonth = 160m;

            if (pricePeriod == "Month")
                return price;

            if (pricePeriod == "Hour")
                return price * workedHoursPerMonth;

            return 0m;
        }
    }
}
