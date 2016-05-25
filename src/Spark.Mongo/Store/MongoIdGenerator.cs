using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Core;
using Spark.Store.Mongo;

namespace Spark.Mongo.Store
{
    public class MongoIdGenerator : IGenerator
    {
        MongoDatabase database;

        public MongoIdGenerator(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
        }
        string IGenerator.NextResourceId(string resource)
        {
            string id = this.Next(resource);
            return string.Format(Format.RESOURCEID, id);
        }

        string IGenerator.NextVersionId(string resource)
        {
            string name = resource + "_history";
            string id = this.Next(name);
            return string.Format(Format.VERSIONID, id);
        }

        string IGenerator.NextVersionId(string resourceType, string resourceIdentifier)
        {
            return ((IGenerator)(this)).NextVersionId(resourceType);
        }

        public string Next(string name)
        {
            var collection = database.GetCollection(Collection.COUNTERS);

            FindAndModifyArgs args = new FindAndModifyArgs();
            args.Query = MongoDB.Driver.Builders.Query.EQ("_id", name);
            args.Update = MongoDB.Driver.Builders.Update.Inc(Field.COUNTERVALUE, 1);
            args.Fields = MongoDB.Driver.Builders.Fields.Include(Field.COUNTERVALUE);
            args.Upsert = true;
            args.VersionReturned = FindAndModifyDocumentVersion.Modified;

            FindAndModifyResult result = collection.FindAndModify(args);
            BsonDocument document = result.ModifiedDocument;

            string value = document[Field.COUNTERVALUE].AsInt32.ToString();
            return value;
        }
        public static class Format
        {
            public static string RESOURCEID = "spark{0}";
            public static string VERSIONID = "spark{0}";
        }
    }
}