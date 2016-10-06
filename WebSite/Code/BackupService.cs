using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Ionic.Zip;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Web.Administration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureBackupManager.Code
{
    public class BackupService
    {
        public const string BackupManagerConnectionStringName = "BackupManager";
        public const string AzureBlobStorageConnectionStringName = "AzureBackupBlobStorage";
        private readonly ManagerSettings _settings;

        public BackupService(ManagerSettings psettings)
        {
            _settings = psettings;
        }
        public static ManagerSettings CreateSettingsFromParamsOrDefault(NameValueCollection requestParams = null)
        {
            if(requestParams == null) requestParams = new NameValueCollection();
            var connectionString = ConfigurationManager.ConnectionStrings[BackupManagerConnectionStringName]?.ConnectionString;
            var databaseName = requestParams["databaseName"]
                               ?? ConfigurationManager.AppSettings["BackupManager.DatabaseName"]
                               ?? new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            return new ManagerSettings()
            {
                LocalFolderPath = requestParams["localFolderPath"]
                                ?? ConfigurationManager.AppSettings["BackupManager.LocalFolderPath"]
                                ?? (new DirectoryInfo(HttpContext.Current.Server.MapPath("~")).Parent?.Parent?.FullName ?? "C:\\temp") + "\\BackupManagerRepository\\",
                AppDataFolder = requestParams["appDataFolder"]
                                ?? ConfigurationManager.AppSettings["BackupManager.AppDataFolder"]
                                ?? TryGetEpiserverAppDataPath()
                                ?? "C:\\Path\\To\\AppData\\Folder",
                ContainerName = requestParams["containerName"]
                                ?? ConfigurationManager.AppSettings["BackupManager.ContainerName"]
                                ?? "backup-manager-repository",
                DatabaseOwner = requestParams["databaseOwner"]
                                ?? ConfigurationManager.AppSettings["BackupManager.DatabaseOwner"]
                                ?? GetDbOwner(databaseName, connectionString)
                                ?? "DbOwner",
                DatabaseServerName = requestParams["databaseServerName"]
                                ?? ConfigurationManager.AppSettings["BackupManager.DatabaseServerName"]
                                ?? new SqlConnectionStringBuilder(connectionString).DataSource,
                DatabaseName = databaseName,
                DbConnectionString = connectionString,
                DbExists = DatabaseExists(databaseName, connectionString),
            };
        }

        public string BackupLocal(string backupInfix)
        {
            string infoMessage;
            string dbBackupFileName = BackupDb(out infoMessage);
            string zipFileName = ZipAppDataFolder(_settings);
            string backupFileName = CreateBackupPackage(dbBackupFileName, zipFileName, backupInfix);
            File.Delete(_settings.LocalFolderPath + dbBackupFileName);
            File.Delete(_settings.LocalFolderPath + zipFileName);
            return backupFileName;
        }

        public string BackupAzure(string backupInfix)
        {
            var backupFileName = BackupLocal(backupInfix);
            var uri = SendBackupPackage(backupFileName);
            File.Delete(_settings.LocalFolderPath + backupFileName);
            return uri;
        }

        public void RestoreLocal(string packateZipFile, bool iisreset = true)
        {
            string backupFolderPath = ExtractPackage(_settings.LocalFolderPath, packateZipFile);
            var files = Directory.GetFiles(backupFolderPath).Select(s => s?.Replace(_settings.LocalFolderPath, "")).ToList();
            string databaseFile = files.FirstOrDefault(f => f.EndsWith(".bak"));
            string appDataZipFile = files.FirstOrDefault(f => f.EndsWith(".zip"));
            RestoreDb(databaseFile);
            RestoreAppData(_settings.LocalFolderPath, appDataZipFile, _settings.AppDataFolder);
            SetDbOwner(_settings);
            Directory.Delete(backupFolderPath, true);

            if (iisreset)
            {
                using (ServerManager iisManager = new ServerManager())
                {
                    SiteCollection sites = iisManager.Sites;
                    foreach (Site site in sites)
                    {
                        if (site.Name == HostingEnvironment.ApplicationHost.GetSiteName())
                        {
                            iisManager.ApplicationPools[site.Applications["/"].ApplicationPoolName].Recycle();
                            break;
                        }
                    }
                }
            }
        }

        public string BackupDb(out string infoMessage)
        {
            //TODO: new database needs to relocate the files (.mdf and .lgf files)
            var backupFileName = $"{_settings.DatabaseName}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Environment.MachineName}_db.bak";
            string info = "";
            using (var connection = new SqlConnection(_settings.DbConnectionString))
            {
                connection.InfoMessage += (sender, args) => info += args.Message + Environment.NewLine;
                var query = $"BACKUP DATABASE {_settings.DatabaseName} TO DISK='{_settings.LocalFolderPath}{backupFileName}' WITH COPY_ONLY";

                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            infoMessage = info;
            return backupFileName;
        }

        public string ZipAppDataFolder(ManagerSettings settings)
        {
            string fileName = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Environment.MachineName}_AppData.zip";
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(settings.AppDataFolder);
                zip.Save(settings.LocalFolderPath + fileName);
            }
            return fileName;
        }

        public string CreateBackupPackage(string dbBackupFileName, string appDataZipFileName, string backupInfix)
        {
            var infix = string.IsNullOrEmpty(backupInfix) ? "_" : "_" + backupInfix.Replace(" ", "") + "_";
            string zipFileNamePrefix = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}{infix}{Environment.MachineName}_package";
            string zipFileName = $"{zipFileNamePrefix}.zip";
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile($"{_settings.LocalFolderPath}{dbBackupFileName}", zipFileNamePrefix);
                zip.AddFile($"{_settings.LocalFolderPath}{appDataZipFileName}", zipFileNamePrefix);
                zip.Save(_settings.LocalFolderPath + zipFileName);
            }
            return zipFileName;
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

        public string ExtractPackage(string localFolderPath, string packageFileName)
        {
            string backupFilePath = localFolderPath + packageFileName;
            using (ZipFile zip = new ZipFile(backupFilePath))
            {
                zip.ExtractAll(localFolderPath);
            }
            string backupFolderPath = backupFilePath.Replace(".zip", "");
            return backupFolderPath;
        }

        public string RestoreAppData(string localFolderPath, string appDataZipFile, string appDataFolder, bool createBackup = false)
        {
            string appDataZipFilePath = localFolderPath + appDataZipFile;
            bool exists = Directory.Exists(appDataFolder);
            if (createBackup && exists)
                Directory.Move(appDataFolder, appDataFolder + "_backup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            else
                Directory.Delete(appDataFolder);
            Directory.CreateDirectory(appDataFolder);
            using (ZipFile zip = new ZipFile(appDataZipFilePath))
            {
                zip.ExtractAll(appDataFolder);
            }
            return appDataFolder;
        }

        public string RestoreDb(string backupFileName)
        {
            string backupFilePath = _settings.LocalFolderPath + backupFileName;
            string infoMessage = "";
            using (var connection = new SqlConnection(_settings.DbConnectionString))
            {
                connection.InfoMessage += (sender, args) => infoMessage += args.Message + Environment.NewLine;
                connection.Open();
                bool exists = (int)(new SqlCommand($"SELECT count(*) FROM master.dbo.sysdatabases where name = '{_settings.DatabaseName}'", connection).ExecuteScalar()) > 0;
                //kick all users out (alias close connections) before restore.
                if (exists) { new SqlCommand($"ALTER DATABASE {_settings.DatabaseName} SET Single_User WITH Rollback Immediate", connection).ExecuteNonQuery(); }
                try
                {
                    new SqlCommand($"RESTORE DATABASE {_settings.DatabaseName} FROM DISK='{backupFilePath}'", connection).ExecuteNonQuery();
                }
                finally
                {
                    if (exists) { new SqlCommand($"ALTER DATABASE {_settings.DatabaseName} SET Multi_User", connection).ExecuteNonQuery(); }
                }
                connection.Close();
            }
            return infoMessage;
        }

        public static void SetDbOwner(ManagerSettings settings)
        {
            if (string.IsNullOrEmpty(settings.DatabaseOwner))
                return;
            Server server = new Server(new ServerConnection(new SqlConnection(settings.DbConnectionString)));
            Database database = server.Databases[settings.DatabaseName];
            database.SetOwner(settings.DatabaseOwner, true);
            database.Refresh();
        }

        public static bool DatabaseExists(string databaseName, string dbConnectionString)
        {
            try
            {
                Server server = new Server(new ServerConnection(new SqlConnection(dbConnectionString)));
                return server?.Databases.Contains(databaseName) ?? false;
            }
            catch (ConnectionFailureException) { return false; }
        }

        public static string GetDbOwner(string databaseName, string dbConnectionString)
        {
            try
            {
                Server server = new Server(new ServerConnection(new SqlConnection(dbConnectionString)));
                Database database = server?.Databases[databaseName];
                return database?.Owner;
            }
            catch (ConnectionFailureException) { return null; }
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

        /// <summary>
        /// Tries to find episerver configuration but if it's nor referenced it will return null;
        /// </summary>
        public static string TryGetEpiserverAppDataPath()
        {
            Type type = Type.GetType("EPiServer.Framework.Configuration.EPiServerFrameworkSection, EPiServer.Framework");
            Type appDataType = Type.GetType("EPiServer.Framework.Configuration.AppDataElement, EPiServer.Framework");
            var instance = type?.GetProperty("Instance").GetValue(null);
            var appDataInstance = type?.GetProperty("AppData")?.GetValue(instance);
            return appDataType?.GetProperty("BasePath")?.GetValue(appDataInstance) as string;
            //return EPiServer.Framework.Configuration.EPiServerFrameworkSection.Instance.AppData.BasePath;
        }
    }
}