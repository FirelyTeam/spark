using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Service
{

    /// <summary>
    /// This Importer can be initialized once. It will instanciate a TransactionImporter on each Import call
    /// </summary>
    public class Importer
    {
        ILocalhost localhost;
        IGenerator generator;

        public Importer(IGenerator generator, ILocalhost localhost)
        {
            this.generator = generator;
            this.localhost = localhost;
        }

        public Interaction Import(Interaction interaction)
        {
            var importer = new TransactionImporter(localhost, generator);
            importer.Add(interaction);
            return importer.Internalize().First();
        }

        public IList<Interaction> Import(IList<Interaction> interactions)
        {
            var importer = new TransactionImporter(localhost, generator);
            importer.AddRange(interactions);
            return importer.Internalize();
        }

    }
}
