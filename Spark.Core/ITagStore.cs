using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public interface ITagStore
    {
        IEnumerable<Tag> Tags();
        IEnumerable<Tag> Tags(string resourcetype);
        //IEnumerable<Uri> Find(params Tag[] tags);
    }
}
