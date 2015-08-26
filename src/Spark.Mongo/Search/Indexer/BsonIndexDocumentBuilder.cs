using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Hl7.Fhir.Introspection;
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
        private BsonIndexDocument document;

        //public Document(MongoCollection<BsonDocument> collection, Definitions definitions)
        public BsonIndexDocumentBuilder(IKey key)
        {
            //this.definitions = definitions;
            this.document = new BsonIndexDocument(key);
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
                    return document.RootId + "#" + code;
                }
            }
            return uri.ToString();
        }

        public void Write(Definition definition, string value)
        {
            if (definition.Argument != null)
                value = definition.Argument.GroomElement(value);

            document.Write(definition.ParamName, value);
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
                document.Write(InternalField.ID, document.RootId);

                string selflink = key.ToUriString();
                document.Write(InternalField.SELFLINK, selflink);

                document.Write(InternalField.JUSTID, key.ResourceId);

                /*
                    //For testing purposes:
                    string term = resloc.Id;
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
                document.Write(InternalField.ID, document.RootId + "#" + id);
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

        private string getEnumLiteral(Enum item)
        {
            Type type = item.GetType();
            EnumMapping mapping = EnumMapping.Create(type);
            //todo: Chaching these mappings should probably optimize performance. But for now load seems managable.
            string literal = mapping.GetLiteral(item);
            return literal;
        }

        public void Write(Definition definition, Enum item)
        {
            var coding = new Coding();
            coding.Code = getEnumLiteral(item);
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
                case Conformance.SearchParamType.Quantity:
                {
                    BsonDocument block = quantity.ToBson();
                    document.Write(definition.ParamName, block);
                    break;
                }
                case Conformance.SearchParamType.Date:
                {
                    break;
                }
                default: return;
            }


        }

        public void Write(Definition definition, Coding coding)
        {
            string system = (coding.System != null) ? coding.System.ToString() : null;
            string code = ((coding.Code != null) && (coding.Code != null)) ? coding.Code : null;

            BsonDocument value = new BsonDocument()
                {
                    { "system", system, system != null },
                    { "code", code },
                    { "display", coding.Display, coding.Display != null }
                };

            document.Write(definition.ParamName, value);

        }

        public void Write(Definition definition, Identifier identifier)
        {
            string system = (identifier.System != null) ? identifier.System.ToString() : null;
            string code = (identifier.Value != null) ? identifier.Value : null;

            BsonDocument value = new BsonDocument()
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

        public void Write(Definition definition, Period period)
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

        private void LogNotImplemented(object item)
        {
            if (!(item is string || item is Uri || item.GetType().IsEnum))
            {
                Debug.WriteLine("Not implemented type: " + item.GetType().ToString());
            }
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
