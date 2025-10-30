using System.Windows;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace WpfEncryptApp
{
    //Window where user can enter new password.
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
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();
            string query = "SELECT Password FROM users WHERE UserID = @ID";
            Error.Text = "";
            bool wrong = false;
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ID", LoginPage.Userid);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(0) == Input.Text)
                    {
                        Error.Text = "New password cannot be the same as current password.";
                        wrong = true;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            if (Input.Text.Length > 9 && Input.Text.Length < 41)
            {
                
            }
            else
            {
                if (!string.IsNullOrEmpty(Error.Text))
                {
                    Error.Text = Error.Text + "\n";
                }
                Error.Text = Error.Text + "Password too short/long. Must be in between 10 and 40 characters long.";
                wrong = true;
            }

            char[] specialchars = { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')' };
            if (!Input.Text.Any(c => specialchars.Contains(c)))
            {
                if (!string.IsNullOrEmpty(Error.Text))
                {
                    Error.Text = Error.Text + "\n";
                }
                Error.Text = Error.Text + "Please add at least 1 special character";
                wrong = true;
            }

            char[] nums = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
            if (!Input.Text.Any(c => nums.Contains(c)))
            {
                if (!string.IsNullOrEmpty(Error.Text))
                {
                    Error.Text = Error.Text + "\n";
                }
                Error.Text = Error.Text + "Please add at least 1 number";
                wrong = true;
            }

            if (wrong == false)
            {
                npassword = Input.Text;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                Error.Visibility = Visibility.Visible;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
