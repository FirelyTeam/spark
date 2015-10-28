using Hl7.Fhir.Model;
using Spark.Engine.Search.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Spark.Engine.Core
{
    public class ResourceVisitor
    {
        public ResourceVisitor(FhirPropertyIndex propIndex)
        {
            _propIndex = propIndex;
        }

        private FhirPropertyIndex _propIndex;

        public void VisitByType(object fhirObject, Action<object> action, params Type[] types)
        {
            throw new NotImplementedException("Should be implemented to replace Auxiliary.ResourceVisitor.");
        }

        public void VisitByPath(object fhirObject, Action<object> action, string path, string predicate = null)
        {
            if (fhirObject == null)
                return;

            //List of items, visit each of them.
            if (fhirObject.GetType().IsGenericType)
            {
                VisitByPath(fhirObject as IEnumerable<Base>, action, path, predicate);
            }
            //Single item, visit it if it adheres to the predicate (if any)
            else if (String.IsNullOrEmpty(predicate) || PredicateIsTrue(predicate, fhirObject))
            {
                //Path has ended, we arrived at the object that needs action.
                if (String.IsNullOrEmpty(path))
                {
                    action(fhirObject);
                }
                //See what else is in the path and recursively visit that.
                else
                {
                    var hpt = headPredicateAndTail(path);
                    var head = hpt.Item1;
                    var headPredicate = hpt.Item2;
                    var tail = hpt.Item3;

                    //Path was not empty, so there should be a head. No need for an extra null-check.
                    var pm = _propIndex.findPropertyInfo(fhirObject.GetType(), head);

                    //Path might denote an unknown property.
                    if (pm != null)
                    {
                        var headValue = pm.PropInfo.GetValue(fhirObject);

                        if (headValue != null)
                        {
                            VisitByPath(headValue, action, tail, headPredicate);
                        }
                    }
                    else
                    {
                        //TODO: Throw exception (Spark.Exception.NotSupportedException for example), to be catched higher up and then translated to the OperationOutcome.
                    }
                }
            }
        }

        private void VisitByPath(IEnumerable<object> fhirObjects, Action<object> action, string path, string predicate)
        {
            if (fhirObjects.Any())
            {
                foreach (var fhirObject in fhirObjects)
                {
                    VisitByPath(fhirObject, action, path, predicate);
                }
            }

        }

        /// <summary>
        /// Matches 
        ///     value       => head     | predicate     | tail
        ///     a           => "a"      | ""            | ""
        ///     a.b.c       => "a"      | ""            | "b.c"
        ///     a[x=y].b.c  => "a"      | "x=y"         | "b.c"
        /// See also ResourceVisitorTests.
        /// </summary>
        private Regex headTailRegex = new Regex(@"(?([^\.]*\[.*])(?<head>[^\[]*)\[(?<predicate>.*)](\.(?<tail>.*))?|(?<head>[^\.]*)(\.(?<tail>.*))?)");

        private Tuple<string, string, string> headPredicateAndTail(string path)
        {
            var match = headTailRegex.Match(path);
            var head = match.Groups["head"].Value;
            var predicate = match.Groups["predicate"].Value;
            var tail = match.Groups["tail"].Value;

            return new Tuple<string, string, string>(head, predicate, tail);
        }

        private Regex predicateRegex = new Regex(@"(?<propname>[^=]*)=(?<filterValue>.*)");

        private bool PredicateIsTrue(string predicate, object fhirObject)
        {
            var match = predicateRegex.Match(predicate);
            if (match == null || !match.Success)
                return false;

            var propertyName = match.Groups["propname"].Value;
            var filterValue = match.Groups["filterValue"].Value;

            bool result = true;

            //Handle the predicate by (again recursively) visiting from here.
            VisitByPath(
                fhirObject: fhirObject,
                action: el => result &= filterValue.Equals(el.ToString(), StringComparison.InvariantCultureIgnoreCase),
                path: propertyName,
                predicate: null //No support for nested predicates.
                );

            return result;
        }

    }
}
