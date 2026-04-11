using SmartMeal.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SmartMeal.Views;
using SmartMeal.core.Services;

namespace SmartMeal
{
    public partial class MainWindow : Window
    {
        // adding services globally
        public AuthService AuthService { get; } = new AuthService();
        public MealService MealService { get; } = new MealService();
        public ActService ActService { get; } = new ActService();
        public MainWindow()
        {
            InitializeComponent();
            MainContent.Content = new RegisterView();
        }

        public void Navigate(UserControl View) // navigate method to switchhh
        {
            MainContent.Content = View;
        }
    }
}