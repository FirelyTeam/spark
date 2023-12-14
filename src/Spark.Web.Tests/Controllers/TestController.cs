/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spark.Web.Tests.Models;
using System;
using System.IO;
using System.Text;

namespace Spark.Web.Tests.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [Route("test-json")]
    public IActionResult TestJson(TestJsonDto dto) => Ok(dto.Name);

    [Route("test-file")]
    public IActionResult TestFile(IFormFile file)
    {
        Stream requestStream = file.OpenReadStream();
        Span<byte> buffer = new(new byte[requestStream.Length]);
        requestStream.ReadExactly(buffer);
        string contentAsString = Encoding.UTF8.GetString(buffer);
        return Ok($"Name: {file.FileName}, Content: {contentAsString}");
    }

    [Route("test-json-file")]
    public IActionResult TestJsonFile(TestFileDto dto) => Ok(dto.Other);

    [Route("test-resource")]
    public IActionResult TestResource(Resource resource) => Ok(resource.Id);
}
