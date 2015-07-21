/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Diagnostics;
using System.Reflection;

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
                return String.Format("{0}.{1}", version.ProductMajorPart, version.ProductMinorPart);
            }
        }
    }
}