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

        public void SearchUsers(string searchText)
        {
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";    //Database credentials. If this were to be put into production,
                                                                                                    //a secure password would be set up along with other security measures.

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            using (MySqlCommand command = new MySqlCommand("SELECT FirstName, LastName FROM users WHERE FirstName LIKE @input", connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@input", searchText + "%");

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
                            //find a way to remove the label that has the current User's name on it
                            //maybe check if label content is like user name text in Home.xaml.cs
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
