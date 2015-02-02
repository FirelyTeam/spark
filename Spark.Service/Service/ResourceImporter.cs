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
using System.Net;

namespace Spark.Service
{

    public class ResourceImporter 
    {
        KeyMapper mapper;
        IGenerator generator;

        public ResourceImporter(IGenerator generator)
        {
            this.generator = generator;
            mapper = new KeyMapper();
        }

        private Queue<Entry> queue = new Queue<Entry>();

        public void AssertEmpty()
        {
            if (queue.Count > 0)
            {
                throw new SparkException("Queue expected to be empty.");
            }
        }

        public Entry Import(Entry entry)
        {
            AssertEmpty();
            Enqueue(entry);
            return Purge().First();
            
        }
        
        /*public Entry ImportDeleted(Uri location)
        {
            AssertEmpty();
            EnqueueDelete(location);
            return Purge().First();
        }
        */

        public Entry ImportEntry(Bundle.BundleEntryComponent bundleEntry)
        {
            Entry entry = bundleEntry.CreateEntry();
            
            Key key = entry.Key;
            if (key.ResourceId == null) key.ResourceId = generator.NextKey(entry.Resource);
            if (key.VersionId == null) key.VersionId = generator.NextHistoryKey(key);
            entry.SetKey(key);

            return entry;
        }

        public IEnumerable<Entry> Import(Bundle bundle)
        {
            foreach (var bundleentry in bundle.Entry)
            {
                Entry entry = ImportEntry(bundleentry);
                Enqueue(entry);
            }
            return Purge();
        }

        public void Enqueue(Entry entry)
        {
            //if (entry == null) throw new ArgumentNullException("entry");
            //if (entry.Resource == null) throw new ArgumentNullException("resource");
            //if (entry.Resource.Id == null) throw new ArgumentNullException("Entry's must have a non-null Id");
            //if (!KeyHelper.HasValidLocalKey(entry.Resource))
            //{
            //    throw new ArgumentException("Resource id not valid");
            //}

            Key key = entry.Key;
            
            
            //todo: DSTU2
            //entry.Title = entry.Title ?? title;

            //var newEntry = BundleEntryFactory.CreateFromResource(entry.Resource, id, DateTimeOffset.Now, title);
            //newEntry.Tags = entry.Tags;
            queue.Enqueue(entry);
        }
        
        /*
        public void QueueNewResourceEntry(string collection, string id, ResourceEntry entry)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (id == null) throw new ArgumentNullException("resource");

            QueueNewResourceEntry(ResourceIdentity.Build(_endpoint, collection, id), entry);
        }
        */

        public  void EnqueueDelete(Key key)
        {
            var newEntry = EntryHelper.CreateDeletedEntry(key);
            queue.Enqueue(newEntry);
        }

        /*
        public void EnqueueDeletedEntry(string collection, string id)
        {
            var location = ResourceIdentity.Build(endpoint, collection, id);

            EnqueueDeletedEntry(location);
        }
        */

        
       
        // The list of id's that have been reassigned. Maps from original id -> new id.


        private IEnumerable<Uri> DoubleEntries()
        {
            var keys = queue.Select(ent => ent.Key.ResourceId);
            // todo: DSTU2
            //var selflinks = queue.Where(e => e.SelfLink != null).Select(e => e.SelfLink);
            //var all = keys.Concat(selflinks);

            //IEnumerable<Uri> doubles = all.GroupBy(u => u.ToString()).Where(g => g.Count() > 1).Select(g => g.First());

            //return doubles; 
            return null;
        }

