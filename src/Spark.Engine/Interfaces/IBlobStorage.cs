/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace Spark.Core
{
    public interface IBlobStorage : IDisposable
    {
        void Open();
        void Close();
        void Store(string blobName, Stream data);
        void Delete(string blobName);
        void Delete(IEnumerable<string> names);
        byte[] Fetch(string blobName);
        string[] ListNames();
        void DeleteAll();
    }
}
