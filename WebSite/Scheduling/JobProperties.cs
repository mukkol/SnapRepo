using System.Web.Helpers;
using SnapRepo.Common;

namespace SnapRepo.Scheduling
{
    public class JobProperties
    {
        public string Name { get; set; }
        public int Interval { get; set; }
        public int AtHours { get; set; }
        public int AtMins { get; set; }
        public ManagerSettings ManagerSettins { get; set; }
        public string Query { get; set; }

        public override string ToString()
        {
            return Json.Encode(this);
        }
    }
}