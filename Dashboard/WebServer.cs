using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Reflection;
using Windows.ApplicationModel;
using System.Collections.Generic;
using System.Linq;

namespace Dashboard
{
    public class WebServer
    {
        private const uint BufferSize = 8192;

        public delegate void OnUrlUpdatedHandler(string url);
        public event OnUrlUpdatedHandler OnUrlUpdated;

        private string currentDashboardUrl;

        public WebServer(string dashboardUrl)
        {
            currentDashboardUrl = dashboardUrl;
        }

        public async Task Start()
        {
            var listener = new StreamSocketListener();

            await listener.BindServiceNameAsync("8081");

            listener.ConnectionReceived += async (sender, args) =>
            {
                var request = new StringBuilder();

                using (var input = args.Socket.InputStream)
                {
                    var data = new byte[BufferSize];
                    IBuffer buffer = data.AsBuffer();
                    var dataRead = BufferSize;

                    while (dataRead == BufferSize)
                    {
                        await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(
                                                      data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                string query = GetQuery(request);
                var html = Encoding.UTF8.GetBytes(await GetDefaultPage());

                if (query.StartsWith("?url"))
                {
                    currentDashboardUrl = query.Substring(query.IndexOf("=") + 1);
                    OnUrlUpdated?.Invoke(currentDashboardUrl);
                    html = Encoding.UTF8.GetBytes("OK");
                }
                else if (query.StartsWith("?config"))
                {
                    html = Encoding.UTF8.GetBytes(await GetInitialConfigPage());
                }

                using (var output = args.Socket.OutputStream)
                {
                    using (var response = output.AsStreamForWrite())
                    {                        

                        using (var bodyStream = new MemoryStream(html))
                        {
                            var header = $"HTTP/1.1 200 OK\r\nContent-Length: {bodyStream.Length}\r\nConnection: close\r\n\r\n";
                            var headerArray = Encoding.UTF8.GetBytes(header);
                            await response.WriteAsync(headerArray,
                                                      0, headerArray.Length);
                            await bodyStream.CopyToAsync(response);
                            await response.FlushAsync();
                        }
                    }
                }
            };
        }

        private async Task<string> GetDefaultPage()
        {
            var content = string.Empty;
            var file = await Package.Current.InstalledLocation.GetFileAsync("Templates\\Dashboard.html");
            using (var streamEntry = await file.OpenReadAsync())
            {
                using (var stream = streamEntry.AsStreamForRead())
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        content = await streamReader.ReadToEndAsync();
                    }
                }
            }

            content = content.Replace("{{DASHBOARD_URL}}", currentDashboardUrl);

            return content;
        }

        private async Task<string> GetInitialConfigPage()
        {
            var content = string.Empty;
            var file = await Package.Current.InstalledLocation.GetFileAsync("Templates\\InitialConfig.html");
            using (var streamEntry = await file.OpenReadAsync())
            {
                using (var stream = streamEntry.AsStreamForRead())
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        content = await streamReader.ReadToEndAsync();
                    }
                }
            }

            content = content.Replace("{{CONFIG_URL}}", $"http://{GetIPAddress()}:8081");

            return content;
        }

        public static IPAddress GetIPAddress()
        {
            List<string> IpAddress = new List<string>();
            var Hosts = Windows.Networking.Connectivity.NetworkInformation.GetHostNames().ToList();
            foreach (var Host in Hosts)
            {
                string IP = Host.DisplayName;
                IpAddress.Add(IP);
            }
            IPAddress address = IPAddress.Parse(IpAddress.Last());
            return address;
        }

        private static string GetQuery(StringBuilder request)
        {
            var requestLines = request.ToString().Split(' ');

            var url = requestLines.Length > 1
                              ? requestLines[1] : string.Empty;

            var uri = new Uri("http://localhost" + url);
            var query = uri.Query;
            return query;
        }

    }

}