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
            SenderName.Text = Sender;
            M = Msg;
            this.MouseLeftButtonUp += RecNotif_MouseLeftButtonUp;
        }

        private void RecNotif_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RaiseEvent(new ClickEventArgs(NotifClickEvent, this, M, SenderName.Text, FileName.Text));
        }
    }
}
