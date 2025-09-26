using MySql.Data.MySqlClient;
using software.amazon.cryptography.services.dynamodb.internaldafny.types;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for ViewSentFiles.xaml
    /// </summary>
    public partial class ViewSentFiles : Page
    {
        public string UsableName;
        public System.IO.MemoryStream Message;

        public ViewSentFiles()
        {
            InitializeComponent();
            if (Home.DarkLight == true)
            {
                HomeGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3C3B3B"));
                Welcome.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                HomeGrid.Background = new SolidColorBrush(Colors.White);
                Welcome.Foreground = new SolidColorBrush(Colors.Black);
            }
            DisplayRecievedFiledata();
        }

        private void H_Click(object sender, RoutedEventArgs e)
        {
            Home home = new Home();
            NavigationService.Navigate(home);
        }

        private void O_Click(object sender, RoutedEventArgs e)
        {
            Options options = new Options();
            NavigationService.Navigate(options);
        }

        //Display recieved data from files table
        private void DisplayRecievedFiledata()
        {
            //recieve data from files table that has SendID the same as Login UID
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";
            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            connection.Open();
            string query = "SELECT Message, RecID, FileName FROM files WHERE SendID = @LoginID";
            string LoginID = LoginPage.Userid;
            Home.ToOrFrom = true;

            try
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LoginID", LoginID);
                    string RecID = "";
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        //find a way to dynamically find an encrpyted document's name
                        RecID = reader["RecID"].ToString();
                        byte[] encryptedBytes = (byte[])reader["Message"];
                        UsableName = reader["FileName"].ToString();

                        //Retrieving data directly from array to avoid corruption
                        MemoryStream memstream = new MemoryStream(encryptedBytes);
                        memstream.Position = 0;
                        Message = memstream;
                        RecNotif newRecNotif = new RecNotif(UsableName, RecID, Message);
                        newRecNotif.NotifClick += newRecNotif_OnClick;
                        Notifs.Children.Add(newRecNotif);
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            connection.Close();
        }

        private void newRecNotif_OnClick(object sender, RecNotif.ClickEventArgs e)
        {
            MemoryStream DisData = e.data;
            string senduser = e.user;
            string fn = e.title;
            RecievedFileDisplay recfiledis = new RecievedFileDisplay(DisData, senduser, fn);
            NavigationService.Navigate(recfiledis);
        }
    }
}
