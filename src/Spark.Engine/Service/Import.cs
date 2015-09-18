/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Hl7.Fhir.Model;

using Spark.Core;
using System.Net;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Auxiliary;

namespace Spark.Service
{
    public class Import
    {
        Mapper<Key, Key> mapper;
        List<Interaction> interactions;
        ILocalhost localhost;
        IGenerator generator;

        public Import(ILocalhost localhost, IGenerator generator)
        {
            this.localhost = localhost;
            this.generator = generator;
            mapper = new Mapper<Key, Key>();
            interactions = new List<Interaction>();
        }

        public void Add(Interaction interaction)
        {
            if (interaction.State == InteractionState.Undefined)
            { 
                interactions.Add(interaction);
            }
            else
            {
                // no need to import again.
                // interaction.State.Assert(InteractionState.Undefined);
            }
        }

        public void Add(IEnumerable<Interaction> interactions)
        {
            foreach (Interaction interaction in interactions)
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
            foreach (Interaction interaction in this.interactions.Transferable())
            {
                interaction.State = InteractionState.Internal;
            }
        }

        void InternalizeKeys()
        {
            foreach (Interaction interaction in this.interactions.Transferable())
            {
                InternalizeKey(interaction);
            }
        }

        void InternalizeReferences()
        {
            foreach (Interaction interaction in interactions.Transferable())
            {
                InternalizeReferences(interaction.Resource);
            }
        }

        Key Remap(Key key)
        {
            Key newKey = generator.NextKey(key).WithoutBase();
            return mapper.Remap(key, newKey);
        }

        Key RemapHistoryOnly(Key key)
        {
            Key newKey = generator.NextHistoryKey(key).WithoutBase();
            return mapper.Remap(key, newKey);
        }

        void InternalizeKey(Interaction interaction)
        {
            if (interaction.IsDeleted) return; 

            Key key = interaction.Key.Clone();

            switch (localhost.GetKeyKind(key))
            {
                case KeyKind.Foreign:
                {
                    interaction.Key = Remap(key);
                    return;
                }
                case KeyKind.Temporary:
                {
                    interaction.Key = Remap(key);
                    return;
                }
                case KeyKind.Local:
                case KeyKind.Internal:
                {
                    if (interaction.Method == Bundle.HTTPVerb.PUT)
                    {
                        interaction.Key = RemapHistoryOnly(key);
                    }
                    else
                    {
                        interaction.Key = Remap(key);
                    }
                    return;

                }
                default:
                {
                    // switch can never get here.
                    throw Error.Internal("Unexpected key for resource: " + interaction.Key.ToString());
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

            ResourceVisitor.VisitByType(resource, action, types);
        }

        Key InternalizeReference(Key original)
        {
            KeyKind triage = (localhost.GetKeyKind(original));
            if (triage == KeyKind.Foreign | triage == KeyKind.Temporary)
            {
                Key replacement = mapper.TryGet(original);
                if (replacement != null)
                {
                    return replacement;
                }
                else
                {
                    throw Error.Create(HttpStatusCode.Conflict, "This reference does not point to a resource in the server or the current transaction: {0}", original);
                }
            }
            else if (triage == KeyKind.Local)
            {
                return original.WithoutBase();
            }
            else
            {
                return original;
            }
        }

        Uri InternalizeReference(Uri uri)
        {
            if (uri == null) return null;

            // If it is a reference to another contained resource don not internalize.
            // BALLOT: this seems very... ad hoc. 
            if (uri.HasFragment()) return uri;
            

            if (localhost.IsBaseOf(uri))
            {
                Key key = localhost.UriToKey(uri);
                return InternalizeReference(key).ToUri();
            }
            else
            {
                return uri;
            }
        }

        String InternalizeReference(String uristring)
        {
            if (String.IsNullOrWhiteSpace(uristring)) return uristring;

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
