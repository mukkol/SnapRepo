using System;
using System.IO;
using System.Linq;
using AzureBackupManager.Azure;
using AzureBackupManager.Common;
using Ionic.Zip;
using Microsoft.Web.Administration;

namespace AzureBackupManager.Backups
{
    public class BackupService
    {
        private readonly BlobStorageService _blobStorageService;
        private readonly DbBackupService _dbBackupService;
        private readonly LogService _logService;

        public BackupService(BlobStorageService blobStorageService, DbBackupService dbBackupService, LogService logService)
        {
            _blobStorageService = blobStorageService;
            _dbBackupService = dbBackupService;
            _logService = logService;
        }

        public string BackupLocal(ManagerSettings settings, string backupInfix)
        {
            string dbBackupFileName = _dbBackupService.BackupDbToLocalRepository(settings);
            string zipFileName = ZipAppDataFolder(settings.LocalRepositoryPath, settings.AppDataFolder);
            string backupFileName = CreateBackupPackage(settings.LocalRepositoryPath, dbBackupFileName, zipFileName, backupInfix);
            File.Delete(settings.LocalRepositoryPath + dbBackupFileName);
            File.Delete(settings.LocalRepositoryPath + zipFileName);
            return backupFileName;
        }

        public string BackupAzure(ManagerSettings settings, string backupInfix)
        {
            var backupFileName = BackupLocal(settings, backupInfix);
            var uri = _blobStorageService.SendBackupPackage(settings, backupFileName);
            File.Delete(settings.LocalRepositoryPath + backupFileName);
            return uri;
        }

        public void RestoreLocal(ManagerSettings settings, string packateZipFile, bool iisreset = true, string siteName = null)
        {
            string backupFolderPath = ExtractPackage(settings.LocalRepositoryPath, packateZipFile);
            var files = Directory.GetFiles(backupFolderPath).Select(s => s?.Replace(backupFolderPath, "")).ToList();
            string databaseFile = files.FirstOrDefault(f => f.EndsWith(".bak"));
            string appDataZipFile = files.FirstOrDefault(f => f.EndsWith(".zip"));
            string packageFolderName = backupFolderPath.Replace(settings.LocalRepositoryPath, "");
            _logService.WriteLog($"Restoring with settins: localRepositoryPath={settings.LocalRepositoryPath}, dbSharedBackupFolder={settings.DbSharedBackupFolder}, packageFolderName={packageFolderName}, databaseFile={databaseFile}, appDataZipFile={appDataZipFile}");
            _dbBackupService.RestoreDbBackupToSqlServer(settings, settings.DatabaseName, packageFolderName, databaseFile);
            RestoreAppData(settings.LocalRepositoryPath, packageFolderName, appDataZipFile, settings.AppDataFolder);
            _dbBackupService.SetDbOwner(settings, settings.DatabaseName);
            _logService.WriteLog($"Set DB ({settings.DatabaseName}) Owner ({settings.DatabaseOwner})");
            Directory.Delete(backupFolderPath, true);

            if (iisreset && !string.IsNullOrEmpty(siteName))
            {
                _logService.WriteLog($"Trying to recycle IIS site ({siteName}).");
                using (ServerManager iisManager = new ServerManager())
                {
                    var site = iisManager.Sites.FirstOrDefault(s => s.Name == siteName);
                    if (site != null)
                    {
                        var appPool = iisManager.ApplicationPools[site.Applications["/"].ApplicationPoolName];
                        _logService.WriteLog($"Recycling Application Pool ({appPool.Name}).");
                        appPool.Recycle();
                    }
                }
            }
        }

        private static string ZipAppDataFolder(string localRepositoryPath, string appDataFolder)
        {
            string fileName = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Environment.MachineName}_AppData.zip";
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(appDataFolder);
                zip.Save(localRepositoryPath + fileName);
            }
            return fileName;
        }

        private static string CreateBackupPackage(string localRepositoryPath, string dbBackupFileName, string appDataZipFileName, string backupInfix)
        {
            var infix = string.IsNullOrEmpty(backupInfix) ? "_" : "_" + backupInfix.Replace(" ", "") + "_";
            string zipFileNamePrefix = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}{infix}{Environment.MachineName}_package";
            string zipFileName = $"{zipFileNamePrefix}.zip";
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile($"{localRepositoryPath}{dbBackupFileName}", zipFileNamePrefix);
                zip.AddFile($"{localRepositoryPath}{appDataZipFileName}", zipFileNamePrefix);
                zip.Save(localRepositoryPath + zipFileName);
            }
            return zipFileName;
        }

        private static string ExtractPackage(string localRepositoryPath, string packageFileName)
        {
            string backupFilePath = localRepositoryPath + packageFileName;
            string backupFolderPath = backupFilePath.Replace(".zip", "") + "\\";
            if (Directory.Exists(backupFolderPath))
            {
                Directory.Delete(backupFolderPath, true);
            }
            using (ZipFile zip = new ZipFile(backupFilePath))
            {
                zip.ExtractAll(localRepositoryPath);
            }
            return backupFolderPath;
        }

        private static void RestoreAppData(string localRepositoryPath, string packageFolderName, string appDataZipFile, string appDataFolder, bool createBackup = false)
        {
            string appDataZipFilePath = localRepositoryPath + packageFolderName + appDataZipFile;
            bool exists = Directory.Exists(appDataFolder);
            if (createBackup && exists)
                Directory.Move(appDataFolder, appDataFolder + "_backup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            else
                Directory.Delete(appDataFolder, true);
            Directory.CreateDirectory(appDataFolder);
            using (ZipFile zip = new ZipFile(appDataZipFilePath))
            {
                zip.ExtractAll(appDataFolder);
            }
        }
        
    }
}