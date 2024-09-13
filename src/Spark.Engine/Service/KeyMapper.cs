/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2017-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;

namespace Spark.Service;

public class Mapper<TKEY, TVALUE>
{
    private readonly Dictionary<TKEY, TVALUE> _mapping = new Dictionary<TKEY, TVALUE>();

    public Mapper() { }

    public void Clear()
    {
        _mapping.Clear();
    }

    public TVALUE TryGet(TKEY key)
    {
        if (_mapping.TryGetValue(key, out TVALUE value))
        {
            return value;
        }
        else
        {
            return default;
        }
    }

    public bool Exists(TKEY key)
    {
        foreach(var item in _mapping)
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
        if (Exists(key))
            _mapping[key] = value;
        else
            _mapping.Add(key, value);
        return value;
    }

    public void Merge(Mapper<TKEY, TVALUE> mapper)
    {
        foreach (KeyValuePair<TKEY, TVALUE> keyValuePair in mapper._mapping)
        {
            if (!Exists(keyValuePair.Key))
            {
                this._mapping.Add(keyValuePair.Key, keyValuePair.Value);
            }
            else if(Exists(keyValuePair.Key) && TryGet(keyValuePair.Key).Equals(keyValuePair.Value) == false)
            {
                throw new InvalidOperationException("Incompatible mappings");
            }
        }
    }
}