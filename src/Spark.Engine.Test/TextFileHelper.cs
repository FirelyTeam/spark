/* 
 * Copyright (c) 2019-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System.IO;

namespace Spark.Engine.Test
{
    public static class TextFileHelper
    {
        public static string ReadTextFileFromDisk(string path)
        {
            using TextReader reader = new StreamReader(path);
            return reader.ReadToEnd();
        }
    }
}
