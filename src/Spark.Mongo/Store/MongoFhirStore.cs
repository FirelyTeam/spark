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

using MongoDB.Bson;
using MongoDB.Driver;
using MonQ = MongoDB.Driver.Builders;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;


namespace Spark.Store.Mongo
{

    public class MongoFhirStore : IFhirStore
    {
        MongoDatabase database;
        MongoCollection<BsonDocument> collection;

        public MongoFhirStore(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection(Collection.RESOURCE);
            //this.transaction = new MongoSimpleTransaction(collection);
        }

        public void Add(Entry entry)
        {
            BsonDocument document = SparkBsonHelper.ToBsonDocument(entry);
            Supercede(entry.Key);
            collection.Save(document);
        }

        public  Entry Get(IKey key)
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

        public  IList<Entry> Get(IEnumerable<IKey> identifiers, string sortby = null)
        {
            IList<IKey> indetifiersList = identifiers.ToList();
            var versionedIdentifiers = GetBsonValues(indetifiersList, k => k.HasVersionId());
            var unversionedIdentifiers = GetBsonValues(indetifiersList, k => k.HasVersionId() == false);

            IMongoQuery query = MonQ.Query.Or(GetCurrentVersionQuery(unversionedIdentifiers), GetSpecificVersionQuery(versionedIdentifiers));

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

        private IEnumerable<BsonValue> GetBsonValues(IEnumerable<IKey> identifiers, Func<IKey, bool> keyCondtion)
        {
            return identifiers.Where(keyCondtion).Select(k => (BsonValue)k.ToString());
        }

        private IMongoQuery GetCurrentVersionQuery(IEnumerable<BsonValue> ids)
        {
            var clauses = new List<IMongoQuery>();
            clauses.Add(MonQ.Query.In(Field.REFERENCE, ids));
            clauses.Add(MonQ.Query.EQ(Field.STATE, Value.CURRENT));
            return MonQ.Query.And(clauses);

        }

        private IMongoQuery GetSpecificVersionQuery(IEnumerable<BsonValue> ids)
        {
            var clauses = new List<IMongoQuery>();
            clauses.Add(MonQ.Query.In(Field.PRIMARYKEY, ids));

            return MonQ.Query.And(clauses);
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
