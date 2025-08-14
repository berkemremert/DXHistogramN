using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DXHistogramN.Models;

namespace DXHistogramN.Services
{
    public class ChartLayoutService : IChartLayoutService
    {
        private const string EXTENDED_LAYOUT_ROOT = "HistogramChartLayout";

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

            // Check if it's an extended layout file
            if (!IsExtendedLayoutFile(filePath))
            {
                // It's a regular DevExpress layout file, read as plain XML
                var plainXml = await File.ReadAllTextAsync(filePath);
                return new ChartLayoutWithMetadata
                {
                    ChartLayoutXml = plainXml,
                    HistogramConfig = new HistogramConfiguration
                    {
                        BinCount = 10, // Default values
                        Description = "Legacy layout file (no metadata)"
                    }
                };
            }

            // It's an extended layout file with metadata
            var serializer = new XmlSerializer(typeof(ChartLayoutWithMetadata), new XmlRootAttribute(EXTENDED_LAYOUT_ROOT));

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                return (ChartLayoutWithMetadata)serializer.Deserialize(fileStream);
            }
        }

        public bool IsExtendedLayoutFile(string filePath)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);
                return xmlDoc.DocumentElement?.Name == EXTENDED_LAYOUT_ROOT;
            }
            catch
            {
                return false;
            }
        }
    }
}