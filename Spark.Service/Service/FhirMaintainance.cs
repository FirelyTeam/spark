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
using Spark.Search;
using Spark.Config;
using Spark.Core;
using Spark.Store;

namespace Spark.Service
{
    public class FhirMaintenanceService
    {
        private FhirService _service;
        IFhirStore _store = DependencyCoupler.Inject<IFhirStore>();
        IFhirIndex _index = DependencyCoupler.Inject<IFhirIndex>();

        public FhirMaintenanceService(FhirService service)
        {
            this._service = service;
        }
        /// <summary>
        /// Reinitializes the (database of) the server to its initial state
        /// </summary>
        /// <returns></returns>
        /// <remarks>Quite a destructive operation, mostly useful in debugging situations</remarks>
        public OperationOutcome Initialize()
        {
            //Note: also clears the counters collection, so id generation starts anew and
            //clears all stored binaries at Amazon S3.
            Stopwatch w = new Stopwatch();

            w.Start();
            _store.Clean();
                //_store.EraseData();
                //_store.EnsureIndices();
            
            _index.Clean();

            w.Stop();
            long cleaning = w.ElapsedMilliseconds;

            //Insert our own conformance statement into Conformance collection

            ResourceEntry conformanceentry = ResourceEntry.Create(ConformanceBuilder.Build());

            _service.Upsert(conformanceentry, ConformanceBuilder.CONFORMANCE_COLLECTION_NAME, ConformanceBuilder.CONFORMANCE_ID);

            //Insert standard examples     
            w.Restart();
            var examples = loadExamples();


            var count = examples.Entries.OfType<Condition>().Count();

            w.Stop();
            long loadex = w.ElapsedMilliseconds;

            w.Restart();
            _service.Transaction(examples);
            w.Stop();
            var batch = w.ElapsedMilliseconds;

            
            //Start numbering new resources at an id higher than the examples (we hope)
            //EK: I like the convention of examples having id <10000, and new records >10.000, so please retain
            _store.EnsureNextSequenceNumberHigherThan(9999);


            string message = String.Format("Database was succesfully re-initialized: cleaning {0}, examples {1}, storage {2} ms", cleaning, loadex, batch);
            return new OperationOutcome().Message(message);
        }

        private Bundle loadExamples()
        {
            var examples = new Spark.Support.ExampleImporter();

            examples.ImportZip(Settings.ExamplesFile);

            var batch = BundleEntryFactory.CreateBundleWithEntries("Imported examples", _service.Endpoint, "ExampleImporter", null);

            foreach (var resourceName in ModelInfo.SupportedResources)
            {
                //var key = resourceName.ToLower(); //  the importedEntry keys are no longer in lower // 2013.12.21 mh
                var key = resourceName; 
                if (examples.ImportedEntries.ContainsKey(key))
                {
                    var exampleEntries = examples.ImportedEntries[key];

                    foreach (var exampleEntry in exampleEntries)
                        batch.Entries.Add(exampleEntry);
                }
            }

            return batch;
        }
    }
}