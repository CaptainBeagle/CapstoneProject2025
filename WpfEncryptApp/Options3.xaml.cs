using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using MySql.Data.MySqlClient;

namespace WpfEncryptApp
{
    //Options page. Has options to change visual appearance of app, change password, and log out.
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

        private void ChangePass_Click(object sender, RoutedEventArgs e)
        {
            //database call to alter password data in record with loginpage.userid
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();
            string update = "UPDATE users SET Password = @pass WHERE users.UserID = @ID";

            //open dialogue box with input field for new password and password requirements
            NewPassword Win = new NewPassword();
            bool? Pass = Win.ShowDialog();
            //If result confirmed, alter password data in DB
            if (Pass == true)
            {
                using (MySqlCommand command = new MySqlCommand(update, connection))
                {
                    command.Parameters.AddWithValue("@pass", Win.npassword);
                    command.Parameters.AddWithValue("@ID", LoginPage.Userid);
                    command.ExecuteNonQuery();
                }
                connection.Close();
                MessageBox.Show("Password Changed Successfully!");
            }
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            //Switch state uppon clicking and update text on button as well as appearance of everything.
            //Update entry in AccountSetting table in DB corresponding to User ID.
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";
            MySqlConnection connection = new MySqlConnection(connectionString);
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
