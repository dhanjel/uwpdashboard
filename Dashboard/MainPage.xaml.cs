using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dashboard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private WebServer server;

        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        public MainPage()
        {
            this.InitializeComponent();

            var dashboardUrl = GetDashboardUrl();

            server = new WebServer(dashboardUrl);
            server.OnUrlUpdated += Server_OnUrlUpdated;

            webView.Source = new Uri(dashboardUrl);
        }

        private async void Server_OnUrlUpdated(string url)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                webView.Source = new Uri(url);
            });
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await server.Start();

        }

        private void SaveDashboardUrl(string url)
        {
            localSettings.Values["dashboardUrl"] = url;
        }

        private string GetDashboardUrl()
        {
            var url = localSettings.Values["dashboardUrl"];
            if (url != null)
                return (string)url;

            return $"http://{WebServer.GetIPAddress()}:8081/?config";
        }
    }
}
