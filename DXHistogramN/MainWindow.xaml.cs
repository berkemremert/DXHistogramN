using System;
using System.Linq;
using System.Windows;
using DevExpress.Charts.Designer;
using DevExpress.Xpf.Charts;
using DXHistogram.Services;
using DXHistogram.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace DXHistogram
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            // Create services
            var dataService = new DataService();
            var histogramService = new HistogramService();

            // Create ViewModel
            _viewModel = new MainViewModel(dataService, histogramService);

            // Subscribe to ViewModel events that require View interaction
            _viewModel.OnDesignerRequested += OpenChartDesigner;
            _viewModel.OnSaveLayoutRequested += SaveChartLayout;
            _viewModel.OnLoadLayoutRequested += LoadChartLayout;

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

        private void SaveChartLayout(string fileName)
        {
            try
            {
                chartControl.SaveToFile(fileName);
                MessageBox.Show($"The Chart Layout saved to the '{fileName}' file", "Layout Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving chart layout: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadChartLayout(string fileName)
        {
            try
            {
                chartControl.LoadFromFile(fileName);
                CreateBindings();
                MessageBox.Show($"The Chart Layout loaded from the '{fileName}' file", "Layout Loaded",
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
            // Unsubscribe from events to prevent memory leaks
            if (_viewModel != null)
            {
                _viewModel.OnDesignerRequested -= OpenChartDesigner;
                _viewModel.OnSaveLayoutRequested -= SaveChartLayout;
                _viewModel.OnLoadLayoutRequested -= LoadChartLayout;
            }
            base.OnClosed(e);
        }
    }
}