/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Mongo.Search.Common;

namespace Spark.Search.Mongo
{
    public class Parameters : List<IParameter>
    {
        public List<IParameter> WhichFilter
        {
            get 
            {
                return this.Where(p => p.IsA(Strain.Internal, Strain.Simple, Strain.Chained, Strain.Universal)).ToList();
            }
            
        }
        public List<IParameter> Used
        {
            get 
            {
                return this.Where(p => p.IsA(Strain.Simple, Strain.Chained, Strain.Meta, Strain.Universal)).ToList();
            }
            
        }
        public string UsedHttpQuery()
        {
            string[] defined = Used.Select(p => p.ToString()).ToArray();
            return string.Join("&", defined);
        }
        public int Limit
        {
            get
            {
                Parameter parameter = Find(p => Spark.Mongo.Search.Common.Config.Equal(p.Field, MetaField.LIMIT)) as Parameter;
                if (parameter != null)
                {
                    try
                    {
                        return Convert.ToInt16(parameter.Value);
                    }
                    catch
                    {
                        return Spark.Mongo.Search.Common.Config.MAX_SEARCH_RESULTS;
                    }

                }
                else return Spark.Mongo.Search.Common.Config.MAX_SEARCH_RESULTS;
            }
        }
        public List<IncludeParameter> Includes
        {
            get 
            {
                return this.Where(p => p is IncludeParameter).Select(p => p as IncludeParameter).ToList();
            }
        }

        public static Parameters operator + (Parameters parameters, IParameter parameter)
        {
            parameters.Add(parameter);
            return parameters;
        }
        public static Parameters operator + (Parameters parameters, List<IParameter> newparameters)
        {
            parameters.AddRange(newparameters);
            return parameters;
        }
    }
}    
