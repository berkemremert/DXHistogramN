using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DXHistogramN.Models;

namespace DXHistogramN.Services
{
    public interface IChartLayoutService
    {
        Task SaveLayoutWithMetadataAsync(string filePath, string chartLayoutXml, HistogramConfiguration config);
        Task<ChartLayoutWithMetadata> LoadLayoutWithMetadataAsync(string filePath);
        Task SaveUnifiedLayoutAsync(string filePath, string chartLayoutXml, UnifiedHistogramConfiguration config);
        Task<UnifiedChartLayoutWithMetadata> LoadUnifiedLayoutAsync(string filePath);
        bool IsExtendedLayoutFile(string filePath);
        bool IsUnifiedLayoutFile(string filePath);
    }

    public class ChartLayoutService : IChartLayoutService
    {
        private const string EXTENDED_LAYOUT_ROOT = "HistogramChartLayout";
        private const string UNIFIED_LAYOUT_ROOT = "UnifiedChartLayoutWithMetadata";

        // Individual histogram layout methods
        public async Task SaveLayoutWithMetadataAsync(string filePath, string chartLayoutXml, HistogramConfiguration config)
        {
            var layoutWithMetadata = new ChartLayoutWithMetadata
            {
                HistogramConfig = config,
                ChartLayoutXml = chartLayoutXml,
                Version = "1.0"
            };

            var serializer = new XmlSerializer(typeof(ChartLayoutWithMetadata), new XmlRootAttribute(EXTENDED_LAYOUT_ROOT));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true }))
            {
                serializer.Serialize(xmlWriter, layoutWithMetadata);
            }
        }

        public async Task<ChartLayoutWithMetadata> LoadLayoutWithMetadataAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Layout file not found: {filePath}");

            // Check if it's a unified layout file
            if (IsUnifiedLayoutFile(filePath))
            {
                throw new InvalidOperationException("This is a unified layout file. Use LoadUnifiedLayoutAsync instead.");
            }

            // Check if it's an extended layout file
            if (!IsExtendedLayoutFile(filePath))
            {
                // It's a regular DevExpress layout file, read as plain XML
                var plainXml = await File.ReadAllTextAsync(filePath);

                // Validate that it's actually a DevExpress chart layout
                if (!IsValidDevExpressLayout(plainXml))
                {
                    throw new InvalidDataException("File does not contain a valid DevExpress chart layout.");
                }

                return new ChartLayoutWithMetadata
                {
                    ChartLayoutXml = plainXml,
                    HistogramConfig = new HistogramConfiguration
                    {
                        BinCount = 10, // Default values
                        Description = "Legacy layout file (no metadata)",
                        SavedDateTime = File.GetLastWriteTime(filePath)
                    }
                };
            }

            // It's an extended layout file with metadata
            try
            {
                var serializer = new XmlSerializer(typeof(ChartLayoutWithMetadata), new XmlRootAttribute(EXTENDED_LAYOUT_ROOT));

                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var result = (ChartLayoutWithMetadata)serializer.Deserialize(fileStream);

                    // Validate the chart layout XML
                    if (string.IsNullOrEmpty(result.ChartLayoutXml))
                    {
                        throw new InvalidDataException("Chart layout XML is empty or missing.");
                    }

                    return result;
                }
            }
            catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
            {
                throw new InvalidDataException($"Invalid XML format in layout file: {ex.InnerException.Message}", ex);
            }
        }

        // Unified layout methods
        public async Task SaveUnifiedLayoutAsync(string filePath, string chartLayoutXml, UnifiedHistogramConfiguration config)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(chartLayoutXml))
                throw new ArgumentException("Chart layout XML cannot be empty", nameof(chartLayoutXml));

            if (config?.Histograms == null || config.Histograms.Count == 0)
                throw new ArgumentException("Configuration must contain at least one histogram", nameof(config));

            var unifiedLayout = new UnifiedChartLayoutWithMetadata
            {
                UnifiedConfig = config,
                ChartLayoutXml = chartLayoutXml,
                Version = "2.0"
            };

            var serializer = new XmlSerializer(typeof(UnifiedChartLayoutWithMetadata));
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true }))
            {
                serializer.Serialize(xmlWriter, unifiedLayout);
            }
        }

        public async Task<UnifiedChartLayoutWithMetadata> LoadUnifiedLayoutAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Layout file not found: {filePath}");

            if (!IsUnifiedLayoutFile(filePath))
                throw new InvalidOperationException("File is not a unified layout file.");

            try
            {
                var serializer = new XmlSerializer(typeof(UnifiedChartLayoutWithMetadata));
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var result = (UnifiedChartLayoutWithMetadata)serializer.Deserialize(fileStream);

                    // Validate the loaded data
                    if (string.IsNullOrEmpty(result.ChartLayoutXml))
                    {
                        throw new InvalidDataException("Chart layout XML is empty or missing.");
                    }

                    if (result.UnifiedConfig?.Histograms == null || result.UnifiedConfig.Histograms.Count == 0)
                    {
                        throw new InvalidDataException("Unified configuration must contain at least one histogram.");
                    }

                    return result;
                }
            }
            catch (InvalidOperationException ex) when (ex.InnerException is XmlException)
            {
                throw new InvalidDataException($"Invalid XML format in layout file: {ex.InnerException.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException($"Unable to deserialize layout file: {ex.Message}", ex);
            }
        }

        // Helper methods
        public bool IsExtendedLayoutFile(string filePath)
        {
            try
            {
                using (var reader = XmlReader.Create(filePath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
                {
                    reader.MoveToContent();
                    return reader.LocalName == EXTENDED_LAYOUT_ROOT;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool IsUnifiedLayoutFile(string filePath)
        {
            try
            {
                using (var reader = XmlReader.Create(filePath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
                {
                    reader.MoveToContent();
                    return reader.LocalName == UNIFIED_LAYOUT_ROOT;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidDevExpressLayout(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                // Check for common DevExpress chart layout elements
                return doc.DocumentElement != null &&
                       (doc.DocumentElement.Name.Contains("Chart") ||
                        doc.SelectNodes("//Series").Count > 0 ||
                        doc.SelectNodes("//Diagram").Count > 0 ||
                        doc.SelectNodes("//Pane").Count > 0);
            }
            catch
            {
                return false;
            }
        }
    }
}