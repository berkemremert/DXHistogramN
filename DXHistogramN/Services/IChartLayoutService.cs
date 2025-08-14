using System.Threading.Tasks;
using DXHistogramN.Models;

namespace DXHistogramN.Services
{
    public interface IChartLayoutService
    {
        Task SaveLayoutWithMetadataAsync(string filePath, string chartLayoutXml, HistogramConfiguration config);
        Task<ChartLayoutWithMetadata> LoadLayoutWithMetadataAsync(string filePath);
        bool IsExtendedLayoutFile(string filePath);
    }
}