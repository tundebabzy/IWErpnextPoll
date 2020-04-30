using System.ServiceProcess;

namespace IWErpnextPoll
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ErpnextService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
