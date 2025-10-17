using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
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
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Win32;
using System.IO;
using AWS.Cryptography.EncryptionSDK;
using AWS.Cryptography.MaterialProviders;
using System.Reflection;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        public static string filename;
        public static string Output;
        public string UsableName;
        public System.IO.MemoryStream Message;
        public static bool ToOrFrom = false; //False: Message is sent to current user by another user. True: Message is sent by current user to another user.
        public static bool DarkLight; //True: Dark Mode. False: Light Mode.
        public Home()
        {
            InitializeComponent();
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";    //Database credentials. If this were meant to be put into production,
                                                                                                    //a secure password would be set up along with other security measures.
            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);   //setting up the connection
            connection.Open();  //opening the connection
            //Grab user data from database using public string Userid
            string query = "SELECT FirstName, LastName FROM users WHERE UserID = @Userid";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Userid", LoginPage.Userid);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Output = reader["FirstName"] + " " + reader["LastName"];
                }
                reader.Close();
            }
            Welcome.Text = "Welcome, " + Output + ".";
            string query2 = "SELECT Theme FROM accountinfo WHERE AccID = @Userid";
            using (MySqlCommand command = new MySqlCommand(query2, connection))
            {
                command.Parameters.AddWithValue("@Userid", LoginPage.Userid);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(0) == "Dark")
                    {
                        DarkLight = true;
                    }
                    else if (reader.GetString(0) == "Light")
                    {
                        DarkLight = false;
                    }
                    else
                    {
                        DarkLight = true;
                    }
                }
                reader.Close();
            }
            ChangeAppearance();
            DisplayRecievedFiledata();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".docx"; // Default file extension
            dialog.Filter = "Word documents (.docx)|*.docx|Excel spreadsheets (.xlsx)|*.xlsx|PDF documents|*.pdf|Text documents (.txt)|*.txt"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                filename = dialog.FileName; //The full file path

                UsableName = dialog.SafeFileName;

                //Use file path to open separate window/dialogue box to view file and select recipient
                FileSendDisplay Win = new FileSendDisplay();
                bool? Send = Win.ShowDialog();

                if (Send == true)
                {
                    // Instantiate the AWS Encryption SDK and material providers
                    var esdk = new ESDK(new AwsEncryptionSdkConfig());
                    var mpl = new MaterialProviders(new MaterialProvidersConfig());

                    var keyNameSpace = "KeyNS01";   //These values are important identifiers for the encryption key. These same values must be used to generate a corresponding key for decryption.
                    var keyName = "Key01";

                    //If in production, a key management service like AWS KMS should be used to generate and store keys.
                    //Unfortunately, I am broke and cannot aford AWS KMS. So I will be generating the public and private keys in the program to prototype the application.

                    string publickeypath = "C:\\Users\\Rhian\\OneDrive\\Desktop\\GitRepository\\CapstoneProject2025\\WpfEncryptApp\\public_key.pem";
                    byte[] rawpublickey = System.IO.File.ReadAllBytes(publickeypath);   //getting data from private key file


                    var encryptKeyringInput = new CreateRawRsaKeyringInput
                    {
                        KeyNamespace = keyNameSpace,
                        KeyName = keyName,
                        PublicKey = new MemoryStream(rawpublickey),
                        PaddingScheme = PaddingScheme.OAEP_SHA384_MGF1
                    };

                    var ekeyring = mpl.CreateRawRsaKeyring(encryptKeyringInput);

                    //Define the encryption context
                    //The AWS website did not say exactly what this does, but I'm assuming it's only for documentation purposes.
                    var encryptionContext = new Dictionary<string, string>()
                    {
                        {"purpose", "Send Message"}
                    };
                    byte[] Transfer = Encoding.UTF8.GetBytes(FileSendDisplay.Data);    //Transfering string data into a byte array to be put into a MemoryStream to be accepted by the function. It hopefully works.
                    MemoryStream message = new(Transfer);                               //This solution is brought to you by the lovely users at stackoverflow.
                    
                    //Define the encrypt input object
                    var encryptInput = new EncryptInput
                    {
                        Plaintext = message,
                        Keyring = ekeyring,
                        EncryptionContext = encryptionContext
                    };

                    //Calling the Encrypt function
                    var encryptOutput = esdk.Encrypt(encryptInput);

                    var encryptedMessage = encryptOutput.Ciphertext;    //The final encrypted message for storage, transfer, and decryption

                    //Converting the MemoryStream to a byte array in order to hopefully prevent the data from becoming malformed
                    byte[] encryptedBytes = encryptedMessage.ToArray();

                    //send to recipient/add to DB
                    string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";
                    MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
                    connection.Open();
                    string insert = "INSERT INTO files (IDNum, RecID, Message, SendID, FileName) VALUES (NULL, @rec, @msg, @send, @fn)";
                    string rec = FileSendDisplay.uID;
                    var msg = encryptedBytes;
                    string send = LoginPage.Userid;

                    try 
                    {
                        using (MySqlCommand command = new MySqlCommand(insert, connection))
                        {
                            command.Parameters.AddWithValue("@rec", rec);
                            command.Parameters.AddWithValue("@msg", msg);
                            command.Parameters.AddWithValue("@send", send);
                            command.Parameters.AddWithValue("@fn", UsableName);
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                    connection.Close();
                }
            }
        }
        
        //Display recieved data from files table
        private void DisplayRecievedFiledata()
        {
            //recieve data from files table that has RecID the same as Login UID
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";
            MySql.Data.MySqlClient.MySqlConnection connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            connection.Open();
            string query = "SELECT Message, SendID, FileName FROM files WHERE RecID = @LoginID";
            string LoginID = LoginPage.Userid;
            ToOrFrom = false;

            try
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LoginID", LoginID);
                    string SendID = "";
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        SendID = reader["SendID"].ToString();
                        byte[] encryptedBytes = (byte[])reader["Message"];
                        UsableName = reader["FileName"].ToString();

                        //Retrieving data directly from array to avoid corruption
                        MemoryStream memstream = new MemoryStream(encryptedBytes);
                        memstream.Position = 0;
                        Message = memstream;
                        RecNotif newRecNotif = new RecNotif(UsableName, SendID, Message);
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

        private void O_Click(object sender, RoutedEventArgs e)
        {
            Options optionspage = new Options();
            NavigationService.Navigate(optionspage);
        }

        private void S_Click(object sender, RoutedEventArgs e)
        {
            ViewSentFiles viewsent = new ViewSentFiles();
            NavigationService.Navigate(viewsent);
        }

        private void ChangeAppearance()
        {
            if (DarkLight == true)
            {
                HomeGrid.Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#FF3C3B3B"));
                Welcome.Foreground = new SolidColorBrush(Colors.White);
                NewF.Foreground = new SolidColorBrush(Colors.White);
                Add.Foreground = new SolidColorBrush(Colors.White);
                Add.BorderBrush = new SolidColorBrush(Colors.White);
                Add.Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#FF3C3B3B"));
                Notice.Foreground = new SolidColorBrush(Colors.White);
                Inbox.Foreground = new SolidColorBrush(Colors.White);
                Labels.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                HomeGrid.Background = new SolidColorBrush(Colors.White);
                Welcome.Foreground = new SolidColorBrush (Colors.Black);
                NewF.Foreground = new SolidColorBrush(Colors.Black);
                Add.Foreground = new SolidColorBrush(Colors.Black);
                Add.BorderBrush = new SolidColorBrush(Colors.Black);
                Add.Background = new SolidColorBrush(Colors.White);
                Notice.Foreground = new SolidColorBrush(Colors.Black);
                Inbox.Foreground = new SolidColorBrush(Colors.Black);
                Labels.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
    }
}
