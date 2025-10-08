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
using System.Windows.Shapes;
using DocumentFormat.OpenXml.Drawing;
using MySql.Data.MySqlClient;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for NewPassword.xaml
    /// </summary>
    public partial class NewPassword : Window
    {
        public string npassword = "";
        public NewPassword()
        {
            InitializeComponent();
            if (Home.DarkLight == true)
            {
                background.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3C3B3B"));
                label.Foreground = new SolidColorBrush(Colors.White);
                Notice1.Foreground = new SolidColorBrush(Colors.White);
                Notice2.Foreground = new SolidColorBrush(Colors.White);
                Notice3.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                background.Background = new SolidColorBrush(Colors.White);
                label.Foreground = new SolidColorBrush(Colors.Black);
                Notice1.Foreground = new SolidColorBrush(Colors.Black);
                Notice2.Foreground = new SolidColorBrush(Colors.Black);
                Notice3.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";
            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            connection.Open();
            string query = "SELECT Password FROM users WHERE UserID = @ID";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ID", LoginPage.Userid);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(0) == Input.Text)
                    {
                        Error.Text = "New password cannot be the same as current password.";
                        Error.Visibility = Visibility.Visible;
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            if (Input.Text.Length > 7 && Input.Text.Length < 41)
            {
                char[] specialchars = { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')'};
                if (Input.Text.Any(c => specialchars.Contains(c)))
                {
                    npassword = Input.Text;
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    Error.Text = "Please add at least 1 special character";
                    Error.Visibility = Visibility.Visible;
                    return;
                }
            }
            else
            {
                Error.Text = "Password too short/long. Must be in between 8 and 40 characters long.";
                Error.Visibility = Visibility.Visible;
                return;
            }
            
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
