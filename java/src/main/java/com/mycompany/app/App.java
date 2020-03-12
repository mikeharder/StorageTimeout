package com.mycompany.app;

import java.io.IOException;
import java.io.OutputStream;
import java.text.SimpleDateFormat;
import java.util.Date;

import com.azure.storage.blob.*;
import com.azure.storage.common.policy.RequestRetryOptions;

public class App {
    public static void main(String[] args) {
        String connectionString = System.getenv("STORAGE_CONNECTION_STRING");

        if (connectionString == null || connectionString.isEmpty()) {
            System.out.println("Environment variable STORAGE_CONNECTION_STRING must be set");
            System.exit(1);
        }

        BlobClientBuilder builder = new BlobClientBuilder()
            .connectionString(connectionString)
            .retryOptions(new RequestRetryOptions(null, 2, null, null, null, null))
            .httpClient(TestHttpClient.create("localhost", 7778))
            .containerName("testcontainer")
            .blobName("testblob");

        BlobClient blobClient = builder.buildClient();
        // BlobAsyncClient blobAsyncClient = builder.buildAsyncClient();

        class LoggingOutputStream extends OutputStream {
            private int _count;

            @Override
            public void write(int b) throws IOException {
                _count += 1;
                if (_count % 10000 == 0) {
                    Log(_count);
                }
            }
        }

        Log("Calling download(OutputStream)...");
        blobClient.download(new LoggingOutputStream());
        Log("Done");
    }

    private static void Log(Object value) {
        String timeStamp = new SimpleDateFormat("hh:mm:ss.SSS").format(new Date());
        System.out.println(String.format("[%s] %s", timeStamp, value));
    }
}