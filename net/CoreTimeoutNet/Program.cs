using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreTimeoutNet
{
    class Program
    {
        private const int _readBufferSize = 16 * 1024;

        static async Task Main(string[] args)
        {
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            var httpClient = new HttpClient(httpClientHandler);
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var httpClientTransport = new HttpClientTransport(httpClient);

            var testOptions = new TestOptions
            {
                Transport = httpClientTransport,
                //  Retry = { MaxRetries = 0 }
            };

            var httpPipeline = HttpPipelineBuilder.Build(testOptions);

            while (true)
            {
                try
                {
                    Log("Creating retriable stream...");
                    var retriableStream = await RetriableStream.CreateAsync(
                        offset => SendTestRequest(httpPipeline, offset),
                        offset => SendTestRequestAsync(httpPipeline, offset),
                        new ResponseClassifier(), maxRetries: 3);

                    Log("Reading response stream...");
                    var buffer = new byte[_readBufferSize];
                    var count = 0;
                    int bytesRead;
                    while ((bytesRead = await retriableStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
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

            //while (true)
            //{
            //    try
            //    {
            //        var message = httpPipeline.CreateMessage();
            //        var request = message.Request;

            //        request.Uri.Reset(new Uri("http://localhost:7777"));
            //        request.Headers.Add("Host", "localhost:5000");
            //        message.BufferResponse = false;

            //        Log("Sending request...");
            //        await httpPipeline.SendAsync(message, CancellationToken.None);

            //        var response = message.Response;

            //        Log($"StatusCode: {response.Status}");

            //        Log("Reading response stream...");
            //        var buffer = new byte[_readBufferSize];
            //        var count = 0;
            //        int bytesRead;
            //        while ((bytesRead = await response.ContentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            //        {
            //            count += bytesRead;
            //            Log(count);
            //        }
            //        Log("Finished reading response stream");
            //    }
            //    catch (Exception e)
            //    {
            //        Log(e);
            //        break;
            //    }
            //}
        }

        private static void Log(object value)
        {
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] {value}");
        }

        private class TestOptions : ClientOptions { }

        private static Stream SendTestRequest(HttpPipeline pipeline, long offset)
        {
            using Request request = CreateRequest(pipeline, offset);

            Response response = pipeline.SendRequest(request, CancellationToken.None);
            return response.ContentStream;
        }

        private static async ValueTask<Stream> SendTestRequestAsync(HttpPipeline pipeline, long offset)
        {
            using Request request = CreateRequest(pipeline, offset);

            Response response = await pipeline.SendRequestAsync(request, CancellationToken.None);
            return response.ContentStream;
        }

        private static Request CreateRequest(HttpPipeline pipeline, long offset)
        {
            Request request = pipeline.CreateRequest();
            request.Method = RequestMethod.Get;
            request.Uri.Reset(new Uri("http://localhost:7777"));
            request.Headers.Add("Host", "localhost:5000");
            request.Headers.Add("Range", "bytes=" + offset);
            return request;
        }
    }
}
