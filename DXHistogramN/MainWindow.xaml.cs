using DevExpress.Charts.Designer;
using DevExpress.Xpf.Charts;
using DXHistogramN.Models;
using DXHistogramN.Services;
using DXHistogramN.ViewModels;
using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace DXHistogramN
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private ChartControl _unifiedChartControl;
        private bool _isLoadingLayout = false; // Flag to prevent overwriting during load

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            // Subscribe to ViewModel events
            _viewModel.OnDesignerRequested += OpenUnifiedChartDesigner;
            _viewModel.OnSaveLayoutRequested += SaveUnifiedChartLayout;
            _viewModel.OnLayoutLoaded += HandleLayoutLoaded;
            _viewModel.OnHistogramUpdated += HandleHistogramUpdated;
            _viewModel.OnHistogramStructureChanged += HandleHistogramStructureChanged; // Add this line

            DataContext = _viewModel;
            Loaded += MainWindow_Loaded;
        }

        private void HandleHistogramStructureChanged()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Histogram structure changed - rebuilding chart");

                // Rebuild the entire chart structure
                if (!_isLoadingLayout)
                {
                    SetupUnifiedChart();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling structure change: {ex.Message}");
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Find the unified chart control
            _unifiedChartControl = FindName("unifiedChartControl") as ChartControl;

            if (_unifiedChartControl != null)
            {
                // Initialize all histograms with sample data first
                //InitializeAllHistogramsWithData();
                SetupUnifiedChart();
            }
        }

        private void InitializeAllHistogramsWithData()
        {
            // Make sure all histograms have some data to display
            var dataService = new DataService();

            for (int i = 0; i < _viewModel.Histograms.Count; i++)
            {
                var histogram = _viewModel.Histograms[i];
                if (histogram.CurrentData == null || !histogram.CurrentData.Any())
                {
                    var random = new Random(i * 1000);
                    var mean = 30 + (i * 15);
                    var stdDev = 8 + (i * 3);

                    histogram.CurrentData = dataService.GenerateSampleData(500 + i * 100, mean, stdDev);

                    var histogramService = new HistogramService();
                    var bins = histogramService.CreateHistogramBins(histogram.CurrentData, histogram.BinCount);

                    histogram.HistogramData.Clear();
                    foreach (var bin in bins)
                    {
                        histogram.HistogramData.Add(bin);
                    }

                    histogram.Statistics = histogramService.CalculateStatistics(histogram.CurrentData);
                }
            }
        }

        private void SetupUnifiedChart()
        {
            if (_unifiedChartControl == null || _isLoadingLayout)
            {
                System.Diagnostics.Debug.WriteLine("Skipping SetupUnifiedChart - chart null or loading layout");
                return;
            }

            // Create XYDiagram2D if not exists
            if (_unifiedChartControl.Diagram == null)
            {
                _unifiedChartControl.Diagram = new XYDiagram2D();
            }

            var diagram = _unifiedChartControl.Diagram as XYDiagram2D;
            if (diagram == null)
            {
                System.Diagnostics.Debug.WriteLine("Diagram is null or not XYDiagram2D!");

                return;
            }

            // Clear existing content
            diagram.Panes.Clear();
            diagram.Series.Clear();
            diagram.SecondaryAxesY.Clear();
            diagram.SecondaryAxesX.Clear();

            // Handle case when no histograms exist
            if (_viewModel.Histograms.Count == 0)
            {
                // Add a placeholder or just leave empty
                UpdateChartTitles();
                _unifiedChartControl.InvalidateVisual();
                return;
            }

            if (diagram.DefaultPane == null)
            {
                diagram.DefaultPane = new Pane();
            }
            var pane = diagram.DefaultPane;
            pane.Title = new PaneTitle
            {
                Visible = true
            };


            // Create panes and series for each histogram
            for (int i = 0; i < _viewModel.Histograms.Count; i++)
            {
                var histogram = _viewModel.Histograms[i];

                if (i == 0)
                {
                    // Use the default pane for the first histogram
                    pane = diagram.DefaultPane;
                    pane.Title = new PaneTitle
                    {
                        Content = histogram.Name,
                        Visible = true
                    };
                }
                else
                {
                    // Create additional panes for the other histograms
                    pane = new Pane
                    {
                        Name = $"Pane{i}",
                        Title = new PaneTitle
                        {
                            Content = histogram.Name,
                            Visible = true
                        }
                    };
                    diagram.Panes.Add(pane);
                }

                // Create secondary axes only for additional histograms
                SecondaryAxisY2D secondaryAxisY = null;
                SecondaryAxisX2D secondaryAxisX = null;

                if (i > 0)
                {
                    secondaryAxisY = new SecondaryAxisY2D
                    {
                        Name = $"SecondaryAxisY{i}",
                        Alignment = AxisAlignment.Near
                    };
                    diagram.SecondaryAxesY.Add(secondaryAxisY);

                    secondaryAxisX = new SecondaryAxisX2D
                    {
                        Name = $"SecondaryAxisX{i}",
                        Alignment = AxisAlignment.Near
                    };
                    diagram.SecondaryAxesX.Add(secondaryAxisX);
                }

                // Create series
                var series = new BarSideBySideSeries2D
                {
                    DisplayName = histogram.Name,
                    ArgumentDataMember = "Range",
                    ValueDataMember = "Frequency",
                    Pane = pane   // assign to the chosen pane
                };

                // Assign secondary axes if needed
                if (secondaryAxisY != null) series.AxisY = secondaryAxisY;
                if (secondaryAxisX != null) series.AxisX = secondaryAxisX;

                // Bind data
                if (histogram.HistogramData != null && histogram.HistogramData.Any())
                {
                    series.DataSource = histogram.HistogramData;
                }

                diagram.Series.Add(series);
            }

            // Update chart titles
            UpdateChartTitles();
            _unifiedChartControl.InvalidateVisual();
        }

        private void RebindDataAfterLayoutLoad()
        {
            if (_unifiedChartControl?.Diagram is XYDiagram2D diagram)
            {
                System.Diagnostics.Debug.WriteLine("Starting data rebinding after layout load/designer close");

                // Rebind each series to its corresponding histogram data
                for (int i = 0; i < Math.Min(diagram.Series.Count, _viewModel.Histograms.Count); i++)
                {
                    var series = diagram.Series[i];
                    var histogram = _viewModel.Histograms[i];

                    if (series is BarSideBySideSeries2D barSeries && histogram.HistogramData != null)
                    {
                        // Restore data binding properties
                        barSeries.ArgumentDataMember = "Range";
                        barSeries.ValueDataMember = "Frequency";

                        // Ensure secondary axis assignments are preserved
                        if (i > 0)
                        {
                            if ((i - 1) < diagram.SecondaryAxesY.Count)
                                barSeries.AxisY = diagram.SecondaryAxesY[i - 1];
                            if ((i - 1) < diagram.SecondaryAxesX.Count)
                                barSeries.AxisX = diagram.SecondaryAxesX[i - 1];
                        }

                        // Update the data source
                        barSeries.DataSource = null;
                        barSeries.DataSource = histogram.HistogramData;
                        barSeries.DisplayName = histogram.Name;

                        System.Diagnostics.Debug.WriteLine($"Rebound series {i} to histogram {histogram.Name} with {histogram.HistogramData.Count} data points");
                    }
                }
                // Update pane titles to match histogram names (preserve other pane formatting)
                for (int i = 0; i < Math.Min(diagram.Panes.Count, _viewModel.Histograms.Count); i++)
                {
                    var pane = diagram.Panes[i];
                    var histogram = _viewModel.Histograms[i];

                    // Only update title content, preserve other title formatting
                    if (pane.Title == null)
                        pane.Title = new PaneTitle();

                    pane.Title.Content = histogram.Name;
                    // Don't change Visible property - let user control this through designer
                }

                _unifiedChartControl.InvalidateVisual();
                System.Diagnostics.Debug.WriteLine("Data rebinding completed - design changes should be preserved");

                for (int i = 0; i < Math.Min(diagram.Series.Count, _viewModel.Histograms.Count); i++)
                {
                    var series = diagram.Series[i];
                    var histogram = _viewModel.Histograms[i];

                    if (series is BarSideBySideSeries2D barSeries && histogram.HistogramData != null)
                    {
                        // Restore data binding properties (these can get lost during design)
                        barSeries.ArgumentDataMember = "Range";
                        barSeries.ValueDataMember = "Frequency";

                        // Ensure secondary axis assignment is preserved
                        if (i > 0 && (i - 1) < diagram.SecondaryAxesY.Count)
                        {
                            barSeries.AxisY = diagram.SecondaryAxesY[i - 1];
                        }

                    }
                }
            }


        }
        private void OpenUnifiedChartDesigner(int chartIndex = 0)
        {
            try
            {
                if (_unifiedChartControl == null)
                {
                    MessageBox.Show("Chart control not initialized.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Only setup the chart if it's empty or we're not loading a layout
                // This preserves any existing design changes
                if (!_isLoadingLayout && ShouldInitializeChart())
                {
                    SetupUnifiedChart();
                }
                else
                {
                    // Chart already exists, just ensure data bindings are current
                    EnsureDataBindingsArePresent();
                }

                // Subscribe to window activation event to detect when we return from designer
                this.Activated += OnMainWindowActivated;

                // Open designer for the unified chart
                var chartDesigner = new ChartDesigner(_unifiedChartControl);
                chartDesigner.Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening chart designer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMainWindowActivated(object sender, EventArgs e)
        {
            // Unsubscribe to avoid multiple calls
            this.Activated -= OnMainWindowActivated;

            // Rebind data when window gets focus back (likely after designer closes)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RebindDataAfterLayoutLoad();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private async void SaveUnifiedChartLayout(int chartIndex, string fileName, HistogramConfiguration config)
        {
            try
            {
                if (_unifiedChartControl == null)
                {
                    MessageBox.Show("Chart control not initialized.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create unified configuration with all histograms
                var unifiedConfig = new UnifiedHistogramConfiguration
                {
                    SavedDateTime = DateTime.Now,
                    Description = "Unified layout for all histograms",
                    Histograms = _viewModel.Histograms.Select(h => new HistogramConfiguration
                    {
                        HistogramName = h.Name,
                        BinCount = h.BinCount,
                        MinValue = double.TryParse(h.MinValue, out var min) ? min : (double?)null,
                        MaxValue = double.TryParse(h.MaxValue, out var max) ? max : (double?)null,
                        Statistics = h.Statistics,
                        Description = h.Name
                    }).ToList()
                };

                // Save chart layout to memory stream
                using (var memoryStream = new MemoryStream())
                {
                    _unifiedChartControl.SaveToStream(memoryStream);
                    memoryStream.Position = 0;

                    using (var reader = new StreamReader(memoryStream))
                    {
                        var chartLayoutXml = await reader.ReadToEndAsync();
                        await SaveUnifiedLayoutAsync(fileName, chartLayoutXml, unifiedConfig);
                    }
                }

                MessageBox.Show($"Unified chart layout saved to '{fileName}'", "Layout Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving chart layout: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleLayoutLoaded(int chartIndex, ChartLayoutWithMetadata layoutData)
        {
            try
            {
                if (_unifiedChartControl == null) return;

                _isLoadingLayout = true; // Prevent SetupUnifiedChart from running

                using (var memoryStream = new MemoryStream())
                using (var writer = new StreamWriter(memoryStream))
                {
                    writer.Write(layoutData.ChartLayoutXml);
                    writer.Flush();
                    memoryStream.Position = 0;

                    // Load the layout from stream
                    _unifiedChartControl.LoadFromStream(memoryStream);
                }

                // Critical: Rebind data after layout is loaded
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _isLoadingLayout = false;
                    RebindDataAfterLayoutLoad();
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                System.Diagnostics.Debug.WriteLine("Layout loaded successfully");
            }
            catch (Exception ex)
            {
                _isLoadingLayout = false;
                MessageBox.Show($"Error loading chart layout: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleHistogramUpdated(HistogramViewModel histogram)
        {
            try
            {
                // Only update the specific series data, don't rebuild entire chart
                // This preserves any design changes made through the designer
                if (!_isLoadingLayout)
                {
                    UpdateSpecificSeries(histogram);
                }

                System.Diagnostics.Debug.WriteLine($"Chart data updated for histogram: {histogram.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating chart: {ex.Message}");
            }
        }

        private void UpdateSpecificSeries(HistogramViewModel histogram)
        {
            if (_unifiedChartControl?.Diagram is XYDiagram2D diagram)
            {
                var histogramIndex = _viewModel.Histograms.IndexOf(histogram);
                if (histogramIndex >= 0 && histogramIndex < diagram.Series.Count)
                {
                    var series = diagram.Series[histogramIndex];
                    if (series is BarSideBySideSeries2D barSeries)
                    {
                        // Preserve the series design but update data
                        barSeries.DataSource = null; // Clear first to force refresh
                        barSeries.DataSource = histogram.HistogramData;

                        // Ensure data members are still set (in case they got cleared)
                        barSeries.ArgumentDataMember = "Range";
                        barSeries.ValueDataMember = "Frequency";

                        System.Diagnostics.Debug.WriteLine($"Updated data for series {histogramIndex} ({histogram.Name}) - design preserved");
                    }
                }
            }
        }

        private bool ShouldInitializeChart()
        {
            if (_unifiedChartControl?.Diagram is XYDiagram2D diagram)
            {
                // Check if chart has the expected number of series and panes
                bool hasCorrectStructure = diagram.Series.Count == _viewModel.Histograms.Count &&
                                         diagram.Panes.Count == _viewModel.Histograms.Count;

                // If structure is wrong, we need to reinitialize
                if (!hasCorrectStructure)
                {
                    System.Diagnostics.Debug.WriteLine($"Chart structure mismatch - Series: {diagram.Series.Count}, Panes: {diagram.Panes.Count}, Expected: {_viewModel.Histograms.Count}");
                    return true;
                }

                // Check if any series lacks data binding (only if we have histograms)
                if (_viewModel.Histograms.Count > 0)
                {
                    for (int i = 0; i < diagram.Series.Count && i < _viewModel.Histograms.Count; i++)
                    {
                        if (diagram.Series[i].DataSource == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Series {i} has no data source");
                            return true;
                        }
                    }
                }

                return false; // Chart structure is good, don't reinitialize
            }

            return true; // No diagram exists, need to initialize
        }
        private void EnsureDataBindingsArePresent()
        {
            if (_unifiedChartControl?.Diagram is XYDiagram2D diagram)
            {
                for (int i = 0; i < Math.Min(diagram.Series.Count, _viewModel.Histograms.Count); i++)
                {
                    var series = diagram.Series[i];
                    var histogram = _viewModel.Histograms[i];

                    if (series is BarSideBySideSeries2D barSeries)
                    {
                        // Ensure data members are set correctly
                        if (string.IsNullOrEmpty(barSeries.ArgumentDataMember))
                            barSeries.ArgumentDataMember = "Range";
                        if (string.IsNullOrEmpty(barSeries.ValueDataMember))
                            barSeries.ValueDataMember = "Frequency";

                        // Ensure secondary axis assignments for non-first histograms
                        if (i > 0)
                        {
                            if (barSeries.AxisY == null && (i - 1) < diagram.SecondaryAxesY.Count)
                                barSeries.AxisY = diagram.SecondaryAxesY[i - 1];
                            if (barSeries.AxisX == null && (i - 1) < diagram.SecondaryAxesX.Count)
                                barSeries.AxisX = diagram.SecondaryAxesX[i - 1];
                        }

                        // Update data source if needed
                        if (barSeries.DataSource != histogram.HistogramData)
                        {
                            barSeries.DataSource = histogram.HistogramData;
                            System.Diagnostics.Debug.WriteLine($"Updated data source for series {i} ({histogram.Name})");
                        }
                    }
                }
            }
        }
        private void UpdateChartTitles()
        {
            if (_unifiedChartControl?.Titles.Count == 0)
            {
                _unifiedChartControl.Titles.Add(new Title { Content = "" });
            }
        }

        private async System.Threading.Tasks.Task SaveUnifiedLayoutAsync(string filePath, string chartLayoutXml, UnifiedHistogramConfiguration config)
        {
            var unifiedLayout = new UnifiedChartLayoutWithMetadata
            {
                UnifiedConfig = config,
                ChartLayoutXml = chartLayoutXml,
                Version = "2.0"
            };

            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(UnifiedChartLayoutWithMetadata));
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var xmlWriter = System.Xml.XmlWriter.Create(fileStream, new System.Xml.XmlWriterSettings { Indent = true }))
            {
                serializer.Serialize(xmlWriter, unifiedLayout);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.OnDesignerRequested -= OpenUnifiedChartDesigner;
                _viewModel.OnSaveLayoutRequested -= SaveUnifiedChartLayout;
                _viewModel.OnLayoutLoaded -= HandleLayoutLoaded;
                _viewModel.OnHistogramUpdated -= HandleHistogramUpdated;
                _viewModel.OnHistogramStructureChanged -= HandleHistogramStructureChanged; // Add this line
            }

            this.Activated -= OnMainWindowActivated;
            base.OnClosed(e);
        }

        // Public method to manually refresh chart bindings
        public void RefreshChartBindings()
        {
            RebindDataAfterLayoutLoad();
        }

        private void ChartControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // TODO: Implement your double-click logic here, or leave empty if not needed
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }
    }
}