/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2018-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
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

namespace Spark.Service;

/// <summary>
/// Import can map id's and references in incoming entries to id's and references that are local to the Spark Server.
/// </summary>
internal class Import
{
    private readonly Mapper<string, IKey> _mapper;
    private readonly List<Entry> _entries;
    private readonly ILocalhost _localhost;
    private readonly IIdentityGenerator _generator;

    public Import(ILocalhost localhost, IIdentityGenerator generator)
    {
        _localhost = localhost;
        _generator = generator;
        _mapper = new Mapper<string, IKey>();
        _entries = new List<Entry>();
    }

    public void Add(Entry interaction)
    {
        if (interaction != null && (interaction.State == EntryState.Undefined || interaction.State == EntryState.External))
        { 
            _entries.Add(interaction);
        }
    }

    public void AddMappings(Mapper<string, IKey> mappings)
    {
        _mapper.Merge(mappings);
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

    private void InternalizeState()
    {
        foreach (Entry interaction in _entries)
        {
            interaction.State = EntryState.Internal;
        }
    }

    private void InternalizeKeys()
    {
        foreach (Entry interaction in _entries)
        {
            InternalizeKey(interaction);
        }
    }

    private void InternalizeReferences()
    {
        foreach (Entry entry in _entries)
        {
            InternalizeReferences(entry.Resource);
        }
    }

    private IKey Remap(Resource resource)
    {
        Key newKey = _generator.NextKey(resource).WithoutBase();
        AddKeyToInternalMapping(resource.ExtractKey(), newKey);
        return newKey;
    }

    private IKey RemapHistoryOnly(IKey key)
    {
        IKey newKey = _generator.NextHistoryKey(key).WithoutBase();
        AddKeyToInternalMapping(key, newKey);
        return newKey;
    }

    private void AddKeyToInternalMapping(IKey localKey, IKey generatedKey)
    {
        if (_localhost.GetKeyKind(localKey) == KeyKind.Temporary)
        {
            _mapper.Remap(localKey.ResourceId, generatedKey.WithoutVersion());
        }
        else
        {
            _mapper.Remap(localKey.ToString(), generatedKey.WithoutVersion());
        }
    }

    private void InternalizeKey(Entry entry)
    {
        IKey key = entry.Key;

        switch (_localhost.GetKeyKind(key))
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
                    if (entry.Method == Bundle.HTTPVerb.PUT || entry.Method == Bundle.HTTPVerb.PATCH || entry.Method == Bundle.HTTPVerb.DELETE)
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

    private void InternalizeReferences(Resource resource)
    {
        Visitor action = (element, name) =>
        {
            if (element == null) return;

            if (element is ResourceReference reference)
            {
                if (reference.Url != null)
                    reference.Url = new Uri(InternalizeReference(reference.Url.ToString()), UriKind.RelativeOrAbsolute);
            }
            else if (element is FhirUri uri)
            {
                uri.Value = InternalizeReference(uri.Value);
            }
            else if (element is Narrative narrative)
            {
                narrative.Div = FixXhtmlDiv(narrative.Div);
            }

        };

        Type[] types = { typeof(ResourceReference), typeof(FhirUri), typeof(Narrative) };
            
        Engine.Auxiliary.ResourceVisitor.VisitByType(resource, action, types);
    }

    private IKey InternalizeReference(IKey localkey)
    {
        KeyKind triage = (_localhost.GetKeyKind(localkey));
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
        while (_mapper.Exists(replacement.ResourceId))
        {
            KeyKind triage = (_localhost.GetKeyKind(localkey));
            if (triage == KeyKind.Temporary)
            {
                replacement = _mapper.TryGet(replacement.ResourceId);
            }
            else
            {
                replacement = _mapper.TryGet(replacement.ToString());
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

        if (uri.IsTemporaryUri() || _localhost.IsBaseOf(uri))
        {
            IKey key = _localhost.UriToKey(uri);
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