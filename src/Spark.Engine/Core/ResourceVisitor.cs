/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Spark.Engine.Core
{
    public class ResourceVisitor
    {
        /// <summary>
        /// Matches 
        ///     value       => head     | predicate     | tail
        ///     a           => "a"      | ""            | ""
        ///     a.b.c       => "a"      | ""            | "b.c"
        ///     a(x=y).b.c  => "a"      | "x=y"         | "b.c"
        /// See also ResourceVisitorTests.
        /// </summary>
        private readonly Regex _headTailRegex = new Regex(@"(?([^\.]*\[.*\])(?<head>[^\[]*)\[(?<predicate>.*)\](\.(?<tail>.*))?|(?<head>[^\.]*)(\.(?<tail>.*))?)");
        private readonly Regex _predicateRegex = new Regex(@"(?<propname>[^=]*)=(?<filterValue>.*)");

        public ResourceVisitor(FhirPropertyIndex propIndex)
        {
            _propIndex = propIndex;
        }

        private FhirPropertyIndex _propIndex;

        public void VisitByType(object fhirObject, Action<object> action, params Type[] types)
        {
            throw new NotImplementedException("Should be implemented to replace Auxiliary.ResourceVisitor.");
        }

        /// <summary>
        /// Walk through an object, following the specified path of properties.
        /// The path should NOT include the name of the resource itself (e.g. "Patient.birthdate" is wrong, "birthdate" is right).
        /// </summary>
        /// <param name="fhirObject"></param>
        /// <param name="action"></param>
        /// <param name="path"></param>
        /// <param name="predicate"></param>
        public void VisitByPath(object fhirObject, Action<object> action, string path, string predicate = null)
        {
            if (fhirObject == null)
                return;

            //List of items, visit each of them.
            if (TestIfGenericList(fhirObject.GetType()))
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
                    var head = hpt.Item1.TrimStart('@');
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

        private Tuple<string, string, string> headPredicateAndTail(string path)
        {
            var match = _headTailRegex.Match(path);
            var head = match.Groups["head"].Value;
            var predicate = match.Groups["predicate"].Value;
            var tail = match.Groups["tail"].Value;

            return new Tuple<string, string, string>(head, predicate, tail);
        }

        private bool PredicateIsTrue(string predicate, object fhirObject)
        {
            var match = _predicateRegex.Match(predicate);
            if (match == null || !match.Success)
                return false;

            var propertyName = match.Groups["propname"].Value;
            var filterValue = match.Groups["filterValue"].Value.Trim('\'');

            bool result = false;

            //Handle the predicate by (again recursively) visiting from here.
            VisitByPath(
                fhirObject: fhirObject,
                action: el =>
                { string actualValue;
                    if (TestIfCodedEnum(el.GetType()))
                        actualValue = el.GetType().GetProperty("Value").GetValue(el).ToString();
                    else
                        actualValue = el.ToString();
                    result = filterValue.Equals(actualValue, StringComparison.InvariantCultureIgnoreCase);
                },
                path: propertyName,
                predicate: null //No support for nested predicates.
                );

            return result;
        }

        /// <summary>
        /// Test if a type derives from IList of T, for any T.
        /// </summary>
        private bool TestIfGenericList(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var interfaceTest = new Predicate<Type>(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

            return interfaceTest(type) || type.GetInterfaces().Any(i => interfaceTest(i));
        }

        //TODO: Do not repeat this code. It is also in ElementIndexer (and in ElementQuery, but that will be retired some day soon).
        private bool TestIfCodedEnum(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            bool? codedEnum = type.GenericTypeArguments?.FirstOrDefault()?.IsEnum;
            if (codedEnum.HasValue && codedEnum.Value)
            {
                return true;
            }
            return false;
        }
    }
}
