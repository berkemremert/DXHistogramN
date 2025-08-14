using System.Collections.Generic;
using DXHistogram.Models;

namespace DXHistogram.Services
{
    public interface IHistogramService
    {
        List<HistogramBin> CreateHistogramBins(List<double> values, int binCount = 10, double? customMin = null, double? customMax = null);
        DataStatistics CalculateStatistics(List<double> values);
    }
}