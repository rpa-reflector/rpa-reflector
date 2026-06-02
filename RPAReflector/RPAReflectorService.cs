using System;
using System.ServiceProcess;
using System.Timers;
using Microsoft.Owin.Hosting;

namespace RPAReflector
{
    public partial class RPAReflector : ServiceBase
    {
        private IDisposable _webApp;
        private System.Timers.Timer _cleanupTimer;

        public RPAReflector()
        {
            InitializeComponent();
        }

        private void OnCleanupTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Filesystem.DeleteOldPayloadFiles();
        }

        protected override void OnStart(string[] args)
        {
            _cleanupTimer = new System.Timers.Timer(60000);
            _cleanupTimer.Elapsed += new ElapsedEventHandler(OnCleanupTimerElapsed);
            _cleanupTimer.Start();


#if DEBUG
            string apiBaseAddress = "http://localhost:9001/";
#else
            string apiBaseAddress = "http://*:9001/";
#endif
            _webApp = WebApp.Start<API>(apiBaseAddress);
        }

        // Method to start the service in debug mode
        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStop()
        {
            _webApp.Dispose();
            _cleanupTimer.Stop();
            _cleanupTimer.Dispose();
        }
    }
}
