/*
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Threading.Tasks;

namespace Spark.Engine.Interfaces
{
    public interface IFhirStoreAdministration
    {
        Task CleanAsync();
    }
}

