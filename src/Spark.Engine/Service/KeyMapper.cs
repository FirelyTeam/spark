/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;

namespace Spark.Service
{
    public class Mapper<TKEY, TVALUE>
    {
        Dictionary<TKEY, TVALUE> mapping = new Dictionary<TKEY, TVALUE>();

        public Mapper() { }

        public void Clear()
        {
            mapping.Clear();
        }

        public TVALUE TryGet(TKEY key)
        {
            TVALUE value;
            if (mapping.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return default(TVALUE);
            }
        }

        public bool Exists(TKEY key)
        {
            foreach(var item in mapping)
            {
                if (item.Key.Equals(key))
                {
                    return true;
                }
            }
            return false;
        }

        public TVALUE Remap(TKEY key, TVALUE value)
        {
            if (Exists(key)) throw new Exception("Duplicate key");
            mapping.Add(key, value);
            return value;
        }

        public void Merge(Mapper<TKEY, TVALUE> mapper)
        {
            foreach (KeyValuePair<TKEY, TVALUE> keyValuePair in mapper.mapping)
            {
                if (!Exists(keyValuePair.Key))
                {
                    this.mapping.Add(keyValuePair.Key, keyValuePair.Value);
                }
                else if(Exists(keyValuePair.Key) && TryGet(keyValuePair.Key).Equals(keyValuePair.Value) == false)
                {
                    throw new InvalidOperationException("Incompatible mappings");
                }
            }
        }
    }
}
