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
using Spark.Engine.Interfaces;
using Spark.Import;

namespace Spark.MetaStore
{
    public class MaintenanceService
    {
        
        private readonly IFhirService _fhirService;
        private readonly IFhirStoreAdministration _fhirStoreAdministration;
        private readonly IFhirIndex _fhirIndex;
        private Bundle _examples;

        [Obsolete("Use constructor with signature ctor(IFhirService, IFhirStoreAdministration, IFhirIndex)")]
        public MaintenanceService(IFhirService fhirService, ILocalhost localhost, IGenerator keyGenerator, IFhirStoreAdministration fhirStoreAdministration, IFhirIndex fhirIndex)
            :this(fhirService, fhirStoreAdministration, fhirIndex)
        {
        }
        
        public MaintenanceService(IFhirService fhirService, IFhirStoreAdministration fhirStoreAdministration, IFhirIndex fhirIndex)
        {
            _fhirService = fhirService;
           _fhirStoreAdministration = fhirStoreAdministration;
           _fhirIndex = fhirIndex;
        }

        private void StoreExamples()
        {
            _fhirService.Transaction(_examples);
        }

        [Obsolete("Use method with signature ImportLimitedExamples().")]
        public void importLimitedExamples()
        {
            ImportLimitedExamples();
        }

        public void ImportLimitedExamples()
        {
            _examples = Examples.ImportEmbeddedZip(Settings.ExamplesFilePath).LimitPerType(5).ToBundle();
        }

        public string Init(string type)
        {
            type = type.ToLower();

            double time_loading = Performance.Measure(ImportLimitedExamples);
            _examples.Entry.RemoveAll(e => e.Resource.TypeName.ToLower() != type);
            double time_storing = Performance.Measure(StoreExamples);

            string message = String.Format(
                "Database was succesfully re-initialized. \nTime spent:" +
                "\nLoading {0} {1}s: {2} seconds, \nStoring: {3} seconds",
                _examples.Entry.Count(), type, time_loading, time_storing);

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

            double time_cleaning = Performance.Measure(_fhirStoreAdministration.Clean) + Performance.Measure(_fhirIndex.Clean); 
            double time_loading = Performance.Measure(ImportLimitedExamples);
            double time_storing = Performance.Measure(StoreExamples);

            string message = String.Format(
                "Database was succesfully re-initialized. \nTime spent:"+
                "\nCleaning: {0}sec \nLoading examples: {1}sec, \nStoring: {2}sec", 
                time_cleaning, time_loading, time_storing);
            
            return message;
        }
        
        public string Clean()
        {
            double time_cleaning = Performance.Measure(_fhirStoreAdministration.Clean) + Performance.Measure(_fhirIndex.Clean);
            
            string message = String.Format(
                "Database was succesfully cleaned. \nTime spent:" +
                "\nCleaning: {0}sec.",
                time_cleaning);

            return message;
        }
      
    }

}