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
        public static DependencyProperty SearchTextProperty =
        DependencyProperty.Register("SearchText", typeof(string), typeof(SearchBox), new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public static readonly RoutedEvent SearchTextChangedEvent =
        EventManager.RegisterRoutedEvent("SearchTextChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchBox));

        public event RoutedEventHandler SearchTextChanged
        {
            add { AddHandler(SearchTextChangedEvent, value); }
            remove { RemoveHandler(SearchTextChangedEvent, value); }
        }

        public SearchBox()
        {
            InitializeComponent();
        }

        public static void OnSearchTextChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SearchBox searchBox = (SearchBox)d;
            searchBox.RaiseEvent(new RoutedEventArgs(SearchTextChangedEvent));
        }

        public void ActiveSearch(object sender, EventArgs e)
        {
            
            //RaiseEvent(new RoutedEventArgs(SearchButtonClickEvent));
        }
    }
}