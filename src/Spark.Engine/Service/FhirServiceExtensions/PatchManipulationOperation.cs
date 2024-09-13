/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2020-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Spark.Engine.Service.FhirServiceExtensions;

public static partial class ResourceManipulationOperationFactory
{
    public class PatchManipulationOperation : ResourceManipulationOperation
    {
        public PatchManipulationOperation(Resource resource, IKey operationKey, SearchResults searchResults, SearchParams searchCommand = null) 
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
            Entry entry = null;
            if (SearchResults != null)
            {
                if (SearchResults.Count > 1)
                {
                    throw new SparkException(
                        HttpStatusCode.PreconditionFailed,
                        $"Multiple matches found when trying to resolve conditional create. Client's criteria were not selective enough. {GetSearchInformation()}"
                    );
                }

                var localKeyLiteral = SearchResults.SingleOrDefault();
                if (!string.IsNullOrEmpty(localKeyLiteral))
                {
                    entry = Entry.PATCH(Key.ParseOperationPath(localKeyLiteral), Resource);
                }
            }
            else
            {
                entry = Entry.PATCH(OperationKey, Resource);
            }

            yield return entry;
        }
    }
}