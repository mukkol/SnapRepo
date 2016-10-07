using System;
using System.IO;
using System.Net;
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
        private readonly BackupJobSettings _backupJobSettings;
        public string Name { get; set; }

        public BackupTask(ManagerSettings settings, string name, BackupJobSettings backupJobSettings, LogService logService = null)
        {
            _settings = settings;
            _logService = logService ?? new LogService(_settings.LocalFolderPath);
            _backupJobSettings = backupJobSettings;
            Name = name;
            HostingEnvironment.RegisterObject(this);
        }

        public void Execute()
        {
            lock (_lock)
            {
                if (_shuttingDown)
                    return;
                var started = DateTime.Now;
                _logService.WriteLog($"Task STARTED (\"{Name}\")! Query: {_backupJobSettings.Query}.");
                var resultStatusCode = GetQueryRequestStatusCode(_backupJobSettings.Query);
                var duration = DateTime.Now.Subtract(started);
                _logService.WriteLog($"Task FINISHED (\"{Name}\")!! Status Code: {resultStatusCode}, Duration: {duration.TotalMinutes} mins.");
            }
        }

        public HttpStatusCode GetQueryRequestStatusCode(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 60*60*1000; //(3600000ms = 60mins)
            request.AutomaticDecompression = DecompressionMethods.GZip;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                return response.StatusCode;
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