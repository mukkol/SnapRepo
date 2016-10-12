using AzureBackupManager.Common.IoC;
using FluentScheduler;

namespace AzureBackupManager.Scheduling
{
    public class ScheduledJobRegistry : Registry
    {
        public ScheduledJobRegistry()
        {
            var jobs = ObjectFactory.Container.GetInstance<JobPersistor>().GetAll();
            foreach (var job in jobs)
            {
                Schedule(new ScheduledJob(job.ManagerSettins, job)).WithName(job.Name).ToRunEvery(job.Interval).Days().At(job.AtHours, job.AtMins);
            }
        }
    }

}