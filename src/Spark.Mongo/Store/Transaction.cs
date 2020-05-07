/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Spark.Store.Mongo
{
    public class MongoTransaction
    {
        string transid = null;

        IMongoCollection<BsonDocument> collection;

        public MongoTransaction(IMongoCollection<BsonDocument> collection)
        {
            this.collection = collection;
        }

        public IEnumerable<BsonValue> KeysOf(IEnumerable<BsonDocument> documents)
        {
            foreach(BsonDocument document in documents)
            {
                BsonValue value = null;
                if (document.TryGetValue(Field.RESOURCEID, out value))
                {
                    yield return value;
                }
            }
        }

        public BsonValue KeyOf(BsonDocument document)
        {
            BsonValue value = null;
            if (document.TryGetValue(Field.RESOURCEID, out value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        private void MarkExisting(BsonDocument document)
        {
            BsonValue id = document.GetValue(Field.RESOURCEID);
            var query = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq(Field.RESOURCEID, id), Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));
            var update = Builders<BsonDocument>.Update.Set(Field.TRANSACTION, transid);
            collection.UpdateMany(query, update);
        }

        public void MarkExisting(IEnumerable<BsonDocument> documents)
        {
            IEnumerable<BsonValue> keys = KeysOf(documents);
            var update = Builders<BsonDocument>.Update.Set(Field.TRANSACTION, transid);
            var query = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT),  Builders<BsonDocument>.Filter.In(Field.RESOURCEID, keys));
            collection.UpdateMany(query, update);
        }

        public void RemoveQueued(string transid)
        {
            var query = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(Field.TRANSACTION, transid),
                Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.QUEUED)
                );
            collection.DeleteMany(query);
        }

        public void RemoveTransaction(string transid)
        {
            var query = Builders<BsonDocument>.Filter.Eq(Field.TRANSACTION, transid);
            var update = Builders<BsonDocument>.Update.Set(Field.TRANSACTION, 0);
            collection.UpdateMany(query, update);
        }
        
        private void PrepareNew(BsonDocument document)
        {
            //document.Remove(Field.RecordId); voor Fhir-documenten niet nodig
            document.Set(Field.TRANSACTION, transid);
            document.Set(Field.STATE, Value.QUEUED);
        }

        private void PrepareNew(IEnumerable<BsonDocument> documents)
        {
            foreach(BsonDocument doc in documents)
            {
                PrepareNew(doc);
            }
        }
        
        private void BulkUpdateStatus(string transid, string statusfrom, string statusto)
        {
            var query = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq(Field.TRANSACTION, transid), Builders<BsonDocument>.Filter.Eq(Field.STATE, statusfrom));
            var update = Builders<BsonDocument>.Update.Set(Field.STATE, statusto);
            collection.UpdateMany(query, update);
            
        }

        public void Begin()
        {
            transid = Guid.NewGuid().ToString();
        }

        public void Rollback()
        {
            RemoveQueued(this.transid);
            RemoveTransaction(this.transid);
        }

        public void Commit()
        {
            BulkUpdateStatus(transid, Value.CURRENT, Value.SUPERCEDED);
            BulkUpdateStatus(transid, Value.QUEUED, Value.CURRENT);
        }
        
        public void Insert(BsonDocument document)
        {
            MarkExisting(document);
            PrepareNew(document);
            collection.InsertOne(document);
        }

        public void InsertBatch(IList<BsonDocument> documents)
        {
            MarkExisting(documents);
            PrepareNew(documents);
            collection.InsertMany(documents);
        }

        public BsonDocument ReadCurrent(string resourceid)
        {
            var query = 
                Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq(Field.RESOURCEID, resourceid),
                    Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT)
                );
            return collection.Find(query).FirstOrDefault();
        }
        
    }
}
