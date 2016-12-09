using System.Collections.Specialized;
using System.IO;
using System.Linq;
using SnapRepo.Azure;
using SnapRepo.Backups;
using SnapRepo.Scheduling;

namespace SnapRepo.Common
{
    public class ActionService
    {
        private readonly BlobStorageService _blobStorageService;
        private readonly BackupService _backupService;
        private readonly ScheduledJobService _scheduledJobService;
        private readonly LogService _logService;

        public ActionService(BlobStorageService blobStorageService, BackupService backupService, ScheduledJobService scheduledJobService, LogService logService)
        {
            _blobStorageService = blobStorageService;
            _backupService = backupService;
            _scheduledJobService = scheduledJobService;
            _logService = logService;
        }

        public string CreateActionResult(NameValueCollection requestParams, ManagerSettings settings)
        { 
            string actionResult = null;
            switch (requestParams["action"])
            {
                case "ping":
                    actionResult = "Pong!";
                    break;
                case "setSettings":
                    //Creates the directory if it does not exist or throws an error if IIS user does not have privileges.
                    Directory.CreateDirectory(settings.LocalRepositoryPath);
                    break;
                case "download":
                    _blobStorageService.DownloadPackage(settings, requestParams["file"]);
                    actionResult = $"File ({requestParams["file"]}) is downloaded to Local Repository!";
                    break;
                case "upload":
                    var backupUri = _blobStorageService.SendBackupPackage(settings, requestParams["file"]);
                    actionResult = $"File ({requestParams["file"]}) has been uploaded into Azure Repository! Backup URI: {backupUri}.";
                    break;
                case "deleteLocal":
                    string fileFullPath = settings.LocalRepositoryPath + requestParams["file"];
                    File.Delete(fileFullPath);
                    actionResult = $"File ({fileFullPath}) is deleted!";
                    break;
                case "backupLocal":
                    string backupFileName = _backupService.BackupLocal(settings, requestParams["backupInfix"]);
                    actionResult = $"Backup Created ({backupFileName}) in Local Repository!";
                    break;
                case "restoreLocal":
                    _backupService.RestoreLocal(settings, requestParams["file"], true, settings.IisSiteName);
                    actionResult = $"Backup ({requestParams["file"]}) restored!";
                    break;
                case "backupAzure":
                    string backupAzureUri = _backupService.BackupAzure(settings, requestParams["backupInfix"]);
                    actionResult = $"Backup Created ({backupAzureUri}) and uploaded in Azure!";
                    break;
                case "cleanAzure":
                    bool simulate = bool.Parse(requestParams["simulate"] ?? "False");
                    string[] deletedFileNames = _blobStorageService.CleanAzure(settings, requestParams["filterInfix"], int.Parse(requestParams["minDays"]), simulate);
                    actionResult = $"{deletedFileNames.Count()} file(s) were deleted from Azure! {(simulate ? "Simulating!!!" : "")}<br/>{string.Join("<br/>", deletedFileNames)}";
                    break;
                case "addBackupJob":
                    _scheduledJobService.AddJob(
                        new JobProperties()
                        {
                            Name = requestParams["jobName"],
                            Interval = int.Parse(requestParams["interval"]),
                            AtHours = int.Parse(requestParams["atHours"]),
                            AtMins = int.Parse(requestParams["atMins"]),
                            Query = requestParams["queryParams"],
                            ManagerSettins = settings,
                        });
                    actionResult = "Scheduled job is CREATED!";
                    break;
                case "removeBackupJob":
                    _scheduledJobService.RemoveJob(requestParams["jobName"]);
                    actionResult = "Scheduled job is DELETED!";
                    break;
                case "restartIisSite":
                    _backupService.RestartIisSite(settings.IisSiteName);
                    actionResult = $"IIS site ({settings.IisSiteName}) is restarted!";
                    break;
            }
            _logService.WriteLog(actionResult);
            return actionResult;
        }
    }
}