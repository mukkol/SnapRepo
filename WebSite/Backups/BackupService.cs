using System;
using System.IO;
using System.Linq;
using SnapRepo.Azure;
using SnapRepo.Common;
using Ionic.Zip;
using Microsoft.Web.Administration;

namespace SnapRepo.Backups
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

        public string BackupAzure(ManagerSettings settings, string backupInfix, bool deleteLocalBackup = true)
        {
            var backupFileName = BackupLocal(settings, backupInfix);
            var uri = _blobStorageService.SendBackupPackage(settings, backupFileName);
            if(deleteLocalBackup)
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
            if (iisreset)
            {
                RestartIisSite(siteName);
            }
        }

        public void RestartIisSite(string siteName)
        {
            if (!string.IsNullOrEmpty(siteName))
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

        private string ZipAppDataFolder(string localRepositoryPath, string appDataFolder)
        {
            string fileName = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Environment.MachineName}_AppData.zip";
            _logService.WriteLog($"Creating AppData zip file {fileName}.");
            using (ZipFile zip = new ZipFile())
            {
                zip.UseZip64WhenSaving = GetZip64Option();
                zip.AddDirectory(appDataFolder);
                zip.Save(localRepositoryPath + fileName);
            }
            return fileName;
        }

        private static Zip64Option GetZip64Option()
        {
            Zip64Option zip64Option;
            return Enum.TryParse(SettingsFactory.UseZip64WhenSaving, out zip64Option) ? zip64Option : Zip64Option.Default;
        }

        private string CreateBackupPackage(string localRepositoryPath, string dbBackupFileName, string appDataZipFileName, string backupInfix)
        {
            var infix = "_" + backupInfix?.Replace(" ", "") + "_";
            string zipFileNamePrefix = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}{infix}{Environment.MachineName}_package";
            string zipFileName = $"{zipFileNamePrefix}.zip";
            _logService.WriteLog($"Creating Package zip file {zipFileName}.");
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile($"{localRepositoryPath}{dbBackupFileName}", zipFileNamePrefix);
                zip.AddFile($"{localRepositoryPath}{appDataZipFileName}", zipFileNamePrefix);
                zip.Save(localRepositoryPath + zipFileName);
            }
            return zipFileName;
        }

        private string ExtractPackage(string localRepositoryPath, string packageFileName)
        {
            string backupFilePath = localRepositoryPath + packageFileName;
            string backupFolderPath = backupFilePath.Replace(".zip", "") + "\\";
            if (Directory.Exists(backupFolderPath))
            {
                Directory.Delete(backupFolderPath, true);
            }
            _logService.WriteLog($"Extracting Package zip file {packageFileName}.");
            using (ZipFile zip = new ZipFile(backupFilePath))
            {
                zip.ExtractAll(localRepositoryPath);
            }
            return backupFolderPath;
        }

        private void RestoreAppData(string localRepositoryPath, string packageFolderName, string appDataZipFile, string appDataFolder, bool renameExistingAppDataFolder = false)
        {
            string appDataZipFilePath = localRepositoryPath + packageFolderName + appDataZipFile;
            bool exists = Directory.Exists(appDataFolder);
            if (renameExistingAppDataFolder && exists)
                Directory.Move(appDataFolder, appDataFolder + "_backup_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            else
                Directory.Delete(appDataFolder, true);
            Directory.CreateDirectory(appDataFolder);
            _logService.WriteLog($"Restoring AppData from zip file {packageFolderName + appDataZipFile} to {appDataFolder}.");
            using (ZipFile zip = new ZipFile(appDataZipFilePath))
            {
                zip.ExtractAll(appDataFolder);
            }
        }
        
    }
}