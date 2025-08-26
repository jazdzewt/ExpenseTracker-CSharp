using Syncfusion.Maui.Charts;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.Json;

namespace MobileApp
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<ChartData> ChartData { get; set; }
        public CircularDataLabelSettings DataLabelSettings { get; set; }
        public ICommand ShowItemDetailsCommand { get; set; }

        private ObservableCollection<ChartData> _filteredData = new();
        private string _searchText = "";
        private string _sortCriteria = "None";


        public MainPage()
        {
            InitializeComponent();
            InitializeChart();
            InitializeCommands();
            BindingContext = this;
        }
        private void InitializeCommands()
        {
            ShowItemDetailsCommand = new Command<ChartData>(async (item) =>
            {
                if (item != null)
                {
                    string message = $"Name: {item.Name}\nValue: {item.Value} $\n" +
                                   $"Category: {item.GetCategoryDescription()}\n\n" +
                                   (!string.IsNullOrWhiteSpace(item.description) ? $"Description: \n{item.description}\n" : "");

                    bool edit = await DisplayAlert("Expense Details", message.Trim(), "Edit", "Close");

                    if (edit)
                    {
                        var editPage = new EditItemPage(item, ChartData.Select(i => i.Name));
                        editPage.ItemDeleted += async (s, itemToRemove) =>
                        {
                            if (ChartData.Contains(itemToRemove))
                            {
                                ChartData.Remove(itemToRemove);
                                RefreshChartAndData();
                                await SaveDataAsync();
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] Item not found in ChartData for deletion: {itemToRemove?.Name} (ID: {itemToRemove?.Id})");
                            }
                        };

                        editPage.Disappearing += async (s, e) =>
                        {
                            RefreshChartAndData();
                            await SaveDataAsync();
                        };
                        await Navigation.PushModalAsync(editPage);
                    }
                }
            });
        }

        private void RefreshChartAndData()
        {
            if (ChartData == null || ChartData.Count == 0)
                return;

            ExpenseListView.ItemsSource = null;
            ExpenseListView.ItemsSource = _filteredData;

            chart.Series.Clear();

            var sortedChartData = ChartData.OrderBy(x => x.Category).ToList();

            var updatedSeries = new PieSeries
            {
                ItemsSource = sortedChartData,
                XBindingPath = "Name",
                YBindingPath = "Value",
                ShowDataLabels = true,
                PaletteBrushes = sortedChartData.Select(x => new SolidColorBrush(x.GetItemColor())).ToArray()
            };

            chart.Series.Add(updatedSeries);

            double monthlyTotal = ChartData.Sum(x => x.Value);
            double yearlyTotal = monthlyTotal * 12;
            MonthlyExpenseLabel.Text = $"Monthly Expense: {monthlyTotal:F2} $";
            YearlyExpenseLabel.Text = $"Yearly Expense: {yearlyTotal:F2} $";

            RefreshCategorySummary();
            ApplyFilterAndSort();

        }

        private void RefreshCategorySummary()
        {
            CategorySummaryGrid.Children.Clear();
            CategorySummaryGrid.RowDefinitions.Clear();

            var categoryGroups = ChartData
                .GroupBy(item => item.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Description = g.First().GetCategoryDescription(),
                    Total = g.Sum(item => item.Value)
                })
                .ToList();

            double overallTotal = ChartData.Sum(item => item.Value);

            int row = 0;
            foreach (var group in categoryGroups)
            {
                double percentage = (group.Total / overallTotal) * 100;
                var firstItemInCategory = ChartData.First(item => item.Category == group.Category);
                var categoryColor = firstItemInCategory.GetItemColor();

                CategorySummaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label1 = new Label
                {
                    Text = group.Description,
                    FontSize = 14,
                    TextColor = categoryColor
                };

                var label2 = new Label
                {
                    Text = $"{group.Total:F2} $ ({percentage:F1}%)",
                    FontSize = 14,
                    TextColor = categoryColor,
                    HorizontalTextAlignment = TextAlignment.End
                };

                Grid.SetRow(label1, row);
                Grid.SetColumn(label1, 0);
                Grid.SetRow(label2, row);
                Grid.SetColumn(label2, 1);

                CategorySummaryGrid.Children.Add(label1);
                CategorySummaryGrid.Children.Add(label2);

                row++;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
            RefreshChartAndData();
        }
        private async void OnCounterClicked(object sender, EventArgs e)
        {
            var addItemPage = new AddItemPage(ChartData.Select(i => i.Name));

            addItemPage.ItemAdded += async (s, newItem) =>
            {
                if (newItem != null)
                {
                    ChartData.Add(newItem);
                    RefreshChartAndData();
                    await SaveDataAsync();
                }
            };

            await Navigation.PushModalAsync(addItemPage);
        }

        private async void InitializeChart()
        {
            await LoadDataAsync();

            _filteredData = new ObservableCollection<ChartData>();
            ApplyFilterAndSort();

            RefreshChartAndData();
        }

        private void InitializeDefaultData()
        {
            ChartData = new ObservableCollection<ChartData>
                {
                    new ChartData { Name = "Example", Value = 100, Category = CategoryType.Other }
                };
        }

        private string GetDataFilePath()
        {
            string folder = FileSystem.AppDataDirectory;
            return Path.Combine(folder, "chartdata.json");
        }

        private async Task SaveDataAsync()
        {
            try
            {
                string json = JsonSerializer.Serialize(ChartData);
                string path = GetDataFilePath();
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                string path = GetDataFilePath();

                if (File.Exists(path))
                {
                    string json = await File.ReadAllTextAsync(path);
                    var loadedData = JsonSerializer.Deserialize<ObservableCollection<ChartData>>(json);

                    if (loadedData != null)
                    {
                        ChartData = loadedData;
                    }
                }
                else
                {
                    InitializeDefaultData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
                InitializeDefaultData();
            }
        }

        private void ApplyFilterAndSort()
        {
            var filtered = ChartData
                .Where(item => string.IsNullOrWhiteSpace(_searchText) ||
                               item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            filtered = _sortCriteria switch
            {
                "Name (A-Z)" => filtered.OrderBy(i => i.Name),
                "Name (Z-A)" => filtered.OrderByDescending(i => i.Name),
                "Value (Low → High)" => filtered.OrderBy(i => i.Value),
                "Value (High → Low)" => filtered.OrderByDescending(i => i.Value),
                _ => filtered
            };

            _filteredData.Clear();
            foreach (var item in filtered)
                _filteredData.Add(item);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue;
            ApplyFilterAndSort();
        }

        private void OnSortPickerChanged(object sender, EventArgs e)
        {
            _sortCriteria = SortPicker.SelectedItem?.ToString() ?? "None";
            ApplyFilterAndSort();
        }
    }
}
