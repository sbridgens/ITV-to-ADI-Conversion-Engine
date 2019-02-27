using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITV2ADI_Engine.ITV2ADI_Managers;

namespace UnitTest
{
    class Program
    {
        static void Main(string[] args)
        {
            UnitTest unitTest = new UnitTest();
            unitTest.DoWork();
        }
    }
    
    public class UnitTest
    {
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


        public void DoWork()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            ITV2ADI_Controller _Controller = new ITV2ADI_Controller();
            Thread thread = new Thread(_Controller.WF_Start);
            thread.Start(_Controller);
            Thread.Sleep(0);
        }
    }
}