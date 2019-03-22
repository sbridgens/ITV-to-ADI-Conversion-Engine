using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITV2ADI_Engine.ITV2ADI_Managers;

namespace ITV2ADI_Engine
{
    public partial class ITV2ADI_Service : ServiceBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        ITV2ADI_Controller _Controller;
        Thread thread;

        public ITV2ADI_Service()
        {
            InitializeComponent();
            this.ServiceName = "ITV2ADI_Engine";
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = false;
            this.CanHandlePowerEvent = true;
            this.CanShutdown = true;
            this.CanStop = true;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                _Controller = new ITV2ADI_Controller();

                thread = new Thread(_Controller.WF_Start);
                thread.Start(_Controller);
                base.OnStart(args);

            }
            catch (Exception OSEx)
            {
                log.Error($"Error starting the service please check permissions or configuration parameters, Message: {OSEx.Message}");
                log.Error($"StackTrace: {OSEx.StackTrace}");
                OnStop();
            }
        }

        protected override void OnStop()
        {
        }
    }
}