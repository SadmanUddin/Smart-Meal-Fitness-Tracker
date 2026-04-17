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
        [PrimaryKey("meal_type_id")]
        public short MealTypeId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;


        [Column("display_order")]
        public short DisplayOrder { get; set; }
    }
}
