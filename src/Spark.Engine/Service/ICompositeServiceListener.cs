/*
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using Spark.Service;
using System.Threading.Tasks;

namespace Spark.Engine.Service
{
    public interface ICompositeServiceListener : IServiceListener
    {
        void Add(IServiceListener listener);
        void Clear();
        Task InformAsync(Entry interaction);
    }
}