/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Config;
using Spark.Core;
using Spark.Data.AmazonS3;
using Spark.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Store
{
    public static class MongoStoreFactory
    {
        public static IBlobStorage GetAmazonStorage()
        {
            // Create your own non public accounts file as "Spark/Accounts.config". See "Spark/Accounts.config.template"

            try
            {
                string accessKey = Settings.AwsAccessKey;
                string secretKey = Settings.AwsSecretKey;
                string bucketName = Settings.AwsBucketName;

                return new AmazonS3Storage(accessKey, secretKey, bucketName);
            }
            catch
            {
                return null;
            }
        }
        public static MongoFhirStore GetMongoFhirStore()
        {
            return new MongoFhirStore(MongoDbConnector.GetDatabase());
        }


        private static MongoFhirStore storage;

        public static MongoFhirStore GetMongoFhirStorage()
        {
            storage = storage ?? new MongoFhirStore(MongoDbConnector.GetDatabase());
            return storage;
        }

    }
}
