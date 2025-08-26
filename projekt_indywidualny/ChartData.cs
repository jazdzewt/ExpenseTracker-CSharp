
using System.ComponentModel;

namespace MobileApp
{
    public enum CategoryType
    {
        [Description("Media & Entertainment")]
        MediaAndEntertainment,

        [Description("Fitness & Health")]
        FitnessAndHealth,

        [Description("Housing & Utilities")]
        HousingAndUtilities,

        [Description("Transportation")]
        Transportation,

        [Description("Insurance & Healthcare")]
        InsuranceAndHealthcare,

        [Description("Education & Personal Development")]
        EducationAndPersonalDevelopment,

        [Description("Software & Tools")]
        SoftwareAndTools,

        [Description("Pets")]
        Pets,

        [Description("Other")]
        Other
    }

    public class ChartData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public double Value { get; set; }
        public string description { get; set; }
        public CategoryType Category { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is not ChartData other) return false;
            return ReferenceEquals(this, other) || Id == other.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();

        private static readonly Dictionary<CategoryType, (float Hue, float Saturation, float Lightness)> _categoryColors = new()
        {
            [CategoryType.MediaAndEntertainment] = (0.96f, 0.8f, 0.7f),
            [CategoryType.FitnessAndHealth] = (0.55f, 0.85f, 0.7f),
            [CategoryType.HousingAndUtilities] = (0.12f, 0.8f, 0.75f),
            [CategoryType.Transportation] = (0.5f, 0.8f, 0.65f),
            [CategoryType.InsuranceAndHealthcare] = (0.78f, 0.75f, 0.7f),
            [CategoryType.EducationAndPersonalDevelopment] = (0.09f, 0.85f, 0.7f),
            [CategoryType.SoftwareAndTools] = (0f, 0f, 0.75f),
            [CategoryType.Pets] = (0.35f, 0.75f, 0.7f),
            [CategoryType.Other] = (0.83f, 0.65f, 0.75f)
        };

        public Color GetItemColor()
        {
            var (hue, saturation, lightness) = _categoryColors.TryGetValue(Category, out var color)
                ? color
                : (0f, 0f, 0.8f);

            float variation = GetNameVariation(Name);
            float finalHue = (hue + variation * 0.1f) % 1f;
            float finalSaturation = Math.Clamp(saturation + variation * 0.1f, 0.4f, 0.75f);
            float finalLightness = Math.Clamp(lightness + variation * 0.05f, 0.7f, 0.85f);

            return FromHsl(finalHue, finalSaturation, finalLightness);
        }

        private static float GetNameVariation(string name)
        {
            return (Math.Abs(name.GetHashCode()) % 1000 / 1000f) - 0.5f;
        }

        private static Color FromHsl(float h, float s, float l)
        {
            return Color.FromHsla(h, s, l, 1.0f);
        }

        public string GetCategoryDescription()
        {
            var field = Category.GetType().GetField(Category.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                               .FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? Category.ToString();
        }
    }
}
