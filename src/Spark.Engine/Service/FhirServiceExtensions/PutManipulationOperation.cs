using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public static partial class ResourceManipulationOperationFactory
    {
        private class PutManipulationOperation : ResourceManipulationOperation
        {
            public PutManipulationOperation(Resource resource, IKey operationKey, SearchResults command) : base(Bundle.HTTPVerb.DELETE, resource, operationKey, command)
            {
            }

            public static Uri ReadSearchUri(Bundle.EntryComponent entry)
            {
                return new Uri(entry.Request.Url, UriKind.RelativeOrAbsolute);
            }

            protected override IEnumerable<Entry> ComputeEntries()
            {
                Entry entry = null;

                if (SearchCommand != null)
                {
                    if(SearchCommand.Count > 1)
                        throw new SparkException(HttpStatusCode.PreconditionFailed, "Multiple matches found when trying to resolve conditional update. Client's criteria were not selective enough");

                    string localKeyValue = SearchCommand.SingleOrDefault();
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