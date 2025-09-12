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
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for SearchBox.xaml
    /// </summary>
    public partial class SearchBox : UserControl
    {
        public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register("SearchText", typeof(string), typeof(SearchBox), new PropertyMetadata(string.Empty));

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public static readonly RoutedEvent SearchButtonClickEvent =
        EventManager.RegisterRoutedEvent("SearchButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchBox));

        public event RoutedEventHandler SearchButtonClick
        {
            add { AddHandler(SearchButtonClickEvent, value); }
            remove { RemoveHandler(SearchButtonClickEvent, value); }
        }

        public SearchBox()
        {
            InitializeComponent();
        }

        public void ActiveSearch(object sender, EventArgs e)
        {
            
            RaiseEvent(new RoutedEventArgs(SearchButtonClickEvent));
        }
    }
}