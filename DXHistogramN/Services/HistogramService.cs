using System;
using System.Collections.Generic;
using System.Linq;
using DXHistogram.Models;

namespace DXHistogram.Services
{
    public class HistogramService : IHistogramService
    {
        public List<HistogramBin> CreateHistogramBins(List<double> values, int binCount = 10, double? customMin = null, double? customMax = null)
        {
            if (!values.Any()) return new List<HistogramBin>();

            var min = customMin ?? values.Min();
            var max = customMax ?? values.Max();

            if (min >= max)
                throw new ArgumentException("Minimum value must be less than maximum value.");

            if (Math.Abs(max - min) < double.Epsilon && customMin == null && customMax == null)
            {
                return new List<HistogramBin>
                {
                    new HistogramBin
                    {
                        Range = $"{min:F2}",
                        Frequency = values.Count,
                        LowerBound = min,
                        UpperBound = min
                    }
                };
            }

            var binWidth = (max - min) / binCount;
            var bins = new List<HistogramBin>();

            for (int i = 0; i < binCount; i++)
            {
                var lowerBound = min + (i * binWidth);
                var upperBound = min + ((i + 1) * binWidth);

                var count = i == binCount - 1
                    ? values.Count(v => v >= lowerBound && v <= upperBound)
                    : values.Count(v => v >= lowerBound && v < upperBound);

                bins.Add(new HistogramBin
                {
                    Range = $"{lowerBound:F2}-{upperBound:F2}",
                    Frequency = count,
                    LowerBound = lowerBound,
                    UpperBound = upperBound
                });
            }

            return bins;
        }

        public DataStatistics CalculateStatistics(List<double> values)
        {
            if (!values.Any())
            {
                return new DataStatistics
                {
                    Count = 0,
                    Mean = 0,
                    StandardDeviation = 0,
                    Minimum = 0,
                    Maximum = 0
                };
            }

            var count = values.Count;
            var mean = values.Average();
            var variance = values.Sum(x => Math.Pow(x - mean, 2)) / count;
            var stdDev = Math.Sqrt(variance);

            return new DataStatistics
            {
                Count = count,
                Mean = mean,
                StandardDeviation = stdDev,
                Minimum = values.Min(),
                Maximum = values.Max()
            };
        }
    }
}