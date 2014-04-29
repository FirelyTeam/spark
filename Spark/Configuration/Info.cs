using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Spark.Configuration
{
    public class Info
    {
        public static string Version
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(asm.Location);
                return String.Format("{0}.{1}", version.ProductMajorPart, version.ProductMinorPart, version);
            }
        }
    }
}