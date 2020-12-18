using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Model;
using Spark.Engine.Search;
using Spark.Engine.Search.Model;
using Spark.Engine.Store.Interfaces;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class IndexService : IIndexService
    {
        private IFhirModel _fhirModel;
        private IIndexStore _indexStore;
        private ElementIndexer _elementIndexer;

        public IndexService(IFhirModel fhirModel, IIndexStore indexStore, ElementIndexer elementIndexer)
        {
            _fhirModel = fhirModel;
            _indexStore = indexStore;
            _elementIndexer = elementIndexer;
        }

        public void Process(Entry entry)
        {
            if (entry.HasResource())
            {
                IndexResource(entry.Resource, entry.Key);
            }
            else
            {
                if (entry.IsDeleted())
                {
                    _indexStore.Delete(entry);
                }
                else throw new Exception("Entry is neither resource nor deleted");
            }
        }

        public IndexValue IndexResource(Resource resource, IKey key)
        {
            Resource resourceToIndex = MakeContainedReferencesUnique(resource);
            IndexValue indexValue = IndexResourceRecursively(resourceToIndex, key);
            _indexStore.Save(indexValue);
            return indexValue;
        }

        private IndexValue IndexResourceRecursively(Resource resource, IKey key, string rootPartName = "root")
        {
            IEnumerable<SearchParameter> searchParameters = _fhirModel.FindSearchParameters(resource.GetType());

            if (searchParameters == null) return null;

            var rootIndexValue = new IndexValue(rootPartName);
            AddMetaParts(resource, key, rootIndexValue);

            ElementNavFhirExtensions.PrepareFhirSymbolTableFunctions();

            foreach (var searchParameter in searchParameters)
            {
                if (string.IsNullOrWhiteSpace(searchParameter.Expression)) continue;
                // TODO: Do we need to index composite search parameters, some 
                // of them are already indexed by ordinary search parameters so
                // need to make sure that we don't do overlapping indexing.
                if (searchParameter.Type == Hl7.Fhir.Model.SearchParamType.Composite) continue;

                var indexValue = new IndexValue(searchParameter.Code);
                var resolvedValues = resource.SelectNew(searchParameter.Expression);
                foreach(var value in resolvedValues)
                {
                    Element element = value as Element;
                    if (element == null) continue;

                    indexValue.Values.AddRange(_elementIndexer.Map(element));
                }
                if (indexValue.Values.Any())
                    rootIndexValue.Values.Add(indexValue);
            }

            if (resource is DomainResource)
                AddContainedResources((DomainResource)resource, rootIndexValue);

            return rootIndexValue;
        }

        /// <summary>
        /// The id of a contained resource is only unique in the context of its 'parent'. 
        /// We want to allow the indexStore implementation to treat the IndexValue that comes from the contained resources just like a regular resource.
        /// Therefore we make the id's globally unique, and adjust the references that point to it from its 'parent' accordingly.
        /// This method trusts on the knowledge that contained resources cannot contain any further nested resources. So one level deep only.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns>A copy of resource, with id's of contained resources and references in resource adjusted to unique values.</returns>
        private Resource MakeContainedReferencesUnique(Resource resource)
        {
            //We may change id's of contained resources, and don't want that to influence other code. So we make a copy for our own needs.
            Resource result = (dynamic)resource.DeepCopy();
            if (resource is DomainResource)
            {
                DomainResource domainResource = (DomainResource)result;
                if (domainResource.Contained != null && domainResource.Contained.Any())
                {
                    var referenceMap = new Dictionary<string, string>();

                    // Create a unique id for each contained resource.
                    foreach (var containedResource in domainResource.Contained)
                    {
                        string oldRef = "#" + containedResource.Id;
                        string newId = Guid.NewGuid().ToString();
                        containedResource.Id = newId;
                        string newRef = containedResource.TypeName + "/" + newId;
                        referenceMap.Add(oldRef, newRef);
                    }

                    // Replace references to these contained resources with the newly created id's.
                    Auxiliary.ResourceVisitor.VisitByType(
                        domainResource,
                         (el, path) => {
                             ResourceReference currentRefence = (el as ResourceReference);
                             if (!string.IsNullOrEmpty(currentRefence.Reference))
                             {
                                 referenceMap.TryGetValue(currentRefence.Reference, out string replacementId);
                                 if (replacementId != null)
                                     currentRefence.Reference = replacementId;
                             }
                         },
                         typeof(ResourceReference));
                }
            }
            return result;
        }

        private void AddContainedResources(DomainResource resource, IndexValue parent)
        {
            parent.Values.AddRange(
                resource.Contained.Where(c => c is DomainResource)
                .Select(c =>
                {
                    IKey containedKey = c.ExtractKey();
                    return IndexResourceRecursively((c as DomainResource), containedKey, "contained");
                })
            );
        }

        private void AddMetaParts(Resource resource, IKey key, IndexValue entry)
        {
            entry.Values.Add(new IndexValue("internal_forResource", new StringValue(key.ToUriString())));
            entry.Values.Add(new IndexValue(IndexFieldNames.RESOURCE, new StringValue(resource.TypeName)));
            entry.Values.Add(new IndexValue(IndexFieldNames.ID, new StringValue(resource.TypeName + "/" + key.ResourceId)));
            entry.Values.Add(new IndexValue(IndexFieldNames.JUSTID, new StringValue(resource.Id)));
            entry.Values.Add(new IndexValue(IndexFieldNames.SELFLINK, new StringValue(key.ToUriString()))); //CK TODO: This is actually Mongo-specific. Move it to Spark.Mongo, but then you will have to communicate the key to the MongoIndexMapper.
        }
    }
}
