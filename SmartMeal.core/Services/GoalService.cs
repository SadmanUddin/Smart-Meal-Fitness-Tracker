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
            for(int i = 0; i < Goals.Count; i++)
            {
                if(Goals[i].UserId == goal.UserId)
                {
                    Goals[i] = goal;
                    return;
                }
            }
            Goals.Add(goal);
        }
        public FitGoal? GetGoal(Guid userId)
        {
            foreach(var goal in Goals)
            {
                if(goal.UserId == userId)
                {
                    return goal;
                }
            }
            return null;
        }
    }
}
