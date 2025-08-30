using AWS.Cryptography.EncryptionSDK;
using AWS.Cryptography.MaterialProviders;
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

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Instantiate the AWS Encryption SDK and material providers
            var esdk = new ESDK(new AwsEncryptionSdkConfig());
            var mpl = new MaterialProviders(new MaterialProvidersConfig());
        }
    }
}