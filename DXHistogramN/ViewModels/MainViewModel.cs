using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DXHistogramN.Commands;
using DXHistogramN.Models;
using DXHistogramN.Services;
using Microsoft.Win32;
using DevExpress.Xpf.Dialogs;
using MessageBox = System.Windows.MessageBox;
using System.IO;

namespace DXHistogramN.ViewModels
{
    public class HistogramViewModel : BaseViewModel
    {
        private string _name;
        private List<double> _currentData;
        private ObservableCollection<HistogramBin> _histogramData;
        private DataStatistics _statistics;
        private string _dataInput;
        private Decimal _binCount = 10;
        private string _minValue;
        private string _maxValue;
        private bool _isActive;

        public HistogramViewModel(string name)
        {
            _name = name;
            _currentData = new List<double>();
            _histogramData = new ObservableCollection<HistogramBin>();
            _statistics = new DataStatistics();
            _isActive = false;
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public List<double> CurrentData
        {
            get => _currentData;
            set => SetProperty(ref _currentData, value);
        }

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

        public decimal BinCount
        {
            get => _binCount;
            set
            {
                var clampedValue = Math.Max(1, Math.Min(100, value));
                System.Diagnostics.Debug.WriteLine($"{Name}: BinCount setter called - Old: {_binCount}, New: {clampedValue}");

                if (SetProperty<decimal>(ref _binCount, clampedValue))
                {
                    System.Diagnostics.Debug.WriteLine($"{Name}: BinCount changed to {clampedValue}");
                }
            }
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
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private readonly IHistogramService _histogramService;
        private readonly IChartLayoutService _chartLayoutService;

        // Private fields
        private HistogramViewModel _selectedHistogram;
        private string _statusMessage;

        public MainViewModel(IDataService dataService, IHistogramService histogramService, IChartLayoutService chartLayoutService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _histogramService = histogramService ?? throw new ArgumentNullException(nameof(histogramService));
            _chartLayoutService = chartLayoutService ?? throw new ArgumentNullException(nameof(chartLayoutService));

            // Initialize with empty collection instead of 4 fixed histograms
            Histograms = new ObservableCollection<HistogramViewModel>();

            InitializeCommands();
            InitializeHistogramEventHandlers();
        }
        private void InitializeAllHistograms()
        {
            try
            {
                // Initialize each histogram with different sample data
                for (int i = 0; i < Histograms.Count; i++)
                {
                    var histogram = Histograms[i];
                    var random = new Random(i * 1000); // Different seed for each

                    // Create different distributions for each histogram
                    var mean = 30 + (i * 15); // Means: 30, 45, 60, 75
                    var stdDev = 8 + (i * 3);  // Std devs: 8, 11, 14, 17
                    var count = 500 + (i * 100); // Counts: 500, 600, 700, 800

                    histogram.CurrentData = _dataService.GenerateSampleData(count, mean, stdDev);
                    histogram.MinValue = histogram.CurrentData.Min().ToString("F2");
                    histogram.MaxValue = histogram.CurrentData.Max().ToString("F2");

                    // Update histogram data
                    UpdateHistogramForSpecific(histogram);
                }

                StatusMessage = "All histograms initialized with sample data";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error initializing histograms: {ex.Message}";
            }

            InitializeHistogramEventHandlers();
        }

        private void InitializeHistogramEventHandlers()
        {
            foreach (var histogram in Histograms)
            {
                SetupHistogramEventHandlers(histogram);
            }
        }

        // Properties
        public ObservableCollection<HistogramViewModel> Histograms { get; }

        public HistogramViewModel SelectedHistogram
        {
            get => _selectedHistogram;
            set
            {
                if (_selectedHistogram != null)
                    _selectedHistogram.IsActive = false;

                SetProperty(ref _selectedHistogram, value);

                if (_selectedHistogram != null)
                    _selectedHistogram.IsActive = true;
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Commands - Original unified commands
        public ICommand SelectHistogramCommand { get; private set; }
        public ICommand LoadDataCommand { get; private set; }
        public ICommand ClearDataCommand { get; private set; }
        public ICommand LoadFromFileCommand { get; private set; }
        public ICommand SaveDataCommand { get; private set; }
        public ICommand ApplyRangeCommand { get; private set; }
        public ICommand ResetRangeCommand { get; private set; }
        public ICommand OpenDesignerCommand { get; private set; }
        public ICommand SaveLayoutCommand { get; private set; }
        public ICommand LoadLayoutCommand { get; private set; }
        public ICommand GenerateSampleDataCommand { get; private set; }
        public ICommand RefreshChartCommand { get; private set; }

        // New Commands - Individual histogram operations
        public ICommand LoadDataForHistogramCommand { get; private set; }
        public ICommand ClearDataForHistogramCommand { get; private set; }
        public ICommand LoadFromFileForHistogramCommand { get; private set; }
        public ICommand SaveDataForHistogramCommand { get; private set; }
        public ICommand ApplyRangeForHistogramCommand { get; private set; }
        public ICommand ResetRangeForHistogramCommand { get; private set; }
        public ICommand GenerateSampleForHistogramCommand { get; private set; }
        public ICommand AddHistogramCommand { get; private set; }
        public ICommand RemoveHistogramCommand { get; private set; }
        public ICommand RenameHistogramCommand
        {
            get; private set;
        }

        private void InitializeCommands()
        {
            // Original commands for backward compatibility
            SelectHistogramCommand = new RelayCommand(param => SelectHistogram(param as HistogramViewModel));
            LoadDataCommand = new RelayCommand(LoadData, () => !string.IsNullOrWhiteSpace(SelectedHistogram?.DataInput));
            ClearDataCommand = new RelayCommand(ClearData);
            LoadFromFileCommand = new RelayCommand(async () => await LoadFromFileAsync());
            SaveDataCommand = new RelayCommand(async () => await SaveDataAsync(), () => SelectedHistogram?.CurrentData?.Any() == true);
            ApplyRangeCommand = new RelayCommand(ApplyRange);
            ResetRangeCommand = new RelayCommand(ResetRange, () => SelectedHistogram?.CurrentData?.Any() == true);

            // Unified commands
            OpenDesignerCommand = new RelayCommand(_ => OpenUnifiedDesigner());
            SaveLayoutCommand = new RelayCommand(async _ => await SaveUnifiedLayoutAsync());
            LoadLayoutCommand = new RelayCommand(async _ => await LoadUnifiedLayoutAsync());
            GenerateSampleDataCommand = new RelayCommand(GenerateSampleData);
            RefreshChartCommand = new RelayCommand(RefreshChart);

            // New individual histogram commands
            LoadDataForHistogramCommand = new RelayCommand(async param => await LoadDataForHistogramAsync(param as HistogramViewModel));
            ClearDataForHistogramCommand = new RelayCommand(param => ClearDataForHistogram(param as HistogramViewModel));
            LoadFromFileForHistogramCommand = new RelayCommand(async param => await LoadFromFileForHistogramAsync(param as HistogramViewModel));
            SaveDataForHistogramCommand = new RelayCommand(async param => await SaveDataForHistogramAsync(param as HistogramViewModel));
            ApplyRangeForHistogramCommand = new RelayCommand(param => ApplyRangeForHistogram(param as HistogramViewModel));
            ResetRangeForHistogramCommand = new RelayCommand(param => ResetRangeForHistogram(param as HistogramViewModel));
            GenerateSampleForHistogramCommand = new RelayCommand(param => GenerateSampleForHistogram(param as HistogramViewModel));

            AddHistogramCommand = new RelayCommand(AddNewHistogram);
            RemoveHistogramCommand = new RelayCommand(param => RemoveHistogram(param as HistogramViewModel),
                param => Histograms.Count > 0);
            RenameHistogramCommand = new RelayCommand(param => RenameHistogram(param as HistogramViewModel));
        }

        private async void AddNewHistogram()
        {
            var histogramCount = Histograms.Count + 1;
            var newHistogram = new HistogramViewModel($"Histogram {histogramCount}");
            Histograms.Add(newHistogram);

            if (Histograms.Count == 1)
                SelectedHistogram = newHistogram;

            SetupHistogramEventHandlers(newHistogram);

            // Load default data.txt for this histogram
            await LoadDefaultDataForHistogramAsync(newHistogram);

            // Rebuild panes/series structure after adding the item
            OnHistogramStructureChanged?.Invoke();

            StatusMessage = $"Added {newHistogram.Name}";
        }


        private async Task LoadDefaultDataForHistogramAsync(HistogramViewModel histogram)
        {
            try
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "C:\\Users\\berk.mert\\source\\repos\\DXHistogramN\\DXHistogramN\\data.txt");
                if (!File.Exists(filePath))
                {
                    StatusMessage = $"Default data file not found: {filePath}";
                    return;
                }

                var dataValues = await _dataService.LoadDataFromFileAsync(filePath);
                if (dataValues.Any())
                {
                    histogram.CurrentData = dataValues;
                    histogram.MinValue = histogram.CurrentData.Min().ToString("F2");
                    histogram.MaxValue = histogram.CurrentData.Max().ToString("F2");
                    UpdateHistogramForSpecific(histogram); // raises OnHistogramUpdated → view refreshes
                }
                else
                {
                    StatusMessage = $"No valid numbers found in {Path.GetFileName(filePath)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading default data for {histogram?.Name}: {ex.Message}";
            }
        }



        private void RemoveHistogram(HistogramViewModel histogram)
        {
            if (histogram == null || Histograms.Count == 0) return;

            var histogramName = histogram.Name;
            var wasSelected = SelectedHistogram == histogram;

            Histograms.Remove(histogram);

            // Select another histogram if the removed one was selected
            if (wasSelected && Histograms.Count > 0)
            {
                SelectedHistogram = Histograms.First();
            }
            else if (Histograms.Count == 0)
            {
                SelectedHistogram = null;
            }

            // Trigger chart rebuild
            OnHistogramStructureChanged?.Invoke();

            StatusMessage = $"Removed {histogramName}";
        }

        private void RenameHistogram(HistogramViewModel histogram)
        {
            if (histogram == null) return;

            // You can implement a simple input dialog here or use a more sophisticated approach
            var newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new name for the histogram:",
                "Rename Histogram",
                histogram.Name);

            if (!string.IsNullOrWhiteSpace(newName) && newName != histogram.Name)
            {
                histogram.Name = newName;
                OnHistogramStructureChanged?.Invoke();
                StatusMessage = $"Renamed histogram to '{newName}'";
            }
        }

        private void SetupHistogramEventHandlers(HistogramViewModel histogram)
        {
            histogram.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(histogram.BinCount))
                {
                    System.Diagnostics.Debug.WriteLine($"PropertyChanged fired for {histogram.Name}.BinCount = {histogram.BinCount}");
                }
            };
        }

