using Spark.Core;
using System.Collections.Generic;
using Spark.Engine.Core;

namespace Spark.Service
{

    /// <summary>
    /// This Importer can be initialized once. It will instanciate a TransactionImporter on each Import call
    /// </summary>
    public class Transfer
    {
        ILocalhost localhost;
        IGenerator generator;

        public Transfer(IGenerator generator, ILocalhost localhost)
        {
            this.generator = generator;
            this.localhost = localhost;
        }

        public void Internalize(Interaction interaction)
        {
            var import = new Import(this.localhost, this.generator);
            import.Add(interaction);
            import.Internalize();
        }

        public void Internalize(IEnumerable<Interaction> interactions)
        {
            var import = new Import(this.localhost, this.generator);
            import.Add(interactions);
            import.Internalize();
        }

        public void Externalize(Interaction interaction)
        {
            Export export = new Export(this.localhost);
            export.Add(interaction);
            export.Externalize();
        }

        public void Externalize(IEnumerable<Interaction> interactions)
        {
            Export export = new Export(this.localhost);
            export.Add(interactions);
            export.Externalize();
        }
    }
}
