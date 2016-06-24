using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MicrosoftAzure.PageBlobSize
{
    class Program
    {
        private static CloudBlobClient client;

        static void Main(string[] args)
        {
            // Header.
            Console.WriteLine("");
            Console.WriteLine(" Microsoft Azure Page Blob Size v1.0.2 - AzPBSize - Tingli Lv");
            Console.WriteLine(" Usage: azpbsize.exe <accountName> <accountKey> <pageblob url|containerName> <AzureCloud|AzureChinaCloud>");
            Console.WriteLine("");

            // Validate args.
            if (args.Length != 4)
            {
                Console.WriteLine(" Invalid number of arguments.");
                Console.WriteLine("");
                return;
            }

            // Get the uri.
            var uri = args[2];
            Console.WriteLine(" Processing: {0}", uri);
            Console.WriteLine("");

            try
            {
                // Init client and blob list.
                var accountName = args[0];
                var accountKey = args[1];
                var environment = args[3];
                var endpointDomain = "core.chinacloudapi.cn";

                if (environment == "AzureChinaCloud")   client = new CloudStorageAccount(new StorageCredentials(args[0], args[1]),endpointDomain, false).CreateCloudBlobClient();
                
               
                else  client = new CloudStorageAccount(new StorageCredentials(args[0], args[1]), false).CreateCloudBlobClient();

                var isBlobUri = false;
                var blobs = new List<CloudPageBlob>();
                long totalSize = 0;

                // It's an uri.
                if (uri.StartsWith("http://") || uri.StartsWith("https://"))
                {
                    var blob = client.GetBlobReferenceFromServer(new Uri(uri)) as CloudPageBlob;
                    if (blob == null)
                        throw new FileNotFoundException("Unable to find the Page Blob.");
                    blobs.Add(blob);
                    isBlobUri = true;
                }
                else
                {
                    // It's a container.
                    var container = client.GetContainerReference(uri);
                    if (!container.Exists())
                        throw new InvalidOperationException("Container does not exist: " + uri);
                    blobs.AddRange(container.ListBlobs().OfType<CloudPageBlob>());
                }

                // Show blob sizes.
                foreach (var blob in blobs)
                {
                    if (!isBlobUri)
                        Console.WriteLine(" Blob: {0}", blob.Uri);

                    // Display length.
                    Console.WriteLine(" > Size: {0} ({1} bytes)", CloudPageBlobExtensions.GetFormattedPageBlobSize(blob.Properties.Length), blob.Properties.Length);

                    // Calculate size.
                    var size = blob.GetActualPageBlobSize(accountName,accountKey);
                    Console.WriteLine(" >>Actual/Billable Size: {0} ({1} bytes)", CloudPageBlobExtensions.GetFormattedPageBlobSize(size), size);
                    Console.WriteLine("");

                    totalSize = totalSize + size;
                }
                var totalCount = blobs.Count;
                // Show total page blob sizes.
                   Console.WriteLine(" >>Total Actual/Billable Page Blob Size for {2} Page Blob: {0} ({1} bytes)", CloudPageBlobExtensions.GetFormattedPageBlobSize(totalSize), totalSize,totalCount);
                   Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Error:");
                Console.WriteLine(" {0}", ex);
            }
        }
    }
}
