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
        private class PostManipulationOperation : ResourceManipulationOperation
        {
            public PostManipulationOperation(Resource resource, IKey operationKey, SearchResults command)
                : base(Bundle.HTTPVerb.POST, resource, operationKey, command)
            {
            }

            public static Uri ReadSearchUri(Bundle.EntryComponent entry)
            {
                return new Uri(string.Format("{0}?{1}", entry.TypeName, entry.Request.IfNoneExist, UriKind.Relative));
            }

            protected override IEnumerable<Entry> ComputeEntries()
            {
                Entry postEntry = null;
                if (SearchCommand != null)
                {
                    if (SearchCommand.Count > 1)
                        throw new SparkException(HttpStatusCode.PreconditionFailed, "Multiple matches found when trying to resolve conditional create. Client's criteria were not selective enough.");
                    string localKeyValue = SearchCommand.SingleOrDefault();
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