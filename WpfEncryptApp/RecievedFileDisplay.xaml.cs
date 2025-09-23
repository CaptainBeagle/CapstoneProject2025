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
using AWS.Cryptography.EncryptionSDK;
using AWS.Cryptography.MaterialProviders;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for RecievedFileDisplay.xaml
    /// </summary>
    public partial class RecievedFileDisplay : Page
    {
        public RecievedFileDisplay(MemoryStream data, string sender, string title)
        {
            InitializeComponent();
            ChangeAppearance();
            FileTitle.Text = title;
            if (Home.ToOrFrom == false)
            {
                FileSender.Text = "From: " + sender;
            }
            else
            {
                FileSender.Text = "To: " + sender;
            }

            // Instantiate the AWS Encryption SDK and material providers
            var esdk = new ESDK(new AwsEncryptionSdkConfig());
            var mpl = new MaterialProviders(new MaterialProvidersConfig());

            var keyNameSpace = "KeyNS01";   //These values are important identifiers for the encryption key. These same values must be used to generate a corresponding key for decryption.
            var keyName = "Key01";

            string privatekeypath = "C:\\Users\\Rhian\\OneDrive\\Desktop\\GitRepository\\CapstoneProject2025\\WpfEncryptApp\\private_key.pem";
            byte[] rawprivatekey = System.IO.File.ReadAllBytes(privatekeypath); //getting data from private key file

            var encryptionContext = new Dictionary<string, string>()
            {
                {"purpose", "Send Message"}
            };

            var decryptKeyringInput = new CreateRawRsaKeyringInput
            {
                KeyNamespace = keyNameSpace,
                KeyName = keyName,
                PrivateKey = new MemoryStream(rawprivatekey),
                PaddingScheme = PaddingScheme.OAEP_SHA384_MGF1
            };

            var dkeyring = mpl.CreateRawRsaKeyring(decryptKeyringInput);

            //Define the decrypt input object
            var decryptInput = new DecryptInput
            {
                Ciphertext = data,
                Keyring = dkeyring,
                EncryptionContext = encryptionContext
            };

            //calling the Decrypt function
            try
            {
                var decryptOutput = esdk.Decrypt(decryptInput);
                using (MemoryStream plaintextStream = (MemoryStream)decryptOutput.Plaintext)    //separating the decrypted message to be displayed in the MainWindow
                {
                    byte[] plaintextBytes = plaintextStream.ToArray();
                    string decryptedString = Encoding.UTF8.GetString(plaintextBytes);
                    Content.Text = decryptedString;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
            
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if(Home.ToOrFrom == false)
            {
                Home home = new Home();
                NavigationService.Navigate(home);
            }
            else
            {
                ViewSentFiles viewsent = new ViewSentFiles();
                NavigationService.Navigate(viewsent);
            }
        }

        private void ChangeAppearance()
        {
            if (Home.DarkLight == true)
            {
                Display.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3C3B3B"));
                FileTitle.Foreground = new SolidColorBrush(Colors.White);
                FileSender.Foreground = new SolidColorBrush(Colors.White);
                Content.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                Display.Background = new SolidColorBrush(Colors.White);
                FileTitle.Foreground = new SolidColorBrush(Colors.Black);
                FileSender.Foreground = new SolidColorBrush(Colors.Black);
                Content.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
    }
}
