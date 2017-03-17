using System;
using Spark.Core;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Service
{

    /// <summary>
    /// Transfer maps between local id's and references and absolute id's and references upon incoming or outgoing Interactions.
    /// It uses an Import or Export to do de actual work for incoming or outgoing Interactions respectively.
    /// </summary>
    public class Transfer : ITransfer
    {
        ILocalhost localhost;
        IGenerator generator;

        public Transfer(IGenerator generator, ILocalhost localhost)
        {
            this.generator = generator;
            this.localhost = localhost;
        }

        public void Internalize(Entry entry)
        {
            var import = new Import(this.localhost, this.generator);
            import.Add(entry);
            import.Internalize();
        }

    
        public void Internalize(IEnumerable<Entry> interactions, Mapper<string, IKey> mapper = null)
        {
            var import = new Import(this.localhost, this.generator);
            if (mapper != null)
            {
                import.AddMappings(mapper);
            }
            import.Add(interactions);
            import.Internalize();
        }

        public void Externalize(Entry interaction)
        {
            Export export = new Export(this.localhost);
            export.Add(interaction);
            export.Externalize();
        }

        public void Externalize(IEnumerable<Entry> interactions)
        {
            Export export = new Export(this.localhost);
            export.Add(interactions);
            export.Externalize();
        }
    }
}
