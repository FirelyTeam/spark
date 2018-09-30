using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Hl7.Fhir.Utility;
using Hl7.Fhir.Model;
using MongoDB.Bson;

using Spark.Engine.Core;
using Spark.Mongo.Search.Common;
using Spark.Engine.Extensions;
using Spark.Search.Mongo;

namespace Spark.Mongo.Search.Indexer
{
    public class BsonIndexDocumentBuilder
    {
        public string RootId;
        private BsonDocument document;

        //public Document(MongoCollection<BsonDocument> collection, Definitions definitions)
        public BsonIndexDocumentBuilder(IKey key)
        {
            //this.definitions = definitions;
            this.document = new BsonDocument();
            this.RootId = key.TypeName + "/" + key.ResourceId;
        }

        public BsonDocument ToDocument()
        {
            return document;
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

        public void Write(String parameterName, FhirDateTime fhirDateTime)
        {
            BsonDocument value = new BsonDocument();
            value.Add(new BsonElement("start", BsonDateTime.Create(fhirDateTime.LowerBound())));
            value.Add(new BsonElement("end", BsonDateTime.Create(fhirDateTime.UpperBound())));
            document.Write(parameterName, value);
        }

        public void Write(Definition definition, FhirDateTime fhirDateTime)
        {
            Write(definition.ParamName, fhirDateTime);
        }

        public void Write(Definition definition, Code code)
        {
            if (code != null)
            {
                Write(definition, code.Value);
            }
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
                    return this.RootId + "#" + code;
                }
            }
            return uri.ToString();
        }

