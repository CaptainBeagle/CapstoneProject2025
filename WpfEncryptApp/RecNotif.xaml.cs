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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using MySql.Data.MySqlClient;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for RecNotif.xaml
    /// </summary>
    public partial class RecNotif : UserControl
    {
        public System.IO.MemoryStream M;
        public class ClickEventArgs : RoutedEventArgs
        {
            public System.IO.MemoryStream data { get; }
            public string user { get; }
            public string title { get; }
            public RecNotif SourceControl { get; }

            public ClickEventArgs(RoutedEvent routedevent, RecNotif SourceControl, System.IO.MemoryStream data, string user, string title) : base(routedevent, SourceControl)
            {
                this.SourceControl = SourceControl;
                this.data = data;
                this.user = user;
                this.title = title;
            }
        }
        
        public static readonly RoutedEvent NotifClickEvent = EventManager.RegisterRoutedEvent("NotifClick", RoutingStrategy.Bubble, typeof(System.EventHandler<RecNotif.ClickEventArgs>), typeof(RecNotif));

        public event System.EventHandler<RecNotif.ClickEventArgs> NotifClick
        {
            add { AddHandler(NotifClickEvent, value); }
            remove { RemoveHandler(NotifClickEvent, value); }
        }

        public RecNotif(string Title, string Sender, System.IO.MemoryStream Msg)
        {
            InitializeComponent();
            FileName.Text = Title;
            //query to find name using Sender
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";
            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            connection.Open();
            string query = "SELECT FirstName, LastName FROM users WHERE UserID = @Sender";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Sender", Sender);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    SenderName.Text = reader["FirstName"].ToString() + " " + reader["LastName"].ToString();
                }
            }
            M = Msg;
            this.MouseLeftButtonUp += RecNotif_MouseLeftButtonUp;
            if (Home.DarkLight == true)
            {
                Bar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3C3B3B"));
                FileName.Foreground = new SolidColorBrush(Colors.White);
                SenderName.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                Bar.Background = new SolidColorBrush(Colors.White);
                FileName.Foreground= new SolidColorBrush(Colors.Black);
                SenderName.Foreground = new SolidColorBrush(Colors.Black);
            }

            if (Title.Contains(".doc"))
            {
                BitmapImage BitmapIcon = new BitmapImage();
                BitmapIcon.BeginInit();
                Icon.Width = BitmapIcon.DecodePixelWidth = 65;
                BitmapIcon.UriSource = new Uri("C:\\Users\\Rhian\\OneDrive\\Desktop\\GitRepository\\CapstoneProject2025\\WpfEncryptApp\\Icons\\Word.png");
                BitmapIcon.EndInit();
                Icon.Source = BitmapIcon;
                
            }
            else if (Title.Contains(".xlsx"))
            {
                BitmapImage BitmapIcon = new BitmapImage();
                BitmapIcon.BeginInit();
                Icon.Width = BitmapIcon.DecodePixelWidth = 65;
                BitmapIcon.UriSource = new Uri("C:\\Users\\Rhian\\OneDrive\\Desktop\\GitRepository\\CapstoneProject2025\\WpfEncryptApp\\Icons\\Excel.png");
                BitmapIcon.EndInit();
                Icon.Source = BitmapIcon;
            }
            else
            {
                BitmapImage BitmapIcon = new BitmapImage();
                BitmapIcon.BeginInit();
                Icon.Width = BitmapIcon.DecodePixelWidth = 65;
                BitmapIcon.UriSource = new Uri("C:\\Users\\Rhian\\OneDrive\\Desktop\\GitRepository\\CapstoneProject2025\\WpfEncryptApp\\Icons\\PDF.png");
                BitmapIcon.EndInit();
                Icon.Source = BitmapIcon;
            }
        }

        private void RecNotif_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RaiseEvent(new ClickEventArgs(NotifClickEvent, this, M, SenderName.Text, FileName.Text));
        }
    }
}
