using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Search
{
    public enum Strain
    {
        Simple,         // field=value
        Chained,        // fieldx.fieldy.fieldz=value
        Universal,      // Valid for all resources _id, _tag, (and unofficially _limit)
        Meta,           // fields that do not filter the results (_include, _limit, _count)
        Internal,       // Index field for internal use, never user specified
        Undefined,      // Not recognized parameter
        Empty           // Paramater without value
    }

    public interface IParameter
    {
        Strain Strain { get; set; }
        string Field { get; set; }
        IMongoQuery ToQuery();
    }

    public static class IParameterLogic
    {
        public static bool IsA(this IParameter parameter, params Strain[] strain)
        {
            return strain.Contains(parameter.Strain);
        }
    }
}
