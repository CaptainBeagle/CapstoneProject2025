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
using MySql.Data;
using MySql.Data.MySqlClient;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private static string UID;
        public static string Userid
        {
            get { return UID; }
            set 
            {
                if (UID != value)
                {
                    UID = value;
                }
            }
        }
        public LoginPage()
        {
            InitializeComponent();
            ErrorMsg.Visibility = Visibility.Collapsed;
        }

        private void SignIn(object sender, EventArgs e)
        {
            string UserN = UserName.Text;
            string Pass = Password.Password.ToString();
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";    //Database credentials. If this were to be put into production,
                                                                                                    //a secure password would be set up along with other security measures.
            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);   //setting up the connection
            connection.Open();  //opening the connection
            string query = "SELECT COUNT(*) FROM users WHERE UserID = @UserN AND Password = @Pass"; //Query to find if user info was correct

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserN", UserN);   //this inserts the text into the query without risking an SQL injection attack
                command.Parameters.AddWithValue("@Pass", Pass);

                int count = Convert.ToInt32(command.ExecuteScalar());

                if (count == 1)
                {
                    connection.Close();
                    ErrorMsg.Visibility = Visibility.Collapsed;
                    Userid = UserN;
                    //Correct login info. Go to a home page determined by user info
                    NavigationService.Navigate(new Uri("Home.xaml", UriKind.Relative));
                }
                else if (count > 1)
                {
                    //Duplicate user info in database. Create console log for the error
                    Console.WriteLine("$Error: Duplicate user information in database");
                }
                else
                {
                    //Incorrect login info. Inform User
                    UserName.Text = null;
                    Password.Password = null;
                    ErrorMsg.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
