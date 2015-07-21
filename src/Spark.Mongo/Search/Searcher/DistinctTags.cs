/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */


namespace Spark.Search.Mongo
{
    // todo: not used. But needs reimplementation
    /*
    public static class ResourceTags
    {
        public static IEnumerable<string> DistinctGlobal()
        {
            // no history!

            MongoDatabase database = MongoDbConnector.Database;
            MongoCollection<BsonDocument> collection = database.GetCollection(Config.MONGOINDEXCOLLECTION);

            return collection.Distinct("internal_tag.term").Select(b => b.ToString());
        }
        public static IEnumerable<string> DistinctPerResourceType(string resource)
        {
            MongoDatabase database = MongoDbConnector.Database;
            MongoCollection<BsonDocument> collection = database.GetCollection(Config.MONGOINDEXCOLLECTION);
            
            IMongoQuery query = Query.EQ(InternalField.RESOURCE, "patient");
            return collection.Distinct("internal_tag.term", query).Select(b => b.ToString());
        }
        public static IEnumerable<string> DistinctGlobal_MapReduced()
        {
            MongoDatabase database = MongoDbConnector.Database;
            MongoCollection<BsonDocument> collection = database.GetCollection(Config.MONGOINDEXCOLLECTION);

            BsonJavaScript map = new BsonJavaScript(
                @"function Map() {
                    for(var i in this.internal_tag)
                    {
                        s = this.internal_tag[i].term;
                        if (!s || s == '') { s = '<none>'; }
                        emit(s, 1);
                    }
                }"
            );

            BsonJavaScript reduce = new BsonJavaScript(
                @"
                function Reduce(key, values) {
                    
                    var res = 0;
                    values.forEach(function(v){ res += 1});
                    return res;
                }
            ");
            MapReduceResult r = collection.MapReduce(map, reduce);
            return r.GetResults().Select(b => b.GetValue("_id").ToString());
        }
        public static IEnumerable<string> OfResource(string id) // "patient/@101"
        {
            // nb. nog niet distinct. maar ga er in test even vanuit dat ze uniek per resource zijn.
            MongoDatabase database = MongoDbConnector.Database;
            MongoCollection<BsonDocument> collection = database.GetCollection(Config.MONGOINDEXCOLLECTION);
            
            BsonDocument document = collection.FindOne(Query.EQ(InternalField.ID, id));
            BsonElement tag;
            if (document.TryGetElement(InternalField.TAG, out tag))
            {
                    return tag.Value.AsBsonArray.Select(b => b.AsBsonDocument.GetValue("term").ToString());
            }
            else
                return null;

        }
    }
    */
}