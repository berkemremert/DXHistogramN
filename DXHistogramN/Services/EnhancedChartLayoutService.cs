using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DXHistogramN.Models;

namespace DXHistogramN.Services
{
    public interface IEnhancedChartLayoutService : IChartLayoutService
    {
        Task SaveMultiChartLayoutAsync(string filePath, List<HistogramChartLayout> histogramLayouts, string description = null);
        Task<MultiChartLayout> LoadMultiChartLayoutAsync(string filePath);
        bool IsMultiChartLayoutFile(string filePath);
        Task<List<ConsolidatedLayoutMetadata>> GetLayoutMetadataListAsync(string directoryPath);
    }

    public class EnhancedChartLayoutService : ChartLayoutService, IEnhancedChartLayoutService
    {
        private const string MULTI_CHART_LAYOUT_ROOT = "MultiHistogramChartLayout";

        public async Task SaveMultiChartLayoutAsync(string filePath, List<HistogramChartLayout> histogramLayouts, string description = null)
        {
            var multiLayout = new MultiChartLayout
            {
                SavedDateTime = DateTime.Now,
                Description = description ?? $"Multi-chart layout with {histogramLayouts.Count} histograms",
                HistogramLayouts = histogramLayouts
            };

            var serializer = new XmlSerializer(typeof(MultiChartLayout), new XmlRootAttribute(MULTI_CHART_LAYOUT_ROOT));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true }))
            {
                serializer.Serialize(xmlWriter, multiLayout);
            }
        }

        public async Task<MultiChartLayout> LoadMultiChartLayoutAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Layout file not found: {filePath}");

            if (!IsMultiChartLayoutFile(filePath))
            {
                // Try to convert from single chart layout if possible
                var singleLayout = await LoadLayoutWithMetadataAsync(filePath);
                return new MultiChartLayout
                {
                    SavedDateTime = DateTime.Now,
                    Description = "Converted from single chart layout",
                    HistogramLayouts = new List<HistogramChartLayout>
                    {
                        new HistogramChartLayout
                        {
                            ChartIndex = 0,
                            HistogramName = singleLayout.HistogramConfig?.HistogramName ?? "Histogram 1",
                            Configuration = singleLayout.HistogramConfig,
                            ChartLayoutXml = singleLayout.ChartLayoutXml,
                            HasData = singleLayout.HistogramConfig?.Statistics?.Count > 0,
                            DataPointCount = singleLayout.HistogramConfig?.Statistics?.Count ?? 0
                        }
                    }
                };
            }

            var serializer = new XmlSerializer(typeof(MultiChartLayout), new XmlRootAttribute(MULTI_CHART_LAYOUT_ROOT));

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                return (MultiChartLayout)serializer.Deserialize(fileStream);
            }
        }

        public bool IsMultiChartLayoutFile(string filePath)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);
                return xmlDoc.DocumentElement?.Name == MULTI_CHART_LAYOUT_ROOT;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ConsolidatedLayoutMetadata>> GetLayoutMetadataListAsync(string directoryPath)
        {
            var metadata = new List<ConsolidatedLayoutMetadata>();

            if (!Directory.Exists(directoryPath))
                return metadata;

            var xmlFiles = Directory.GetFiles(directoryPath, "*.xml");

            foreach (var file in xmlFiles)
            {
                try
                {
                    if (IsMultiChartLayoutFile(file))
                    {
                        var layout = await LoadMultiChartLayoutAsync(file);
                        metadata.Add(new ConsolidatedLayoutMetadata
                        {
                            FileName = Path.GetFileName(file),
                            SavedDateTime = layout.SavedDateTime,
                            Description = layout.Description,
                            TotalCharts = layout.HistogramLayouts.Count,
                            HistogramNames = layout.HistogramLayouts.Select(h => h.HistogramName).ToList(),
                            DataPointCounts = layout.HistogramLayouts.Select(h => h.DataPointCount).ToList()
                        });
                    }
                    else if (IsExtendedLayoutFile(file))
                    {
                        var layout = await LoadLayoutWithMetadataAsync(file);
                        metadata.Add(new ConsolidatedLayoutMetadata
                        {
                            FileName = Path.GetFileName(file),
                            SavedDateTime = layout.HistogramConfig?.SavedDateTime ?? DateTime.MinValue,
                            Description = layout.HistogramConfig?.Description ?? "Single chart layout",
                            TotalCharts = 1,
                            HistogramNames = new List<string> { layout.HistogramConfig?.HistogramName ?? "Unknown" },
                            DataPointCounts = new List<int> { layout.HistogramConfig?.Statistics?.Count ?? 0 }
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Skip problematic files but log the error
                    System.Diagnostics.Debug.WriteLine($"Error reading layout file {file}: {ex.Message}");
                }
            }

            return metadata.OrderByDescending(m => m.SavedDateTime).ToList();
        }
    }
}