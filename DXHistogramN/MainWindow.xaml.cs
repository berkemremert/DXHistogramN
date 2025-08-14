using DevExpress.Charts.Designer;
using DevExpress.Xpf.Charts;
using DXHistogramN.Models;
using DXHistogramN.Services;
using DXHistogramN.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace DXHistogramN
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        // Constructor with dependency injection - ONLY constructor needed
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            // Subscribe to ViewModel events that require View interaction
            _viewModel.OnDesignerRequested += OpenChartDesigner;
            _viewModel.OnSaveLayoutRequested += SaveChartLayoutWithMetadata;
            _viewModel.OnLoadLayoutRequested += LoadChartLayout;
            _viewModel.OnLayoutLoaded += HandleLayoutLoaded;

            // Set DataContext
            DataContext = _viewModel;
        }

        private void OpenChartDesigner()
        {
            try
            {
                var chartDesigner = new ChartDesigner(chartControl);
                chartDesigner.Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening chart designer: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveChartLayoutWithMetadata(string fileName, HistogramConfiguration config)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    chartControl.SaveToStream(memoryStream);
                    memoryStream.Position = 0;

                    using (var reader = new StreamReader(memoryStream))
                    {
                        var chartLayoutXml = await reader.ReadToEndAsync();
                        var layoutService = new ChartLayoutService();
                        await layoutService.SaveLayoutWithMetadataAsync(fileName, chartLayoutXml, config);
                    }
                }

                MessageBox.Show($"Chart layout with histogram metadata saved to '{fileName}'", "Layout Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving chart layout: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleLayoutLoaded(ChartLayoutWithMetadata layoutData)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                using (var writer = new StreamWriter(memoryStream))
                {
                    writer.Write(layoutData.ChartLayoutXml);
                    writer.Flush();
                    memoryStream.Position = 0;

                    chartControl.LoadFromStream(memoryStream);
                    CreateBindings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading chart layout: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadChartLayout(string fileName)
        {
            try
            {
                MessageBox.Show($"Chart layout loaded from '{fileName}'", "Layout Loaded",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading chart layout: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateBindings()
        {
            try
            {
                var barSeries = chartControl.Diagram.Series.OfType<BarSideBySideSeries2D>().FirstOrDefault();
                if (barSeries != null)
                {
                    barSeries.DataSource = _viewModel.HistogramData;
                    barSeries.ArgumentDataMember = "Range";
                    barSeries.ValueDataMember = "Frequency";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recreating bindings: {ex.Message}", "Binding Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.OnDesignerRequested -= OpenChartDesigner;
                _viewModel.OnSaveLayoutRequested -= SaveChartLayoutWithMetadata;
                _viewModel.OnLoadLayoutRequested -= LoadChartLayout;
                _viewModel.OnLayoutLoaded -= HandleLayoutLoaded;
            }
            base.OnClosed(e);
        }
    }
}