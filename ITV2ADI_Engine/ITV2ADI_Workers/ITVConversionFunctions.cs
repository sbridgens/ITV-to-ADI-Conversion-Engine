using ITV2ADI_Engine.ITV2ADI_Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITV2ADI_Engine.ITV2ADI_Workers
{
    public class ITVConversionFunctions
    {
        /// <summary>
        /// Set/get the db context from the processing classes
        /// </summary>
        public ITVConversionContext Db { get; set; }

        /// <summary>
        /// String value to store Adult flag from the itv file
        /// used later as a boolean
        /// </summary>
        public string IsAdult { get; set; }

        /// <summary>
        /// Flag to state if the package is a movie
        /// </summary>
        public bool IsMovie { get; set; }
        
        /// <summary>
        /// Functon to parse the showtype and declare if isadult or movie
        /// </summary>
        /// <param name="showtype"></param>
        private void SetFlags(string showtype)
        {
            IsAdult = "N";
            IsMovie = false;

            switch (showtype)
            {
                case "movie":
                    IsMovie = true;
                    break;
                case "adult":
                    IsAdult = "Y";
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// The itv reporting class holds the data on the show types, this has the data in the db and is checked for 
        /// type and its folder locations, if the class has includes ie for cutv kids then this is detected and passed to the 
        /// ParseReportClassIncludes class, see the db table for more info.
        /// </summary>
        /// <param name="ReportingClass"></param>
        /// <param name="isShowType"></param>
        /// <returns></returns>
        public string ParseReportingClass(string ReportingClass, string adiElement, bool isShowType)
        {

            var result = Db.ReportClassMapping.Where(r => r.ReportingClass.ToLower() == ReportingClass.ToLower() &&
                                                          r.ClassIncludes == null)
                                              .Select(f =>  new
                                              {
                                                 f.FolderLocation,
                                                 f.ShowType
                                              })
                                              .FirstOrDefault();

            SetFlags(result.ShowType.ToLower());
            
            if (!string.IsNullOrEmpty(result.FolderLocation) && !isShowType)
            {
                return result.FolderLocation;
            }
            if(adiElement.Equals("Folder_Location"))
            {
                return result.FolderLocation;
            }
            else
            {
                return ParseReportClassIncludes(ReportingClass, isShowType);
            }
        }
        
        /// <summary>
        /// Function to parse the includes list and set the correct showtype or folderlocation
        /// id	Reporting_Class	ClassIncludes	Folder_Location	ShowTyp
        /// ReportingClass: CUTV Cartoon, Cartoons, Children, Childrens, Kids, Kids Music,Pre-School,R_Children,RT_Children Kids CUTV Series
        /// ReportingClass: Kids NULL    Kids Archive    Series
        /// ReportingClass: TVSVOD NULL    Archive Series
        /// ReportingClass: CUTV NULL    CUTV CUTV
        /// ReportingClass: Movies NULL    Movies Movie
        /// ReportingClass: Adult NULL    Adult Adult
        /// </summary>
        /// <param name="ReportingClass"></param>
        /// <param name="isShowType"></param>
        /// <returns></returns>
        private string ParseReportClassIncludes(string ReportingClass, bool isShowType)
        {
            var includes = Db.ReportClassMapping.Where(r => ReportingClass.ToLower().Contains(r.ReportingClass.ToLower()))
                                                   .Select(r => new
                                                   {
                                                       r.ClassIncludes,
                                                       r.ReportingClass,
                                                       r.FolderLocation,
                                                       r.ShowType
                                                   })
                                                   .FirstOrDefault();


            List<string> incList = includes.ClassIncludes?.Split(',').ToList();

            if (!string.IsNullOrEmpty(includes.ClassIncludes))
            {
                int count = incList.Count;

                foreach (var include in incList)
                {
                    if (ReportingClass.ToLower() == $"{includes.ReportingClass} {include}".ToLower())
                    {
                        if (isShowType)
                        {
                            return includes.ShowType;
                        }
                        else
                        {
                            return includes.FolderLocation;
                        }
                    }

                    count--;
                    //If zero then the class is a part of excludes list ie cutv asset so we can assume here 
                    //that we matched no kids based include categories and can return the location associated
                    //with cutv that has no include classes.
                    if (count == 0)
                    {
                        return Db.ReportClassMapping.Where(r => ReportingClass.ToLower().Contains(r.ReportingClass.ToLower()) &&
                                                                   r.ClassIncludes == null)
                                                    .Select(f => f.FolderLocation)
                                                    .FirstOrDefault().ToString();
                    }
                }
            }
            else if (isShowType)
            {
                return includes.ShowType;
            }

            return "";
        }
        
    }
}
