// FoodService is a read-only service that fetches reference data from the database.
// It provides the dropdown options that users pick from when logging a meal:
//   - The list of foods to choose from (e.g. Chicken Breast, Rice, Banana...)
//   - The list of meal types (Breakfast, Lunch, Dinner, Snack)
//
// This service never writes to the database — it only reads.
// Both lists are loaded fresh from the DB each time AddMealView opens.

using SmartMeal.core.Models;
using Supabase;

namespace SmartMeal.core.Services
{
    public class FoodService
    {
        private readonly Client _client;

        public FoodService(Client client)
        {
            _client = client;
        }

        // Fetches all public, active food items sorted A to Z.
        // "Public" means the food was added by the system (seeded data), not by a specific user.
        // "Active" means the food has not been soft-deleted (is_active = true).
        // This gives us the 44 seeded foods (Chicken Breast, Rice, Banana, etc.)
        // that all users can pick from when logging a meal.
        public async Task<List<FoodItem>> GetPublicFoodsAsync()
        {
            var result = await _client.From<FoodItem>()
                .Where(f => f.IsPublic == true && f.IsActive == true)
                .Order("name", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            return result.Models;
        }

        // Fetches all meal types in display order (Breakfast first, Snack last).
        // These are the options shown in the Meal Type dropdown on AddMealView.
        // The display_order column in the database controls the sort order so it
        // can be adjusted without changing code.
        public async Task<List<MealType>> GetMealTypesAsync()
        {
            var result = await _client.From<MealType>()
                .Order("display_order", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            return result.Models;
        }

        // Returns all foods visible to the given user: public seeded foods + the user's own
        // private USDA-sourced foods. Two parallel queries are used because PostgREST OR
        // filters are cumbersome with the C# SDK, and both lists are small.
        public async Task<List<FoodItem>> GetAccessibleFoodsAsync(string userId)
        {
            var publicTask = _client.From<FoodItem>()
                .Where(f => f.IsPublic == true && f.IsActive == true)
                .Get();
            var privateTask = _client.From<FoodItem>()
                .Where(f => f.CreatedByUserId == userId && f.IsActive == true)
                .Get();
            await Task.WhenAll(publicTask, privateTask);

            return publicTask.Result.Models
                .Concat(privateTask.Result.Models)
                .OrderBy(f => f.Name)
                .ToList();
        }

        // Finds an existing food_items row for a USDA search result (by name) or inserts a
        // new private row for the user. This caches the USDA food locally so the FK in
        // meal_logs always points to a real food_items row, and calorie lookups continue to work.
        public async Task<FoodItem> UpsertSearchedFoodAsync(FoodSearchResult result, string userId)
        {
            // Reuse any existing food with the same name — public seeded or previously cached.
            var check = await _client.From<FoodItem>()
                .Where(f => f.Name == result.Name && f.IsActive == true)
                .Get();

            if (check.Models.FirstOrDefault() is { } existing)
                return existing;

            var inserted = await _client.From<FoodItem>().Insert(new FoodItem
            {
                Name = result.Name,
                CaloriesPer100g = result.CaloriesPer100g,
                ProteinPer100g = result.ProteinPer100g,
                CarbsPer100g = result.CarbsPer100g,
                FatsPer100g = result.FatsPer100g,
                IsPublic = false,
                IsActive = true,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            return inserted.Models.First();
        }
    }
}
