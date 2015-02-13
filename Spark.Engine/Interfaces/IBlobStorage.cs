/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

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
