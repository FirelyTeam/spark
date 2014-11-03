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
using System.Linq;
using System.Web;

namespace Spark.Core
{
    public enum StoreAction { Update, Create, Delete }
    
    public enum OperationScope
    {
        ResourceInstance,
        Resource,
        Server
    }

    /*
        FHIR is HTTP based (POST, GET, PUT, DELETE)
        But refers is CRUD (CREATE, READ, UPDATE, DELETE).
        To keep the interface close to the FHIR definition, we use CRUD.
        But this results in an ambiguous UPDATE. I.e. does it do a Update (CRUD) or an Upsert (HTTP GET).
        For now, we've added the Upsert for that reason. 
        But it would be better, to make a choice in the FHIR Standard
    */

    public interface IFhirService
    {
        ResourceEntry Conformance();

        ResourceEntry Read(string type, string id);
        ResourceEntry VRead(string type, string id, string version);
        ResourceEntry Create(string collection, ResourceEntry entry, string newId = null);
        ResourceEntry Update(string collection, string id, ResourceEntry entry, Uri updatedVersionUri = null);
        void Delete(string collection, string id);

        Bundle Search(string collection, IEnumerable<Tuple<string, string>> parameters, int pageSize);

        Bundle History(DateTimeOffset? since);
        Bundle History(string collection, DateTimeOffset? since);
        Bundle History(string collection, string id, DateTimeOffset? since);

        Bundle Transaction(Bundle postedBundle);
        Bundle Mailbox(Bundle b, Binary body);

        TagList TagsFromServer();
        TagList TagsFromResource(string collection);
        TagList TagsFromInstance(string collection, string id);
        TagList TagsFromHistory(string collection, string id, string vid);
        void AffixTags(string collection, string id, IEnumerable<Tag> tags);
        void AffixTags(string collection, string id, string vid, IEnumerable<Tag> tags);
        void RemoveTags(string collection, string id, IEnumerable<Tag> tags);
        void RemoveTags(string collection, string id, string vid, IEnumerable<Tag> tags);

        ResourceEntry<OperationOutcome> Validate(string collection, ResourceEntry entry, string id = null);

        Bundle GetSnapshot(string snapshot, int index, int count);
    }
}