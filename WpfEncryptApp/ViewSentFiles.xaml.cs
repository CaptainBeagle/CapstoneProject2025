using MySql.Data.MySqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace WpfEncryptApp
{
    //Page to view the files that the user sent to other users.
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
                Labels.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                HomeGrid.Background = new SolidColorBrush(Colors.White);
                Welcome.Foreground = new SolidColorBrush(Colors.Black);
                Labels.Foreground = new SolidColorBrush(Colors.Black);
            }
            //Programatically setting the file paths so it should work on a different computer 
            BitmapImage BitmapIcon2 = new BitmapImage();
            BitmapIcon2.BeginInit();
            var fullpath2 = Path.GetFullPath("Icons/Home.png");
            BitmapIcon2.UriSource = new Uri(fullpath2, UriKind.Absolute);
            BitmapIcon2.EndInit();
            Himg.Source = BitmapIcon2;

            BitmapImage BitmapIcon3 = new BitmapImage();
            BitmapIcon3.BeginInit();
            var fullpath3 = Path.GetFullPath("Icons/Mail.png");
            BitmapIcon3.UriSource = new Uri(fullpath3, UriKind.Absolute);
            BitmapIcon3.EndInit();
            Mimg.Source = BitmapIcon3;

            BitmapImage BitmapIcon4 = new BitmapImage();
            BitmapIcon4.BeginInit();
            var fullpath4 = Path.GetFullPath("Icons/Options.png");
            BitmapIcon4.UriSource = new Uri(fullpath4, UriKind.Absolute);
            BitmapIcon4.EndInit();
            Oimg.Source = BitmapIcon4;
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
            MySqlConnection connection = new MySqlConnection(connectionString);
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

        private bool inprogress = false;

        private void newRecNotif_OnClick(object sender, RecNotif.ClickEventArgs e)
        {
            if (inprogress == true)
            {
                return;
            }
            inprogress = true;
            MemoryStream DisData = e.data;
            string senduser = e.user;
            string fn = e.title;
            RecievedFileDisplay recfiledis = new RecievedFileDisplay(DisData, senduser, fn);
            NavigationService.Navigate(recfiledis);
        }
    }
}
