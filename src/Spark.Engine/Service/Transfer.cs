using Spark.Core;
using System.Collections.Generic;
using Spark.Engine;
using Spark.Engine.Core;

namespace Spark.Service
{
    using System.Threading.Tasks;

    /// <summary>
    /// Transfer maps between local id's and references and absolute id's and references upon incoming or outgoing Interactions.
    /// It uses an Import or Export to do de actual work for incoming or outgoing Interactions respectively.
    /// </summary>
    public class Transfer : ITransfer
    {
        private readonly ILocalhost localhost;
        private readonly IGenerator generator;
        private readonly SparkSettings sparkSettings;

        public Transfer(IGenerator generator, ILocalhost localhost, SparkSettings sparkSettings = null)
        {
            this.generator = generator;
            this.localhost = localhost;
            this.sparkSettings = sparkSettings;
        }

        public Task Internalize(Entry entry)
        {
            var import = new Import(localhost, generator);
            import.Add(entry);
            return import.Internalize();
        }


        public Task Internalize(IEnumerable<Entry> interactions, Mapper<string, IKey> mapper = null)
        {
            var import = new Import(this.localhost, this.generator);
            if (mapper != null)
            {
                import.AddMappings(mapper);
            }
            import.Add(interactions);
            return import.Internalize();
        }

        public void Externalize(Entry interaction)
        {
            Export export = new Export(this.localhost, this.sparkSettings?.ExportSettings ?? new ExportSettings());
            export.Add(interaction);
            export.Externalize();
        }

        public void Externalize(IEnumerable<Entry> interactions)
        {
            Export export = new Export(this.localhost, this.sparkSettings?.ExportSettings ?? new ExportSettings());
            export.Add(interactions);
            export.Externalize();
        }
    }
}
