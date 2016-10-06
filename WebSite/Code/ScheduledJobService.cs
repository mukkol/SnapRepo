using System.Linq;
using FluentScheduler;

namespace AzureBackupManager.Code
{
    public class ScheduledJobService
    {
        private readonly string _localFolderPath;
        private readonly ScheduledJobPersistor _scheduledJobPersistor;

        public ScheduledJobService(string localFolderPath, ScheduledJobPersistor scheduledJobPersistor)
        {
            _localFolderPath = localFolderPath;
            _scheduledJobPersistor = scheduledJobPersistor;
        }

        public void AddJob(BackupJobSettings job)
        {
            var list = _scheduledJobPersistor.GetAll()
                .ToList();
            list.Add(job);
            _scheduledJobPersistor.Store(list.ToArray());
            ResetJobManager();
        }

        public void RemoveJob(BackupJobSettings job)
        {
            var list = _scheduledJobPersistor.GetAll()
                .Where(j => j.Name != job.Name && j.Interval != job.Interval && j.AtHours != job.AtHours && j.AtMins != job.AtMins)
                .ToList();
            _scheduledJobPersistor.Store(list.ToArray());
            ResetJobManager();
        }
        public void RemoveJob(string jobName)
        {
            var list = _scheduledJobPersistor.GetAll()
                .Where(j => j.Name != jobName)
                .ToList();
            _scheduledJobPersistor.Store(list.ToArray());
            ResetJobManager();
        }

        public BackupJobSettings[] GetAll()
        {
            return _scheduledJobPersistor.GetAll();
        }

        private void ResetJobManager()
        {
            foreach (var schedule in JobManager.AllSchedules)
            {
                JobManager.RemoveJob(schedule.Name);
            }
            JobManager.Initialize(new BackupRegistry(_localFolderPath));
        }

    }

}