using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using AzureBackupManager.Azure;
using AzureBackupManager.Common;
using Ionic.Zip;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Web.Administration;

namespace AzureBackupManager.Backups
{
    public class BackupService
    {
        private readonly BlobStorageService _blobStorageService;
        private readonly LogService _logService;

        public BackupService(BlobStorageService blobStorageService, LogService logService)
        {
            _blobStorageService = blobStorageService;
            _logService = logService;
        }

        public string BackupLocal(ManagerSettings settings, string backupInfix)
        {
            string infoMessage;
            string dbBackupFileName = BackupDb(settings, settings.DatabaseName, out infoMessage);
            string zipFileName = ZipAppDataFolder(settings.LocalFolderPath, settings.AppDataFolder);
            string backupFileName = CreateBackupPackage(settings.LocalFolderPath, dbBackupFileName, zipFileName, backupInfix);
            File.Delete(settings.LocalFolderPath + dbBackupFileName);
            File.Delete(settings.LocalFolderPath + zipFileName);
            return backupFileName;
        }

        public string BackupAzure(ManagerSettings settings, string backupInfix)
        {
            var backupFileName = BackupLocal(settings, backupInfix);
            var uri = _blobStorageService.SendBackupPackage(settings, backupFileName);
            File.Delete(settings.LocalFolderPath + backupFileName);
            return uri;
        }

        public void RestoreLocal(ManagerSettings settings, string packateZipFile, bool iisreset = true, string siteName = null)
        {
            string backupFolderPath = ExtractPackage(settings.LocalFolderPath, packateZipFile);
            var files = Directory.GetFiles(backupFolderPath).Select(s => s?.Replace(settings.LocalFolderPath, "")).ToList();
            string databaseFile = files.FirstOrDefault(f => f.EndsWith(".bak"));
            string appDataZipFile = files.FirstOrDefault(f => f.EndsWith(".zip"));
            RestoreDb(settings, settings.DatabaseName, databaseFile);
            RestoreAppData(settings.LocalFolderPath, appDataZipFile, settings.AppDataFolder);
            SetDbOwner(settings, settings.DatabaseName);
            Directory.Delete(backupFolderPath, true);

            if (iisreset && !string.IsNullOrEmpty(siteName))
            {
                _logService.WriteLog($"Trying to recycle IIS site ({siteName}).");
                using (ServerManager iisManager = new ServerManager())
                {
                    foreach (Site site in iisManager.Sites)
                    {
                        if (site.Name == siteName)
                        {
                            var appPool = iisManager.ApplicationPools[site.Applications["/"]?.ApplicationPoolName];
                            _logService.WriteLog($"Recycling Application Pool ({appPool?.Name}).");
                            appPool?.Recycle();
                            break;
                        }
                    }
                }
            }
        }

        public string BackupDb(ManagerSettings settings, string dbName, out string infoMessage)
        {
            var backupFileName = $"{dbName}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Environment.MachineName}_db.bak";
            string info = "";
            using (var connection = new SqlConnection(settings.DbConnectionString))
            {
                connection.InfoMessage += (sender, args) => info += args.Message + Environment.NewLine;
                //TODO: SqlServer.SMO would be better tool for this
                var query = $"BACKUP DATABASE {dbName} TO DISK='{settings.LocalFolderPath}{backupFileName}' WITH COPY_ONLY";

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

        public static string ZipAppDataFolder(string localFolderPath, string appDataFolder)
        {
            string fileName = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Environment.MachineName}_AppData.zip";
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(appDataFolder);
                zip.Save(localFolderPath + fileName);
            }
            return fileName;
        }

        public static string CreateBackupPackage(string localFolderPath, string dbBackupFileName, string appDataZipFileName, string backupInfix)
        {
            var infix = string.IsNullOrEmpty(backupInfix) ? "_" : "_" + backupInfix.Replace(" ", "") + "_";
            string zipFileNamePrefix = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}{infix}{Environment.MachineName}_package";
            string zipFileName = $"{zipFileNamePrefix}.zip";
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile($"{localFolderPath}{dbBackupFileName}", zipFileNamePrefix);
                zip.AddFile($"{localFolderPath}{appDataZipFileName}", zipFileNamePrefix);
                zip.Save(localFolderPath + zipFileName);
            }
            return zipFileName;
        }

        public static string ExtractPackage(string localFolderPath, string packageFileName)
        {
            string backupFilePath = localFolderPath + packageFileName;
            using (ZipFile zip = new ZipFile(backupFilePath))
            {
                zip.ExtractAll(localFolderPath);
            }
            string backupFolderPath = backupFilePath.Replace(".zip", "");
            return backupFolderPath;
        }

        public static string RestoreAppData(string localFolderPath, string appDataZipFile, string appDataFolder, bool createBackup = false)
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

        public string RestoreDb(ManagerSettings settings, string dbName, string backupFileName)
        {
            //TODO: for new database needs to relocate the files (.mdf and .lgf files)
            string backupFilePath = settings.LocalFolderPath + backupFileName;
            string infoMessage = "";
            using (var connection = new SqlConnection(settings.DbConnectionString))
            {
                connection.InfoMessage += (sender, args) => infoMessage += args.Message + Environment.NewLine;
                connection.Open();
                //TODO: SqlServer.SMO would be better tool for this
                bool exists = (int)(new SqlCommand($"SELECT count(*) FROM master.dbo.sysdatabases where name = '{dbName}'", connection).ExecuteScalar()) > 0;
                //kick all users out (alias close connections) before restore.
                if (exists) { new SqlCommand($"ALTER DATABASE {dbName} SET Single_User WITH Rollback Immediate", connection).ExecuteNonQuery(); }
                try
                {
                    new SqlCommand($"RESTORE DATABASE {dbName} FROM DISK='{backupFilePath}'", connection).ExecuteNonQuery();
                }
                finally
                {
                    if (exists) { new SqlCommand($"ALTER DATABASE {dbName} SET Multi_User", connection).ExecuteNonQuery(); }
                }
                connection.Close();
            }
            return infoMessage;
        }

        public static void SetDbOwner(ManagerSettings settings, string dbName)
        {
            if (string.IsNullOrEmpty(settings.DatabaseOwner))
                return;
            Server server = new Server(new ServerConnection(new SqlConnection(settings.DbConnectionString)));
            Database database = server.Databases[dbName];
            database.SetOwner(settings.DatabaseOwner, true);
            database.Refresh();
        }
    }
}