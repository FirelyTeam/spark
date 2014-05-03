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
using System.Web;

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Introspection;

namespace Spark.Search
{
    public class Document
    {
        private BsonDocument document;
        private string container_id;
        private MongoCollection<BsonDocument> collection;
        
        private Definitions definitions;
        public Document(MongoCollection<BsonDocument> collection, Definitions definitions)
        {
            this.collection = collection;
            this.definitions = definitions;
            this.document = new BsonDocument();
        }

        public void Write(string fieldname, BsonValue value)
        {
            document.Write(fieldname, value);
        }
        public void Write(Definition definition, string value)
        {
            if (definition.Argument != null)
                value = definition.Argument.GroomElement(value);

            Write(definition.ParamName, value);
        }
        public void Write(Definition definition, IEnumerable<string> items)
        {
            if (items != null)
            {
                foreach (string item in items)
                {
                    Write(definition, item);
                }
            }
        }
        public void WriteMetaData(ResourceEntry entry, int level, Resource resource)
        {
            if (level == 0)
            {
                Write(InternalField.ID, container_id);

                string selflink = entry.Links.SelfLink.ToString();
                Write(InternalField.SELFLINK, selflink);

                var resloc = new ResourceIdentity(container_id);
                Write(InternalField.JUSTID, resloc.Id);

                /*
                    //For testing purposes:
                    string term = resloc.Id;
                    List<Tag> tags = new List<Tag>() { new Tag(term, "http://tags.hl7.org", "labello"+term) } ;
                    tags.ForEach(Collect);
                /* */
                
                if (entry.Tags != null)
                {
                    entry.Tags.ToList().ForEach(Collect);
                }
                
            }
            else
            {
                
                string id = resource.Id;
                Write(InternalField.ID, container_id + "#" + id);
            }

            string category = resource.GetCollectionName();
                //ModelInfo.GetResourceNameForType(resource.GetType()).ToLower();
            Write(InternalField.RESOURCE, category);
            Write(InternalField.LEVEL, level);
        }

        public string Cast(FhirString s)
        {
            if (s != null)
                return s.Value;
            else
                return null;
        }
        public string Cast(Resource resource)
        {
            return ModelInfo.GetResourceNameForType(resource.GetType());

        }
        public string Cast(ResourceReference reference)
        {
            if (reference == null) return null;
            if (reference.Url == null) return null;
            string uri = reference.Url.ToString();

            string[] s = uri.ToString().Split('#');
            if (s.Count() == 2)
            {
                string system = s[0];
                string code = s[1];
                if (string.IsNullOrEmpty(system))
                {
                    return container_id + "#" + code;
                }
            }
            return uri.ToString();
        }
        public string Cast(FhirDateTime dt)
        {
            if (dt != null)
                return dt.Value;
            else
                return null;
        }
        public string Cast(FhirUri uri)
        {
            if (uri != null)
                return uri.ToString();
            else
                return null;
        }
        public string Cast(Code code)
        {
            if (code != null)
                return code.Value;
            else
                return null;
        }
       
        public void Collect(Definition definition, List<FhirString> list)
        {
            foreach (FhirString fs in list)
            {
                Write(definition, Cast(fs));
            }
        }

        private string getEnumLiteral(Enum item)
        {
            Type type = item.GetType();
            EnumMapping mapping = EnumMapping.Create(type);
            //todo: Chaching these mappings should probably optimize performance. But for now load seems managable.
            string literal = mapping.GetLiteral(item);
            return literal;
        }

        public void Collect(Definition definition, Enum item)
        {
            var coding = new Coding();
            coding.Code = getEnumLiteral(item);
            Collect(definition, coding);    
        }

        public bool Null(object x)
        {
            return (x == null);
        }
        public bool Assigned(object x)
        {
            return !Null(x);
        }

        private string glue(string glue, string left, string right)
        {
            string result;
            result = left ?? string.Empty;
            if (result != string.Empty && right != string.Empty) 
                result += glue+right;

            return result;
        }
        public void Collect(Tag tag)
        {
            string scheme = Assigned(tag.Scheme) ? tag.Scheme.ToString() : null;
            string term = tag.Term;
            string label = tag.Label;
            //string tagstring = glue("/", scheme, term);
            BsonDocument value = new BsonDocument()
                {
                    { "scheme", scheme },
                    { "term", term },
                    { "label", label }

                };
            Write(InternalField.TAG, value);
        }
        public void Collect(Definition definition, Quantity quantity)
        {
            if (definition.ParamType != Conformance.SearchParamType.Quantity)
                return;
 
            Quantity q = quantity.Standardize();
            string system = (quantity.System != null) ? quantity.System.ToString() : null;
            
            BsonDocument block = new BsonDocument()
                {
                    { "system", system },
                    { "value", q.GetValueAsBson() },
                    { "decimals", q.GetDecimalSearchableValue() },
                    { "unit", q.Units }
                };
            Write(definition.ParamName, block); 
        }

