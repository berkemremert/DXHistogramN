using System.Xml.Serialization;

namespace DXHistogramN.Models
{
    // Update existing HistogramConfiguration to include HistogramName
    [System.Serializable]
    public class HistogramConfiguration
    {
        public string HistogramName { get; set; }
        public Decimal BinCount { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public DateTime SavedDateTime { get; set; }
        public string Description { get; set; }
        public DataStatistics Statistics { get; set; }
    }

    [System.Serializable]
    public class UnifiedHistogramConfiguration
    {
        public DateTime SavedDateTime { get; set; }
        public string Description { get; set; }
        public System.Collections.Generic.List<HistogramConfiguration> Histograms { get; set; }
    }

    [System.Serializable]
    public class UnifiedChartLayoutWithMetadata
    {
        public UnifiedHistogramConfiguration UnifiedConfig { get; set; }
        [System.Xml.Serialization.XmlElement("DevExpressChartLayout")]
        public string ChartLayoutXml { get; set; }
        public string Version { get; set; } = "2.0";
    }

    // Missing model classes for your enhanced service
    [System.Serializable]
    public class HistogramChartLayout
    {
        public int ChartIndex { get; set; }
        public string HistogramName { get; set; }
        public HistogramConfiguration Configuration { get; set; }
        public string ChartLayoutXml { get; set; }
        public bool HasData { get; set; }
        public int DataPointCount { get; set; }
    }

    [System.Serializable]
    public class MultiChartLayout
    {
        public DateTime SavedDateTime { get; set; }
        public string Description { get; set; }
        public System.Collections.Generic.List<HistogramChartLayout> HistogramLayouts { get; set; }
        public string Version { get; set; } = "1.0";
    }

    [System.Serializable]
    public class ConsolidatedLayoutMetadata
    {
        public string FileName { get; set; }
        public DateTime SavedDateTime { get; set; }
        public string Description { get; set; }
        public int TotalCharts { get; set; }
        public System.Collections.Generic.List<string> HistogramNames { get; set; }
        public System.Collections.Generic.List<int> DataPointCounts { get; set; }
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