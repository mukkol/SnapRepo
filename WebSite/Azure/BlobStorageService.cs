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

        public string SendBackupPackage(ManagerSettings settings, string backupFileName)
        {

            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings[SettingsFactory.AzureBlobStorageConnectionStringName].ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(settings.ContainerName);
            container.CreateIfNotExists();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(backupFileName);
            using (var fileStream = File.OpenRead($"{settings.LocalFolderPath}{backupFileName}"))
            {
                blockBlob.UploadFromStream(fileStream);
            }
            return blockBlob.Uri.ToString();
        }

        public string DownloadPackage(ManagerSettings settings, string fileName)
        {
            string downloadPath = settings.LocalFolderPath + fileName.Replace("/", "");
            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings[SettingsFactory.AzureBlobStorageConnectionStringName].ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(settings.ContainerName);
            CloudBlob blob = container.GetBlobReference(fileName);
            blob.DownloadToFile(downloadPath, FileMode.CreateNew);
            return downloadPath;
        }
        
        public string[] CleanAzure(ManagerSettings settings, string backupInfix, int daysOld, bool simulate = false)
        {
            DateTime minAge = DateTime.UtcNow.AddDays(0 - daysOld);
            var deletedItems = GetListOfBlobStorageItems(settings)
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

        public IEnumerable<ICloudBlob> GetListOfBlobStorageItems(ManagerSettings settings, bool recursive = true)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings[SettingsFactory.AzureBlobStorageConnectionStringName].ConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(settings.ContainerName);
            container.CreateIfNotExists();
            var blobQuery = container.ListBlobs(null, recursive).OfType<ICloudBlob>() ?? Enumerable.Empty<ICloudBlob>();
            return blobQuery;
        }
    }
}