        public void Collect(Definition definition, Coding coding)
        {
            string system = (coding.System != null) ? coding.System.ToString() : null;
            string code = ((coding.Code != null) && (coding.Code != null)) ? coding.Code : null;

            BsonDocument value = new BsonDocument()
                {
                    { "system", system },
                    { "code", code },
                    { "display", coding.Display }
                };
            Write(definition.ParamName, value); 

        }
        public void Collect(Definition definition, Identifier identifier)
        {
            string system = (identifier.System != null) ? identifier.System.ToString() : null;
            string code = (identifier.Value != null) ? identifier.Value: null;

            BsonDocument value = new BsonDocument()
                {
                    { "system", system },
                    { "code", code },
                    // eigenlijk moet het ook een Display bevatten (om dat search daarop kan zoeken bij een token)
                };
            Write(definition.ParamName, value); 
        }
        public void Collect(Definition definition, Contact contact)
        {
            Write(definition, Cast(contact.ValueElement));
        }
        public void Collect(Definition definition, Address address)
        {
            Write(definition, address.City);
            Write(definition, address.Country);
            Write(definition, address.Line); // ienumerable
            Write(definition, address.State);
            Write(definition, address.Text);
            Write(definition, address.Use.ToString());
            Write(definition, address.Zip);
        }
        public void Collect(Definition definition, HumanName name)
        {
            Write(definition, name.Given);
            Write(definition, name.Prefix);
            Write(definition, name.Family);
            Write(definition, name.Suffix);
            //Write(definition, name.Use.ToString());
        }
        public void Collect(Definition definition, CodeableConcept concept)
        {
            Write(definition.ParamName + "_text", concept.Text);
            if (concept.Coding != null)
            {
                foreach (Coding coding in concept.Coding)
                {
                    Collect(definition, coding);
                }
            }
        }
        public void Collect(Definition definition, Period period)
        {
            string start = definition.Argument.GroomElement(period.Start);
            string end = definition.Argument.GroomElement(period.End);

            BsonDocument value = new BsonDocument()
                {
                    { "start", start },
                    { "end", end }
                };
            document.Write(definition.ParamName, value);
        }

        private void ReportNotImplemented(object item)
        {
            if (!(item is string || item is Uri || item.GetType().IsEnum))
            {
                Debug.WriteLine("Not implemented type: " + item.GetType().ToString());
            }
        }
        public void InvokeCollect(Definition definition, object item)
        {
            if (item != null)
            {
                Type type = item.GetType();
                MethodInfo m = this.GetType().GetMethod("Collect", new Type[] { typeof(Definition), type });
                if (m != null)
                {
                    var result = m.Invoke(this, new object[] { definition, item });
                }
                else
                {
                    string result = null;
                    m = this.GetType().GetMethod("Cast", new Type[] { type });
                    if (m != null)
                    {
                        var cast = m.Invoke(this, new object[] { item });
                        result = (string)cast;
                    }
                    else
                    {
                        result = item.ToString();
                        ReportNotImplemented(item);
                    }
                    Write(definition, result);
                }
            }
            else
            {
                Write(definition, (string)null);
            }
        }

        public void Harvest(Resource resource)
        {
            // Get all search parameters that are relevant for this resource
            var matches = definitions.MatchesFor(resource);

            // Harvest the data from the resource that is needed in the index for that parameter
            foreach (var definition in matches)
            {
                definition.Harvest(resource, this.InvokeCollect);
            }
        }
        private void Initialize(ResourceEntry entry)
        {
            container_id = entry.Id.ToString();
        }
        public void Put(ResourceEntry entry, int level, Resource resource)
        {
            Initialize(entry);
            WriteMetaData(entry, level, resource);
            Harvest(resource);
            Save();
        }
        private void Save()
        {
            string keyvalue = document.GetValue(InternalField.ID).ToString();
            IMongoQuery query = MongoDB.Driver.Builders.Query.EQ(InternalField.ID, keyvalue);
            collection.Remove(query);
            collection.Save(document);
        }
    }

    public static class MongoDocExtention
    {
        public static void Write(this BsonDocument document, string field, BsonValue value)
        {
            if (field.StartsWith("_")) field = "PREFIX" + field;

            bool forcearray = (value != null) ? (value.BsonType == BsonType.Document) : false;
            // anders kan er op zo'n document geen $elemMatch gedaan worden.

            BsonElement element;

            if (document.TryGetElement(field, out element))
            {
                if (element.Value.BsonType == BsonType.Array)
                {
                    element.Value.AsBsonArray.Add(value);
                }
                else
                {
                    document.Remove(field);
                    document.Add(field, new BsonArray() { element.Value, value });
                }
            }
            else
            {
                if (forcearray)
                    document.Add(field, new BsonArray() { value });
                else 
                    document.Add(field, value);
            }
        }
    }
    

}