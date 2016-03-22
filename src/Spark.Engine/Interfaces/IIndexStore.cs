using Spark.Engine.Core;
using Spark.Engine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Interfaces
{
    public interface IIndexStore
    {
        void Save(IndexValue indexValue);

        void Delete(Entry entry);

        void Clean();
    }
}
