/*
 * Copyright (c) 2019-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spark.Web.Settings;

namespace Spark.Web;

public class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args)
            .Build()
            .Run();
    }

    private static IHostBuilder CreateWebHostBuilder(string[] args)
    {
        var configuration = AppSettings.BuildDefaultConfiguration();
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
            })
            .ConfigureWebHostDefaults(webHostBuilder =>
            {
                webHostBuilder.UseStartup<Startup>();
                webHostBuilder.UseConfiguration(configuration);
            });
    }
}
