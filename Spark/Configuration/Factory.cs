/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Spark.Store;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Hl7.Fhir.Search;
using Spark.Search;
using Spark.Service;
using Spark.Config;
using Spark.Data.AmazonS3;
using System.Configuration;
using Spark.Core;
using Spark.Mongo.Utils;

namespace Spark.Support
{
    
    public static class Factory
    {

        public static ResourceImporter GetResourceImporter()
        {
            IFhirStore store = Spark.Store.MongoStoreFactory.GetMongoFhirStore();
            ResourceImporter importer = new ResourceImporter(store, Settings.Endpoint);

            importer.SharedEndpoints.Add("http://hl7.org/fhir/");

            importer.SharedEndpoints.Add("localhost");
            importer.SharedEndpoints.Add("localhost.");

            return importer;
        }
       
        public static ResourceExporter GetResourceExporter()
        {
            return new ResourceExporter(Settings.Endpoint);
        }

        public static FhirMaintenanceService GetFhirMaintenanceService()
        {
            FhirService service = new FhirService(new Uri(Settings.Endpoint, "maintenance")); // example: http://spark.furore.com/maintenance/
            return new FhirMaintenanceService(service);
        }
       
    }
}