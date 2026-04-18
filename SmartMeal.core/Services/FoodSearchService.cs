// FoodSearchService queries the USDA FoodData Central API to search for foods.
//
// The USDA FDC database contains ~700,000 foods including generic items (Foundation/SR Legacy)
// and branded products. This service uses Foundation and SR Legacy data types only because they
// store nutrition values per 100g — the same unit the rest of the app is built around.
//
// API key: get a free key at https://fdc.nal.usda.gov/api-key-signup.html
// If no key is provided, DEMO_KEY is used (rate-limited to ~30 requests/hour per IP — fine for testing).
//
// Rate limits for registered keys: 3,600 requests/hour — more than enough for interactive search.

using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartMeal.core.Services
{
    public class FoodSearchService
    {
        private static readonly HttpClient _http = new();
        private readonly string _apiKey;

        private const string SearchUrl = "https://api.nal.usda.gov/fdc/v1/foods/search";

        public FoodSearchService(string? apiKey)
        {
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? "DEMO_KEY" : apiKey.Trim();
        }

        // Searches the USDA FDC database and returns up to 20 matching foods.
        // Returns an empty list on empty query — callers can skip the API call check
        // by passing at least 2 characters (enforced in the view layer).
        public async Task<List<FoodSearchResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new();

            var url = $"{SearchUrl}?query={Uri.EscapeDataString(query)}" +
                      "&dataType=Foundation,SR%20Legacy" +
                      "&pageSize=20" +
                      $"&api_key={_apiKey}";

            var json = await _http.GetStringAsync(url);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = JsonSerializer.Deserialize<UsdaSearchResponse>(json, options);

            if (response?.Foods == null) return new();

            return response.Foods
                .Select(f => new FoodSearchResult(
                    FdcId: f.FdcId,
                    Name: ToTitleCase(f.Description),
                    CaloriesPer100g: Math.Round((decimal)GetNutrient(f, 1008), 1),
                    ProteinPer100g: Math.Round((decimal)GetNutrient(f, 1003), 1),
                    CarbsPer100g: Math.Round((decimal)GetNutrient(f, 1005), 1),
                    FatsPer100g: Math.Round((decimal)GetNutrient(f, 1004), 1)))
                .Where(r => r.CaloriesPer100g > 0)
                .ToList();
        }

        private static double GetNutrient(UsdaFoodItem food, int nutrientId)
            => food.FoodNutrients.FirstOrDefault(n => n.NutrientId == nutrientId)?.Value ?? 0.0;

        // Converts USDA ALL-CAPS descriptions to Title Case for cleaner display.
        private static string ToTitleCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(s.ToLowerInvariant());
        }

        // --- USDA API response deserialization types (private to this service) ---

        private sealed class UsdaSearchResponse
        {
            [JsonPropertyName("foods")]
            public List<UsdaFoodItem> Foods { get; set; } = new();
        }

        private sealed class UsdaFoodItem
        {
            [JsonPropertyName("fdcId")]
            public int FdcId { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("foodNutrients")]
            public List<UsdaNutrient> FoodNutrients { get; set; } = new();
        }

        private sealed class UsdaNutrient
        {
            [JsonPropertyName("nutrientId")]
            public int NutrientId { get; set; }

            [JsonPropertyName("value")]
            public double Value { get; set; }
        }
    }

    // Lightweight DTO returned from the USDA search — not a DB model.
    // All nutrition values are per 100g (Foundation/SR Legacy guarantee this).
    public sealed record FoodSearchResult(
        int FdcId,
        string Name,
        decimal CaloriesPer100g,
        decimal ProteinPer100g,
        decimal CarbsPer100g,
        decimal FatsPer100g)
    {
        public string CaloriesLabel =>
            $"{CaloriesPer100g:F0} kcal  ·  {ProteinPer100g:F1}g protein  ·  {CarbsPer100g:F1}g carbs  ·  {FatsPer100g:F1}g fat   per 100g";
    }
}
