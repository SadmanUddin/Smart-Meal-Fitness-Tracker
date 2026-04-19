using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMeal.core.Models
{
    public class HistoryItem
    {
        public string Type { get; set; } =string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;

        public int Calories { get; set; }
        public DateTime Date { get; set; }
    }
}
