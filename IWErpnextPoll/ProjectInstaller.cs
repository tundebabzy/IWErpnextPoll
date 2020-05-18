using System.ComponentModel;

namespace IWErpnextPoll
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller1_AfterInstall(object sender, System.Configuration.Install.InstallEventArgs e)
        {

        }
    }
}
