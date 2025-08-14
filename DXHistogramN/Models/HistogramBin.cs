using System;

namespace DXHistogram.Models
{
    public class HistogramBin
    {
        public string Range { get; set; }
        public int Frequency { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
    }

    public class DataStatistics
    {
        public int Count { get; set; }
        public double Mean { get; set; }
        public double StandardDeviation { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
    }
}