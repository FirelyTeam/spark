/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using System;
using System.Threading.Tasks;

namespace Spark.Engine.Service;

public interface IServiceListener
{
    Task InformAsync(Uri location, Entry interaction);
}
