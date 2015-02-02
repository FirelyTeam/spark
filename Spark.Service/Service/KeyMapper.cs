/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Rest;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Service
{
    public class KeyMapper
    {
        Dictionary<Uri, Uri> map = new Dictionary<Uri, Uri>();
        //private IGenerator generator;

        public KeyMapper()
        {
            //this.generator = generator;
            
        }

        public void Clear()
        {
            map.Clear();
        }

        public void Map(Uri external, Uri local)
        {
            external = Localhost.Absolute(external);
            map.Add(external, local);
        }

        public bool Exists(Uri external)
        {
            external = Localhost.Absolute(external);
            return map.ContainsKey(external);
        }

        public Uri Remap(Uri external, Uri local)
        {
            this.Map(external, local);
            return local;
        }
        
        public Uri Get(Uri external)
        {
            external = Localhost.Absolute(external);
            return map[external];
        }

        public Uri TryGet(Uri external)
        {
            if (Exists(external))
            {
                return Get(external);
            }
            else
            {
                return external;
            }
        }

      
    }
}
