/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Core;
using System.Collections.Generic;
using Spark.Engine;
using Spark.Engine.Core;

namespace Spark.Service
{

    /// <summary>
    /// Transfer maps between local id's and references and absolute id's and references upon incoming or outgoing Interactions.
    /// It uses an Import or Export to do de actual work for incoming or outgoing Interactions respectively.
    /// </summary>
    public class Transfer : ITransfer
    {
        private readonly ILocalhost _localhost;
        private readonly IIdentityGenerator _generator;
        private readonly SparkSettings _settings;

        public Transfer(IIdentityGenerator generator, ILocalhost localhost, SparkSettings settings = null)
        {
            _generator = generator;
            _localhost = localhost;
            _settings = settings;
        }

        public void Internalize(Entry entry)
        {
            var import = new Import(_localhost, _generator);
            import.Add(entry);
            import.Internalize();
        }

        public void Internalize(IEnumerable<Entry> interactions, Mapper<string, IKey> mapper = null)
        {
            var import = new Import(_localhost, _generator);
            if (mapper != null)
            {
                import.AddMappings(mapper);
            }
            import.Add(interactions);
            import.Internalize();
        }

        public void Externalize(Entry interaction)
        {
            Export export = new Export(_localhost, _settings?.ExportSettings ?? new ExportSettings());
            export.Add(interaction);
            export.Externalize();
        }

        public void Externalize(IEnumerable<Entry> interactions)
        {
            Export export = new Export(_localhost, _settings?.ExportSettings ?? new ExportSettings());
            export.Add(interactions);
            export.Externalize();
        }
    }
}
