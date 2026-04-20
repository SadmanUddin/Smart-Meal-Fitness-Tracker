// MealService handles all database operations for the meal_logs table.
// A "meal log" is a single record of a user eating something:
// "User X ate 150g of Chicken Breast (food_id=1) for Lunch (meal_type_id=2) on 2026-04-16."
//
// This service does NOT know about food names or meal type names — it only works with IDs.
// The views are responsible for loading the food list and meal type list separately
// (via FoodService) and passing the selected IDs in here.

using SmartMeal.core.Models;
using Supabase;

namespace SmartMeal.core.Services
{
    public class MealService
    {
        // The live Supabase client used for all database queries in this service.
        private readonly Client _client;

        public MealService(Client client)
        {
            _client = client;
        }

        // Saves a new meal log to the database.
        // Called by AddMealView when the user clicks "Add Meal".
        //
        // Parameters:
        //   userId     — from AuthService.CurrentUser.Id (the logged-in user's UUID)
        //   foodId     — from the selected FoodItem.FoodId (which food they ate)
        //   grams      — from the GramsTextBox (how much they ate)
        //   mealTypeId — from the selected MealType.MealTypeId (breakfast/lunch/etc.)
        public async Task AddMealLogAsync(string userId, long foodId, decimal grams, short mealTypeId)
        {
            var log = new MealLog
            {
                UserId = userId,
                FoodId = foodId,
                Grams = grams,
                MealTypeId = mealTypeId,
                // Format the date as "yyyy-MM-dd" — PostgreSQL's DATE column accepts this string.
                // We use the local machine time (not UTC) so the date matches the user's calendar day.
                LogDate = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd"),
                // CreatedAt is also set by the DB default (now()), but we set it here too
                // so the in-memory object has a valid timestamp right away.
                CreatedAt = DateTime.UtcNow
            };
            await _client.From<MealLog>().Insert(log);
        }

        // Permanently removes a meal log from the database.
        // Called by MealsView when the user clicks the Delete button on a row.
        // The mealLogId comes from the button's Tag, which is bound to MealLog.MealLogId in the XAML.
        public async Task DeleteMealLogAsync(long mealLogId)
        {
            await _client.From<MealLog>()
                .Where(m => m.MealLogId == mealLogId)
                .Delete();
        }

        // Returns only today's meal logs for a given user.
        // Used by DashboardView to calculate the daily totals (grams consumed, meal count).
        // "Today" is always the local machine date, formatted to match the log_date column.
        public async Task<List<MealLog>> GetTodayLogsAsync(string userId)
        {
            var today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
            var result = await _client.From<MealLog>()
                .Where(m => m.UserId == userId && m.LogDate == today)
                .Get();
            return result.Models;
        }

        // Calculates total consumed calories for a set of meal logs.
        // Formula: (grams / 100) * calories_per_100g, summed across all logs.
        public async Task<decimal> CalculateCaloriesConsumedAsync(IEnumerable<MealLog> logs)
        {
            var logList = logs.ToList();
            if (logList.Count == 0)
                return 0;

            var foodsResult = await _client.From<FoodItem>().Get();
            var caloriesPer100gByFoodId = new Dictionary<long, decimal>();
            foreach (var food in foodsResult.Models)
                caloriesPer100gByFoodId[food.FoodId] = food.CaloriesPer100g;

            decimal totalCalories = 0;
            foreach (var log in logList)
            {
                if (caloriesPer100gByFoodId.TryGetValue(log.FoodId, out var caloriesPer100g))
                    totalCalories += (log.Grams / 100m) * caloriesPer100g;
            }

            return totalCalories;
        }

        // Returns every meal log ever recorded by a given user, newest first.
        // Used by MealsView to populate the full meal history table.
        // Ordering by log_date descending means the most recent meals appear at the top.
        public async Task<List<MealLog>> GetAllLogsAsync(string userId)
        {
            var result = await _client.From<MealLog>()
                .Where(m => m.UserId == userId)
                .Order("log_date", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }
    }
}
