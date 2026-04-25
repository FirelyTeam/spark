/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Microsoft.AspNetCore.Http;

namespace Spark.Web.Tests.Models;

public class TestFileDto
{
    public required IFormFile File { get; init; }
    public required string Other { get; init; }
}
