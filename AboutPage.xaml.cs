using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using Windows.System;
using Windows.UI.Xaml;

namespace SystemInfoViewer
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        private async void GithubLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var hyperlink = sender as Hyperlink;
                if (hyperlink?.NavigateUri != null)
                {
                    await Launcher.LaunchUriAsync(hyperlink.NavigateUri);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to launch URI: {ex.Message}");
            }
        }
    }
}