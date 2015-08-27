/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */


namespace Spark.Store.Mongo
{
    public struct IncludeParameter
    {
        public string Resource;
        public string Field;
    }
    public class MongoIncluder
    {
        /*
        private MongoCollection<BsonDocument> collection;
        public MongoIncluder(MongoCollection<BsonDocument> collection)
        {
            this.collection = collection;
        }

        private List<BsonValue> CollectKeys(IMongoQuery query)
        {
            MongoCursor<BsonDocument> cursor = collection.Find(query).SetFields(InternalField.ID);
            return cursor.Select(doc => doc.GetValue(InternalField.ID)).ToList();
        }

        private IEnumerable<BsonValue> CollectForeignKeys(List<BsonValue> keys, string resource, string foreignkey)
        {
            IMongoQuery query = Query.And(Query.EQ(InternalField.RESOURCE, resource), Query.In(InternalField.ID, keys));
            MongoCursor<BsonDocument> cursor = collection.Find(query).SetFields(foreignkey);
            return cursor.Select(doc => doc.GetValue(foreignkey));
        }

               private void Merge(List<BsonValue> keys, IEnumerable<BsonValue> mergekeys)
        {
            IEnumerable<BsonValue> newkeys = mergekeys.Except(keys);
            keys.AddRange(newkeys);
        }

        private void Include(IncludeParameter include, List<BsonValue> keys)
        {
            IEnumerable<BsonValue> foreignkeys = CollectForeignKeys(keys, include.Resource, include.Field);
            Merge(keys, foreignkeys);
        }
        private void Include(List<IncludeParameter> includes, List<BsonValue> keys)
        {
            includes.ForEach(include => Include(include, keys));
        }

        public void Include(ICollection<IncludeParameter> includes, List<BsonValue> keys)
        {

        }

        

        public void Include(Bundle bundle, ICollection<string> includes)
        {
            
            
        }

        private void RecursiveInclude(List<IncludeParameter> includes, List<BsonValue> keys)
        {
            int lastcount, count = 0;
            do
            {
                lastcount = count;
                Include(includes, keys);
                count = keys.Count();
            }
            while (lastcount != count);
        }

        */
    }
}
