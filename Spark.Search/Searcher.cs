using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Spark.Core;

namespace Spark.Search
{
    public interface ISearcher
    {
        SearchResults Search(Parameters parameters);
    }
}