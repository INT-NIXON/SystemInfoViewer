using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace SystemInfoViewer
{
    public sealed partial class AppMgmt : Page
    {
        public AppMgmt()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) => this.Focus(FocusState.Programmatic);
        }

        private void ToolsLink_Click(object sender, RoutedEventArgs e)
        {
            NavigateBackToToolsPage();
        }

        private void NavigateBackToToolsPage()
        {
            MainWindow.NavigationService.Instance.Navigate(typeof(ToolsPage));
        }
    }
}