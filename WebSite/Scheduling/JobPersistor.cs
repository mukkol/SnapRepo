using System.IO;
using System.Text;
using System.Web.Helpers;
using SnapRepo.Common;

namespace SnapRepo.Scheduling
{
    public class JobPersistor
    {
        private readonly string _localRepositoryPath;
        
        public JobPersistor()
        {
            _localRepositoryPath = SettingsFactory.StaticLocalRepositoryPath;
        }

        public void Store(JobProperties[] backupJobs)
        {
            StreamWriter file = new StreamWriter(_localRepositoryPath + "ScheduledJobsRepository.json", false, Encoding.UTF8);
            file.WriteLine(Json.Encode(backupJobs));
            file.Close();
        }

        public JobProperties[] GetAll()
        {
            try
            {
                StreamReader file = new StreamReader(_localRepositoryPath + "ScheduledJobsRepository.json", Encoding.UTF8);
                var str = file.ReadToEnd();
                var s = Json.Decode<JobProperties[]>(str);
                file.Close();
                return s ?? new JobProperties[] {};
            }
            catch (FileNotFoundException) { return new JobProperties[] { }; }
            catch (DirectoryNotFoundException) { return new JobProperties[] { }; }
        }

    }

}