        #region Individual Histogram Commands

        private async Task LoadDataForHistogramAsync(HistogramViewModel histogram)
        {
            if (histogram == null) return;

            try
            {
                var dataValues = _dataService.ParseDataFromString(histogram.DataInput);

                if (!dataValues.Any())
                {
                    MessageBox.Show($"No valid numbers found in the input for {histogram.Name}.", "No Valid Data",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                histogram.CurrentData = dataValues;
                histogram.MinValue = histogram.CurrentData.Min().ToString("F2");
                histogram.MaxValue = histogram.CurrentData.Max().ToString("F2");
                UpdateHistogramForSpecific(histogram);
                StatusMessage = $"Successfully loaded {dataValues.Count} data points to {histogram.Name}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data to {histogram.Name}: {ex.Message}";
                MessageBox.Show($"Error loading data to {histogram.Name}: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearDataForHistogram(HistogramViewModel histogram)
        {
            if (histogram == null) return;

            histogram.DataInput = string.Empty;
            histogram.CurrentData.Clear();
            histogram.HistogramData.Clear();
            histogram.Statistics = new DataStatistics();
            histogram.MinValue = string.Empty;
            histogram.MaxValue = string.Empty;

            // Update chart
            OnHistogramUpdated?.Invoke(histogram);
            StatusMessage = $"Data cleared from {histogram.Name}";
        }

        private async Task LoadFromFileForHistogramAsync(HistogramViewModel histogram)
        {
            if (histogram == null) return;

            try
            {
                var dialog = new DXOpenFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = $"Select Data File for {histogram.Name}",
                    DefaultExt = "txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    var dataValues = await _dataService.LoadDataFromFileAsync(dialog.FileName);

                    if (dataValues.Any())
                    {
                        histogram.CurrentData = dataValues;
                        histogram.MinValue = histogram.CurrentData.Min().ToString("F2");
                        histogram.MaxValue = histogram.CurrentData.Max().ToString("F2");

                        // Clear the text input since we loaded from file
                        histogram.DataInput = $"Loaded {dataValues.Count} values from file";

                        UpdateHistogramForSpecific(histogram);
                        StatusMessage = $"Successfully loaded {dataValues.Count} data points from file to {histogram.Name}.";
                    }
                    else
                    {
                        MessageBox.Show($"No valid numbers found in the selected file for {histogram.Name}.", "No Data Found",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading file for {histogram.Name}: {ex.Message}";
                MessageBox.Show($"Error loading file for {histogram.Name}: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveDataForHistogramAsync(HistogramViewModel histogram)
        {
            if (histogram?.CurrentData?.Any() != true)
            {
                MessageBox.Show($"No data to save for {histogram?.Name ?? "Unknown"}.", "No Data",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new DXSaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv",
                    Title = $"Save Data File for {histogram.Name}",
                    DefaultExt = "txt",
                    FileName = $"HistogramData_{histogram.Name.Replace(" ", "_")}"
                };

                if (dialog.ShowDialog() == true)
                {
                    await _dataService.SaveDataToFileAsync(dialog.FileName, histogram.CurrentData);
                    StatusMessage = $"Data from {histogram.Name} saved successfully to {Path.GetFileName(dialog.FileName)}";
                    MessageBox.Show($"Data from {histogram.Name} saved successfully to {Path.GetFileName(dialog.FileName)}", "Data Saved",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving file for {histogram.Name}: {ex.Message}";
                MessageBox.Show($"Error saving file for {histogram.Name}: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ApplyRangeForHistogram(HistogramViewModel histogram)
        {
            if (histogram == null) return;

            System.Diagnostics.Debug.WriteLine($"ApplyRange called for {histogram.Name} - BinCount: {histogram.BinCount}");

            try
            {
                if (histogram.CurrentData?.Any() == true)
                {
                    double? customMin = double.TryParse(histogram.MinValue, out var min) ? min : (double?)null;
                    double? customMax = double.TryParse(histogram.MaxValue, out var max) ? max : (double?)null;

                    // Cast decimal to int here
                    int binCount = (int)histogram.BinCount;
                    var bins = _histogramService.CreateHistogramBins(histogram.CurrentData, binCount, customMin, customMax);

                    histogram.HistogramData.Clear();
                    foreach (var bin in bins)
                    {
                        histogram.HistogramData.Add(bin);
                    }

                    histogram.Statistics = _histogramService.CalculateStatistics(histogram.CurrentData);
                    OnHistogramUpdated?.Invoke(histogram);

                    StatusMessage = $"Range applied to {histogram.Name} with {binCount} bins";
                    System.Diagnostics.Debug.WriteLine($"Successfully updated {histogram.Name} with {bins.Count} bins");
                }
                else
                {
                    StatusMessage = $"No data available for {histogram.Name} to apply range";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error applying range to {histogram.Name}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error in ApplyRangeForHistogram: {ex.Message}");
            }
        }

        // Temporary debug method - add this to MainViewModel
        public void DebugBinCounts()
        {
            for (int i = 0; i < Histograms.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine($"Histogram {i}: {Histograms[i].Name} - BinCount: {Histograms[i].BinCount}");
            }
        }
        private void ResetRangeForHistogram(HistogramViewModel histogram)
        {
            if (histogram?.CurrentData?.Any() == true)
            {
                System.Diagnostics.Debug.WriteLine($"ResetRange called for {histogram.Name} - BinCount before: {histogram.BinCount}");

                histogram.MinValue = histogram.CurrentData.Min().ToString("F2");
                histogram.MaxValue = histogram.CurrentData.Max().ToString("F2");
                UpdateHistogramForSpecific(histogram);
                StatusMessage = $"Range reset for {histogram.Name}";

                System.Diagnostics.Debug.WriteLine($"ResetRange finished for {histogram.Name} - BinCount after: {histogram.BinCount}");
            }
        }

        private void GenerateSampleForHistogram(HistogramViewModel histogram)
        {
            if (histogram == null) return;

            try
            {
                // Generate different sample data for variety based on histogram index
                var histogramIndex = Histograms.IndexOf(histogram);
                var random = new Random(histogramIndex * 1000 + DateTime.Now.Millisecond);

                // Create different parameters for each histogram
                var mean = 30 + (histogramIndex * 15) + random.NextDouble() * 10; // Add some randomness
                var stdDev = 5 + (histogramIndex * 3) + random.NextDouble() * 10;
                var count = 500 + (histogramIndex * 100) + random.Next(500);

                histogram.CurrentData = _dataService.GenerateSampleData(count, mean, stdDev);
                histogram.MinValue = histogram.CurrentData.Min().ToString("F2");
                histogram.MaxValue = histogram.CurrentData.Max().ToString("F2");

                // Update the data input field to show what was generated
                histogram.DataInput = $"Generated {count} sample points (μ={mean:F1}, σ={stdDev:F1})";

                UpdateHistogramForSpecific(histogram);
                StatusMessage = $"Generated {count} sample data points for {histogram.Name} (μ={mean:F1}, σ={stdDev:F1})";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating sample data for {histogram.Name}: {ex.Message}";
            }
        }

        #endregion

        #region Original Methods (for backward compatibility)

        private void RefreshChart()
        {
            try
            {
                // Instead of rebuilding everything, just trigger data rebinding
                // This preserves design changes while ensuring data is current
                foreach (var histogram in Histograms)
                {
                    if (histogram.CurrentData?.Any() == true)
                    {
                        OnHistogramUpdated?.Invoke(histogram);
                    }
                }

                StatusMessage = "Chart data refreshed - design changes preserved";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing chart: {ex.Message}";
            }
        }

        private void OpenUnifiedDesigner()
        {
            OnDesignerRequested?.Invoke(0); // Index doesn't matter for unified approach
        }

        private async System.Threading.Tasks.Task SaveUnifiedLayoutAsync()
        {
            try
            {
                var dialog = new DevExpress.Xpf.Dialogs.DXSaveFileDialog
                {
                    DefaultExt = "xml",
                    Filter = "Unified Chart Layout (*.xml)|*.xml",
                    Title = "Save Unified Chart Layout",
                    FileName = "UnifiedHistogramLayout.xml"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Create unified configuration with all histograms
                    var unifiedConfig = new UnifiedHistogramConfiguration
                    {
                        SavedDateTime = DateTime.Now,
                        Description = "Unified layout for all histograms",
                        Histograms = Histograms.Select(h => new HistogramConfiguration
                        {
                            HistogramName = h.Name,
                            BinCount = h.BinCount,
                            MinValue = double.TryParse(h.MinValue, out var min) ? min : (double?)null,
                            MaxValue = double.TryParse(h.MaxValue, out var max) ? max : (double?)null,
                            Statistics = h.Statistics,
                            Description = $"{h.Name} - {h.CurrentData?.Count ?? 0} data points"
                        }).ToList()
                    };

                    // Trigger the save event - the view will handle the actual chart XML extraction
                    OnSaveLayoutRequested?.Invoke(0, dialog.FileName, new HistogramConfiguration
                    {
                        Description = unifiedConfig.Description,
                        SavedDateTime = unifiedConfig.SavedDateTime
                    });

                    StatusMessage = $"Unified chart layout saved to {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving unified layout: {ex.Message}";
                MessageBox.Show($"Error saving unified layout: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadUnifiedLayoutAsync()
        {
            try
            {
                var dialog = new DevExpress.Xpf.Dialogs.DXOpenFileDialog
                {
                    DefaultExt = "xml",
                    Filter = "Unified Chart Layout (*.xml)|*.xml|All XML files (*.xml)|*.xml",
                    Title = "Load Unified Chart Layout"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Check if it's a unified layout file
                    if (!_chartLayoutService.IsUnifiedLayoutFile(dialog.FileName))
                    {
                        StatusMessage = "Selected file is not a unified layout file.";
                        MessageBox.Show("The selected file is not a unified layout file. Please select a file saved from the unified chart.",
                            "Invalid File Type", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var unifiedLayoutData = await _chartLayoutService.LoadUnifiedLayoutAsync(dialog.FileName);

                    // Apply configurations to individual histograms
                    if (unifiedLayoutData.UnifiedConfig?.Histograms != null)
                    {
                        for (int i = 0; i < Math.Min(unifiedLayoutData.UnifiedConfig.Histograms.Count, Histograms.Count); i++)
                        {
                            var config = unifiedLayoutData.UnifiedConfig.Histograms[i];
                            var histogram = Histograms[i];

                            // Apply the configuration
                            histogram.BinCount = config.BinCount;
                            histogram.MinValue = config.MinValue?.ToString("F2") ?? "";
                            histogram.MaxValue = config.MaxValue?.ToString("F2") ?? "";

                            // Update histogram if it has data
                            if (histogram.CurrentData?.Any() == true)
                            {
                                UpdateHistogramForSpecific(histogram);
                            }
                        }
                    }

                    // Convert unified layout to individual layout format for the view
                    var layoutData = new ChartLayoutWithMetadata
                    {
                        ChartLayoutXml = unifiedLayoutData.ChartLayoutXml,
                        HistogramConfig = unifiedLayoutData.UnifiedConfig?.Histograms?.FirstOrDefault() ?? new HistogramConfiguration(),
                        Version = unifiedLayoutData.Version
                    };

                    OnLayoutLoaded?.Invoke(0, layoutData);

                    StatusMessage = $"Unified chart layout loaded successfully from {Path.GetFileName(dialog.FileName)}";

                    // Show information about what was loaded
                    var info = $"Loaded unified layout:\n" +
                              $"Saved: {unifiedLayoutData.UnifiedConfig?.SavedDateTime}\n" +
                              $"Description: {unifiedLayoutData.UnifiedConfig?.Description}\n" +
                              $"Histograms: {unifiedLayoutData.UnifiedConfig?.Histograms?.Count ?? 0}";

                    MessageBox.Show(info, "Layout Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading unified layout: {ex.Message}";
                MessageBox.Show($"Error loading unified layout: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectHistogram(HistogramViewModel histogram)
        {
            if (histogram != null)
            {
                SelectedHistogram = histogram;
                StatusMessage = $"Selected {histogram.Name}";
            }
        }

        private void LoadData()
        {
            if (SelectedHistogram == null) return;
            LoadDataForHistogramAsync(SelectedHistogram);
        }

        private void ClearData()
        {
            if (SelectedHistogram == null) return;
            ClearDataForHistogram(SelectedHistogram);
        }

        private async Task LoadFromFileAsync()
        {
            if (SelectedHistogram == null) return;
            await LoadFromFileForHistogramAsync(SelectedHistogram);
        }

        private async Task SaveDataAsync()
        {
            if (SelectedHistogram == null) return;
            await SaveDataForHistogramAsync(SelectedHistogram);
        }

        private void ApplyRange()
        {
            if (SelectedHistogram == null) return;
            ApplyRangeForHistogram(SelectedHistogram);
        }

        private void ResetRange()
        {
            if (SelectedHistogram == null) return;
            ResetRangeForHistogram(SelectedHistogram);
        }

        private void GenerateSampleData()
        {
            if (SelectedHistogram == null) return;
            GenerateSampleForHistogram(SelectedHistogram);
        }

        #endregion

        #region Helper Methods

        private void UpdateHistogramForSpecific(HistogramViewModel histogram)
        {
            if (histogram?.CurrentData?.Any() != true)
            {
                // Clear histogram if no data
                histogram?.HistogramData.Clear();
                if (histogram != null)
                {
                    histogram.Statistics = new DataStatistics();
                    OnHistogramUpdated?.Invoke(histogram);
                }
                return;
            }

            try
            {
                double? customMin = double.TryParse(histogram.MinValue, out var min) ? min : (double?)null;
                double? customMax = double.TryParse(histogram.MaxValue, out var max) ? max : (double?)null;

                var bins = _histogramService.CreateHistogramBins(histogram.CurrentData, histogram.BinCount, customMin, customMax);

                histogram.HistogramData.Clear();
                foreach (var bin in bins)
                {
                    histogram.HistogramData.Add(bin);
                }

                histogram.Statistics = _histogramService.CalculateStatistics(histogram.CurrentData);
                OnHistogramUpdated?.Invoke(histogram);

                System.Diagnostics.Debug.WriteLine($"Updated histogram {histogram.Name} with {bins.Count} bins and {histogram.CurrentData.Count} data points");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating histogram for {histogram.Name}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error updating histogram {histogram.Name}: {ex.Message}");
            }
        }

        #endregion

        // Events for View interaction
        public event Action<int> OnDesignerRequested;
        public event Action<int, string, HistogramConfiguration> OnSaveLayoutRequested;
        public event Action<string> OnLoadLayoutRequested;
        public event Action<int, ChartLayoutWithMetadata> OnLayoutLoaded;
        public event Action<HistogramViewModel> OnHistogramUpdated;
        public event Action OnHistogramStructureChanged;
    }
}