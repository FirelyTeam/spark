/* 
 * Copyright (c) 2019-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.IO;

namespace Spark.Engine.Test;

public static class TextFileHelper
{
    public static string ReadTextFileFromDisk(string path)
    {
        using TextReader reader = new StreamReader(path);
        return reader.ReadToEnd();
    }
}