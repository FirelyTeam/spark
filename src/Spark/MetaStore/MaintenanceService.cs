/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Linq;
using Spark.Service;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Import;

namespace Spark.MetaStore
{
    public class MaintenanceService
    {
        
        FhirService service;
        ILocalhost localhost;
        IGenerator generator;
        IFhirStore store;
        IFhirIndex index;
        Bundle examples;

        public MaintenanceService(Infrastructure infrastructure, FhirService service)
        {
            this.service = service;
            this.localhost = infrastructure.Localhost;
            this.generator = infrastructure.Generator;
            this.store = infrastructure.Store;
            this.index = infrastructure.Index;
        }
        
        private void storeExamples()
        {
            service.Transaction(examples);
        }

        public void importLimitedExamples()
        {

            examples = Examples.ImportEmbeddedZip().LimitPerType(5).ToBundle(localhost.Base);
        }

        public string Init(string type)
        {
            type = type.ToLower();

            double time_loading = Performance.Measure(importLimitedExamples);
            examples.Entry.RemoveAll(e => e.Resource.TypeName.ToLower() != type);
            double time_storing = Performance.Measure(storeExamples);

            string message = String.Format(
                "Database was succesfully re-initialized. \nTime spent:" +
                "\nLoading {0} {1}s: {2} seconds, \nStoring: {3} seconds",
                examples.Entry.Count(), type, time_loading, time_storing);

            return message;
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

            double time_cleaning = Performance.Measure(store.Clean) + Performance.Measure(index.Clean); 
            double time_loading = Performance.Measure(importLimitedExamples);
            double time_storing = Performance.Measure(storeExamples);

            string message = String.Format(
                "Database was succesfully re-initialized. \nTime spent:"+
                "\nCleaning: {0}sec \nLoading examples: {1}sec, \nStoring: {2}sec", 
                time_cleaning, time_loading, time_storing);
            
            return message;
        }
        
        public string Clean()
        {
            double time_cleaning = Performance.Measure(store.Clean) + Performance.Measure(index.Clean);
            
            string message = String.Format(
                "Database was succesfully cleaned. \nTime spent:" +
                "\nCleaning: {0}sec.",
                time_cleaning);

            return message;
        }
      
    }

}