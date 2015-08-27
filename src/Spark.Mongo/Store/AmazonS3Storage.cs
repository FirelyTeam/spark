/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */


namespace Spark.Store.Mongo.AmazonS3
{
    //    public sealed class AmazonS3Storage : IBlobStorage
    //    {
    //        Amazon.S3.IAmazonS3 _client = null;
    //        string accessKey, secretKey, bucketName;

    //        public AmazonS3Storage(string accessKey, string secretKey, string bucketName)
    //        {
    //            this.accessKey = accessKey;
    //            this.secretKey = secretKey;
    //            this.bucketName = bucketName;
    //        }

    //        public void Open()
    //        {
    //            if( string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) )
    //                throw new ArgumentException("AmazonS3 service requires an AWSAccessKey and AWSSecretKey in app.config");

    //            // in the previous version, s3.amazonaws.com was used. This has the same serviceurl. /mh
    //            // http://docs.aws.amazon.com/general/latest/gr/rande.html  
    //            Amazon.RegionEndpoint endpoint = Amazon.RegionEndpoint.USEast1;

    //            _client = Amazon.AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey, endpoint);
    //        }
    //        public void Close()
    //        {
    //            if (_client != null)
    //            {
    //                _client.Dispose();
    //                _client = null;
    //            }
    //        }

    //        public void Store(string blobName, Stream data)
    //        {
    //            PutObjectRequest request = new PutObjectRequest();

    //            request.BucketName = BucketName;
    //            request.Key = blobName;
    //            //request.ContentType = contentType;
    //            request.InputStream = data;
    //                //.WithBucketName(BucketName)
    //                //.WithKey(blobName)
    //                //.WithContentType(contentType)
    ////                .WithMetaData("BatchId", batchId.ToString())
    //                //.WithInputStream(data);

    //            PutObjectResponse response = _client.PutObject(request);
    //        }
    //        public string[] ListNames()
    //        {
    //            ListObjectsRequest request = new ListObjectsRequest();
    //            request.BucketName = BucketName;

    //            // removed: using() / mh
    //            ListObjectsResponse response = _client.ListObjects(request);
    //            return response.S3Objects.Select(s3o => s3o.Key).ToArray();
    //        }
    //        public void DeleteAll()
    //        {
    //            var names = ListNames();

    //            Delete(names);
    //        }
    //        public void Delete(IEnumerable<string> names)
    //        {
    //            if (names.Count() > 0)
    //            {
    //                DeleteObjectsRequest request = new DeleteObjectsRequest();

    //                foreach (var name in names)
    //                    request.AddKey(name);

    //                request.BucketName = BucketName;

    //                DeleteObjectsResponse response = _client.DeleteObjects(request);
    //            }

    //        }
    //        public void Delete(string blobName)
    //        {
    //            DeleteObjectRequest request = new DeleteObjectRequest();
    //            request.BucketName = BucketName;
    //            request.Key = blobName;

    //            DeleteObjectResponse response = _client.DeleteObject(request);
    //        }
    //        public byte[] Fetch(string blobName)
    //        {
    //            GetObjectRequest request = new GetObjectRequest();
    //            request.BucketName = BucketName;
    //            request.Key = blobName;

    //            using (GetObjectResponse response = _client.GetObject(request))
    //            {
    //                return HttpUtil.ReadAllFromStream(response.ResponseStream, (int)response.ContentLength);
    //            }
    //        }
    //        public string BucketName
    //        {
    //            get
    //            {


    //                if (string.IsNullOrEmpty(bucketName))
    //                    return "SparkBlobStorage";
    //                else
    //                    return bucketName;
    //            }
    //        }
    //        public void Dispose()
    //        {
    //            Close();
    //        }
    //    }
}