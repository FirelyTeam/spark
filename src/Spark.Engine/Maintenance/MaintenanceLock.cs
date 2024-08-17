/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
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
