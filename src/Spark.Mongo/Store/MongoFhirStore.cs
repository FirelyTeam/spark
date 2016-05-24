/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;
using System.Linq;

using MongoDB.Bson;
using MongoDB.Driver;
using MonQ = MongoDB.Driver.Builders;

using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Store;
using Spark.Engine.Store.Interfaces;


namespace Spark.Store.Mongo
{

    public class MongoFhirStore : BaseExtensibleStore
    {
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;

        public MongoFhirStore(string mongoUrl, IFhirStoreExtension[] extensions)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection(Collection.RESOURCE);
            foreach (IFhirStoreExtension fhirStoreExtension in extensions)
            {
                this.AddExtension(fhirStoreExtension);
            }
            //this.transaction = new MongoSimpleTransaction(collection);
        }

        public override Entry Get(IKey key)
        {
            var clauses = new List<IMongoQuery>();

            clauses.Add(MonQ.Query.EQ(Field.TYPENAME, key.TypeName));
            clauses.Add(MonQ.Query.EQ(Field.RESOURCEID, key.ResourceId));

            if (key.HasVersionId())
            {
                clauses.Add(MonQ.Query.EQ(Field.VERSIONID, key.VersionId));
            }
            else
            {
                clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            }

            IMongoQuery query = MonQ.Query.And(clauses);

            BsonDocument document = collection.FindOne(query);
            return document.ToEntry();

        }

        public override IList<Entry> Get(IEnumerable<string> identifiers, string sortby = null)
        {
            var clauses = new List<IMongoQuery>();
            IEnumerable<BsonValue> ids = identifiers.Select(i => (BsonValue)i);

            clauses.Add(MonQ.Query.In(Field.PRIMARYKEY, ids));

            IMongoQuery query = MonQ.Query.And(clauses);
            MongoCursor<BsonDocument> cursor = collection.Find(query);

            if (sortby != null)
            {
                cursor = cursor.SetSortOrder(MonQ.SortBy.Ascending(sortby));
            }
            else
            {
                cursor = cursor.SetSortOrder(MonQ.SortBy.Descending(Field.WHEN));
            }

            return cursor.ToEntries().ToList();
        }

        protected override void InternalAdd(Entry entry)
        {
            BsonDocument document = SparkBsonHelper.ToBsonDocument(entry);
            Supercede(entry.Key);
            collection.Save(document);
        }
   

        private void Supercede(IKey key)
        {
            var pk = key.ToBsonReferenceKey();
            IMongoQuery query = MonQ.Query.And(
                MonQ.Query.EQ(Field.REFERENCE, pk),
                MonQ.Query.EQ(Field.STATE, Value.CURRENT)
            );

            IMongoUpdate update = new UpdateDocument("$set",
            new BsonDocument
            {
                { Field.STATE, Value.SUPERCEDED },
            }
            );
            collection.Update(query, update);
        }

    

    }
}
