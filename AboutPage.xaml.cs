using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SystemInfoViewer
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
            LoadLatestVersion();
        }

        private async void LoadLatestVersion()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SystemInfoViewer");
                    var response = await client.GetAsync("https://api.github.com/repos/xrlzu/SystemInfoViewer/releases");
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        var releases = doc.RootElement.EnumerateArray();
                        if (releases.MoveNext())
                        {
                            var latestRelease = releases.Current;
                            var version = latestRelease.GetProperty("tag_name").GetString();
                            LatestversionRun.Text = version;
                        }
                        else
                        {
                            LatestversionRun.Text = "无发布版本";
                        }
                    }
                }
            }
            catch (Exception)
            {
                LatestversionRun.Text = "获取失败";
            }
        }
    }
}