        public void Write(Definition definition, string value)
        {
            if (definition.Argument != null)
                value = definition.Argument.GroomElement(value);
            if (definition.ParamType == SearchParamType.Token && value != null)
            {
                var tokenValue = new BsonDocument
                {
                    {"code", value}
                };

                document.Write(definition.ParamName, tokenValue);
            }
            else
            {
                document.Write(definition.ParamName, value);
            }
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

        public void WriteMetaData(IKey key, int level, Resource resource)
        {
            if (level == 0)
            {
                document.Write(InternalField.ID, this.RootId);

                string selflink = key.ToUriString();
                document.Write(InternalField.SELFLINK, selflink);

                document.Write(InternalField.JUSTID, key.ResourceId);

                var fdt = resource.Meta.LastUpdated.HasValue ? new FhirDateTime(resource.Meta.LastUpdated.Value) : FhirDateTime.Now();
                Write(InternalField.LASTUPDATED, fdt);

                /*
                    //For testing purposes:
                    string term = resloc.Id;.
                    List<Tag> tags = new List<Tag>() { new Tag(term, "http://tags.hl7.org", "labello"+term) } ;
                    tags.ForEach(Collect);
                /* */

                // DSTU2: tags
                //if (entry.Tags != null)
                //{
                //    entry.Tags.ToList().ForEach(Collect);
                //}

            }
            else
            {

                string id = resource.Id;
                document.Write(InternalField.ID, this.RootId + "#" + id);
            }

            string category = resource.TypeName;
            //ModelInfo.GetResourceNameForType(resource.GetType()).ToLower();
            document.Write(InternalField.RESOURCE, category);
            document.Write(InternalField.LEVEL, level);
        }

        public void Write(Definition definition, List<FhirString> list)
        {
            foreach (FhirString fs in list)
            {
                Write(definition, Cast(fs));
            }
        }

        public void Write(Definition definition, Enum item)
        {
            var coding = new Coding();
            coding.Code = item.GetLiteral();
            Write(definition, coding);
        }

        // DSTU2: tags
        //public void Collect(Tag tag)
        //{
        //    string scheme = Assigned(tag.Scheme) ? tag.Scheme.ToString() : null;
        //    string term = tag.Term;
        //    string label = tag.Label;
        //    //string tagstring = glue("/", scheme, term);
        //    BsonDocument value = new BsonDocument()
        //        {
        //            { "scheme", scheme },
        //            { "term", term },
        //            { "label", label }
        //        };
        //    Write(InternalField.TAG, value);
        //}

        public void Write(Definition definition, Quantity quantity)
        {
            switch (definition.ParamType)
            {
                case SearchParamType.Quantity:
                    {
                        BsonDocument block = quantity.ToBson();
                        document.Write(definition.ParamName, block);
                        break;
                    }
                case SearchParamType.Date:
                    {
                        break;
                    }
                default: return;
            }


        }

        public void Write(Definition definition, Coding coding)
        {
            BsonValue system = (coding.System != null) ? (BsonValue)coding.System : BsonNull.Value;
            BsonValue code = (coding.Code != null) ? (BsonValue)coding.Code : BsonNull.Value;

            var value = new BsonDocument
                {
                    { "system", system, system != null },
                    { "code", code },
                    { "display", coding.Display, coding.Display != null }
                };

            document.Write(definition.ParamName, value);
        }

        public void Write(Definition definition, Identifier identifier)
        {
            BsonValue system = (identifier.System != null) ? (BsonValue)identifier.System : BsonNull.Value;
            BsonValue code = (identifier.Value != null) ? (BsonValue)identifier.Value : BsonNull.Value;
            
            var value = new BsonDocument
                {
                    { "system", system },
                    { "code", code },
                    // eigenlijk moet het ook een Display bevatten (om dat search daarop kan zoeken bij een token)
                };
            document.Write(definition.ParamName, value);
        }

        public void Write(Definition definition, ContactPoint contact)
        {
            Write(definition, Cast(contact.ValueElement));
        }

        public void Write(Definition definition, Address address)
        {
            Write(definition, address.City);
            Write(definition, address.Country);
            Write(definition, address.Line); // ienumerable
            Write(definition, address.State);
            Write(definition, address.Text);
            Write(definition, address.Use.ToString());
            Write(definition, address.PostalCode);
        }

        public void Write(Definition definition, HumanName name)
        {
            Write(definition, name.Given);
            Write(definition, name.Prefix);
            Write(definition, name.Family);
            Write(definition, name.Suffix);
            //Write(definition, name.Use.ToString());
        }

        public void Write(Definition definition, CodeableConcept concept)
        {
            document.Write(definition.ParamName + "_text", concept.Text);
            if (concept.Coding != null)
            {
                foreach (Coding coding in concept.Coding)
                {
                    Write(definition, coding);
                }
            }
        }

        public void Write(String parameterName, Period period)
        {
            BsonDocument value = new BsonDocument();
            if (period.StartElement != null)
                value.Add(new BsonElement("start", BsonDateTime.Create(period.StartElement.LowerBound())));
            if (period.EndElement != null)
                value.Add(new BsonElement("end", BsonDateTime.Create(period.EndElement.UpperBound())));
            document.Write(parameterName, value);
        }

        public void Write(Definition definition, Period period)
        {
            Write(definition.ParamName, period);
        }

        private void LogNotImplemented(object item)
        {
            if (!(item is string || item is Uri || item.GetType().IsEnum))
            {
                Debug.WriteLine("Not implemented type: " + item.GetType().ToString());
            }
        }

        public void Write<T>(Definition definition, Code<T> code) where T : struct
        {
            InvokeWrite(definition, code.Value);
        }

        public void InvokeWrite(Definition definition, object item)
        {
            if (item != null)
            {
                Type type = item.GetType();
                MethodInfo m = this.GetType().GetMethod("Write", new Type[] { typeof(Definition), type });
                if (m != null)
                {
                    var result = m.Invoke(this, new object[] { definition, item });
                }
                else
                {
                    string result = null;
                    m = typeof(BsonIndexDocumentBuilder).GetMethod("Cast", new Type[] { type });
                    if (m != null)
                    {
                        var cast = m.Invoke(this, new object[] { item });
                        result = (string)cast;
                    }
                    else
                    {
                        result = item.ToString();
                        LogNotImplemented(item);
                    }
                    Write(definition, result);
                }
            }
            else
            {
                Write(definition, (string)null);
            }
        }

    }

}
