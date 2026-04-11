using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMeal.core.Models
{
    public class Activity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CaloriesBurned { get; set; }
        public DateTime Date { get; set; }
        public int Duration { get; set; } // duration in minutes
    }
}
