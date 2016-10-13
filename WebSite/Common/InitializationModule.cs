using System;
using System.Web;
using SnapRepo.Common.IoC;
using SnapRepo.Scheduling;
using FluentScheduler;

namespace SnapRepo.Common
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

            JobManager.Initialize(ObjectFactory.Container.GetInstance<ScheduledJobRegistry>());
            JobManager.JobException += (info) => _logService.WriteLog("An error just happened with a scheduled job: " + info?.Exception, true);
        }

        public static void ApplicationEnd()
        {
            _logService.WriteLog("Application Stopped!", true);
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

        public void Dispose() { }
    }
}