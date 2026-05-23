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
