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

namespace Spark.Service
{
    using System.Threading.Tasks;

    /// <summary>
    /// Import can map id's and references in incoming entries to id's and references that are local to the Spark Server.
    /// </summary>
    internal class Import
    {
        private readonly Mapper<string, IKey> mapper;
        private readonly List<Entry> entries;
        private readonly ILocalhost localhost;
        private readonly IGenerator generator;

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

        public async Task Internalize()
        {
            await InternalizeKeys().ConfigureAwait(false);
            InternalizeReferences();
            InternalizeState();
        }

        private void InternalizeState()
        {
            foreach (Entry interaction in this.entries.Transferable())
            {
                interaction.State = EntryState.Internal;
            }
        }

        private async Task InternalizeKeys()
        {
            foreach (Entry interaction in this.entries.Transferable())
            {
                await InternalizeKey(interaction).ConfigureAwait(false);
            }
        }

        private void InternalizeReferences()
        {
            foreach (Entry entry in entries.Transferable())
            {
                InternalizeReferences(entry.Resource);
            }
        }

        private async Task<IKey> Remap(Resource resource)
        {
            Key newKey = await generator.NextKey(resource).ConfigureAwait(false);
            AddKeyToInternalMapping(resource.ExtractKey(), newKey.WithoutBase());
            return newKey;
        }

        private async Task<IKey> RemapHistoryOnly(IKey key)
        {
            IKey newKey = await generator.NextHistoryKey(key).ConfigureAwait(false);
            AddKeyToInternalMapping(key, newKey.WithoutBase());
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

        private async Task InternalizeKey(Entry entry)
        {
            IKey key = entry.Key;

            switch (localhost.GetKeyKind(key))
            {
                case KeyKind.Foreign:
                    {
                        entry.Key = await Remap(entry.Resource).ConfigureAwait(false);
                        return;
                    }
                case KeyKind.Temporary:
                    {
                        entry.Key = await Remap(entry.Resource).ConfigureAwait(false);
                        return;
                    }
                case KeyKind.Local:
                case KeyKind.Internal:
                    {
                        if (entry.Method == Bundle.HTTPVerb.PUT || entry.Method == Bundle.HTTPVerb.DELETE)
                        {
                            entry.Key = await RemapHistoryOnly(key).ConfigureAwait(false);
                        }
                        else if (entry.Method == Bundle.HTTPVerb.POST)
                        {
                            entry.Key = await Remap(entry.Resource).ConfigureAwait(false);
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

        private void InternalizeReferences(Resource resource)
        {
            void Visitor(Element element, string name)
            {
                if (element == null) return;

                if (element is ResourceReference reference)
                {
                    if (reference.Url != null) reference.Url = new Uri(InternalizeReference(reference.Url.ToString()), UriKind.RelativeOrAbsolute);
                }
                else if (element is FhirUri uri)
                {
                    uri.Value = InternalizeReference(uri.Value);
                }
                else if (element is Narrative n)
                {
                    n.Div = FixXhtmlDiv(n.Div);
                }
            }

            Type[] types = { typeof(ResourceReference), typeof(FhirUri), typeof(Narrative) };

            Engine.Auxiliary.ResourceVisitor.VisitByType(resource, Visitor, types);
        }

        private IKey InternalizeReference(IKey localkey)
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

        private IKey GetReplacement(IKey localkey)
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

        private string InternalizeReference(string uristring)
        {
            if (string.IsNullOrWhiteSpace(uristring)) return uristring;

            Uri uri = new Uri(uristring, UriKind.RelativeOrAbsolute);

            // If it is a reference to another contained resource do not internalize.
            // BALLOT: this seems very... ad hoc.
            if (uri.HasFragment()) return uristring;

            if (uri.IsTemporaryUri() || localhost.IsBaseOf(uri))
            {
                IKey key = localhost.UriToKey(uri);
                return InternalizeReference(key).ToUri().ToString();
            }
            else
            {
                return uristring;
            }
        }

        private string FixXhtmlDiv(string div)
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
