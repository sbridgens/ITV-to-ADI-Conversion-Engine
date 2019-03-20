/* Copyright (C) 2018 SCH Tech Ltd - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the GNU General Public License v3.0 license.
 * email: simon[[@]]schtech.co.uk
 * (without the [[]] obviously)
 * https://www.linkedin.com/in/simonbridgens/
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Design;

namespace ITV2ADI_Engine
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
                new ITV2ADI_Service()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
