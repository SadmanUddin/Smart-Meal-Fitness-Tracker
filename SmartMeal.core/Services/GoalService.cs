using SmartMeal.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMeal.core.Services
{
    public class GoalService
    {
        private static readonly List<FitGoal> Goals = new();

        public void AddGoal(FitGoal goal)
        {   
            Goals.Clear();
            Goals.Add(goal);
        }
        public FitGoal? GetGoal()
        {
            if(Goals.Count > 0)
            {
                return Goals[0];
            }
            return null;
        }
    }
}
