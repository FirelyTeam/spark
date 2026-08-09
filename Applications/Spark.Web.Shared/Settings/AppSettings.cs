/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.IO;
using Microsoft.Extensions.Configuration;

namespace Spark.Web.Settings;

public static class AppSettings
{
    private const string SETTINGS_FILE_PATH = "Settings/appsettings.json";
    private const string LOCAL_SETTINGS_FILE_PATH = "Settings/appsettings.local.json";

    public static IConfiguration BuildDefaultConfiguration()
    {
        return GetDefaultConfigurationBuilder().Build();
    }

    private static IConfigurationBuilder GetDefaultConfigurationBuilder()
    {
        return new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(SETTINGS_FILE_PATH, false, true)
            .AddJsonFile(LOCAL_SETTINGS_FILE_PATH, true, true)
            .AddEnvironmentVariables();
    }
}
