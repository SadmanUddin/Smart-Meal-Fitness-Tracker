using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMeal.core.Models
{
    public class FitGoal
    {
        public Guid Id { get;set; }
        public Guid UserId { get; set; }
        public int DailyCalorieGoal { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