        private void AssertUnicity()
        {
            // todo: DSTU2
            //var doubles = DoubleEntries();
            //if (doubles.Count() > 0)
            //{
            //    string s = string.Join(", ", doubles);
            //    throw new SparkException("There are entries with duplicate SelfLinks or SelfLinks that are the same as an entry.Id: " + s);
            //}

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
        public IList<Entry> Purge()
        {
            lock (queue)
            {
                AssertUnicity();
                //internalizeIds(queue);
                //internalizeReferences(queue);

                var list = new List<Entry>();
                list.AddRange(queue.Purge());
                mapper.Clear();

                return list;
            }
        }


        /*
        public static Uri InternalizeKey(Uri key)
        {
            return (host.KeyNeedsRemap(key)) ? mapper.Remap(key) : Localize(key);

        }
        */

        public void AssertIdAllowed(string id)
        {
            if (id != null)
            {
                bool allowed = generator.KeyAllowed(id);
                if (!allowed)
                    throw new SparkException(HttpStatusCode.Conflict, "A client generated key id is not allowed to have this value ({0})");
            }
        }

        public void AssertKeyAllowed(Uri key)
        {
            AssertIdAllowed(new ResourceIdentity(key).Id);
            
        }
        
        // todo: DSTu2
        /*
        private Uri internalizeKey(Entry entry)
        {
           
            Uri key = entry.Id;
            
            if (KeyHelper.IsCID(key))
            {
                string type = entry.TypeName;
                string _id = generator.NextKey(type);
                return ResourceIdentity.Build(type, _id).OperationPath;

            }
            else if (KeyHelper.IsHttpScheme(key))
            {
                AssertKeyAllowed(key);
                return key.GetOperationpath();
            }
            else 
            {
                throw new SparkException((HttpStatusCode)422, "Id is not a http location or a CID: " + key.ToString());
                
            }
        
        }
        */


        // todo: DSTu2
        /*
        private void internalizeIds(IEnumerable<Entry> entries)
        {
            foreach (Entry entry in queue)
            {
                internalizeIds(entry);
            }
        }
        */

        // todo: DSTU2
        /*
        private void internalizeIds(Entry entry)
        {
            Uri local = internalizeKey(entry);
            Uri history =  generator.HistoryKeyFor(local);

            mapper.Map(entry.Id, local);
            if (entry.SelfLink != null) mapper.Map(entry.SelfLink, history);
            
            entry.OverloadKey(local);
            entry.SelfLink = history;
        }

        private void internalizeReferences(IEnumerable<Entry> entries)
        {
            foreach (BundleEntry entry in queue)
            {
                InternalizeReferences(entry);
            }
        }

        public void InternalizeReferences(Entry entry)
        {
            if (entry is ResourceEntry)
            {
                internalizeReferences((ResourceEntry)entry);
            }
        }

        private void internalizeReferences(Entry entry)
        {
            Visitor action = (element, name) =>
                {
                    if (element == null) return;

                    if (element is ResourceReference)
                    {
                        ResourceReference rr = (ResourceReference)element;
                        if (rr.Url != null)
                            rr.Url = internalizeReference(rr.Url);
                    }
                    else if (element is FhirUri)
                    { 
                        ((FhirUri)element).Value = internalizeReference(new Uri( ((FhirUri)element).Value, UriKind.RelativeOrAbsolute)).ToString();
                    }
                    else if (element is Narrative)
                    {
                        ((Narrative)element).Div = fixXhtmlDiv(((Narrative)element).Div);
                    }

                };
            Type[] types = { typeof(ResourceReference), typeof(FhirUri), typeof(Narrative) };

            ResourceVisitor.VisitByType(entry.Resource, action, types);
        }
        */

        // todo: This constant has become internal. Please undo. We need it. 
        // Update: new location: XHtml.XHTMLNS / XHtml
        // private XNamespace xhtml = XNamespace.Get(Util.XHTMLNS);

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

            var srcAttrs = xdoc.Descendants(Namespaces.XHtml + "img").Attributes("src");
            // todo: DSTU2
            /*
            foreach (var srcAttr in srcAttrs)
                srcAttr.Value = internalizeReference(new Uri(srcAttr.Value, UriKind.RelativeOrAbsolute)).ToString();

            var hrefAttrs = xdoc.Descendants(Namespaces.XHtml + "a").Attributes("href");
            foreach (var hrefAttr in hrefAttrs)
                hrefAttr.Value = internalizeReference(new Uri(hrefAttr.Value, UriKind.RelativeOrAbsolute)).ToString();
            */
            return xdoc.ToString();
        }

        /*
        private Uri internalizeReference(Uri location)
        {
            if (location == null) return null;

            // For relative uri's, make them absolute using the service base
            //reference = reference.IsAbsoluteUri ? reference : new Uri(endpoint, reference.ToString());
            
            // See if we have remapped this uri
            if (mapper.Exists(location))
            {
                return mapper.Get(location);
            }
            else
            {
                // if we encounter a cid Url in the resource that's not in the mapping,
                // we have an orphaned cid, complain about that
                if (KeyHelper.IsCID(location))
                {
                    string message = String.Format("Reference to entry not found: '{0}'", location);
                    throw new InvalidOperationException(message);
                }

                // If this is a local url, make it relative for storage
                else if (host.HasEndpointFor(location))
                {
                    Uri local = KeyHelper.FromLocation(location);
                    // not necessary: mapper.Map(external, local);
                    return local;
                }
                else 
                {
                    return location;
                }
                
            }
        }
        */

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
