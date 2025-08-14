using System;
using System.Xml.Serialization;

namespace DXHistogramN.Models
{
    [Serializable]
    public class HistogramConfiguration
    {
        public int BinCount { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public DateTime SavedDateTime { get; set; }
        public string Description { get; set; }
        public DataStatistics Statistics { get; set; }
    }

    [Serializable]
    public class ChartLayoutWithMetadata
    {
        public HistogramConfiguration HistogramConfig { get; set; }
        [XmlElement("DevExpressChartLayout")]
        public string ChartLayoutXml { get; set; }
        public string Version { get; set; } = "1.0";
    }
}