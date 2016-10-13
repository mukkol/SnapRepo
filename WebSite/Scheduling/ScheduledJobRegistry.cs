using SnapRepo.Common.IoC;
using FluentScheduler;

namespace SnapRepo.Scheduling
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