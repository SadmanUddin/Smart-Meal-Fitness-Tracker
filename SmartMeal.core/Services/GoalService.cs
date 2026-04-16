// GoalService manages the user's fitness goals in the Supabase database.
//
// Each user has exactly ONE goal row in the goals table (enforced by a UNIQUE constraint
// on the user_id column). This means:
//   - The first time a user sets a goal  → we INSERT a new row.
//   - Every subsequent time they change it → we UPDATE the existing row.
//
// The UpsertGoalAsync method handles both cases automatically by checking first.
//
// Currently only the calorie_goal column is used by the UI, but the Goal model and
// the database table support protein, carbs, fat, and target weight goals too —
// they just need UI forms to go with them.

using SmartMeal.core.Models;
using Supabase;

namespace SmartMeal.core.Services
{
    public class GoalService
    {
        private readonly Client _client;

        public GoalService(Client client)
        {
            _client = client;
        }

        // Saves or updates the user's daily calorie goal.
        //
        // We check whether a goal row already exists for this user:
        //   - If YES → update the existing row's CalorieGoal value.
        //   - If NO  → insert a brand new goal row.
        //
        // This avoids duplicate rows, since the database also has a UNIQUE constraint
        // on user_id as a safety net. We handle it in code first for a better error message.
        public async Task UpsertGoalAsync(string userId, int calorieGoal)
        {
            var existing = await GetGoalAsync(userId);

            if (existing != null)
            {
                // Update the calorie goal on the row we already have,
                // then push the change to the database.
                existing.CalorieGoal = calorieGoal;
                await _client.From<Goal>()
                    .Where(g => g.UserId == userId)
                    .Update(existing);
            }
            else
            {
                // No goal row exists yet — create the first one.
                // Other goal fields (protein, carbs, fat, target weight) are left as NULL
                // because the UI currently only collects the calorie goal.
                await _client.From<Goal>().Insert(new Goal
                {
                    UserId = userId,
                    CalorieGoal = calorieGoal,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Retrieves the user's current goal from the database.
        // Returns null if the user has never set a goal.
        //
        // We use .Get() (which returns a list) rather than .Single() (which throws on no result)
        // because it's perfectly valid for a new user to have no goal row yet.
        // .FirstOrDefault() then gives us either the single row or null — clean and safe.
        public async Task<Goal?> GetGoalAsync(string userId)
        {
            var result = await _client.From<Goal>()
                .Where(g => g.UserId == userId)
                .Get();
            return result.Models.FirstOrDefault();
        }
    }
}
