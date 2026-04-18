using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartMeal.core.Services
{
    // Calls the Gemini 1.5 Flash REST API to generate a personalised daily meal plan.
    // The prompt is built from the user's profile so recommendations respect their
    // calorie goal, dietary preferences, and allergies.
    public class GeminiService
    {
        private readonly string _apiKey;
        private static readonly HttpClient _http = new();
        private const string Endpoint =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

        public GeminiService(string apiKey)
        {
            _apiKey = apiKey;
        }

        // Generates a full-day meal plan split into Breakfast, Lunch, Dinner, Snacks.
        // Returns null if the API call fails or the response cannot be parsed.
        public async Task<MealPlan?> GenerateMealPlanAsync(MealPlanRequest request)
        {
            var prompt = BuildPrompt(request);

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1024
                }
            };

            try
            {
                var url = $"{Endpoint}?key={_apiKey}";
                var response = await _http.PostAsJsonAsync(url, body);
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Gemini API error {(int)response.StatusCode}: {raw}");

                using var doc = JsonDocument.Parse(raw);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                return ParseMealPlan(text);
            }
            catch
            {
                return null;
            }
        }

        private static string BuildPrompt(MealPlanRequest r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a certified nutritionist. Generate a one-day meal plan for this person.");
            sb.AppendLine();
            sb.AppendLine($"Daily calorie goal: {r.CalorieGoal} kcal");
            if (r.Age.HasValue)       sb.AppendLine($"Age: {r.Age} years");
            if (r.Gender != null)     sb.AppendLine($"Gender: {r.Gender}");
            if (r.WeightKg.HasValue)  sb.AppendLine($"Weight: {r.WeightKg} kg");
            if (r.HeightCm.HasValue)  sb.AppendLine($"Height: {r.HeightCm} cm");
            if (!string.IsNullOrWhiteSpace(r.FoodPreferences))
                sb.AppendLine($"Dietary preferences: {r.FoodPreferences}");
            if (!string.IsNullOrWhiteSpace(r.Allergies))
                sb.AppendLine($"Allergies / foods to avoid: {r.Allergies}");
            sb.AppendLine();
            sb.AppendLine("Return ONLY valid JSON in this exact format (no markdown, no extra text):");
            sb.AppendLine(@"{
  ""breakfast"": [{ ""name"": ""..."", ""calories"": 0, ""description"": ""..."" }],
  ""lunch"":     [{ ""name"": ""..."", ""calories"": 0, ""description"": ""..."" }],
  ""dinner"":    [{ ""name"": ""..."", ""calories"": 0, ""description"": ""..."" }],
  ""snacks"":    [{ ""name"": ""..."", ""calories"": 0, ""description"": ""..."" }]
}");
            sb.AppendLine("Each section should have 2-3 items. Calories should be realistic numbers.");
            return sb.ToString();
        }

        private static MealPlan? ParseMealPlan(string text)
        {
            // Strip markdown code fences if Gemini wrapped the JSON
            var clean = text.Trim();
            if (clean.StartsWith("```"))
            {
                var first = clean.IndexOf('\n');
                var last  = clean.LastIndexOf("```");
                if (first >= 0 && last > first)
                    clean = clean[(first + 1)..last].Trim();
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<MealPlan>(clean, options);
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
