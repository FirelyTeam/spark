/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Model;
using Spark.Engine.Search;
using Spark.Engine.Search.Model;
using Spark.Engine.Store.Interfaces;
using Spark.Search;
using A = Spark.Engine.Auxiliary;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    /// <summary>
    /// IndexEntry is the collection of indexed values for a resource.
    /// IndexPart is 
    /// </summary>
    public class IndexService
    {
        IFhirModel _fhirModel;
        FhirPropertyIndex _propIndex;
        ResourceVisitor _resourceVisitor;
        ElementIndexer _elementIndexer;
        IIndexStore _indexStore; 

        public IndexService(IFhirModel fhirModel, FhirPropertyIndex propIndex, ResourceVisitor resourceVisitor, ElementIndexer elementIndexer, IIndexStore indexStore)
        {
            _fhirModel = fhirModel;
            _propIndex = propIndex;
            _resourceVisitor = resourceVisitor;
            _elementIndexer = elementIndexer;
            _indexStore = indexStore;
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

        private Resource CloneResource(Resource input)
        {
            return (Resource)FhirParser.ParseFromXml(FhirSerializer.SerializeResourceToXml(input), input.GetType());
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
            //Resource result = (dynamic)resource.DeepCopy(); //CK: This is how it should work, but unfortunately there is an error in the API (#146). So we invent a method of our own. 
            Resource result = CloneResource(resource);

            if (resource is DomainResource)
            {
                var domainResource = (DomainResource)result;
                if (domainResource.Contained != null && domainResource.Contained.Any())
                {
                    var refMap = new Dictionary<string, string>();

                    //Create a unique id for each contained resource.
                    foreach (var containedResource in domainResource.Contained)
                    {
                        var oldRef = "#" + containedResource.Id;
                        var newId = Guid.NewGuid().ToString();
                        containedResource.Id = newId;
                        var newRef = containedResource.TypeName + "/" + newId;
                        refMap.Add(oldRef, newRef);
                    }

                    //Replace references to these contained resources with the newly created id's.
                    A.ResourceVisitor.VisitByType(domainResource,
                         (el, path) =>
                         { var currentRef = (el as ResourceReference);
                             string replacementId;
                             refMap.TryGetValue(currentRef.Reference, out replacementId);
                             if (replacementId != null)
                                 currentRef.Reference = replacementId;
                         }
                        , typeof(ResourceReference));
                }
            }
            return result;
        }

        public IndexValue IndexResource(Resource resource, IKey key)
        {
            var toIndex = MakeContainedReferencesUnique(resource);

            //var toIndex = resource;
            var result = IndexResourceRecursively(toIndex, key);
            _indexStore.Save(result);
            return result;
        }

        private IndexValue IndexResourceRecursively(Resource resource, IKey key, string rootPartName = "root")
        {
            var searchParametersForResource = _fhirModel.FindSearchParameters(resource.GetType());

            if(searchParametersForResource != null)
            {
                var result = new IndexValue(rootPartName);

                AddMetaParts(resource, key, result);

                foreach (var par in searchParametersForResource)
                {
                    var newIndexPart = new IndexValue(par.Code); 
                    foreach (var path in par.GetPropertyPath())
                        _resourceVisitor.VisitByPath(resource,
                            obj => 
                            {
                                if (obj is Element)
                                {
                                    newIndexPart.Values.AddRange(_elementIndexer.Map(obj as Element));
                                }
                            }
                            , path);
                    if (newIndexPart.Values.Any())
                    {
                        result.Values.Add(newIndexPart);
                    }
                }

                if (resource is DomainResource)
                    AddContainedResources((DomainResource)resource, result);

                return result;
            }
            return null;
        }

        private void AddMetaParts(Resource resource, IKey key, IndexValue entry)
        {
            entry.Values.Add(new IndexValue("internal_forResource", new StringValue(key.ToUriString())));
            entry.Values.Add(new IndexValue(IndexFieldNames.RESOURCE, new StringValue(resource.TypeName)));
            entry.Values.Add(new IndexValue(IndexFieldNames.ID, new StringValue(resource.TypeName + "/" + key.ResourceId)));
            entry.Values.Add(new IndexValue(IndexFieldNames.JUSTID, new StringValue(resource.Id)));
            entry.Values.Add(new IndexValue(IndexFieldNames.SELFLINK, new StringValue(key.ToUriString()))); //CK TODO: This is actually Mongo-specific. Move it to Spark.Mongo, but then you will have to communicate the key to the MongoIndexMapper.
            //var fdt = resource.Meta?.LastUpdated != null ? new FhirDateTime(resource.Meta.LastUpdated.Value) : FhirDateTime.Now();
            //entry.Values.Add(new IndexValue(IndexFieldNames.LASTUPDATED, (_elementIndexer.Map(fdt))));
        }

        private void AddContainedResources(DomainResource resource, IndexValue parent)
        {
            parent.Values.AddRange(resource.Contained.Where(c => c is DomainResource).Select(
                c => {
                    IKey containedKey = c.ExtractKey();
                    //containedKey.ResourceId = key.ResourceId + "#" + c.Id;
                    return IndexResourceRecursively((c as DomainResource), containedKey, "contained");
                    }));
        }
    }
}
