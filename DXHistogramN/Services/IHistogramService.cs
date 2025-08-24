using System.Collections.Generic;
using DXHistogramN.Models;

namespace DXHistogramN.Services
{
    public interface IHistogramService
    {
        List<HistogramBin> CreateHistogramBins(List<double> values, Decimal binCount = 10, double? customMin = null, double? customMax = null);
        DataStatistics CalculateStatistics(List<double> values);
    }
}