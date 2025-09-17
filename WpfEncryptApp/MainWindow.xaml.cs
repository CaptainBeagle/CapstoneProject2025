using AWS.Cryptography.EncryptionSDK;
using AWS.Cryptography.MaterialProviders;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //This code before public MainWindow() should allow MainWindow.xaml to read MyVariable in this file.
        private string _myVariable;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string MyVariable
        {
            get { return _myVariable; }
            set
            {
                if (_myVariable != value)
                {
                    _myVariable = value;
                    
                }
            }
        }
        //This code should extract text from files like docx, xlxs, etc.
        public static string ExtractTextFromDocx(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: The file was not found at path: {filePath}");
                return string.Empty;
            }

            try
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                {
                    var mainPart = wordDoc.MainDocumentPart;
                    if (mainPart == null || mainPart.Document == null || mainPart.Document.Body == null)
                    {
                        return string.Empty;
                    }

                    //The following code formats the displayed output to be the same as the original word document.
                    StringBuilder textBuilder = new StringBuilder();

                    // Iterate through each paragraph in the document's body
                    foreach (DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph in mainPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        // Append the text of the current paragraph
                        textBuilder.Append(paragraph.InnerText);

                        // Append a new line to separate paragraphs
                        textBuilder.AppendLine();
                    }

                    return textBuilder.ToString();
                }
            }
            catch (System.Exception ex)
            {
                //Console log to report the exception.
                Console.WriteLine($"An error occurred while processing the document: {ex.Message}");
                return string.Empty;
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            LoginFrame.Navigate(new LoginPage());
            DataContext = this;
            // Instantiate the AWS Encryption SDK and material providers
            var esdk = new ESDK(new AwsEncryptionSdkConfig());
            var mpl = new MaterialProviders(new MaterialProvidersConfig());

            var keyNameSpace = "KeyNS01";   //These values are important identifiers for the encryption key. These same values must be used to generate a corresponding key for decryption.
            var keyName = "Key01";

            //If in production, a key management service like AWS KMS would be used to generate and store keys.
            //Unfortunately, I am broke and cannot aford AWS KMS. So I will be generating the public and private keys in OpenSSL.

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
                {"purpose", "test"}
            };

            string docxFilePath = "C:\\Users\\Rhian\\Documents\\test.docx";
            string plaintext = ExtractTextFromDocx(docxFilePath);
            byte[] Transfer = Encoding.ASCII.GetBytes(plaintext);   //Transfering string data into a byte array to be put into a MemoryStream to be accepted by the function. It hopefully works.
            MemoryStream message = new(Transfer);                   //This solution is brought to you by the lovely users at stackoverflow.


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

            string privatekeypath = "C:\\Users\\Rhian\\OneDrive\\Desktop\\GitRepository\\CapstoneProject2025\\WpfEncryptApp\\private_key.pem";
            byte[] rawprivatekey = System.IO.File.ReadAllBytes(privatekeypath); //getting data from public key file

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
                Ciphertext = encryptedMessage,
                Keyring = dkeyring,
                EncryptionContext = encryptionContext
            };

            //calling the Decrypt function
            var decryptOutput = esdk.Decrypt(decryptInput);

            using (MemoryStream plaintextStream = (MemoryStream)decryptOutput.Plaintext)    //separating the decrypted message to be displayed in the MainWindow
            {
                byte[] plaintextBytes = plaintextStream.ToArray();
                string decryptedString = Encoding.UTF8.GetString(plaintextBytes);
                _myVariable = decryptedString;
            }
        }
    }
}