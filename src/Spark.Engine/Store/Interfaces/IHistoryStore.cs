/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Spark.Engine.Core;

namespace Spark.Engine.Store.Interfaces
{
    public interface IHistoryStore
    {
        Snapshot History(string typename, HistoryParameters parameters);

        Snapshot History(IKey key, HistoryParameters parameters);

        Snapshot History(HistoryParameters parameters);
    }
}
