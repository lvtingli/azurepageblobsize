MicrosoftAzure-PageBlobSize
====================

Calculate how much space a Microsfot Azure Page Blob/Disk is really using.


     Microsoft Azure Page Blob Size v1.0.2 - AzPBSize - Tingli Lv
     Usage: azpbsize.exe <accountName> <accountKey> <pageblob url|containerName> <AzureCloud|AzureChinaCloud>
     
     You can control the size of page range each service call will cover from GetPageRangesRewrite() to avoid timeout of Storage Front End server.
     
     Default is divided by 500MB：
     pageBlob.GetPageRangesRewrite(500 * 1024 * 1024,accountName,accountKey,null)
     
