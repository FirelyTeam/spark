/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public static partial class ResourceManipulationOperationFactory
    {
        private class PostManipulationOperation : ResourceManipulationOperation
        {
            public PostManipulationOperation(Resource resource, IKey operationKey, SearchResults searchResults, SearchParams searchCommand = null)
                : base(resource, operationKey, searchResults, searchCommand)
            {
            }

            public static Uri ReadSearchUri(Bundle.EntryComponent entry)
            {
                if (string.IsNullOrEmpty(entry.Request?.IfNoneExist) == false)
                {
                    return new Uri(string.Format("{0}?{1}", entry.Resource.TypeName, entry.Request.IfNoneExist), UriKind.Relative);
                }
                return null;
            }

            protected override IEnumerable<Entry> ComputeEntries()
            {
                Entry postEntry = null;
                if (SearchResults != null)
                {
                    if (SearchResults.Count > 1)
                        throw new SparkException(HttpStatusCode.PreconditionFailed, 
                           string.Format( "Multiple matches found when trying to resolve conditional create. Client's criteria were not selective enough.{0}", 
                           GetSearchInformation()));
                    string localKeyValue = SearchResults.SingleOrDefault();
                    //throw exception. probably we should manually throw this in order to add fhir specific details
                    if (string.IsNullOrEmpty(localKeyValue) == false)
                    {
                        Key localKey = Core.Key.ParseOperationPath(localKeyValue);
                        postEntry = Entry.Create(Bundle.HTTPVerb.GET, localKey, null);
                    }
                }
                postEntry = postEntry ?? Entry.POST(OperationKey, Resource);

                yield return postEntry;
            }
        }
    }
}