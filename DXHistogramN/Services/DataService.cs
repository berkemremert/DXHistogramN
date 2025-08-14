using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DXHistogram.Services;

namespace DXHistogram.Services
{
    public class DataService : IDataService
    {
        public async Task<List<double>> LoadDataFromFileAsync(string filePath)
        {
            var content = await File.ReadAllTextAsync(filePath);
            return ParseDataFromString(content);
        }

        public async Task SaveDataToFileAsync(string filePath, List<double> data)
        {
            string content;
            string extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".csv")
            {
                content = "Value" + Environment.NewLine +
                         string.Join(Environment.NewLine, data.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                content = string.Join(Environment.NewLine, data.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            }

            await File.WriteAllTextAsync(filePath, content);
        }

        public List<double> ParseDataFromString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<double>();

            var dataValues = new List<double>();
            var separators = new char[] { ',', ' ', '\t', '\n', '\r', ';' };
            var tokens = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                if (double.TryParse(token.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    dataValues.Add(value);
                }
            }

            return dataValues;
        }

        public List<double> GenerateSampleData(int count, double mean, double stdDev)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var data = new List<double>();

            for (int i = 0; i < count; i++)
            {
                double value = GenerateNormalDistribution(random, mean, stdDev);
                data.Add(value);
            }

            return data;
        }

        private double GenerateNormalDistribution(Random random, double mean, double stdDev)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }
    }
}