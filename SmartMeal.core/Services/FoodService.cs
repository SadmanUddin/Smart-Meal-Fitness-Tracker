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
    }
}
