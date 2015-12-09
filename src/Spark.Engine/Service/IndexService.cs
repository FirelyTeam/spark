/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;
using Spark.Engine.Model;
using Spark.Engine.Search;
using Spark.Engine.Search.Model;
using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Service
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

        public void Process(Interaction interaction)
        {
            if (interaction.HasResource())
            {
                IndexResource(interaction.Resource, interaction.Key);
            }
            else
            {
                if (interaction.IsDeleted())
                {
                    _indexStore.Delete(interaction);
                }
                else throw new Exception("Entry is neither resource nor deleted");
            }
        }

        public IndexValue IndexResource(Resource resource, IKey key, string rootPartName = "root")
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
                                    newIndexPart.Values.AddRange(_elementIndexer.Map((obj as Element)));
                                }
                            }
                            , path);
                    if (newIndexPart.Values.Any())
                    {
                        result.Values.Add(newIndexPart);
                    }
                }

                if (resource is DomainResource)
                    AddContainedResources((DomainResource)resource, key, result);

                _indexStore.Save(result);

                return result;
            }
            return null;
        }

        private void AddMetaParts(Resource resource, IKey key, IndexValue entry)
        {
            entry.Values.Add(new IndexValue(IndexFieldNames.RESOURCE, new StringValue(resource.TypeName)));
            entry.Values.Add(new IndexValue(IndexFieldNames.ID, new StringValue(resource.TypeName + "/" + resource.Id)));
            entry.Values.Add(new IndexValue(IndexFieldNames.SELFLINK, new StringValue(key.ToUriString())));
            var fdt = resource.Meta?.LastUpdated != null ? new FhirDateTime(resource.Meta.LastUpdated.Value) : FhirDateTime.Now();
            entry.Values.Add(new IndexValue(IndexFieldNames.LASTUPDATED, (_elementIndexer.Map(fdt))));
        }

        private void AddContainedResources(DomainResource resource, IKey key, IndexValue parent)
        {
            parent.Values.AddRange(resource.Contained.Where(c => c is DomainResource).Select(
                c => {
                    IKey containedKey = key.Clone();
                    containedKey.ResourceId = key.ResourceId + "#" + c.Id;
                    return IndexResource((c as DomainResource), containedKey, "contained");
                    }));
        }
    }
}
