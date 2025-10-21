using System.Windows;

namespace WpfEncryptApp
{
    //The main window of the app, holds the frames of the app
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoginFrame.Navigate(new LoginPage());
            DataContext = this;
        }
    }
}