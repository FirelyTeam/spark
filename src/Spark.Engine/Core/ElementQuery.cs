/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Spark.Engine.Core
{
    public class ElementQuery
    {
        private readonly List<Chain> _chains = new List<Chain>();
        public void Add(string path)
        {
            _chains.Add(new Chain(path));
        }
        public ElementQuery(params string[] paths)
        {
            foreach (string path in paths)
            {
                this.Add(path);
            }
        }
        public ElementQuery(string path)
        {
            this.Add(path);
        }

        public void Visit(object field, Action<object> action)
        {
            foreach (Chain chain in _chains)
            {
                chain.Visit(field, action);
            }
        }

        // Legenda:
        // path:  string path  = "person.family.name";
        // string chain: List<string> chain = { "person", "family", "name" };
        // Segment Chain : List<Segment> Chain;
        public class Segment
        {
            public Type FhirType;
            public string Name;
            public PropertyInfo Property;
            public Type AllowedType;
            public Predicate<object> Filter;

            public object GetValue(object field)
            {
                if (field == null || Property == null)
                {
                    return null;
                }
                else
                {
                    return Property.GetValue(field);
                }
            }
        }

        public class Chain
        {
            private List<Segment> _segments = new List<Segment>();

            public Chain(string path)
            {
                List<string> chain = SplitPath(path);

                // Keep the typename separate.
                var typeName = chain.First();
                chain.RemoveAt(0);

                _segments = BuildSegments(typeName, chain);
            }

            // segments is a cache of PropertyInfo elements for every link in the chain. We have to cache this for performance.
            // Every item contains: <Fhir type, property name, info of that property, specific type in case of a ChoiceType.DatatypeChoice, predicate for filtering multiple items in an IEnumerable>
            // Example: ClinicalImpression.trigger: <ClinicalImpression, "trigger", (propertyinfo of property Trigger), CodeableConcept, null>
            // Example: Practitioner.practitionerRole.Extension[url=http://hl7.no/fhir/StructureDefinition/practitionerRole-identifier]:
            //  <Practitioner, "practitionerRole", (propertyInfo of practitionerRole), null, null>
            //  <PractitionerRoleComponent, "Extension", (propertyInfo of Extension), null, extension => extension.url = "http://hl7.no/fhir/StructureDefinition/practitionerRole-identifier">
            private List<string> SplitPath(string path)
            {
                // todo: This whole function can probably be replaced by a single RegExp. --MH
                //var path = path.Replace("[x]", ""); // we won't remove this, and start treating it as a predicate.

                path = Regex.Replace(path, @"\b(\w)", match => match.Value.ToUpper());
                var chain = new List<string>();

                // Split on the dots, except when the dot is inside square brackets, because then it is part of a predicate value.
                while (path.Length > 0)
                {
                    int firstBracket = path.IndexOf('[');
                    int firstDot = path.IndexOf('.');
                    if (firstDot == -1)
                    {
                        chain.Add(path);
                        break;
                    }
                    if (firstBracket > -1 && firstBracket < firstDot)
                    {
                        int endBracket = path.IndexOf(']');
                        chain.Add(path.Substring(0, endBracket + 1)); //+1 to include the bracket itself.
                        path = path.Remove(0, Math.Min(path.Length, endBracket + 2)); //+2 for the bracket itself and the dot after the bracket
                    }
                    else
                    {
                        chain.Add(path.Substring(0, firstDot));
                        path = path.Remove(0, firstDot + 1); //+1 to remove the dot itself.
                    }
                }
                return chain;
            }

            private List<Segment> BuildSegments(string classname, List<string> chain)
            {
                var segments = new List<Segment>();

                Type baseType = ModelInfo.FhirTypeToCsType[classname];
                foreach (string linkString in chain)
                {
                    var segment = new Segment
                    {
                        FhirType = baseType
                    };
                    var predicateRegex = new Regex(@"(?<propname>[^\[]*)(\[(?<predicate>.*)\])?");
                    var match = predicateRegex.Match(linkString);
                    var predicate = match.Groups["predicate"].Value;
                    segment.Name = match.Groups["propname"].Value;

                    segment.Filter = ParsePredicate(predicate);

                    var matchingFhirElements = baseType.FindMembers(MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public, new MemberFilter(IsFhirElement), segment.Name);
                    if (matchingFhirElements.Any())
                    {
                        segment.Property = baseType.GetProperty(matchingFhirElements.First().Name);
                        // TODO: Ugly repetitive code from IsFhirElement(), since that method can only return a boolean...
                        FhirElementAttribute feAtt = segment.Property.GetCustomAttribute<FhirElementAttribute>();
                        if (feAtt != null)
                        {
                            if (feAtt.Choice == ChoiceType.DatatypeChoice || feAtt.Choice == ChoiceType.ResourceChoice)
                            {
                                AllowedTypesAttribute atAtt = segment.Property.GetCustomAttribute<AllowedTypesAttribute>();
                                if (atAtt != null)
                                {
                                    foreach (Type allowedType in atAtt.Types)
                                    {
                                        var curTypeName = segment.Name.Remove(0, feAtt.Name.Length);
                                        Type curType = ModelInfo.GetTypeForFhirType(curTypeName);
                                        if (allowedType.IsAssignableFrom(curType))
                                        {
                                            segment.AllowedType = allowedType;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        segment.Property = baseType.GetProperty(segment.Name);
                    }
                    if (segment.Property == null)
                        break;

                    segments.Add(segment);

                    if (segment.Property.PropertyType.IsGenericType)
                        //For instance AllergyIntolerance.Event, which is a List<Hl7.Fhir.Model.AllergyIntolerance.AllergyIntoleranceEventComponent>
                        baseType = segment.Property.PropertyType.GetGenericArguments().First();
                    else if (segment.AllowedType != null)
                        baseType = segment.AllowedType;
                    else
                        baseType = segment.Property.PropertyType;
                }

                return segments;
            }

            private Predicate<object> ParsePredicate(string predicate)
            {
                //TODO: CK: Search for 'FhirElement' with the name 'propname' first, just like we do in fillChainLinks above.
                var predicateRegex = new Regex(@"(?<propname>[^=]*)=(?<filterValue>.*)");
                var match = predicateRegex.Match(predicate);
                if (match == null || !match.Success)
                    return null;

                var propertyName = match.Groups["propname"].Value;
                var filterValue = match.Groups["filterValue"].Value;

                Predicate<object> result =
                    (obj) => GetPredicateForPropertyAndFilter(propertyName, filterValue, obj);

                return result;
            }

            private bool GetPredicateForPropertyAndFilter(string propertyName, string filterValue, object obj)
            {
                PropertyInfo property = obj.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    object value = property.GetValue(obj);
                    if (value != null)
                    {
                        return filterValue.Equals(value.ToString(), StringComparison.CurrentCultureIgnoreCase);
                    }
                }

                return false;
            }

            private static bool IsFhirElement(MemberInfo member, object criterium)
            {
                string fhirElementName = (string)criterium;
                FhirElementAttribute feAtt = member.GetCustomAttribute<FhirElementAttribute>();

                if (feAtt != null)
                {
                    if (fhirElementName.Equals(feAtt.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                    if (fhirElementName.StartsWith(feAtt.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (feAtt.Choice == ChoiceType.DatatypeChoice || feAtt.Choice == ChoiceType.ResourceChoice)
                        {
                            AllowedTypesAttribute atAtt = member.GetCustomAttribute<AllowedTypesAttribute>();
                            if (atAtt != null)
                            {
                                foreach (Type allowedType in atAtt.Types)
                                {
                                    var curTypeName = fhirElementName.Remove(0, feAtt.Name.Length);
                                    Type curType = ModelInfo.GetTypeForFhirType(curTypeName);
                                    if (allowedType.IsAssignableFrom(curType))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                //if it has no FhirElementAttribute, it is not a FhirElement...
                return false;
            }

            public void Visit(object field, Action<object> action)
            {
                Visit(field, this._segments, action, null);
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

            private bool TestIfCodedEnum(Type type)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }

                bool? codedEnum = type.GenericTypeArguments?.FirstOrDefault()?.IsEnum;
                return codedEnum.HasValue && codedEnum.Value;
            }

            private void Visit(object field, IEnumerable<Segment> chain, Action<object> action, Predicate<object> predicate)
            {
                Type type = field.GetType();

                if (TestIfGenericList(type))
                {
                    var list = field as IEnumerable<object>;
                    if ((list != null) && (list.Count() > 0))
                    {
                        foreach (var subfield in list)
                        {
                            Visit(subfield, chain, action, predicate);
                        }
                    }
                }
                else if (TestIfCodedEnum(type))
                {
                    //Quite a special case, see for example Patient.GenderElement: Code<AdministrativeGender>
                    Visit(field.GetType().GetProperty("Value").GetValue(field), chain, action, predicate);
                }
                else //single value
                { 
                    //Patient.address.city, current field is address
                    if (predicate == null || predicate(field))
                    {
                        if ((chain != null) && (chain.Count() > 0)) //not at the end of the chain, follow the next link in the chain
                        {
                            var next = chain.First(); //{ FhirString, "city", (propertyInfo of city), AllowedTypes = null, Filter = null }
                            IEnumerable<Segment> subchain = chain.Skip(1); //subpath = <empty> (city is the last item)

                            //if (field.GetType().GetProperty(next.Name) == null)
                            //    throw new ArgumentException(string.Format("'{0}' is not a valid property for '{1}'", next.Name, field.GetType().Name));
                            // resolved this issue by using next.GetValue() which may return null -- MH

                            var subfield = next.GetValue(field); //value of city
                            if (subfield != null && next != null && next.Property != null && (next.AllowedType == null || next.AllowedType.IsAssignableFrom(subfield.GetType())))
                            {
                                Visit(subfield, subchain, action, next.Filter);
                            }
                            else
                            {
                                action(null);
                            }
                        }
                        else
                        {
                            action(field);
                        }
                    }
                }
            }

            public override string ToString()
            {
                return string.Join(".", _segments.Select(l => l.Name));
            }
        }

        public override string ToString()
        {
            return string.Join(", ", _chains.Select(chain => string.Join(".", chain)));
        }
    }
}