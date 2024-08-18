/* 
 * Copyright (c) 2016-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;

namespace Spark.Engine.Model
{
    /// <summary>
    /// Class for holding information as present in a CompartmentDefinition resource.
    /// This is a (hopefully) temporary solution, since the Hl7.Fhir api does not containt CompartmentDefinition yet.
    /// </summary>
    public class CompartmentInfo
    {
        public ResourceType ResourceType { get; set; }

        private List<string> _revIncludes = new List<string>();
        public List<string> ReverseIncludes { get { return _revIncludes; }  }

        public CompartmentInfo(ResourceType resourceType)
        {
            this.ResourceType = resourceType;
        }

        public void AddReverseInclude(string revInclude)
        {
            _revIncludes.Add(revInclude);
        }

        public void AddReverseIncludes(IEnumerable<string> revIncludes)
        {
            this._revIncludes.AddRange(revIncludes);
        }
    }
}
