using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Linq;

public class GeocodingResult
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("admin1")]
    public string? State { get; set; }  // <-- add this line
}

public class GeocodingResponse
{
    [JsonPropertyName("results")]
    public GeocodingResult[]? Results { get; set; }
}

public class CurrentWeather
{
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("temperature_2m")]
    public double? Temperature_2m { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public double? Wind_Speed_10m { get; set; }

    [JsonPropertyName("precipitation")]
    public double? Precipitation { get; set; }

    [JsonPropertyName("precipitation_probability")]
    public double? Precipitation_Probability { get; set; }

    [JsonPropertyName("apparent_temperature")]
    public double? Apparent_Temperature { get; set; }
}

public class WeatherResponse
{
    [JsonPropertyName("current")]
    public CurrentWeather? Current { get; set; }
}

public class WeatherService
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<(string City, string State, string Country, double TempF, double FeelsLikeF, double WindMph, double RainChance, double RainInches)>
    GetWeatherAsync(string city, string? state = null, string? country = null)

    {
        // 🗺️ Build location query
        string locationQuery = city;
        if (!string.IsNullOrWhiteSpace(state))
            locationQuery += $", {state}";
        if (!string.IsNullOrWhiteSpace(country))
            locationQuery += $", {country}";

        // Encode safely for URL
        string encodedLocation = Uri.EscapeDataString(locationQuery);
        string geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={encodedLocation}&count=1&language=en&format=json";

        Console.WriteLine($"🔍 Trying location search: {locationQuery}");
        string geoJson = await client.GetStringAsync(geoUrl);
        var geoData = JsonSerializer.Deserialize<GeocodingResponse>(geoJson, options);
        var loc = geoData?.Results?.FirstOrDefault();

        // 🧭 Fallback — try city only if nothing found
        if (loc == null)
        {
            Console.WriteLine($"⚠️  No results for \"{locationQuery}\" — retrying with just city name.");
            string simpleQuery = Uri.EscapeDataString(city);
            string fallbackUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={simpleQuery}&count=1&language=en&format=json";

            string fallbackJson = await client.GetStringAsync(fallbackUrl);
            geoData = JsonSerializer.Deserialize<GeocodingResponse>(fallbackJson, options);
            loc = geoData?.Results?.FirstOrDefault();

            if (loc == null)
                throw new Exception($"City not found: {city}");
        }

        Console.WriteLine($"✅ Found: {loc.Name}, {loc.Country} ({loc.Latitude}, {loc.Longitude})");

        // 🌦️ Get weather data
        string vars = "temperature_2m,precipitation_probability,wind_speed_10m,precipitation,apparent_temperature";
        string weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={loc.Latitude}&longitude={loc.Longitude}&current={vars}";

        string weatherJson = await client.GetStringAsync(weatherUrl);
        var weather = JsonSerializer.Deserialize<WeatherResponse>(weatherJson, options)
                      ?? throw new Exception("Weather data missing.");

        // 📏 Convert to U.S. units
        double tempC = weather.Current?.Temperature_2m ?? 0;
        double feelsLikeC = weather.Current?.Apparent_Temperature ?? 0;
        double windKmh = weather.Current?.Wind_Speed_10m ?? 0;
        double rainMm = weather.Current?.Precipitation ?? 0;

        return (
            City: loc.Name ?? city,
            State: loc.State ?? state ?? "?",
            Country: loc.Country ?? "?",
            TempF: (tempC * 9 / 5) + 32,
            FeelsLikeF: (feelsLikeC * 9 / 5) + 32,
            WindMph: windKmh / 1.609,
            RainChance: weather.Current?.Precipitation_Probability ?? 0,
            RainInches: rainMm / 25.4
        );
    }
}
