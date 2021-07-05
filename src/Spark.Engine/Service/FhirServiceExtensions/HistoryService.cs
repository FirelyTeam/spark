/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class HistoryService : IHistoryService
    {
        private IHistoryStore _historyStore;

        public HistoryService(IHistoryStore historyStore)
        {
            _historyStore = historyStore;
        }

        public Snapshot History(string typename, HistoryParameters parameters)
        {
            return _historyStore.History(typename, parameters);
        }

        public async Task<Snapshot> HistoryAsync(string typename, HistoryParameters parameters)
        {
            return await _historyStore.HistoryAsync(typename, parameters).ConfigureAwait(false);
        }

        public Snapshot History(IKey key, HistoryParameters parameters)
        {
            return _historyStore.History(key, parameters);
        }

        public async Task<Snapshot> HistoryAsync(IKey key, HistoryParameters parameters)
        {
            return await _historyStore.HistoryAsync(key, parameters).ConfigureAwait(false);
        }

        public Snapshot History(HistoryParameters parameters)
        {
            return _historyStore.History(parameters);
        }

        public async Task<Snapshot> HistoryAsync(HistoryParameters parameters)
        {
            return await _historyStore.HistoryAsync(parameters).ConfigureAwait(false);
        }
    }
}