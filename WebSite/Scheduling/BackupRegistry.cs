using AzureBackupManager.Common.IoC;
using FluentScheduler;

namespace AzureBackupManager.Scheduling
{
    public class BackupRegistry : Registry
    {
        public BackupRegistry()
        {
            var jobs = ObjectFactory.Container.GetInstance<ScheduledJobPersistor>().GetAll();
            foreach (var job in jobs)
            {
                Schedule(new BackupTask(job.ManagerSettins, job)).WithName(job.Name).ToRunEvery(job.Interval).Days().At(job.AtHours, job.AtMins);
            }
        }
    }

}