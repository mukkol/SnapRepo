using System;
using SnapRepo.Common;

namespace SnapRepo
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            InitializationModule.ApplicationStart();
        }

        protected new void BeginRequest(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
            InitializationModule.ApplicationEnd();
        }
    }

}