using System.ComponentModel;

namespace MobileApp
{
    public partial class EditItemPage : ContentPage
    {
        public event EventHandler<ChartData> ItemDeleted;
        private readonly ChartData editingItem;

        private readonly IEnumerable<string> existingNames;

        public EditItemPage(ChartData item, IEnumerable<string> existingNames)
        {
            InitializeComponent();
            this.existingNames = existingNames
                .Where(n => !n.Equals(item.Name, StringComparison.OrdinalIgnoreCase)) // wyklucz aktualny
                .Select(n => n.ToLowerInvariant())
                .ToList();

            editingItem = item;

            LoadCategories();
            PeriodPicker.SelectedIndex = 0;

            NameEntry.Text = item.Name;
            ValueEntry.Text = item.Value.ToString();
            DescriptionEntry.Text = item.description;
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

            var selected = categories.FirstOrDefault(c => c.Value == editingItem.Category);
            if (selected != null)
                CategoryPicker.SelectedItem = selected;
        }

        private string GetEnumDescription(CategoryType value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                 .FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? value.ToString();
        }


        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Confirm Delete",
                "Are you sure you want to delete this item?",
                "Yes, Delete", "Cancel");

            if (confirm)
            {
                ItemDeleted?.Invoke(this, editingItem);
                await Navigation.PopModalAsync();
            }
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

                editingItem.Name = name;
                editingItem.Value = normalizedValue;
                editingItem.description = DescriptionEntry.Text;
                editingItem.Category = (CategoryPicker.SelectedItem as CategoryPickerItem)?.Value ?? CategoryType.Other;

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
}