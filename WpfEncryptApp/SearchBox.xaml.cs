using System.Windows;
using System.Windows.Controls;

namespace WpfEncryptApp
{
    //The text box that the user is searching for other users in
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
    }
}