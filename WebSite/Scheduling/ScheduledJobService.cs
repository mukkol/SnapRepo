using System;
using System.Linq;
using FluentScheduler;

namespace AzureBackupManager.Scheduling
{
    public class ScheduledJobService
    {
        private readonly ScheduledJobPersistor _scheduledJobPersistor;

        public ScheduledJobService(ScheduledJobPersistor scheduledJobPersistor)
        {
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
                .Where(j => j.Name != job.Name && j.Interval != job.Interval && j.AtHours != job.AtHours && j.AtMins != job.AtMins && j.Query == job.Query)
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

        public Tuple<BackupJobSettings, Schedule>[] GetJobSettingsAndSchedule()
        {
            return _scheduledJobPersistor.GetAll().Select(s => new Tuple<BackupJobSettings, Schedule>(s, JobManager.GetSchedule(s.Name))).ToArray();
        }

        public BackupJobSettings[] GetBackupJobSettingses()
        {
            return _scheduledJobPersistor.GetAll();
        }

        private void ResetJobManager()
        {
            foreach (var schedule in JobManager.AllSchedules)
            {
                JobManager.RemoveJob(schedule.Name);
            }
            JobManager.Initialize(new BackupRegistry());
        }

    }

}