using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Auxiliary
{
    class RestOperation
    {
        // API: this constant can be derived from TransactionBuilder. 
        // But the History keyword has a bigger scope than just TransactionBuilder.
        // proposal: move HISTORY and other URL/operation constants to Hl7.Fhir.Rest.Operation or something.
        public static string HISTORY = TransactionBuilder.HISTORY;
    }
}
