using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Service
{
    using System.Threading.Tasks;

    public interface ITransfer
    {
        void Externalize(IEnumerable<Entry> interactions);
        void Externalize(Entry interaction);
        Task Internalize(IEnumerable<Entry> interactions, Mapper<string, IKey> mapper);
        Task Internalize(Entry entry);
    }
}