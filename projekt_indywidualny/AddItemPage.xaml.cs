using System.ComponentModel;
namespace MobileApp
{
    public partial class AddItemPage : ContentPage
    {
        public event EventHandler<ChartData> ItemAdded;
        private readonly IEnumerable<string> existingNames;

        public AddItemPage(IEnumerable<string> existingNames)
        {
            InitializeComponent();
            this.existingNames = existingNames.Select(n => n.ToLowerInvariant()).ToList();
            PeriodPicker.SelectedIndex = 0;
            LoadCategories();
        }

        private string GetEnumDescription(CategoryType value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                 .FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? value.ToString();
        }

        private void LoadCategories()
        {
            var categories = Enum.GetValues(typeof(CategoryType))
                                 .Cast<CategoryType>()
                                 .Select(ct => new CategoryPickerItem
                                 {
                                     Value = ct,
                                     Description = GetEnumDescription(ct)
                                 })
                                 .ToList();

            CategoryPicker.ItemsSource = categories;
        }
        private double NormalizeToMonthlyValue(double value, string period)
        {
            return period switch
            {
                "1 month" => value,
                "3 months" => value / 3,
                "6 months" => value / 6,
                "1 year" => value / 12,
                _ => value
            };
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            string name = NameEntry.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                await DisplayAlert("Error", "Name is required.", "OK");
                return;
            }

            if (existingNames.Contains(name.ToLowerInvariant()))
            {
                await DisplayAlert("Error", "Name must be unique.", "OK");
                return;
            }

            if (double.TryParse(ValueEntry.Text, out double value))
            {
                var selectedPeriod = PeriodPicker.SelectedItem?.ToString() ?? "1 month";
                var normalizedValue = NormalizeToMonthlyValue(value, selectedPeriod);
                normalizedValue = Math.Round(normalizedValue, 2);

                var newItem = new ChartData
                {
                    Name = name,
                    Value = normalizedValue,
                    description = DescriptionEntry.Text,
                    Category = (CategoryPicker.SelectedItem as CategoryPickerItem)?.Value ?? CategoryType.Other

                };

                ItemAdded?.Invoke(this, newItem);
                await Navigation.PopModalAsync();
            }
            else
            {
                await DisplayAlert("Error", "Invalid value entered.", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }

    public class CategoryPickerItem
    {
        public CategoryType Value { get; set; }
        public string Description { get; set; }
        public override string ToString() => Description;
    }

}