/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Maintenance;
using System.Linq;
using System.Net;
using Xunit;

namespace Spark.Engine.Test.Maintenance
{
    public class MaintenanceModeTests
    {
        [Theory]
        [MemberData(nameof(AllLockModes))]
        internal void IsEnabled_Should_Return_False_If_Maintenance_Mode_Is_Not_Enabled(MaintenanceLockMode mode)
        {
            Assert.False(MaintenanceMode.IsEnabled(mode));
        }

        [Theory]
        [MemberData(nameof(AllLockModes))]
        internal void IsEnabled_Should_Not_Lock_If_Lock_None_Is_Provided(MaintenanceLockMode mode)
        {
            using (MaintenanceMode.Enable(MaintenanceLockMode.None))
            {
                Assert.False(MaintenanceMode.IsEnabled(mode));
            }
        }

        [Fact]
        internal void IsEnabled_Should_Not_Lock_Read_If_Write_Only_Mock_Mode_Is_Provided()
        {
            using (MaintenanceMode.Enable(MaintenanceLockMode.Write))
            {
                Assert.True(MaintenanceMode.IsEnabled(MaintenanceLockMode.None));
                Assert.True(MaintenanceMode.IsEnabled(MaintenanceLockMode.Write));
                Assert.False(MaintenanceMode.IsEnabled(MaintenanceLockMode.Full));
            }
        }

        [Fact]
        internal void IsEnabled_Full_Lock_Mode_Should_Lock_Read_And_Write()
        {
            using (MaintenanceMode.Enable(MaintenanceLockMode.Full))
            {
                Assert.True(MaintenanceMode.IsEnabled(MaintenanceLockMode.None));
                Assert.True(MaintenanceMode.IsEnabled(MaintenanceLockMode.Write));
                Assert.True(MaintenanceMode.IsEnabled(MaintenanceLockMode.Full));
            }
        }

        [Theory]
        [MemberData(nameof(ActualLockModes))]
        internal void Should_Not_Allow_To_Lock_Twice(MaintenanceLockMode mode)
        {
            using (MaintenanceMode.Enable(mode))
            {
                var e = Assert.Throws<MaintenanceModeEnabledException>(() => MaintenanceMode.Enable(mode));
                Assert.Equal(HttpStatusCode.ServiceUnavailable, e.StatusCode);
            }
        }

        [Theory]
        [MemberData(nameof(AllHttpMethods))]
        internal void IsEnabledForHttpMethod_Should_Return_False_If_Maintenance_Mode_Is_Not_Enabled(string method)
        {
            Assert.False(MaintenanceMode.IsEnabledForHttpMethod(method));
        }

        [Theory]
        [MemberData(nameof(ReadHttpMethods))]
        internal void IsEnabledForHttpMethod_Should_Return_False_For_Read_Methods_Write_Only_Lock(string method)
        {
            using (MaintenanceMode.Enable(MaintenanceLockMode.Write))
            {
                Assert.False(MaintenanceMode.IsEnabledForHttpMethod(method));
            }
        }

        [Theory]
        [MemberData(nameof(ReadHttpMethods))]
        internal void IsEnabledForHttpMethod_Should_Return_True_For_Read_Methods_Full_Lock(string method)
        {
            using (MaintenanceMode.Enable(MaintenanceLockMode.Full))
            {
                Assert.True(MaintenanceMode.IsEnabledForHttpMethod(method));
            }
        }

        [Theory]
        [MemberData(nameof(WriteHttpMethods))]
        internal void IsEnabledForHttpMethod_Should_Return_True_For_Update_Methods_Write_Only_Lock(string method)
        {
            using (MaintenanceMode.Enable(MaintenanceLockMode.Write))
            {
                Assert.True(MaintenanceMode.IsEnabledForHttpMethod(method));
            }
        }

        [Theory]
        [MemberData(nameof(WriteHttpMethods))]
        internal void IsEnabledForHttpMethod_Should_Return_True_For_Update_Methods_Full_Lock(string method)
        {
            using (MaintenanceMode.Enable(MaintenanceLockMode.Full))
            {
                Assert.True(MaintenanceMode.IsEnabledForHttpMethod(method));
            }
        }

        public static object[][] ReadHttpMethods => new[]
        {
            new object[] {"GET"},
            new object[] {"HEAD"},
            new object[] {"OPTIONS"}
        };

        public static object[][] WriteHttpMethods => new[]
        {
            new object[] {"POST"},
            new object[] {"PUT"},
            new object[] {"PATCH"},
            new object[] {"DELETE"}
        };

        public static object[][] AllHttpMethods => ReadHttpMethods.Concat(WriteHttpMethods).ToArray();

        public static object[][] AllLockModes => ((MaintenanceLockMode[])typeof(MaintenanceLockMode).GetEnumValues())
            .Select(v => new object[] { v })
            .ToArray();

        public static object[][] ActualLockModes => ((MaintenanceLockMode[])typeof(MaintenanceLockMode).GetEnumValues())
            .Where(m => m != MaintenanceLockMode.None)
            .Select(v => new object[] { v })
            .ToArray();
    }
}
