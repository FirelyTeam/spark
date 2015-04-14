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
    public class MaintenanceService
    {
        FhirService service;
        ILocalhost localhost;
        IGenerator generator;
        IFhirStore store;
        Bundle examples;

        public MaintenanceService(ILocalhost localhost, IGenerator generator, IFhirStore store, FhirService service)
        {
            this.service = service;
            this.localhost = localhost;
            this.generator = generator;
            this.store = store;
        }

        private void createConformance()
        {
            Resource conformance = DependencyCoupler.Inject<Conformance>();
            IKey key = generator.NextKey(conformance);
            service.Create(key, conformance);
        }
        
        private void storeExamples()
        {
            service.Transaction(examples);
        }

        private void importExamples()
        {

            examples = Examples.LoadAsBundle(localhost.Base);
        }

        /// <summary>
        /// Reinitializes the (database of) the server to its initial state
        /// </summary>
        /// <returns></returns>
        /// <remarks>Quite a destructive operation, mostly useful in debugging situations</remarks>
        public string Initialize()
        {
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

}