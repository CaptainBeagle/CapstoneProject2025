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

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Page
    {
        public Options()
        {
            InitializeComponent();
        }

        private void H_Click(object sender, RoutedEventArgs e)
        {
            Home homepage = new Home();
            NavigationService.Navigate(homepage);
        }

        private void S_Click(object sender, RoutedEventArgs e)
        {
            ViewSentFiles viewsent = new ViewSentFiles();
            NavigationService.Navigate(viewsent);
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            LoginPage login = new LoginPage();
            NavigationService.Navigate(login);
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            //Switch state uppon clicking and update text on button as well as appearance of everything.
            //Maybe set a universal Static bool in Home that determines whether the theme is light or dark.
            //All pages access this variable to set the colors of their background and text.
            //Whenever clicked, update entry in AccountSetting table in DB corresponding to User ID.
            //Reload page or otherwise update appearance to be in line with new theme bool value.
        }
    }
}
