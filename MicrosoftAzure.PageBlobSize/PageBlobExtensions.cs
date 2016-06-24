using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Auth.Protocol;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage.Core.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace MicrosoftAzure.PageBlobSize
{
    public static class CloudPageBlobExtensions
    {
        /// <summary>
        /// Based on this script: http://gallery.technet.microsoft.com/scriptcenter/Get-Billable-Size-of-32175802
        /// </summary>
        /// <returns></returns>
        /// 


        /// <summary>
        /// Enumerates the page ranges of a page blob, sending one service call as needed for each
        /// <paramref name="rangeSize"/> bytes.
        /// </summary>
        /// <param name="pageBlob">The page blob to read.</param>
        /// <param name="rangeSize">The range, in bytes, that each service call will cover. This must be a multiple of
        ///     512 bytes.</param>
        /// <param name="options">The request options, optionally specifying a timeout for the requests.</param>
        /// <returns>An <see cref="IEnumerable"/> object that enumerates the page ranges.</returns>
        /// 
        public static IEnumerable<PageRange> GetPageRangesRewrite(
        this CloudPageBlob pageBlob,
        long rangeSize,
        string accountName,
        string accountKey,
        BlobRequestOptions options)
        {
            int timeout=270;


            if ((rangeSize % 512) != 0)
            {
                throw new ArgumentOutOfRangeException("rangeSize", "The range size must be a multiple of 512 bytes.");
            }

            long startOffset = 0;
            long blobSize;


            do
            {
                // Generate a web request for getting page ranges

                HttpWebRequest webRequest = BlobHttpWebRequestFactory.GetPageRanges(
                    pageBlob.Uri,
                    timeout,
                    pageBlob.SnapshotTime,
                    null,
                    null,
                    null,
                    null
                    );

                // Specify a range of bytes to search
                webRequest.Headers["x-ms-range"] = string.Format(
                    "bytes={0}-{1}",
                    startOffset,
                    startOffset + rangeSize - 1);

                // Sign the request
                var can = SharedKeyCanonicalizer.Instance;
                var creds = new StorageCredentials(accountName,accountKey);
                var skh = new SharedKeyAuthenticationHandler(can, creds, creds.AccountName);

                skh.SignRequest(webRequest, new OperationContext());


                List<PageRange> pageRanges;

                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    // Refresh the size of the blob
                    blobSize = long.Parse(webResponse.Headers["x-ms-blob-content-length"]);

                    GetPageRangesResponse getPageRangesResponse = new GetPageRangesResponse(webResponse.GetResponseStream());

                    // Materialize response so we can close the webResponse
                    pageRanges = getPageRangesResponse.PageRanges.ToList();
                }

                // Lazily return each page range in this result segment.
                foreach (PageRange range in pageRanges)
                {
                    yield return range;
                }

                startOffset += rangeSize;
            }
            while (startOffset < blobSize);
        }





        public static long GetActualPageBlobSize(this CloudPageBlob pageBlob,string accountName,string accountKey)
        {
            {
                pageBlob.FetchAttributes();
                return 124 + pageBlob.Name.Length * 2 + pageBlob.Metadata.Sum(m => m.Key.Length + m.Value.Length + 3) + pageBlob.GetPageRangesRewrite(500 * 1024 * 1024,accountName,accountKey,null).Sum(r => 12 + (r.EndOffset - r.StartOffset));
            }
        }

        // public static long GetActualBlockBlobSize(this CloudBlockBlob blockBlob)
        // {
        //    blockBlob.FetchAttributes();
        //    return 124 + blockBlob.Name.Length * 2 + blockBlob.Metadata.Sum(m => m.Key.Length + m.Value.Length + 3) + blockBlob.DownloadBlockList().Sum(b => b.Length + b.Name.Length);
        // }

        [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
        public static extern long StrFormatByteSize(long fileSize, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer, int bufferSize);

        public static string GetFormattedPageBlobSize(long size)
        {
            var sb = new StringBuilder(11);
            StrFormatByteSize(size, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}