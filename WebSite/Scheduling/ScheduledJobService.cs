using System;
using System.Linq;
using FluentScheduler;

namespace SnapRepo.Scheduling
{
    public class ScheduledJobService
    {
        private readonly JobPersistor _scheduledJobPersistor;

        public ScheduledJobService(JobPersistor scheduledJobPersistor)
        {
            _scheduledJobPersistor = scheduledJobPersistor;
        }

        public void AddJob(JobProperties job)
        {
            var list = _scheduledJobPersistor.GetAll()
                .ToList();
            list.Add(job);
            _scheduledJobPersistor.Store(list.ToArray());
            ResetJobManager();
        }

        public void RemoveJob(JobProperties job)
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

        public Tuple<JobProperties, Schedule>[] GetJobSettingsAndSchedule()
        {
            return _scheduledJobPersistor.GetAll().Select(s => new Tuple<JobProperties, Schedule>(s, JobManager.GetSchedule(s.Name))).ToArray();
        }

        public JobProperties[] GetBackupJobSettingses()
        {
            return _scheduledJobPersistor.GetAll();
        }

        private void ResetJobManager()
        {
            foreach (var schedule in JobManager.AllSchedules)
            {
                JobManager.RemoveJob(schedule.Name);
            }
            JobManager.Initialize(new ScheduledJobRegistry());
        }

    }

}