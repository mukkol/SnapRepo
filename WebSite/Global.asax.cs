using System;
using AzureBackupManager.Backups;
using AzureBackupManager.Common;
using AzureBackupManager.Scheduling;
using FluentScheduler;

namespace AzureBackupManager
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            string localFolderPath = BackupService.CreateSettingsFromParamsOrDefault().LocalFolderPath;
            JobManager.Initialize(new BackupRegistry(localFolderPath));
            JobManager.JobException += (info) => new LogService(localFolderPath).WriteLog("An error just happened with a scheduled job: " + info.Exception);
        }
        protected void Application_End(object sender, EventArgs e)
        {
        }
    }
}