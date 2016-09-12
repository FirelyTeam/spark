/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using Spark.Core;
using System.Xml.Linq;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Auxiliary;

namespace Spark.Service
{
    /// <summary>
    /// Import can map id's and references  that are local to the Spark Server to absolute id's and references in outgoing Interactions.
    /// </summary>
    internal class Export
    {
        ILocalhost localhost;
        List<Entry> entries;

        public Export(ILocalhost localhost)
        {
            this.localhost = localhost;
            entries = new List<Entry>();
        }

        public void Add(Entry interaction)
        {
            if (interaction != null && interaction.State == EntryState.Undefined)
            {
                entries.Add(interaction);
            }
        }

        public void Add(IEnumerable<Entry> set)
        {
            foreach (Entry interaction in set)
            {
                Add(interaction);
            }
        }

        public void Externalize()
        {
            ExternalizeKeys();
            ExternalizeReferences();
            ExternalizeState();
        }

        void ExternalizeState()
        {
            foreach (Entry entry in this.entries)
            {
                entry.State = EntryState.External;
            }
        }

        void ExternalizeKeys()
        {
            foreach(Entry entry in this.entries)
            {
                ExternalizeKey(entry);
            }
        }

        void ExternalizeReferences()
        {
            foreach(Entry entry in this.entries)
            {
                if (entry.Resource != null)
                {
                    ExternalizeReferences(entry.Resource);
                }
            }
        }

        void ExternalizeKey(Entry entry)
        {
            entry.SupplementBase(localhost.DefaultBase);
        }

        void ExternalizeReferences(Resource resource)
        {
            Visitor action = (element, name) =>
            {
                if (element == null) return;

                if (element is ResourceReference)
                {
                    ResourceReference reference = (ResourceReference)element;
                    reference.Url = ExternalizeReference(reference.Url);
                }
                else if (element is FhirUri)
                {
                    FhirUri uri = (FhirUri)element;
                    uri.Value = ExternalizeReference(uri.Value);
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

        //Key ExternalizeReference(Key original)
        //{
        //    KeyKind triage = (localhost.GetKeyKind(original));
        //    if (triage == KeyKind.Foreign | triage == KeyKind.Temporary)
        //    {
        //        Key replacement = mapper.TryGet(original);
        //        if (replacement != null)
        //        {
        //            return replacement;
        //        }
        //        else
        //        {
        //            throw new SparkException(HttpStatusCode.Conflict, "This reference does not point to a resource in the server or the current transaction: {0}", original);
        //        }
        //    }
        //    else if (triage == KeyKind.Local)
        //    {
        //        return original.WithoutBase();
        //    }
        //    else
        //    {
        //        return original;
        //    }
        //}

        Uri ExternalizeReference(Uri uri)
        {
            if (uri == null)
            {
                return null;
            }
            else if (!uri.IsAbsoluteUri)
            {
                var absoluteUri = localhost.Absolute(uri);
                if (absoluteUri.Fragment == uri.ToString()) //don't externalize uri's that are just anchor fragments
                {
                    return uri;
                }
                else
                {
                    return absoluteUri;
                }
            }
            else
            {
                return uri;
            }
        }

        String ExternalizeReference(String uristring)
        {
            if (String.IsNullOrWhiteSpace(uristring)) return uristring;

            Uri uri = new Uri(uristring, UriKind.RelativeOrAbsolute);
            return ExternalizeReference(uri).ToString();
        }

        string FixXhtmlDiv(string div)
        {
            try
            {
                XDocument xdoc = XDocument.Parse(div);
                xdoc.VisitAttributes("img", "src", (n) => n.Value = ExternalizeReference(n.Value));
                xdoc.VisitAttributes("a", "href", (n) => n.Value = ExternalizeReference(n.Value));
                return xdoc.ToString();

            }
            catch
            {
                // illegal xml, don't bother, just return the argument
                // todo: should we really allow illegal xml ? /mh
                return div;
            }

        }

        /*
        public void RemoveBodyFromEntries(List<Entry> entries)
        {
            foreach (Entry entry in entries)
            {
                if (entry.IsResource())
                {
                    entry.Resource = null;
                }
            }
        }
        */
    }
}