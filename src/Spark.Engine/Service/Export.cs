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
    public class Export
    {
        ILocalhost localhost;
        List<Interaction> interactions;

        public Export(ILocalhost localhost)
        {
            this.localhost = localhost;
            interactions = new List<Interaction>();
        }

        public void Add(Interaction interaction)
        {
            if (interaction.State == InteractionState.Undefined)
            {
                interactions.Add(interaction);
            }
        }

        public void Add(IEnumerable<Interaction> set)
        {
            foreach (Interaction interaction in set)
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
            foreach (Interaction interaction in this.interactions)
            {
                interaction.State = InteractionState.External;
            }
        }

        void ExternalizeKeys()
        {
            foreach(Interaction interaction in this.interactions)
            {
                ExternalizeKey(interaction);
            }
        }

        void ExternalizeReferences()
        {
            foreach(Interaction interaction in this.interactions)
            {
                if (interaction.Resource != null)
                {
                    ExternalizeReferences(interaction.Resource);
                }
            }
        }

        void ExternalizeKey(Interaction interaction)
        {
            interaction.SupplementBase(localhost.Base);
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

            ResourceVisitor.VisitByType(resource, action, types);
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
                return localhost.Absolute(uri);
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