using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Hosting;
using AzureBackupManager.Common;
using AzureBackupManager.Common.IoC;
using FluentScheduler;

namespace AzureBackupManager.Scheduling
{
    public class BackupTask : IJob, IRegisteredObject
    {
        private readonly object _lock = new object();
        private bool _shuttingDown;
        private readonly ManagerSettings _settings;
        private readonly BackupJobSettings _backupJobSettings;
        public string Name { get; set; }

        public BackupTask(ManagerSettings settings, BackupJobSettings jobSettings)
        {
            _settings = settings;
            _backupJobSettings = jobSettings;
            Name = jobSettings.Name;
            HostingEnvironment.RegisterObject(this);
        }

        public void Execute()
        {
            lock (_lock)
            {
                if (_shuttingDown)
                    return;
                var started = DateTime.Now;
                var logService = ObjectFactory.Container.GetInstance<LogService>();
                logService.WriteLog($"Task STARTED (\"{Name}\")! Query: {_backupJobSettings.Query}.");
                var result = "";
                try
                {
                    result = ExecuteActionByQueryString(_backupJobSettings.Query, _settings);
                }
                catch (Exception e)
                {
                    logService.WriteLog($"Exception in Execution: {e}");
                    return;
                }
                var duration = DateTime.Now.Subtract(started);
                logService.WriteLog($"Task FINISHED (\"{Name}\")!! Duration: {duration.TotalMinutes} mins, Result: {result}.");
            }
        }

        public static string ExecuteActionByQueryString(string queryString, ManagerSettings settings)
        {
            var actionService = ObjectFactory.Container.GetInstance<ActionService>();
            NameValueCollection parms = HttpUtility.ParseQueryString(queryString);
            return actionService.CreateActionResult(parms, settings);
        }

        public void Stop(bool immediate)
        {
            // Locking here will wait for the lock in Execute to be released until this code can continue.
            lock (_lock)
            {
                _shuttingDown = true;
            }
            HostingEnvironment.UnregisterObject(this);
        }
    }
}