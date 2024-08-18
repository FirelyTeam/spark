/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * Copyright (c) 2018-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using Spark.Core;
using System.Xml.Linq;
using Spark.Engine;
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
        private readonly ILocalhost _localhost;
        private readonly List<Entry> _entries;
        private readonly ExportSettings _exportSettings;

        public Export(ILocalhost localhost, ExportSettings exportSettings)
        {
            _localhost = localhost;
            _exportSettings = exportSettings;
            _entries = new List<Entry>();
        }

        public void Add(Entry interaction)
        {
            if (interaction != null && (interaction.State == EntryState.Undefined || interaction.State == EntryState.Internal))
            {
                _entries.Add(interaction);
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

        private void ExternalizeState()
        {
            foreach (Entry entry in _entries)
            {
                entry.State = EntryState.External;
            }
        }

        private void ExternalizeKeys()
        {
            foreach(Entry entry in _entries)
            {
                ExternalizeKey(entry);
            }
        }

        private void ExternalizeReferences()
        {
            foreach(Entry entry in _entries)
            {
                if (entry.Resource != null)
                {
                    ExternalizeReferences(entry.Resource);
                }
            }
        }

        private void ExternalizeKey(Entry entry)
        {
            entry.SupplementBase(_localhost.DefaultBase);
        }

        private void ExternalizeReferences(Resource resource)
        {
            Visitor action = (element, name) =>
            {
                if (element == null) return;

                if (element is ResourceReference reference)
                {
                    if (reference.Url != null)
                        reference.Url = new Uri(ExternalizeReference(reference.Url.ToString()), UriKind.RelativeOrAbsolute);
                }
                else if (element is FhirUri uri)
                {
                    uri.Value = ExternalizeReference(uri.Value);
                }
                else if (element is Narrative narrative)
                {
                    narrative.Div = FixXhtmlDiv(narrative.Div);
                }

            };

            Type[] types = { typeof(ResourceReference), typeof(FhirUri), typeof(Narrative) };

            Engine.Auxiliary.ResourceVisitor.VisitByType(resource, action, types);
        }

        private string ExternalizeReference(string uristring)
        {
            if (string.IsNullOrWhiteSpace(uristring)) return uristring;

            Uri uri = new Uri(uristring, UriKind.RelativeOrAbsolute);

            if (!uri.IsAbsoluteUri && _exportSettings.ExternalizeFhirUri)
            {
                var absoluteUri = _localhost.Absolute(uri);
                if (absoluteUri.Fragment == uri.ToString()) //don't externalize uri's that are just anchor fragments
                {
                    return uristring;
                }
                else
                {
                    return absoluteUri.ToString();
                }
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
    }
}
