/*
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2018-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
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
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class IndexService : IIndexService
    {
        private readonly IFhirModel _fhirModel;
        private readonly IIndexStore _indexStore;
        private readonly ElementIndexer _elementIndexer;
        private readonly ResourceResolver _elementResolver;

        public IndexService(IFhirModel fhirModel, IIndexStore indexStore, ElementIndexer elementIndexer, ResourceResolver elementResolver)
        {
            _fhirModel = fhirModel ?? throw new ArgumentNullException(nameof(fhirModel));
            _indexStore = indexStore ?? throw new ArgumentNullException(nameof(indexStore));
            _elementIndexer = elementIndexer ?? throw new ArgumentNullException(nameof(elementIndexer));
            _elementResolver = elementResolver ?? throw new ArgumentNullException(nameof(elementResolver));
        }
        public async Task ProcessAsync(Entry entry)
        {
            if (entry.HasResource())
            {
                await IndexResourceAsync(entry.Resource, entry.Key).ConfigureAwait(false);
            }
            else
            {
                if (entry.IsDeleted())
                {
                    await _indexStore.DeleteAsync(entry).ConfigureAwait(false);
                }
                else throw new Exception("Entry is neither resource nor deleted");
            }
        }

        public async Task<IndexValue> IndexResourceAsync(Resource resource, IKey key)
        {
            Resource resourceToIndex = MakeContainedReferencesUnique(resource);
            IndexValue indexValue = IndexResourceRecursively(resourceToIndex, key);
            await _indexStore.SaveAsync(indexValue).ConfigureAwait(false);
            return indexValue;
        }

        private EvaluationContext GetEvaluationContext(Func<string, ITypedElement> elementResolver = null)
        {
            return new FhirEvaluationContext
            {
                ElementResolver = elementResolver,
            };
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
                IEnumerable<Base> resolvedValues;
                // HACK: Ignoring search parameter expressions which the FhirPath engine does not yet have support for

                try
                {
                    resolvedValues = resource.SelectNew(searchParameter.Expression, new FhirEvaluationContext { ElementResolver = _elementResolver.Resolve });
                }
                catch (Exception)
                {
                    // TODO: log error!
                    resolvedValues = new List<Base>();
                }
                foreach (var value in resolvedValues)
                {
                    if (!(value is Element element)) continue;

                    indexValue.Values.AddRange(_elementIndexer.Map(element));
                }
                if (indexValue.Values.Any())
                    rootIndexValue.Values.Add(indexValue);
            }

            if (resource is DomainResource domainResource)
                AddContainedResources(domainResource, rootIndexValue);

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
