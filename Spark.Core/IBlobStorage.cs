using System;
using System.Collections.Generic;

namespace Spark.Core
{
    public interface IBlobStorage : IDisposable
    {
        void Close();
        void Delete(string blobName);
        void Delete(IEnumerable<string> names);
        void DeleteAll();
        byte[] Fetch(string blobName);
        string[] ListNames();
        void Open();
        void Store(string blobName, System.IO.Stream data);
    }
}
