/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using System.Collections.Generic;

namespace Spark.Engine.Service;

public interface ITransfer
{
    void Externalize(IEnumerable<Entry> interactions);
    void Externalize(Entry interaction);
    void Internalize(IEnumerable<Entry> interactions, Mapper<string, IKey> mapper);
    void Internalize(Entry entry);
}
