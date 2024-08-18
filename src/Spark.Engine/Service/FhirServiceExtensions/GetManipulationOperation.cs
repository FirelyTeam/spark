/* 
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class GetManipulationOperation : ResourceManipulationOperation
    {
        public GetManipulationOperation(Resource resource, IKey operationKey, SearchResults searchResults, SearchParams searchCommand = null) 
            : base(resource, operationKey, searchResults, searchCommand)
        {
        }
        
        public static Uri ReadSearchUri(Bundle.EntryComponent entry)
        {
            return entry.Request != null
                ? new Uri(entry.Request.Url, UriKind.RelativeOrAbsolute)
                : null;
        }

        protected override IEnumerable<Entry> ComputeEntries()
        {
            if (SearchResults != null)
            {
                foreach (string localKeyLiteral in SearchResults)
                {
                    yield return Entry.Create(Bundle.HTTPVerb.GET, Key.ParseOperationPath(localKeyLiteral));
                }
            }
            else
            {
                yield return Entry.Create(Bundle.HTTPVerb.GET, OperationKey);
            }
        }
    }
}