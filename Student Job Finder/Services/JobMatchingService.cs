using System;
using System.Collections.Generic;
using System.Linq;

namespace Student_Job_Finder.Services
{
    public class JobMatchingService
    {
        private const decimal NOT_RATED = -1m;

        public static decimal ComputeRowMean(List<decimal> row)
        {
            var ratedValues = row.Where(r => r != NOT_RATED).ToList();
            if (!ratedValues.Any()) return 0m;
            return ratedValues.Average();
        }

        public static decimal ComputeCosineSimilarity(List<decimal> row1, List<decimal> row2)
        {
            if (row1.Count != row2.Count)
                throw new ArgumentException("Rows must have the same length");

            decimal mean1 = ComputeRowMean(row1);
            decimal mean2 = ComputeRowMean(row2);

            decimal numerator = 0m;
            decimal denom1 = 0m;
            decimal denom2 = 0m;

            for (int i = 0; i < row1.Count; i++)
            {
                if (row1[i] != NOT_RATED && row2[i] != NOT_RATED)
                {
                    decimal diff1 = row1[i] - mean1;
                    decimal diff2 = row2[i] - mean2;

                    numerator += diff1 * diff2;
                    denom1 += diff1 * diff1;
                    denom2 += diff2 * diff2;
                }
            }

            decimal denominator = (decimal)Math.Sqrt((double)denom1) * (decimal)Math.Sqrt((double)denom2);

            if (denominator == 0m) return 0m;

            return numerator / denominator;
        }
    }
}
