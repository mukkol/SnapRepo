using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using AzureBackupManager.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureBackupManager.Azure
{
    public class BlobStorageService
    {
        public const string AzureBlobStorageConnectionStringName = "AzureBackupBlobStorage";
        private readonly ManagerSettings _settings;

        public BlobStorageService(ManagerSettings psettings)
        {
            _settings = psettings;
        }

        public string SendBackupPackage(string backupFileName)
        {

            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings[AzureBlobStorageConnectionStringName].ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(_settings.ContainerName);
            container.CreateIfNotExists();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(backupFileName);
            using (var fileStream = File.OpenRead($"{_settings.LocalFolderPath}{backupFileName}"))
            {
                blockBlob.UploadFromStream(fileStream);
            }
            return blockBlob.Uri.ToString();
        }

        public string DownloadPackage(string fileName)
        {
            string downloadPath = _settings.LocalFolderPath + fileName.Replace("/", "");
            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings[AzureBlobStorageConnectionStringName].ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(_settings.ContainerName);
            CloudBlob blob = container.GetBlobReference(fileName);
            blob.DownloadToFile(downloadPath, FileMode.CreateNew);
            return downloadPath;
        }
        
        public string[] CleanAzure(string backupInfix, int daysOld, bool simulate = false)
        {
            DateTime minAge = DateTime.UtcNow.AddDays(0 - daysOld);
            var deletedItems = GetListOfBlobStorageItems()
                .Where(b =>
                    b.Name.Contains(backupInfix) &&
                    b.Properties.LastModified < minAge)
                .ToArray();
            if (!simulate)
            {
                foreach (var b in deletedItems)
                {
                    b.Delete(DeleteSnapshotsOption.IncludeSnapshots);
                }
            }
            return deletedItems.Select(b => b.Name).ToArray();
        }

        public IEnumerable<ICloudBlob> GetListOfBlobStorageItems(bool recursive = true)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings[AzureBlobStorageConnectionStringName].ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(_settings.ContainerName);
            container.CreateIfNotExists();
            var blobQuery = container.ListBlobs(null, recursive).OfType<ICloudBlob>() ?? Enumerable.Empty<ICloudBlob>();
            return blobQuery;
        }
    }
}