using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HttpClientTimeout
{
    class Program
    {
        private const int _readBufferSize = 16 * 1024;

        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            while (true)
            {
                try
                {
                    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost:7777");
                    httpRequestMessage.Headers.Add("Host", "localhost:5000");

                    Log("Sending request...");
                    var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);

                    Log($"StatusCode: {response.StatusCode}");

                    Log("Getting content stream...");
                    var contentStream = await response.Content.ReadAsStreamAsync();

                    Log("Reading response stream...");
                    var buffer = new byte[_readBufferSize];
                    var count = 0;
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        count += bytesRead;
                        Log(count);
                    }
                    Log("Finished reading response stream");
                }
                catch (Exception e)
                {
                    Log(e);
                    break;
                }
            }
        }

        private static void Log(object value)
        {
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] {value}");
        }
    }
}
