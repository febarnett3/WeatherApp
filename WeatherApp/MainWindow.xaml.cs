using System;
using System.Windows;
using System.Windows.Media;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void GetWeather_Click(object sender, RoutedEventArgs e)
        {
            string input = CityInput.Text.Trim();

            // Ignore placeholder text
            if (input == "City, State, Country" || string.IsNullOrWhiteSpace(input))
            {
                OutputBlock.Text = "Please enter a location.";
                return;
            }

            // Split by commas → [city, state, country]
            var parts = input.Split(',', StringSplitOptions.TrimEntries);
            string city = parts.ElementAtOrDefault(0) ?? "";
            string? state = parts.ElementAtOrDefault(1);
            string? country = parts.ElementAtOrDefault(2);

            OutputBlock.Text = "Fetching weather data...";

            try
            {
                var result = await WeatherService.GetWeatherAsync(city, state, country);
                OutputBlock.Text =
                    $"{result.City}, {result.State}, {result.Country}\n" +
                    $"Currently {result.TempF:F1}°F (feels like {result.FeelsLikeF:F1}°F)\n" +
                    $"{result.WindMph:F1} mph wind\n" +
                    $"{result.RainChance:F0}% chance of rain\n";
            }
            catch (Exception ex)
            {
                OutputBlock.Text = $"Error: {ex.Message}";
            }
        }


        private void CityInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (CityInput.Text == "City, State, Country")
            {
                CityInput.Text = "";
                CityInput.Foreground = Brushes.Black;
            }
        }

        private void CityInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CityInput.Text))
            {
                CityInput.Text = "City, State, Country";
                CityInput.Foreground = Brushes.Gray;
            }
        }
    }
}
