using System.Web.Hosting;
using FluentScheduler;

namespace AzureBackupManager.Scheduling
{
    public class BackupRegistry : Registry
    {
        public BackupRegistry(string localFolderPath)
        {
            var jobs = new ScheduledJobPersistor(localFolderPath).GetAll();
            foreach (var j in jobs)
            {
                Schedule(new BackupTask(j.ManagerSettins, j.Name)).WithName(j.Name).ToRunEvery(j.Interval).Days().At(j.AtHours, j.AtMins);
            }
        }
    }

}