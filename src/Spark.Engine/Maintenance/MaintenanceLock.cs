/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Maintenance
{
    internal class MaintenanceLock : IDisposable
    {
        public MaintenanceLockMode Mode { get; private set; }

        public bool IsLocked => Mode > MaintenanceLockMode.None;

        public MaintenanceLock(MaintenanceLockMode mode)
        {
            Mode = mode;
        }

        public void Unlock()
        {
            Mode = MaintenanceLockMode.None;
        }

        public void Dispose()
        {
            Unlock();
        }
    }
}
