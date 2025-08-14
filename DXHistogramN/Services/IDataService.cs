using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DXHistogramN.Services
{
    public interface IDataService
    {
        Task<List<double>> LoadDataFromFileAsync(string filePath);
        Task SaveDataToFileAsync(string filePath, List<double> data);
        List<double> ParseDataFromString(string input);
        List<double> GenerateSampleData(int count, double mean, double stdDev);
    }
}