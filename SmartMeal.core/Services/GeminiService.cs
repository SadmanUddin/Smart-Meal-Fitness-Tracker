using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartMeal.core.Services
{
    public class GeminiService
    {
        private readonly string _apiKey;
        private static readonly HttpClient _http = new();
        private const string Model = "gemini-2.5-flash";
        private const string Endpoint =
            $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent";

        // Reusable schema object — tells Gemini the exact JSON shape to produce.
        // Using uppercase type names required by the Gemini structured-output API.
        private static readonly object _responseSchema = new
        {
            type = "OBJECT",
            properties = new
            {
                breakfast = MealArraySchema(),
                lunch     = MealArraySchema(),
                dinner    = MealArraySchema(),
                snacks    = MealArraySchema()
            },
            required = new[] { "breakfast", "lunch", "dinner", "snacks" }
        };

        private static object MealArraySchema() => new
        {
            type  = "ARRAY",
            items = new
            {
                type = "OBJECT",
                properties = new
                {
                    name        = new { type = "STRING" },
                    calories    = new { type = "INTEGER" },
                    description = new { type = "STRING" }
                },
                required = new[] { "name", "calories", "description" }
            }
        };

        public GeminiService(string apiKey) => _apiKey = apiKey;

        public async Task<MealPlan> GenerateMealPlanAsync(MealPlanRequest request)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException(
                    "Gemini API key is empty. Set GeminiApiKey in supabase.config.json or SMARTMEAL_GEMINI_API_KEY.");

            var bodyJson = JsonSerializer.Serialize(new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = BuildPrompt(request) } } }
                },
                generationConfig = new
                {
                    temperature      = 0.7,
                    maxOutputTokens  = 8192,
                    responseMimeType = "application/json",
                    responseSchema   = _responseSchema
                }
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            req.Headers.Add("x-goog-api-key", _apiKey);

            var response = await _http.SendAsync(req);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gemini API returned {(int)response.StatusCode}: {raw}");

            using var doc = JsonDocument.Parse(raw);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            var plan = ParseMealPlan(text);
            if (plan == null)
                throw new InvalidOperationException(
                    $"Could not parse Gemini response. Raw text:{Environment.NewLine}{text[..Math.Min(text.Length, 500)]}");

            return plan;
        }

        private static string BuildPrompt(MealPlanRequest r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a nutritionist. Generate a one-day meal plan.");
            sb.AppendLine($"Calorie goal: {r.CalorieGoal} kcal/day.");
            if (r.Age.HasValue)      sb.AppendLine($"Age: {r.Age}.");
            if (r.Gender != null)    sb.AppendLine($"Gender: {r.Gender}.");
            if (r.WeightKg.HasValue) sb.AppendLine($"Weight: {r.WeightKg} kg.");
            if (r.HeightCm.HasValue) sb.AppendLine($"Height: {r.HeightCm} cm.");
            if (!string.IsNullOrWhiteSpace(r.FoodPreferences))
                sb.AppendLine($"Dietary preferences: {r.FoodPreferences}.");
            if (!string.IsNullOrWhiteSpace(r.Allergies))
                sb.AppendLine($"Avoid: {r.Allergies}.");
            sb.AppendLine("Provide 2-3 items per meal. Keep descriptions under 10 words.");
            return sb.ToString();
        }

        private static MealPlan? ParseMealPlan(string text)
        {
            var clean = text.Trim();

            if (clean.StartsWith("```"))
            {
                var first = clean.IndexOf('\n');
                var last  = clean.LastIndexOf("```");
                if (first >= 0 && last > first)
                    clean = clean[(first + 1)..last].Trim();
            }

            var start = clean.IndexOf('{');
            var end   = clean.LastIndexOf('}');
            if (start >= 0 && end > start)
                clean = clean[start..(end + 1)];

            try
            {
                return JsonSerializer.Deserialize<MealPlan>(clean,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }
    }

    public sealed class MealPlanRequest
    {
        public int CalorieGoal { get; init; }
        public int? Age { get; init; }
        public string? Gender { get; init; }
        public decimal? WeightKg { get; init; }
        public decimal? HeightCm { get; init; }
        public string? FoodPreferences { get; init; }
        public string? Allergies { get; init; }
    }

    public sealed class MealPlan
    {
        [JsonPropertyName("breakfast")]
        public List<MealItem> Breakfast { get; set; } = new();
        [JsonPropertyName("lunch")]
        public List<MealItem> Lunch { get; set; } = new();
        [JsonPropertyName("dinner")]
        public List<MealItem> Dinner { get; set; } = new();
        [JsonPropertyName("snacks")]
        public List<MealItem> Snacks { get; set; } = new();
    }

    public sealed class MealItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("calories")]
        public int Calories { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
