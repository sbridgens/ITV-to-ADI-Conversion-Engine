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

        public ITVConversionContext Db { get; set; }

        public string ParseReportingClass(string ReportingClass, bool isShowType)
        {
            var result = Db.ReportClassMapping.Where(r => r.ReportingClass.ToLower() == ReportingClass.ToLower()
                                                      && r.ClassIncludes == null)
                                             .Select(f => f.FolderLocation).FirstOrDefault();

            if (!string.IsNullOrEmpty(result) && !isShowType)
            {
                return result;
            }
            else
            {
                return ParseReportClassIncludes(ReportingClass, isShowType);
            }
        }

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
