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
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Support;

using Spark.Support;
using Spark.Core;
using Spark.Store;


namespace Spark.Service
{
    //TODO: rewrite #local to root-resource-id#local on Import
    public class ResourceImporter // : IResourceImporter
    {
        private IFhirStore store;
        private Uri endpoint;
        public SharedEndpoints SharedEndpoints = new SharedEndpoints();
        private const string CID = "cid";

        public ResourceImporter(IFhirStore store, Uri endpoint)
        {
            this.endpoint = endpoint;
            this.store = store;
            SharedEndpoints.Add(endpoint);
        }

        private Queue<BundleEntry> queue = new Queue<BundleEntry>();

        // The list of hosts + paths that we share id's with:
        // Any resource we import with an url starting with a prefix from this list, will be treated 
        // as an insert/update and use the id specified. Any resource that is not from the list will
        // get a new id assigned based at the first Url in this list.

        public ResourceEntry Import(ResourceEntry entry, Uri key)
        {
            EnqueueResourceEntry(entry);
            return (ResourceEntry)Purge().First();
        }

        public IEnumerable<BundleEntry> Import(IEnumerable<BundleEntry> entries)
        {
            foreach (BundleEntry entry in entries) EnqueueBundleEntry(entry);

            return Purge();
        }

        public void EnqueueResourceEntry(ResourceEntry entry)
        {
            //if (id == null) throw new ArgumentNullException("id");
            //if (!id.IsAbsoluteUri) throw new ArgumentException("Uri for new resource must be absolute");
            
            var location = new ResourceIdentity(entry.Id);
            var title = String.Format("{0} resource with id {1}", location.Collection, location.Id);

            //var newEntry = BundleEntryFactory.CreateFromResource(entry.Resource, id, DateTimeOffset.Now, title);
            //newEntry.Tags = entry.Tags;
            queue.Enqueue(entry);
        }
        
        /*public void QueueNewResourceEntry(string collection, string id, ResourceEntry entry)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (id == null) throw new ArgumentNullException("resource");

            QueueNewResourceEntry(ResourceIdentity.Build(_endpoint, collection, id), entry);
        }
        */

        public  void EnqueueDeletedEntry(Uri key)
        {
            if (key == null) throw new ArgumentNullException("id");
            if (!key.IsAbsoluteUri) throw new ArgumentException("Uri for new resource must be absolute");

            var newEntry = BundleEntryFactory.CreateNewDeletedEntry(key);
            queue.Enqueue(newEntry);
        }

        /*
        public void EnqueueDeletedEntry(string collection, string id)
        {
            var location = ResourceIdentity.Build(endpoint, collection, id);

            EnqueueDeletedEntry(location);
        }
        */

        
        public void EnqueueBundleEntry(BundleEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            if (entry.Id == null) throw new ArgumentNullException("Entry's must have a non-null Id");
            if (!entry.Id.IsAbsoluteUri) throw new ArgumentException("Uri for new resource must be absolute");

            //  Clone entry so we won't be updating our source data
            
            //var newEntry = FhirParser.ParseBundleEntryFromXml(FhirSerializer.SerializeBundleEntryToXml(entry));
            queue.Enqueue(entry);
        }
        

        

        private bool inSharedIdSpace(Uri key)
        {
            // relative uri's are always local to our service. 
            if (!key.IsAbsoluteUri) return true;

            // cid: urls signal placeholder id's and so are never to be considered as true identities
            if( key.Scheme.ToLower() == CID ) return false;

            // Check whether the uri starts with a well-known service path that shares our ID space.
            // Or is an external path that we don't share id's with
            return SharedEndpoints.HasEndpointFor(key);
        }

        // The list of id's that have been reassigned. Maps from original id -> new id.
        Dictionary<Uri, Uri> uriMap = new Dictionary<Uri, Uri>();

        internal Dictionary<Uri, Uri> UriMap { get { return uriMap; } }

