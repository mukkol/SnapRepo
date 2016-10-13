using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using SnapRepo.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SnapRepo.Azure
{
    public class BlobStorageService
    {
        public string SendBackupPackage(ManagerSettings settings, string backupFileName)
        {
            CloudStorageAccount account = GetCloudStorageAccount(settings);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(settings.ContainerName);
            container.CreateIfNotExists();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(backupFileName);
            using (var fileStream = File.OpenRead($"{settings.LocalRepositoryPath}{backupFileName}"))
            {
                blockBlob.UploadFromStream(fileStream);
            }
            return blockBlob.Uri.ToString();
        }


        public string DownloadPackage(ManagerSettings settings, string fileName)
        {
            string downloadPath = settings.LocalRepositoryPath + fileName.Replace("/", "");
            CloudStorageAccount account = GetCloudStorageAccount(settings);
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
            CloudStorageAccount account = GetCloudStorageAccount(settings);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(settings.ContainerName);
            container.CreateIfNotExists();
            var blobQuery = container.ListBlobs(null, recursive).OfType<ICloudBlob>().OrderByDescending(b => b.Properties.LastModified);
            return blobQuery ?? Enumerable.Empty<ICloudBlob>();
        }

        public static string GetBlobStorageUri(string blobStorageConnectionString)
        {
            CloudStorageAccount account = GetCloudStorageAccount(blobStorageConnectionString);
            return account?.BlobStorageUri?.PrimaryUri.ToString();
        }

        public static CloudStorageAccount GetCloudStorageAccount(ManagerSettings settings)
        {
            return GetCloudStorageAccount(settings.BlobStorageConnectionString);
        }
        public static CloudStorageAccount GetCloudStorageAccount(string blobStorageConnectionString)
        {
            return CloudStorageAccount.Parse(blobStorageConnectionString);
        }

        public static string BlobStorageConnectionString => ConfigurationManager.ConnectionStrings[SettingsFactory.AzureBlobStorageConnectionStringName]?.ConnectionString;

    }
}