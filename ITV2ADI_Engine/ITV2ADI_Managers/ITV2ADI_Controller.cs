using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITV2ADI_Engine.ITV2ADI_Workers;
using SCH_CONFIG;
using SCH_QUEUE;

namespace ITV2ADI_Engine.ITV2ADI_Managers
{
    public class ITV2ADI_Controller
    { 
        /// <summary>
      /// Initialize Log4net
      /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ITV2ADI_Controller));

        /// <summary>
        /// Declare Pollcontroller dll
        /// </summary>
        private PollController PollHandler;

        /// <summary>
        /// Decalre MapITVtoADI Class
        /// </summary>
        private MapITVtoADI Mapping;

        /// <summary>
        /// Property: Boolean value to indicate if a method executed correctly.
        /// </summary>
        private bool B_IsSuccess { get; set; }

        /// <summary>
        /// Function to resolve application assemblies that are contained in sub directories.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;

            // check for assemblies already loaded
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            // Try to load by filename - split out the filename of the full assembly name
            // and append the base path of the original assembly (ie. look in the same dir)
            string filename = args.Name.Split(',')[0] + ".dll".ToLower();

            string asmFile = Path.Combine(@".\", "mslib", filename);

            try
            {
                return Assembly.LoadFrom(asmFile);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Service Function entry point, this function dedects if the application can start correctly
        /// if the config is not found or correctly loaded the service will fail to start.
        /// </summary>
        /// <param name="objdata"></param>
        public void WF_Start(object objdata)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            ITV2ADI_Controller _Controller = (ITV2ADI_Controller)objdata;

            log.Info("************* Service Starting **************");
            LoadAppConfig();
            if (ITV2ADI_Config_Handler.B_IsRunning)
            {
                log.Info("Service Started Successfully");

                PollHandler = new PollController();
                StartITV2ADI_Engine();
            }
            else
            {
                log.Error("Service Stopping due to error or manual stop.");
            }
        }

        /// <summary>
        /// Load and validate the application configuration
        /// </summary>
        void LoadAppConfig()
        {
            try
            {
                log.Info("Loading Application Configuration.");
                ITV2ADI_Config_Handler.B_IsRunning = false;
                ITV2ADI_Config_Handler.LoadApplicationConfig(Properties.Settings.Default.XmlConfigFile);
            }
            catch(Exception LAC_EX)
            {
                log.Error($"Failed loading Service configuration: {LAC_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {LAC_EX.StackTrace}");
            }
        }

        /// <summary>
        /// Begin processing operations
        /// </summary>
        void StartITV2ADI_Engine()
        {
            while (ITV2ADI_Config_Handler.B_IsRunning == true)
            {
                string pollFiles = PollHandler.StartPolling(ITV2ADI_CONFIG.InputDirectory,".itv");
                if (!string.IsNullOrEmpty(pollFiles))
                    log.Info(pollFiles);


                if (PollHandler.B_FilesToProcess)
                {
                    ProcessQueuedItems();
                }

                Thread.Sleep(Convert.ToInt32(ITV2ADI_CONFIG.PollIntervalInSeconds) * 1000);
            }
        }

        /// <summary>
        /// Main worker function. This function iterates each deliverable in turn and executes the main processing entry points.
        /// </summary>
        private void ProcessQueuedItems()
        {
            if(WF_WorkQueue.queue.Count >= 1)
            {
                for (int q = 0; q < WF_WorkQueue.queue.Count; q++)
                {
                    WorkQueueItem itvFile = (WorkQueueItem)WF_WorkQueue.queue[q];

                    try
                    {
                        log.Info($"############### Processing STARTED For Queued item {q + 1} of {WF_WorkQueue.queue.Count}: {itvFile.file.Name} ###############\r\n\r\n");

                        Mapping = new MapITVtoADI
                        {
                            ITV_FILE = itvFile.file.FullName
                        };

                        if (Mapping.StartItvMapping())
                        {
                            if(ITV2ADI_CONFIG.DeleteITVFileUponSuccess)
                            {
                                log.Info($"Delete source itv file upon success is true, removing source file: {itvFile.file.FullName}");
                                File.Delete(itvFile.file.FullName);

                                if(!File.Exists(itvFile.file.FullName))
                                {
                                    log.Info($"ITV File: {itvFile.file.FullName} successfully deleted");
                                }
                                else
                                {
                                    log.Error($"Failed to delete source ITV File: {itvFile.file.FullName}?");
                                }
                            }

                            log.Info($"############### Processing SUCCESSFUL For Queued file: {Mapping.ITV_FILE} ###############\r\n\r\n");
                            
                        }
                        else
                        {
                            throw new Exception($"Failed during itv Mapping process, check the logs for more information.");
                        }
                    }
                    catch (Exception PQI_EX)
                    {
                        log.Error($"Caught Exception during Process of Queued Items: {PQI_EX.Message}");
                        
                        log.Info($"############### Processing FAILED For Queued file: {itvFile.file.Name} ###############\r\n\r\n");

                        continue;
                    }
                    finally
                    {
                        Mapping.CleanUp();
                    }
                }
            }
        }
    }
}
