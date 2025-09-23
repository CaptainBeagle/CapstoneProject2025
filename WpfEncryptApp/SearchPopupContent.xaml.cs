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
using MySqlX.XDevAPI.Common;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for SearchPopupContent.xaml
    /// </summary>
    public partial class SearchPopupContent : UserControl
    {
        public SearchPopupContent()
        {
            InitializeComponent();
        }

        public void SearchUsers(string searchText, string filter)
        {
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";    //Database credentials. If this were to be put into production,
                                                                                                    //a secure password would be set up along with other security measures.
            string query = "";

            if (filter != null)
            {
                query = "SELECT FirstName, LastName FROM users WHERE FirstName LIKE @input AND Position LIKE @filter";
            }
            else
            {
                query = "SELECT FirstName, LastName FROM users WHERE FirstName LIKE @input";
            }
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@input", searchText + "%");
                    command.Parameters.AddWithValue("@filter", filter);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        Results.Children.Clear();

                        while (reader.Read())
                        {
                            Label newLabel = new Label();
                            newLabel.Content = reader["FirstName"] + " " + reader["LastName"];
                            newLabel.Background = new SolidColorBrush(Colors.White);
                            newLabel.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ClickLabel);
                            Results.Children.Add(newLabel);
                            //Remove the label that has the current User's name on it
                            if(Home.Output == newLabel.Content.ToString())
                            {
                                Results.Children.Remove(newLabel);
                            }
                        }
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        public void ClickLabel (object sender, MouseButtonEventArgs e)
        {
            Label name = sender as Label;
            
            FileSendDisplay parentwindow = Window.GetWindow(this) as FileSendDisplay;

            if (name != null)
            {
                parentwindow.MySearchBox.SearchText = name.Content.ToString();
            }
        }
    }
}