        private void ValidateUnicity()
        {
            // First, do a "select distinct" on the entry id's, they may occur more than once.
            var entryIds = queue.Select(ent => ent.Id).GroupBy(uri => uri).Select(group => group.Key);

            // But they cannot be duplicated in the SelfLinks, nor may SelfLinks be repeated
            var allIds = queue
                .Where(ent => ent.SelfLink != null).Select(ent => ent.SelfLink)
                .Concat(entryIds);

            var doubles = allIds.GroupBy(uri => uri.ToString()).Where(g => g.Count() > 1);

            //var list = doubles.ToList();

            if (doubles.Count() > 0)
            {
                throw new ArgumentException("There are entries with duplicate SelfLinks or SelfLinks that are the same as an entry.Id. First one is " +
                        doubles.First().Key.ToString());
            }
        }

        private void moveIdToRelated(BundleEntry entry)
        {
            entry.Links.Alternate = entry.Id;
        }

        /// <summary>
        /// Import all queued Resources by localizing their Id, SelfLink and other referring uri's
        /// </summary>
        /// <returns></returns>
        /// <remarks>Localization means making the Id and SelfLink relative links so they can be stored
        /// without depending on the actual URL of the hosting service). Resource Id will be localized to
        /// resourcename/id and selflinks to resourcename/id/history/vid. Additionally, Id's coming from
        /// outside servers (as specified by the Shared Id Space) and cid:'s will be reassigned a new id.
        /// Any url's and resource references pointing to the localized id's will be updated.</remarks>
        public IEnumerable<BundleEntry> Purge()
        {
            ValidateUnicity();

            foreach(BundleEntry entry in queue.Purge())
            {
                moveIdToRelated(entry);
                localizeEntryIds(entry);
                if (entry is ResourceEntry)
                    fixUriReferences((ResourceEntry)entry);

                yield return entry;
            }



        }

        private string getCollectionNameFromEntry(BundleEntry entry)
        {
            ResourceIdentity identity;
 
            if (entry.Id.Scheme == Uri.UriSchemeHttp)
            {
                identity = new ResourceIdentity(entry.Id);

                if (identity.Collection != null)
                    return identity.Collection;
            }

            if (entry.SelfLink != null && entry.SelfLink.Scheme == Uri.UriSchemeHttp)
            {
                identity = new ResourceIdentity(entry.SelfLink);

                if (identity.Collection != null)
                    return identity.Collection;
                
            }

            if (entry is ResourceEntry)
                return (entry as ResourceEntry).Resource.GetCollectionName();
                //return ((ResourceEntry)entry).Resource.GetType().Name.ToLower();

            throw new InvalidOperationException("Encountered a entry without an id, self-link or content that indicates the resource's type");
        }

        private void localizeEntryIds(BundleEntry entry)
        {
          
            // Did we already reassign this entry.Id within this batch?
            if (!uriMap.ContainsKey(entry.Id))
            {
                Uri localUri;
                ResourceIdentity identity;

                identity = new ResourceIdentity(entry.Id);

                // If we shared this id space, use the relative path as id
                if (inSharedIdSpace(entry.Id) && (identity.Collection != null && identity.Id != null))
                {
                    localUri = identity.OperationPath; 

                    // If we're about to add an entry with a numerical id > than our current
                    // "new record counter", make sure the next new record gets an id 1 higher
                    // than this entries id.
                    int newIdNum = 0;
                    if (Int32.TryParse(identity.Id, out newIdNum))
                        store.EnsureNextSequenceNumberHigherThan(newIdNum);
                
                }
                else
                {
                    // Otherwise, give it a new relative, local id
                    var newResourceId = store.GenerateNewIdSequenceNumber();
                    
                    string collectionName = getCollectionNameFromEntry(entry);
                    localUri = ResourceIdentity.Build(collectionName, newResourceId.ToString());
                }

                uriMap.Add(entry.Id, localUri);
            }

            // Reassign the resultString to our new local resultString
            entry.Id = uriMap[entry.Id];

            // Now, build a new version-specific link (always, no reuse)
            string vid = store.GenerateNewVersionSequenceNumber().ToString();
            var id = new ResourceIdentity(entry.Id).WithVersion(vid);
            

            // If the entry did carry an version-specific resultString originally,
            // keep it in the map so we can update references to it.
            if (entry.SelfLink != null)
                uriMap.Add(entry.SelfLink, id);

            // Assign a new version-specific link to entry
            entry.SelfLink = id;
        }

