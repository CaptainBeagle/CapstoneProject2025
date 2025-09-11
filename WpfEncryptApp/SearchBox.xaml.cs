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
using DocumentFormat.OpenXml.Drawing;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for SearchBox.xaml
    /// </summary>
    public partial class SearchBox : UserControl
    {
        public SearchBox()
        {
            InitializeComponent();
            Results.Visibility = Visibility.Collapsed;
        }

        public void ActiveSearch(object sender, EventArgs e)
        {
            //run a query for users by name.
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";    //Database credentials. If this were meant to be put into production,
                                                                                                    //a secure password would be set up along with other security measures.
            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            connection.Open();
            string query = "SELECT FirstName, LastName WHERE FirstName LIKE @input";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                command.Parameters.AddWithValue("@input", Search.Text + "%");

                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Results.Children.Clear();
                    Label newLabel = new Label();
                    newLabel.Content = reader["FirstName"].ToString() + " " + reader["LastName"];
                    Results.Children.Add(newLabel);
                }
                reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            //List all users' names that are "like" what is currently in the textbox inside the border as labels.

            //Figure Out Where Fatal Error Is Occuring

            //set border visibility to visible
        }
    }
}
