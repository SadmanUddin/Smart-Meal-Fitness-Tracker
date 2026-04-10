using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMeal.core.Models
{
    public class Meal
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty; // to avoid null issues
        public int Calories { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; } = string.Empty; // Type of meals..like brakfast or lunch etc
    }
}
