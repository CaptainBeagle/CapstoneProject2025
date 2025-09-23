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
using MySql.Data.MySqlClient;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Page
    {
        public Options()
        {
            InitializeComponent();
            ChangeAppearance();
        }

        private void H_Click(object sender, RoutedEventArgs e)
        {
            Home homepage = new Home();
            NavigationService.Navigate(homepage);
        }

        private void S_Click(object sender, RoutedEventArgs e)
        {
            ViewSentFiles viewsent = new ViewSentFiles();
            NavigationService.Navigate(viewsent);
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            LoginPage login = new LoginPage();
            NavigationService.Navigate(login);
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            //Switch state uppon clicking and update text on button as well as appearance of everything.
            //Update entry in AccountSetting table in DB corresponding to User ID.
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";
            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            connection.Open();
            string update = "UPDATE accountinfo SET Theme = @theme WHERE accountinfo.AccID = @ID";
            using (MySqlCommand command = new MySqlCommand(update, connection))
            {
                command.Parameters.AddWithValue("@ID", LoginPage.Userid);
                if (Theme.IsChecked == true)
                {
                    Home.DarkLight = true;
                    Theme.Content = "Dark Mode";
                    command.Parameters.AddWithValue("@theme", "Dark");
                }
                else
                {
                    Home.DarkLight = false;
                    Theme.Content = "Light Mode";
                    command.Parameters.AddWithValue("@theme", "Light");
                }
                command.ExecuteNonQuery();
            }
            connection.Close();
            //Update appearance of page
            ChangeAppearance();
        }

        private void ChangeAppearance()
        {
            if (Home.DarkLight == true)
            {
                Theme.IsChecked = true;
                Theme.Content = "Dark Mode";
                //rest of visual differences
                background.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3C3B3B"));
                Header.Foreground = new SolidColorBrush(Colors.White);
                General.Foreground = new SolidColorBrush(Colors.White);
                Account.Foreground = new SolidColorBrush(Colors.White);
                Label.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                Theme.IsChecked = false;
                Theme.Content = "Light Mode";
                //rest of visual differences
                background.Background = new SolidColorBrush(Colors.White);
                Header.Foreground = new SolidColorBrush(Colors.Black);
                General.Foreground = new SolidColorBrush(Colors.Black);
                Account.Foreground = new SolidColorBrush(Colors.Black);
                Label.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
    }
}
