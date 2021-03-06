﻿using ITV2ADI_Engine.ITV2ADI_Database;
using ITV2ADI_Engine.ITV2ADI_Workers;
using SCH_CONFIG;
using SCH_IO;
using SCH_QUEUE;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;


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

        private System.Timers.Timer _Timer;

        private bool IsProcessing { get; set; }

        private ConfigHandler<ITV2ADI_CONFIG> _Config_Handler;

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
            if (ConfigHandler<ITV2ADI_CONFIG>.B_IsRunning)
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
                _Config_Handler = new ConfigHandler<ITV2ADI_CONFIG>();
                log.Info("Loading Application Configuration.");
                _Config_Handler.LoadApplicationConfig(Properties.Settings.Default.XmlConfigFile);
                ConfigHandler<ITV2ADI_CONFIG>.B_IsRunning = true;
            }
            catch (Exception LAC_EX)
            {
                log.Error($"Failed loading Service configuration: {LAC_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {LAC_EX.StackTrace}");
                ConfigHandler<ITV2ADI_CONFIG>.B_IsRunning = false;
            }
        }

        /// <summary>
        /// Begin processing operations
        /// </summary>
        void StartITV2ADI_Engine()
        {
            _Timer = new System.Timers.Timer(Convert.ToInt32(ITV2ADI_CONFIG.ExpiredAssetCleanupIntervalHours) * 60 * 60 * 1000);
            _Timer.Elapsed += new ElapsedEventHandler(ElapsedTime);
            _Timer.Start();
            ///fire on startup.
            ElapsedTime(IsProcessing, null);

            while (ConfigHandler<ITV2ADI_CONFIG>.B_IsRunning == true)
            {
                CleanTempDirectory();
                string pollFiles = PollHandler.StartPolling(ITV2ADI_CONFIG.InputDirectory, ".itv");
                if (!string.IsNullOrEmpty(pollFiles))
                    log.Info(pollFiles);


                if (PollHandler.B_FilesToProcess)
                {
                    IsProcessing = false;
                    ProcessQueuedItems();
                }
                if (ConfigHandler<ITV2ADI_CONFIG>.B_IsRunning)
                {
                    Thread.Sleep(Convert.ToInt32(ITV2ADI_CONFIG.PollIntervalInSeconds) * 1000);
                }
            }
        }

        void CleanTempDirectory()
        {
            FileDirectoryOperations.CleanTempDirectory(ITV2ADI_CONFIG.TempWorkingDirectory);
        }

        /// <summary>
        /// Timer Event for cleanup, flags a boolean in case there is processing underway
        /// allowing the clean up to occur post processing
        /// </summary>
        /// <param name="src"></param>
        /// <param name="e"></param>
        private void ElapsedTime(object src, ElapsedEventArgs e)
        {
            if (!IsProcessing)
            {
                using (ITVConversionContext db = new ITVConversionContext())
                {
                    log.Info("Checking for expired data in the ITV Conversion db");
                    var rowData = db.ItvConversionData.Where(le => Convert.ToDateTime(le.LicenseEndDate.Value.ToString().Trim(' ')) < DateTime.Now).ToList();

                    if (rowData == null)
                    {
                        log.Info("No expired data present.");
                    }
                    else
                    {
                        foreach (var row in rowData)
                        {
                            string paid = row.Paid;
                            log.Debug($"DB Row ID {row.Id} with PAID Value: {paid} has expired with License End date: {row.LicenseEndDate.Value}, removing from database.");
                            var maprow = db.ItvConversionData
                                           .Where(r => r.Paid == paid)
                                           .FirstOrDefault();

                            if (maprow != null)
                            {

                                db.ItvConversionData.Remove(maprow);
                                db.SaveChanges();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Main worker function. This function iterates each deliverable in turn and executes the main processing entry points.
        /// </summary>
        private void ProcessQueuedItems()
        {
            if (WF_WorkQueue.queue.Count >= 1)
            {
                Drive_Info drive_Info = new Drive_Info();
                for (int q = 0; q < WF_WorkQueue.queue.Count; q++)
                {
                    ConfigHandler<ITV2ADI_CONFIG>.B_IsRunning = drive_Info.GetDriveSpace();
                    if (ConfigHandler<ITV2ADI_CONFIG>.B_IsRunning)
                    {
                        WorkQueueItem itvFile = (WorkQueueItem)WF_WorkQueue.queue[q];

                        try
                        {
                            log.Info($"############### Processing STARTED For Queued item {q + 1} of {WF_WorkQueue.queue.Count}: {itvFile.file.Name} ###############\r\n\r\n");
                            IsProcessing = true;
                            Mapping = new MapITVtoADI
                            {
                                ITV_FILE = itvFile.file.FullName
                            };

                            if (Mapping.StartItvMapping())
                            {
                                B_IsSuccess = true;
                                if (Convert.ToBoolean(ITV2ADI_CONFIG.DeleteITVFileUponSuccess))
                                {
                                    log.Info($"Delete source itv file upon success is true, removing source file: {itvFile.file.FullName}");
                                    File.Delete(itvFile.file.FullName);

                                    if (!File.Exists(itvFile.file.FullName))
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
                            Mapping.CleanUp();
                            log.Info($"############### Processing FAILED For Queued file: {itvFile.file.Name} ###############\r\n\r\n");
                            B_IsSuccess = false;
                            continue;
                        }
                        finally
                        {
                            if(Mapping.ItvFailure)
                            {
                                log.Info($"A Product/Products were flagged as failed during processing, Removing Source ITV File: {itvFile.file.FullName}");
                                File.Delete(itvFile.file.FullName);
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            IsProcessing = false;
        }
    }
}
