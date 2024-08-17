/* 
 * Copyright (c) 2014-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */


namespace Spark.Engine.Core
{
    public static class FhirRestOp
    {
        public const string SNAPSHOT = "_snapshot";
    }

    public static class FhirHeader
    {
        public const string CATEGORY = "Category";
    }

    public static class FhirParameter
    {
        public const string SNAPSHOT_ID = "id";
        public const string SNAPSHOT_INDEX = "start";
        public const string OFFSET = "_offset";
        public const string SUMMARY = "_summary";
        public const string COUNT = "_count";
        public const string SINCE = "_since";
        public const string SORT = "_sort";
    }
}
