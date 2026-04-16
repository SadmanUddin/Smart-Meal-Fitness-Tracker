// Represents a row in the public.meal_types table.
// This is a small lookup/reference table that defines the available meal periods:
//   1 = Breakfast, 2 = Lunch, 3 = Dinner, 4 = Snack
//
// These rows are seeded into the database and never change at runtime.
// They are loaded once when AddMealView opens and used to populate the
// Meal Type dropdown. The user picks one, and we store its MealTypeId in meal_logs.

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SmartMeal.core.Models
{
    [Table("meal_types")]
    public class MealType : BaseModel
    {
        // The primary key — a small integer (smallint) because there will only ever
        // be a handful of meal types, so a full 64-bit integer is unnecessary.
        [PrimaryKey("meal_type_id")]
        public short MealTypeId { get; set; }

        // The display name shown in dropdowns — "breakfast", "lunch", "dinner", "snack".
        // Stored lowercase in the database.
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        // Controls the order items appear in the dropdown (1 = first, 4 = last).
        // Breakfast always appears first, snack always appears last.
        [Column("display_order")]
        public short DisplayOrder { get; set; }
    }
}
