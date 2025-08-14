using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DXHistogram.Commands;
using DXHistogram.Models;
using DXHistogram.Services;
using Microsoft.Win32;
using DevExpress.Xpf.Dialogs;
using MessageBox = System.Windows.MessageBox;

namespace DXHistogram.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private readonly IHistogramService _histogramService;

        // Private fields
        private List<double> _currentData;
        private ObservableCollection<HistogramBin> _histogramData;
        private DataStatistics _statistics;
        private string _dataInput;
        private int _binCount = 10;
        private string _minValue;
        private string _maxValue;
        private string _statusMessage;

        public MainViewModel(IDataService dataService, IHistogramService histogramService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _histogramService = histogramService ?? throw new ArgumentNullException(nameof(histogramService));

            _currentData = new List<double>();
            _histogramData = new ObservableCollection<HistogramBin>();
            _statistics = new DataStatistics();

            InitializeCommands();
            LoadDefaultData();
        }

        // Properties
        public ObservableCollection<HistogramBin> HistogramData
        {
            get => _histogramData;
            set => SetProperty(ref _histogramData, value);
        }

        public DataStatistics Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        public string DataInput
        {
            get => _dataInput;
            set => SetProperty(ref _dataInput, value);
        }

        public int BinCount
        {
            get => _binCount;
            set => SetProperty(ref _binCount, Math.Max(1, Math.Min(100, value)));
        }

        public string MinValue
        {
            get => _minValue;
            set => SetProperty(ref _minValue, value);
        }

        public string MaxValue
        {
            get => _maxValue;
            set => SetProperty(ref _maxValue, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Commands
        public ICommand LoadDataCommand { get; private set; }
        public ICommand ClearDataCommand { get; private set; }
        public ICommand LoadFromFileCommand { get; private set; }
        public ICommand SaveDataCommand { get; private set; }
        public ICommand ApplyRangeCommand { get; private set; }
        public ICommand ResetRangeCommand { get; private set; }
        public ICommand OpenDesignerCommand { get; private set; }
        public ICommand SaveLayoutCommand { get; private set; }
        public ICommand LoadLayoutCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadDataCommand = new RelayCommand(LoadData, () => !string.IsNullOrWhiteSpace(DataInput));
            ClearDataCommand = new RelayCommand(ClearData);
            LoadFromFileCommand = new RelayCommand(async () => await LoadFromFileAsync());
            SaveDataCommand = new RelayCommand(async () => await SaveDataAsync(), () => _currentData?.Any() == true);
            ApplyRangeCommand = new RelayCommand(ApplyRange);
            ResetRangeCommand = new RelayCommand(ResetRange, () => _currentData?.Any() == true);
            OpenDesignerCommand = new RelayCommand(OpenDesigner);
            SaveLayoutCommand = new RelayCommand(async () => await SaveLayoutAsync());
            LoadLayoutCommand = new RelayCommand(async () => await LoadLayoutAsync());
        }

        private void LoadDefaultData()
        {
            try
            {
                _currentData = _dataService.GenerateSampleData(1000, 50, 15);
                UpdateHistogram();
                StatusMessage = "Default sample data loaded (1000 points)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading default data: {ex.Message}";
            }
        }

        private void LoadData()
        {
            try
            {
                var dataValues = _dataService.ParseDataFromString(DataInput);

                if (!dataValues.Any())
                {
                    MessageBox.Show("No valid numbers found in the input.", "No Valid Data",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentData = dataValues;
                UpdateHistogram();
                StatusMessage = $"Successfully loaded {dataValues.Count} data points.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearData()
        {
            DataInput = string.Empty;
            _currentData.Clear();
            HistogramData.Clear();
            Statistics = new DataStatistics();
            StatusMessage = "Data cleared";
        }

        private async Task LoadFromFileAsync()
        {
            try
            {
                var dialog = new DXOpenFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Select Data File",
                    DefaultExt = "txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    var dataValues = await _dataService.LoadDataFromFileAsync(dialog.FileName);

                    if (dataValues.Any())
                    {
                        _currentData = dataValues;
                        MinValue = _currentData.Min().ToString("F2");
                        MaxValue = _currentData.Max().ToString("F2");
                        UpdateHistogram();
                        StatusMessage = $"Successfully loaded {dataValues.Count} data points from file.";
                    }
                    else
                    {
                        MessageBox.Show("No valid numbers found in the selected file.", "No Data Found",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading file: {ex.Message}";
                MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveDataAsync()
        {
            try
            {
                if (!_currentData?.Any() == true)
                {
                    MessageBox.Show("No data to save.", "No Data",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new DXSaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv",
                    Title = "Save Data File",
                    DefaultExt = "txt",
                    FileName = "HistogramData"
                };

                if (dialog.ShowDialog() == true)
                {
                    await _dataService.SaveDataToFileAsync(dialog.FileName, _currentData);
                    StatusMessage = $"Data saved successfully to {dialog.FileName}";
                    MessageBox.Show($"Data saved successfully to {dialog.FileName}", "Data Saved",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving file: {ex.Message}";
                MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyRange()
        {
            UpdateHistogram();
        }

        private void ResetRange()
        {
            if (_currentData?.Any() == true)
            {
                MinValue = _currentData.Min().ToString("F2");
                MaxValue = _currentData.Max().ToString("F2");
                UpdateHistogram();
            }
        }

        private void UpdateHistogram()
        {
            try
            {
                if (!_currentData?.Any() == true)
                {
                    HistogramData.Clear();
                    Statistics = new DataStatistics();
                    return;
                }

                double? customMin = double.TryParse(MinValue, out var min) ? min : (double?)null;
                double? customMax = double.TryParse(MaxValue, out var max) ? max : (double?)null;

                var bins = _histogramService.CreateHistogramBins(_currentData, BinCount, customMin, customMax);

                HistogramData.Clear();
                foreach (var bin in bins)
                {
                    HistogramData.Add(bin);
                }

                Statistics = _histogramService.CalculateStatistics(_currentData);
                StatusMessage = $"Histogram updated with {bins.Count} bins";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating histogram: {ex.Message}";
                MessageBox.Show($"Error updating histogram: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDesigner()
        {
            // This would need to be handled in the View since it requires access to the ChartControl
            OnDesignerRequested?.Invoke();
        }

        private async Task SaveLayoutAsync()
        {
            try
            {
                var dialog = new DXSaveFileDialog
                {
                    DefaultExt = "xml",
                    Filter = "XML files (*.xml)|*.xml",
                    Title = "Save Chart Layout",
                    FileName = "ChartLayout.xml"
                };

                if (dialog.ShowDialog() == true)
                {
                    OnSaveLayoutRequested?.Invoke(dialog.FileName);
                    StatusMessage = $"Chart layout saved to {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving chart layout: {ex.Message}";
            }
        }

        private async Task LoadLayoutAsync()
        {
            try
            {
                var dialog = new DXOpenFileDialog
                {
                    DefaultExt = "xml",
                    Filter = "XML files (*.xml)|*.xml",
                    Title = "Load Chart Layout"
                };

                if (dialog.ShowDialog() == true)
                {
                    OnLoadLayoutRequested?.Invoke(dialog.FileName);
                    StatusMessage = $"Chart layout loaded from {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading chart layout: {ex.Message}";
            }
        }

        // Events for View interaction
        public event Action OnDesignerRequested;
        public event Action<string> OnSaveLayoutRequested;
        public event Action<string> OnLoadLayoutRequested;
    }
}