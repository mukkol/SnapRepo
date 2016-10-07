using System.Web.Hosting;
using AzureBackupManager.Common;
using FluentScheduler;

namespace AzureBackupManager.Scheduling
{
    public class BackupTask : IJob, IRegisteredObject
    {
        private readonly object _lock = new object();
        private bool _shuttingDown;
        private readonly ManagerSettings _settings;
        private readonly LogService _logService;
        public string Name { get; set; }

        public BackupTask(ManagerSettings settings, string name)
        {
            _settings = settings;
            _logService = new LogService(_settings.LocalFolderPath);
            Name = name;
            HostingEnvironment.RegisterObject(this);
        }

        public void Execute()
        {
            lock (_lock)
            {
                if (_shuttingDown)
                    return;
                _logService.WriteLog($"BackupTask ({Name}) Executed! {_settings}");
            }
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