        private void fixUriReferences(ResourceEntry entry)
        {
            Action<Element, string> action = (element, name) =>
                {
                    if (element == null) return;

                    if (element is ResourceReference)
                    {
                        ResourceReference rr = (ResourceReference)element;
                        if (rr.Url != null)
                            rr.Url = fixUri(rr.Url);
                    }
                    else if (element is FhirUri)
                    { 
                        ((FhirUri)element).Value = fixUri(new Uri( ((FhirUri)element).Value, UriKind.RelativeOrAbsolute)).ToString();
                    }
                    else if (element is Narrative)
                    {
                        ((Narrative)element).Div = fixXhtmlDiv(((Narrative)element).Div);
                    }

                };

            ResourceInspector.VisitByType(entry.Resource, action, 
                typeof(ResourceReference), typeof(FhirUri), typeof(Narrative));
        }

        // todo: This constant has become internal. Please undo. We need it. 
        // Update: new location: XHtml.XHTMLNS / XHtml
        // private XNamespace xhtml = XNamespace.Get(Util.XHTMLNS);
        private XNamespace xhtml = XNamespace.Get("http://www.w3.org/1999/xhtml");

        private string fixXhtmlDiv(string div)
        {
            XDocument xdoc = null;

            try
            {
                xdoc = XDocument.Parse(div);
            }
            catch
            {
                // illegal xml, don't bother, just return the argument
                return div;
            }

            var srcAttrs = xdoc.Descendants(xhtml + "img").Attributes("src");
            foreach (var srcAttr in srcAttrs)
                srcAttr.Value = fixUri(new Uri(srcAttr.Value, UriKind.RelativeOrAbsolute)).ToString();

            var hrefAttrs = xdoc.Descendants(xhtml + "a").Attributes("href");
            foreach (var hrefAttr in hrefAttrs)
                hrefAttr.Value = fixUri(new Uri(hrefAttr.Value, UriKind.RelativeOrAbsolute)).ToString();

            return xdoc.ToString();
        }

        private Uri fixUri(Uri rr)
        {
            if (rr == null) return null;

            // For relative uri's, make them absolute using the service base
            var rrIn = rr.IsAbsoluteUri ? rr : new Uri(endpoint, rr.ToString());
            
            // See if we have remapped this uri
            if (uriMap.ContainsKey(rrIn))
                return uriMap[rrIn];
            else
            {
                // if we encounter a cid Url in the resource that's not in the mapping,
                // we have an orphaned cid, complain about that
                if (rrIn.Scheme.ToLower() == CID)
                {
                    string message = String.Format("Cannot fix cid uri '{0}': " +
                            "the corresponding entry was not found in this scope.", rr);
                    throw new InvalidOperationException(message);
                }

                // If this is a local url, make it relative for storage
                if (rr.ToString().StartsWith(endpoint.ToString()))
                {
                    return new ResourceIdentity(rrIn);
                }

                // Else, do nothing: leave this id as it is
                return rr;
            }
        }

        public void ResolveBaselink(Bundle bundle)
        {
            // todo: this function adds the base link of the bundle to all relative uri's in the entries.
            throw new NotImplementedException();
            // to be used in (FhirService.Transaction)
        }

    }

    public static class QueueExtensions
    {
        public static IEnumerable<T> Purge<T>(this Queue<T> queue)
        {
            while (queue.Count > 0)
            {
                yield return queue.Dequeue();
            }
        }
    }
}
