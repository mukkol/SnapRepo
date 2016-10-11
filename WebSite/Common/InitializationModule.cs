using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using AzureBackupManager.Common.IoC;
using AzureBackupManager.Scheduling;
using FluentScheduler;
using Microsoft.Web.Administration;

namespace AzureBackupManager.Common
{
    public class InitializationModule : IHttpModule
    {
        private static LogService _logService;
        public static string MonitorHostUrl;

        public static void ApplicationStart()
        {
            ObjectFactory.InitIoC();
            _logService = ObjectFactory.Container.GetInstance<LogService>();
            _logService.WriteLog("Application started!", true);

            MonitorHostUrl = GetCurrentSiteHostAddresses().FirstOrDefault();
            _logService.WriteLog("MonitorHostUrl: " + MonitorHostUrl, true);

            JobManager.Initialize(ObjectFactory.Container.GetInstance<BackupRegistry>());
            JobManager.JobException += (info) => _logService.WriteLog("An error just happened with a scheduled job: " + info.Exception, true);
        }

        public static void ApplicationEnd()
        {
            _logService.WriteLog("Application Stopped!", true);
            var client = new WebClient();
            client.DownloadString(MonitorHostUrl);
            _logService.WriteLog("Application Shut Down Ping: " + MonitorHostUrl, true);
        }

        public void Init(HttpApplication application)
        {
            application.Error += ApplicationOnError;
        }

        private void ApplicationOnError(object sender, EventArgs e)
        {
            var httpException = ((sender as HttpApplication)?.Server.GetLastError());
            var exception = httpException?.InnerException;
            _logService.WriteLog("Unhandled Exception: " + (exception ?? httpException));
        }

        private static IEnumerable<string> GetCurrentSiteHostAddresses()
        {
            string siteName = System.Web.Hosting.HostingEnvironment.SiteName;
            using (ServerManager serverManager = new ServerManager())
            {
                var site = serverManager.Sites.FirstOrDefault(s => s.Name == siteName);
                return site?.Bindings.Select(b => b.Protocol + "://" + b.Host) ?? Enumerable.Empty<string>();
            }
        }

        public void Dispose() { }
    }
}