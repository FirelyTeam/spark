/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2017-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;

namespace Spark.Engine.Service;

public class Mapper<TKey, TValue>
{
    private readonly Dictionary<TKey, TValue> _mapping = new Dictionary<TKey, TValue>();

    public Mapper() { }

    public void Clear()
    {
        _mapping.Clear();
    }

    public TValue TryGet(TKey key)
    {
        if (_mapping.TryGetValue(key, out TValue value))
        {
            return value;
        }
        else
        {
            return default;
        }
    }

    public bool Exists(TKey key)
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

    public TValue Remap(TKey key, TValue value)
    {
        if (Exists(key))
            _mapping[key] = value;
        else
            _mapping.Add(key, value);
        return value;
    }

    public void Merge(Mapper<TKey, TValue> mapper)
    {
        foreach (KeyValuePair<TKey, TValue> keyValuePair in mapper._mapping)
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
