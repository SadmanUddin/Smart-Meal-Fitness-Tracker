using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class AddMealView : UserControl
    {
        private readonly MealService mealService;
        public AddMealView()
        {
            InitializeComponent();
            mealService = ((MainWindow)Application.Current.MainWindow).MealService;
        }

        private void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            if(!int.TryParse(CaloriesTextBox.Text, out int calories))
            {
                MessageBox.Show("Please enter a valid number for calories.");
                return;
            }

            var meal = new core.Models.Meal
            {
                Id = Guid.NewGuid(),
                Name = MealNameTextBox.Text,
                Calories = calories,
                Category = ((ComboBoxItem)CategoryComboBox.SelectedItem)?.Content.ToString() ?? "",
                Date = DateTime.Now
            };
            mealService.AddMeal(meal);
            MessageBox.Show("Meal added");
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new DashboardView());
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new DashboardView());
        }
    }
}
