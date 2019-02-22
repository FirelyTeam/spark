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
        
        private IFhirService _fhirServiceFull;
        private ILocalhost localhost;
        private IGenerator keyGenerator;
        private IFhirStoreAdministration fhirStoreAdministration;
        private IFhirIndex fhirIndex;
        private Bundle examples;

        public MaintenanceService(IFhirService _fhirServiceFull, ILocalhost localhost, IGenerator keyGenerator, IFhirStoreAdministration fhirStoreAdministration, IFhirIndex fhirIndex)
        {
            this._fhirServiceFull = _fhirServiceFull;
            this.localhost = localhost;
            this.keyGenerator = keyGenerator;
            this.fhirStoreAdministration = fhirStoreAdministration;
            this.fhirIndex = fhirIndex;
        }

        private void storeExamples()
        {
            _fhirServiceFull.Transaction(examples);
        }

        public void importLimitedExamples()
        {

            examples = Examples.ImportEmbeddedZip(Settings.ExamplesFilePath).LimitPerType(5).ToBundle(localhost.DefaultBase);
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

            double time_cleaning = Performance.Measure(fhirStoreAdministration.Clean) + Performance.Measure(fhirIndex.Clean); 
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
            double time_cleaning = Performance.Measure(fhirStoreAdministration.Clean) + Performance.Measure(fhirIndex.Clean);
            
            string message = String.Format(
                "Database was succesfully cleaned. \nTime spent:" +
                "\nCleaning: {0}sec.",
                time_cleaning);

            return message;
        }
      
    }

}