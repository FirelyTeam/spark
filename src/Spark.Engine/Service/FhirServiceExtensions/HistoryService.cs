/*
 * Copyright (c) 2016-2018, Firely (info@fire.ly)
 * Copyright (c) 2018-2024, Incendi (info@incendi.no)
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

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

        public async Task<Snapshot> HistoryAsync(string typename, HistoryParameters parameters)
        {
            return await _historyStore.HistoryAsync(typename, parameters).ConfigureAwait(false);
        }

        public async Task<Snapshot> HistoryAsync(IKey key, HistoryParameters parameters)
        {
            return await _historyStore.HistoryAsync(key, parameters).ConfigureAwait(false);
        }

        public async Task<Snapshot> HistoryAsync(HistoryParameters parameters)
        {
            return await _historyStore.HistoryAsync(parameters).ConfigureAwait(false);
        }
    }
}