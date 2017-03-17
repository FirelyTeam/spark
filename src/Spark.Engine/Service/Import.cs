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
using System.Xml.Linq;

using Hl7.Fhir.Model;

using Spark.Core;
using System.Net;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Auxiliary;

namespace Spark.Service
{
    /// <summary>
    /// Import can map id's and references in incoming entries to id's and references that are local to the Spark Server.
    /// </summary>
    internal class Import
    {
        Mapper<string, IKey> mapper;
        List<Entry> entries;
        ILocalhost localhost;
        IGenerator generator;

        public Import(ILocalhost localhost, IGenerator generator)
        {
            this.localhost = localhost;
            this.generator = generator;
            mapper = new Mapper<string, IKey>();
            entries = new List<Entry>();
        }

        public void Add(Entry interaction)
        {
            if (interaction != null && interaction.State == EntryState.Undefined)
            { 
                entries.Add(interaction);
            }
            else
            {
                // no need to import again.
                // interaction.State.Assert(InteractionState.Undefined);
            }
        }

        public void AddMappings(Mapper<string, IKey> mappings)
        {
            mapper.Merge(mappings);
        }
        public void Add(IEnumerable<Entry> interactions)
        {
            foreach (Entry interaction in interactions)
            {
                Add(interaction);
            }
        }

        public void Internalize()
        {
            InternalizeKeys();
            InternalizeReferences();
            InternalizeState();
        }

        void InternalizeState()
        {
            foreach (Entry interaction in this.entries.Transferable())
            {
                interaction.State = EntryState.Internal;
            }
        }

        void InternalizeKeys()
        {
            foreach (Entry interaction in this.entries.Transferable())
            {
                InternalizeKey(interaction);
            }
        }

        void InternalizeReferences()
        {
            foreach (Entry entry in entries.Transferable())
            {
                InternalizeReferences(entry.Resource);
            }
        }

        IKey Remap(Resource resource)
        {
            Key newKey = generator.NextKey(resource).WithoutBase();
            AddKeyToInternalMapping(resource.ExtractKey(), newKey);
            return newKey;
        }

        IKey RemapHistoryOnly(IKey key)
        {
            IKey newKey = generator.NextHistoryKey(key).WithoutBase();
            AddKeyToInternalMapping(key, newKey);
            return newKey;
        }

        private void AddKeyToInternalMapping(IKey localKey, IKey generatedKey)
        {
            if (localhost.GetKeyKind(localKey) == KeyKind.Temporary)
            {
                mapper.Remap(localKey.ResourceId, generatedKey.WithoutVersion());
            }
            else
            {
                mapper.Remap(localKey.ToString(), generatedKey.WithoutVersion());
            }
        }

        void InternalizeKey(Entry entry)
        {
            IKey key = entry.Key;

            switch (localhost.GetKeyKind(key))
            {
                case KeyKind.Foreign:
                {
                    entry.Key = Remap(entry.Resource);
                    return;
                }
                case KeyKind.Temporary:
                {
                    entry.Key = Remap(entry.Resource);
                    return;
                }
                case KeyKind.Local:
                case KeyKind.Internal:
                {
                    if (entry.Method == Bundle.HTTPVerb.PUT || entry.Method == Bundle.HTTPVerb.DELETE)
                    {
                        entry.Key = RemapHistoryOnly(key);
                    }
                    else if(entry.Method == Bundle.HTTPVerb.POST)
                    {
                        entry.Key = Remap(entry.Resource);
                    }
                    return;

                }
                default:
                {
                    // switch can never get here.
                    throw Error.Internal("Unexpected key for resource: " + entry.Key.ToString());
                }
            }
        }
      
        void InternalizeReferences(Resource resource)
        {
            Visitor action = (element, name) =>
            {
                if (element == null) return;

                if (element is ResourceReference)
                {
                    ResourceReference reference = (ResourceReference)element;
                    reference.Url = InternalizeReference(reference.Url);
                }
                else if (element is FhirUri)
                {
                    FhirUri uri = (FhirUri)element;
                    uri.Value = InternalizeReference(uri.Value);
                    //((FhirUri)element).Value = LocalizeReference(new Uri(((FhirUri)element).Value, UriKind.RelativeOrAbsolute)).ToString();
                }
                else if (element is Narrative)
                {
                    Narrative n = (Narrative)element;
                    n.Div = FixXhtmlDiv(n.Div);
                }

            };

            Type[] types = { typeof(ResourceReference), typeof(FhirUri), typeof(Narrative) };
            
            Engine.Auxiliary.ResourceVisitor.VisitByType(resource, action, types);
        }

        IKey InternalizeReference(IKey localkey)
        {
            KeyKind triage = (localhost.GetKeyKind(localkey));
            if (triage == KeyKind.Foreign) throw new ArgumentException("Cannot internalize foreign reference");

            if (triage == KeyKind.Temporary)
            {
                return GetReplacement(localkey);
            }
            else if (triage == KeyKind.Local)
            {
                return localkey.WithoutBase();
            }
            else
            {
                return localkey;
            }
        }

        IKey GetReplacement(IKey localkey)
        {
          
            IKey replacement = localkey;
            //CCR: To check if this is still needed. Since we don't store the version in the mapper, do we ever need to replace the key multiple times? 
            while (mapper.Exists(replacement.ResourceId))
            {
                KeyKind triage = (localhost.GetKeyKind(localkey));
                if (triage == KeyKind.Temporary)
                {
                    replacement = mapper.TryGet(replacement.ResourceId);
                }
                else
                {
                    replacement = mapper.TryGet(replacement.ToString());
                }
            }

            if (replacement != null)
            {
                return replacement;
            }
            else
            {
                throw Error.Create(HttpStatusCode.Conflict, "This reference does not point to a resource in the server or the current transaction: {0}", localkey);
            }
        }

        Uri InternalizeReference(Uri uri)
        {
            if (uri == null) return uri;

            // If it is a reference to another contained resource do not internalize.
            // BALLOT: this seems very... ad hoc. 
            if (uri.HasFragment()) return uri;

            if (uri.IsTemporaryUri() || localhost.IsBaseOf(uri))
            {
                IKey key = localhost.UriToKey(uri);
                return InternalizeReference(key).ToUri();
            }
            else
            {
                return uri;
            }
        }

        string InternalizeReference(string uristring)
        {
            if (string.IsNullOrWhiteSpace(uristring)) return uristring;

            Uri uri = new Uri(uristring, UriKind.RelativeOrAbsolute);
            return InternalizeReference(uri).ToString();
        }

        string FixXhtmlDiv(string div)
        {
            try
            {
                XDocument xdoc = XDocument.Parse(div);
                xdoc.VisitAttributes("img", "src", (n) => n.Value = InternalizeReference(n.Value));
                xdoc.VisitAttributes("a", "href", (n) => n.Value = InternalizeReference(n.Value));
                return xdoc.ToString();

            }
            catch
            {
                // illegal xml, don't bother, just return the argument
                // todo: should we really allow illegal xml ? /mh
                return div;
            }

        }

    }

   
}
