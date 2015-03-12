/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Spark.Support;
using System.Diagnostics;
using System.IO;
using Spark.Data;
//using Spark.Search;
using Spark.Config;
using Spark.Core;
using Spark.Store;

namespace Spark.Service
{
    public class FhirMaintenanceService
    {
        private FhirService service;
        IFhirStore store = DependencyCoupler.Inject<IFhirStore>();
        IGenerator generator = DependencyCoupler.Inject<IGenerator>();
        // IFhirIndex index = DependencyCoupler.Inject<IFhirIndex>();

        string zipfile;
        Bundle examples;

        public FhirMaintenanceService(FhirService service)
        {
            this.service = service;
        }


        private void createConformance()
        {
            Resource conformance = DependencyCoupler.Inject<Conformance>();
            IKey key = generator.NextKey(conformance);
            service.Create(key, conformance);
        }
        
        private void importExamples()
        {
            examples = FhirZipImporter.UnzipAsBundle(zipfile);
            examples.Entry = examples.Entry.Where(e => !(e.Resource is Bundle)).ToList();
        }
        
        private void storeExamples()
        {
            service.Transaction(examples);
        }

        /// <summary>
        /// Reinitializes the (database of) the server to its initial state
        /// </summary>
        /// <returns></returns>
        /// <remarks>Quite a destructive operation, mostly useful in debugging situations</remarks>
      
        public string Initialize(string exampleszip)
        {
            this.zipfile = exampleszip;
            //Note: also clears the counters collection, so id generation starts anew and
            //clears all stored binaries at Amazon S3.
            
            double time_cleaning = Performance.Measure(store.Clean);
            double time_loading = Performance.Measure(importExamples);
            double time_storing = Performance.Measure(storeExamples);

            string message = String.Format(
                "Database was succesfully re-initialized. \nTime spent:"+
                "\nCleaning: {0}sec \nLoading examples: {1}sec, \nStoring: {2}sec", 
                time_cleaning, time_loading, time_storing);
            
            return message;
        }
        
        public string Clean()
        {
            double time_cleaning = Performance.Measure(store.Clean);
            
            string message = String.Format(
                "Database was succesfully cleaned. \nTime spent:" +
                "\nCleaning: {0}sec.",
                time_cleaning);

            return message;
        }
      
    }

    internal static class Performance
    {
        public static int Measure(Action action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            action();

            stopwatch.Stop();
            return stopwatch.Elapsed.Seconds;

        }
    }
}