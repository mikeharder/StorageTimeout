﻿using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace StorageTimeoutNet
{
    class Program
    {
        private const int _blobSize = 10 * 1024 * 1024;
        private const int _readBufferSize = 16 * 1024;

        static async Task Main(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Undefined environment variable STORAGE_CONNECTION_STRING");
            }

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            var httpClient = new HttpClient(httpClientHandler);
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var httpClientTransport = new HttpClientTransport(httpClient);
            var changeUriTransport = new ChangeUriTransport(httpClientTransport, "localhost", 7778);

            var blobClientOptions = new BlobClientOptions
            {
                Transport = changeUriTransport,
                Retry = { MaxRetries = 1 },
            };

            var blobClient = new BlobClient(connectionString, "testcontainer", "testblob", blobClientOptions);

            if (args.Length > 0 && args[0] == "upload")
            {
                var randomBytes = new byte[_blobSize];
                (new Random(0)).NextBytes(randomBytes);

                Log("Calling UploadAsync() ...");
                var response = await blobClient.UploadAsync(new MemoryStream(randomBytes));
                Log("Received Response");

                Log($"LastModified: {response.Value.LastModified}");
            }
            else
            {
                Log("Calling DownloadAsync() ...");
                var response = await blobClient.DownloadAsync();
                Log("Received Response");

                Log($"ContentLength: {response.Value.ContentLength}");

                Log("Reading response stream...");
                var buffer = new byte[_readBufferSize];
                var count = 0;
                int bytesRead;
                while ((bytesRead = await response.Value.Content.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    count += bytesRead;
                    Log(count);
                }
                Log("Finished reading response stream");
            }
        }

        private static void Log(object value)
        {
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] {value}");
        }

        private class ChangeUriTransport : HttpPipelineTransport
        {
            private readonly HttpPipelineTransport _transport;
            private readonly string _host;
            private readonly int? _port;

            public ChangeUriTransport(HttpPipelineTransport transport, string host, int? port)
            {
                _transport = transport;
                _host = host;
                _port = port;
            }

            public override Request CreateRequest()
            {
                return _transport.CreateRequest();
            }

            public override void Process(HttpMessage message)
            {
                ChangeUri(message);
                _transport.Process(message);
            }

            public override ValueTask ProcessAsync(HttpMessage message)
            {
                ChangeUri(message);
                return _transport.ProcessAsync(message);
            }

            private void ChangeUri(HttpMessage message)
            {
                // Ensure Host header is only set once, since the same HttpMessage will be reused on retries
                if (!message.Request.Headers.Contains("Host"))
                {
                    message.Request.Headers.Add("Host", message.Request.Uri.Host);
                }

                message.Request.Uri.Host = _host;
                if (_port.HasValue)
                {
                    message.Request.Uri.Port = _port.Value;
                }
            }
        }
    }
}