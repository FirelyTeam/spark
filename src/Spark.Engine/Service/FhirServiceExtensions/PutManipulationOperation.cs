﻿using System;
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
        private class PutManipulationOperation : ResourceManipulationOperation
        {
            public PutManipulationOperation(Resource resource, IKey operationKey, SearchResults searchResults, SearchParams searchCommand = null) 
                : base(resource, operationKey, searchResults, searchCommand)
            {
            }

            public static Uri ReadSearchUri(Bundle.EntryComponent entry)
            {
                return new Uri(entry.Request.Url, UriKind.RelativeOrAbsolute);
            }

            protected override IEnumerable<Entry> ComputeEntries()
            {
                Entry entry = null;

                if (SearchResults != null)
                {
                    if(SearchResults.Count > 1)
                        throw new SparkException(HttpStatusCode.PreconditionFailed, "Multiple matches found when trying to resolve conditional update. Client's criteria were not selective enough");

                    string localKeyValue = SearchResults.SingleOrDefault();
                    if (localKeyValue != null)
                    {
                        IKey localKey = Key.ParseOperationPath(localKeyValue);

                        entry = Entry.PUT(localKey, Resource); 
                    }
                    else
                    {
                        entry = Entry.POST(OperationKey, Resource);
                    }
                }

                entry = entry ?? Entry.PUT(OperationKey, Resource);
                yield return entry;
            }
        }
    }
}