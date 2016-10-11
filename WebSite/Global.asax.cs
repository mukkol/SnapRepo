using System;
using AzureBackupManager.Common;
using AzureBackupManager.Common.IoC;
using AzureBackupManager.Scheduling;
using FluentScheduler;

namespace AzureBackupManager
{
    public class Global : System.Web.HttpApplication
    {
        private LogService _logService;

        protected void Application_Start(object sender, EventArgs e)
        {
            ObjectFactory.InitIoC();

            _logService = ObjectFactory.Container.GetInstance<LogService>();
            JobManager.Initialize(ObjectFactory.Container.GetInstance<BackupRegistry>());
            JobManager.JobException += (info) => _logService.WriteLog("An error just happened with a scheduled job: " +  info.Exception);

            _logService.WriteLog("Site started!");
        }

        protected void Application_End(object sender, EventArgs e)
        {
            _logService.WriteLog("Site Stopped!");
        }
    }


}