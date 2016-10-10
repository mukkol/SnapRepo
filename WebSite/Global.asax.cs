using System;
using AzureBackupManager.Common;
using AzureBackupManager.Common.IoC;
using AzureBackupManager.Scheduling;
using FluentScheduler;

namespace AzureBackupManager
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            JobManager.Initialize(ObjectFactory.Container.GetInstance<BackupRegistry>());
            var logService = ObjectFactory.Container.GetInstance<LogService>();
            JobManager.JobException += (info) => logService.WriteLog("An error just happened with a scheduled job: " +  info.Exception);

            ObjectFactory.InitIoC();
        }

        protected void Application_End(object sender, EventArgs e)
        {
        }
    }


}