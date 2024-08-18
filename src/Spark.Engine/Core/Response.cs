/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System.Net;

namespace Spark.Engine.Core
{
    // THe response class is an abstraction of the Fhir REST responses
    // This way, it's easier to implement multiple WebApi controllers
    // without having to implement functionality twice.
    // The FhirService always responds with a "Response"

    public class FhirResponse
    {
        public HttpStatusCode StatusCode;
        public IKey Key;
        public Resource Resource;

        public FhirResponse(HttpStatusCode code, IKey key, Resource resource)
        {
            StatusCode = code;
            Key = key;
            Resource = resource;
        }

        public FhirResponse(HttpStatusCode code, Resource resource)
        {
            StatusCode = code;
            Key = null;
            Resource = resource;
        }

        public FhirResponse(HttpStatusCode code)
        {
            StatusCode = code;
            Key = null;
            Resource = null;
        }

        public bool IsValid
        {
            get
            {
                int code = (int)StatusCode;
                return code <= 300;
            }
        }

        public bool HasBody
        {
            get
            {
                return Resource != null;
            }
        }

        public override string ToString()
        {
            string details = (Resource != null) ? string.Format("({0})", Resource.TypeName) : null;
            string location = Key?.ToString();
            return string.Format("{0}: {1} {2} ({3})", (int)StatusCode, StatusCode.ToString(), details, location);
        }
    }
}