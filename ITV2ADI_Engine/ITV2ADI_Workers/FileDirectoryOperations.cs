using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITV2ADI_Engine.ITV2ADI_Workers
{
    public class FileDirectoryOperations
    {

        /// <summary>
        /// Intialize Log4net
        /// </summary>
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(FileDirectoryOperations));

        public bool MoveFile(string sourceFile, string destFile)
        {
            try
            {
                File.Move(sourceFile, destFile);
                return File.Exists(destFile);
            }
            catch(IOException MF_EX)
            {
                log.Error($"Failed to Move file: {sourceFile} to {destFile} Error: {MF_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {MF_EX.StackTrace}");
                return false;
            }
        }

        public bool CreateDirectory(string FullDirectoryName, bool deleteIfExists=true)
        {
            try
            {
                if(Directory.Exists(FullDirectoryName) && deleteIfExists)
                {
                    DeleteDirectory(FullDirectoryName);
                }

                Directory.CreateDirectory(FullDirectoryName);

                return Directory.Exists(FullDirectoryName);
            }
            catch(IOException CD_EX)
            {
                log.Error($"Failed to Create directory: {FullDirectoryName} - {CD_EX.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {CD_EX.StackTrace}");
                return false;
            }
            
        }

        public bool DeleteDirectory(string FullDirectoryName)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(FullDirectoryName, "*.*", searchOption: SearchOption.AllDirectories))
                {
                    File.Delete(file);
                }
                Directory.Delete(FullDirectoryName);
                log.Info($"Directory {FullDirectoryName} Successfully removed.");

                return true;
            }
            catch (IOException delex)
            {
                log.Error($"Failed to delete directory: {FullDirectoryName} - {delex.Message}");
                if (log.IsDebugEnabled)
                    log.Debug($"STACK TRACE: {delex.StackTrace}");

                return false;
            }

        }
    }
}
