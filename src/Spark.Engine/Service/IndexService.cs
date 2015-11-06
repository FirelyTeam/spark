using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
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

        public IndexService(IFhirModel fhirModel, FhirPropertyIndex propIndex, ResourceVisitor resourceVisitor, ElementIndexer elementIndexer)
        {
            _fhirModel = fhirModel;
            _propIndex = propIndex;
            _resourceVisitor = resourceVisitor;
            _elementIndexer = elementIndexer;
        }

        public IndexValue IndexResource(DomainResource resource, IKey key, string rootPartName = "root")
        {
            var searchParametersForResource = _fhirModel.FindSearchParameters(resource.GetType());

            if(searchParametersForResource != null)
            {
                var result = new IndexValue(rootPartName);

                foreach (var par in searchParametersForResource)
                {
                    var newEntryPart = new IndexValue(par.Code); 
                    foreach (var path in par.GetPropertyPath())
                        _resourceVisitor.VisitByPath(resource,
                            obj => 
                            {
                                AddIndexEntryParts(obj, par.Code, result);
                            }
                            , path);
                }
                AddMetaParts(resource, key, result);
                return result;
            }
            return null;
        }

        private void AddIndexEntryParts(object obj, string partName, IndexValue entry)
        {
            //Multiple indexparts could be found for one partName, e.g. in the case of a CodeableConcept (a part for every Coding in it).
            if (obj is Element)
            {
                entry.Values.AddRange(_elementIndexer.ToExpressions((obj as Element)).Select(ex => new IndexValue(partName, ex)));
            }
        }

        private void AddMetaParts(DomainResource resource, IKey key, IndexValue entry)
        {
            entry.Values.Add(new IndexValue(IndexFieldNames.RESOURCE, new StringValue(resource.TypeName)));
            entry.Values.Add(new IndexValue(IndexFieldNames.ID, new StringValue(resource.Id)));
            entry.Values.Add(new IndexValue(IndexFieldNames.SELFLINK, new StringValue(key.ToUriString())));
            var fdt = resource.Meta.LastUpdated.HasValue ? new FhirDateTime(resource.Meta.LastUpdated.Value) : FhirDateTime.Now();
            entry.Values.Add(new IndexValue(IndexFieldNames.LASTUPDATED, new DateValue(fdt.ToDateTimeOffset())));
        }

        private void AddContainedResources(DomainResource resource, IKey key, IndexValue entry)
        {
            entry.Values.AddRange(resource.Contained.Where(c => c is DomainResource).Select(
                c => {
                    IKey containedKey = key.Clone();
                    containedKey.ResourceId = key.ResourceId + "#" + c.Id;
                    return IndexResource((c as DomainResource), containedKey, "contained");
                    }));
        }
    }
}
