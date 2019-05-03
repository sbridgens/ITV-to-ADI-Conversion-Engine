using ITV2ADI_Engine.ITV2ADI_Database;
using SCH_ADI;
using SCH_ITV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITV2ADI_Engine.ITV2ADI_Workers
{
    public partial class MapITVtoADI
    {
        /// <summary>
        /// Intialize Log4net
        /// </summary>
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(MapITVtoADI));


        /// <summary>
        /// Required in order to understand if the asset is an update as this is used to compare to 
        /// previously stored date
        /// </summary>
        private DateTime Publication_Date { get; set; }

        /// <summary>
        /// Class definition that holds functions to convert itv data to adi format
        /// </summary>
        private ITVConversionFunctions iTVConversion;

        /// <summary>
        /// Boolean to track successful returns
        /// </summary>
        public bool B_IsSuccess { get; private set; }

        /// <summary>
        /// Full working temp directory path
        /// </summary>
        private string WorkingDirectory { get; set; }

        /// <summary>
        /// Flag obtained from the media locations table in the db
        /// this is used to define whether we remove source media from the media location
        /// post processing
        /// </summary>
        private bool DeleteFromSource { get; set; }

        /// <summary>
        /// Required to check if the asset is valid or can be removed from the db
        /// </summary>
        private DateTime LicenseStart { get; set; }

        /// <summary>
        /// Media Directory full path inside the package this is where the final asset
        /// will be copied too prior to checksum and packaging
        /// </summary>
        private string MediaDirectory { get; set; }

        /// <summary>
        /// Filename of the Media file
        /// </summary>
        private string MediaFileName { get; set; }

        /// <summary>
        /// Source media location based on the db value returned this is used as a source for copy
        /// </summary>
        private string MediaLocation { get; set; }

        /// <summary>
        /// MD5 checksum of the asset
        /// </summary>
        private string MediaChecksum { get; set; }

        /// <summary>
        /// CP Provider name
        /// </summary>
        private string ProviderName { get; set; }

        /// <summary>
        /// Required to check if the asset is valid or can be removed from the db
        /// </summary>
        private DateTime LicenseEnd { get; set; }

        /// <summary>
        /// Asset Title
        /// </summary>
        private string ProgramTitle { get; set; }

        /// <summary>
        /// Asset Deactivate date for tvod assets
        /// </summary>
        public string DeactiveDate { get; set; }

        /// <summary>
        /// /Version major increments base on updates
        /// </summary>
        private int? Version_Major { get; set; }

        /// <summary>
        /// Directory name for the tmp working dir used later for cleanup and packaging
        /// </summary>
        private string WorkDirname { get; set; }

        /// <summary>
        /// Boolean flag to state the ingest is to be rejected
        /// </summary>
        private bool RejectIngest { get; set; }

        /// <summary>
        /// CP 3 char id
        /// </summary>
        private string ProviderId { get; set; }

        /// <summary>
        /// DB Row id used for lookups later in the workflow
        /// </summary>
        private int ItvData_RowId { get; set; }

        /// <summary>
        /// /Asset product id 
        /// </summary>
        private string ProductId { get; set; }
        
        /// <summary>
        /// Asset Active date for tvod assets
        /// </summary>
        public string ActiveDate { get; set; }

        /// <summary>
        /// ITV file as a string used for storing into the db column
        /// </summary>
        public string ITV_FILE { get; set; }

        /// <summary>
        /// Current asset id used for mapping into the adi
        /// </summary>
        private string AssetId { get; set; }

        /// <summary>
        /// Adi file as a string used for storing in the db column
        /// </summary>
        public string ADI_FILE { get; set; }

        /// <summary>
        /// Boolean flag to determine if the package is an update
        /// </summary>
        private bool IsUpdate { get; set; }

        /// <summary>
        /// Boolean flag to determine if the package is an TVOD asset
        /// </summary>
        private bool IsTVOD { get; set; }

        /// <summary>
        /// Pre initialiser of the itvconversion context class
        /// </summary>
        private ITVConversionContext db;

        /// <summary>
        /// pre initialiser of the adi mapping class
        /// </summary>
        private ADI_Mapping AdiMapping;

        /// <summary>
        /// pre initialiser of the itv parser
        /// </summary>
        private ITV_Parser ITVParser;
    }
}
