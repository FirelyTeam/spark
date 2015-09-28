/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
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
    // Legenda:
    // chain: List<string> chain = { "person", "family", "name" };
    // path:  string path  = "person.family.name";

    
    public class ElementQuery
    {
        private List<Chain> chains = new List<Chain>();
        public void Add(string path)
        {
            chains.Add(new Chain(path));
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
            foreach (Chain chain in chains)
            {
                chain.Visit(field, action);
            }
        }

        public class Chain
        {
            private List<string> chain;
            private List<ChainLink> links = new List<ChainLink>();

            private class ChainLink
            {
                internal Type FhirType;
                internal string propertyName;
                internal PropertyInfo propertyInfo;
                internal Type AllowedPropertyType;
                internal Predicate<object> Filter;
            }

            public Chain(string path)
            {
                this.chain = ParsePathToChain(path);

                // Keep the typename separate.
                var typeName = chain.First();
                chain.RemoveAt(0);

                links = BuildChainLinks(typeName, chain);
            }

            // links is a cache of PropertyInfo elements for every link in the chain. We have to cache this for performance.
            // Every item contains: <Fhir type, property name, info of that property, specific type in case of a ChoiceType.DatatypeChoice, predicate for filtering multiple items in an IEnumerable>
            // Example: ClinicalImpression.trigger: <ClinicalImpression, "trigger", (propertyinfo of property Trigger), CodeableConcept, null>
            // Example: Practitioner.practitionerRole.Extension[url=http://hl7.no/fhir/StructureDefinition/practitionerRole-identifier]:
            //  <Practitioner, "practitionerRole", (propertyInfo of practitionerRole), null, null>
            //  <PractitionerRoleComponent, "Extension", (propertyInfo of Extension), null, extension => extension.url = "http://hl7.no/fhir/StructureDefinition/practitionerRole-identifier">

            private List<string> ParsePathToChain(string path)
            {
                // todo: This whole function can probably be replaced by a single RegExp. --MH
                var restOfPath = path.Replace("[x]", "");
                restOfPath = Regex.Replace(restOfPath, @"\b(\w)", match => match.Value.ToUpper());
                chain = new List<string>();

                // Split on the dots, except when the dot is inside square brackets, because then it is part of a predicate value.
                while (restOfPath.Length > 0)
                {
                    int firstBracket = restOfPath.IndexOf('[');
                    int firstDot = restOfPath.IndexOf('.');
                    if (firstDot == -1)
                    {
                        chain.Add(restOfPath);
                        break;
                    }
                    if (firstBracket > -1 && firstBracket < firstDot)
                    {
                        int endBracket = restOfPath.IndexOf(']');
                        chain.Add(restOfPath.Substring(0, endBracket + 1)); //+1 to include the bracket itself.
                        restOfPath = restOfPath.Remove(0, Math.Min(restOfPath.Length, endBracket + 2)); //+2 for the bracket itself and the dot after the bracket
                    }
                    else
                    {
                        chain.Add(restOfPath.Substring(0, firstDot));
                        restOfPath = restOfPath.Remove(0, firstDot + 1); //+1 to remove the dot itself.
                    }
                }
                return chain;
            }

            private List<ChainLink> BuildChainLinks(string classname, List<string> chain)
            {
                var links = new List<ChainLink>();

                Type baseType = ModelInfo.FhirTypeToCsType[classname];
                foreach (string linkString in chain)
                {
                    var link = new ChainLink();
                    link.FhirType = baseType;
                    var predicateRegex = new Regex(@"(?<propname>[^\[]*)(\[(?<predicate>.*)\])?");
                    var match = predicateRegex.Match(linkString);
                    var predicate = match.Groups["predicate"].Value;
                    link.propertyName = match.Groups["propname"].Value;

                    link.Filter = ParsePredicate(predicate);

                    var matchingFhirElements = baseType.FindMembers(MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public, new MemberFilter(IsFhirElement), link.propertyName);
                    if (matchingFhirElements.Count() > 0)
                    {
                        link.propertyInfo = baseType.GetProperty(matchingFhirElements.First().Name);
                        //TODO: Ugly repetitive code from IsFhirElement(), since that method can only return a boolean...
                        FhirElementAttribute feAtt = link.propertyInfo.GetCustomAttribute<FhirElementAttribute>();
                        if (feAtt != null)
                        {
                            if (feAtt.Choice == ChoiceType.DatatypeChoice || feAtt.Choice == ChoiceType.ResourceChoice)
                            {
                                AllowedTypesAttribute atAtt = link.propertyInfo.GetCustomAttribute<AllowedTypesAttribute>();
                                if (atAtt != null)
                                {
                                    foreach (Type allowedType in atAtt.Types)
                                    {
                                        var curTypeName = link.propertyName.Remove(0, feAtt.Name.Length);
                                        Type curType = ModelInfo.GetTypeForFhirType(curTypeName);
                                        if (allowedType.IsAssignableFrom(curType))
//                                        if (link.propertyName.Equals(feAtt.Name + ModelInfo.FhirCsTypeToString[allowedType], StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            link.AllowedPropertyType = allowedType;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        link.propertyInfo = baseType.GetProperty(link.propertyName);
                    }
                    if (link.propertyInfo == null)
                        break;

                    links.Add(link);
                    //infoChain.Add(Tuple.Create<Type, string, PropertyInfo, Type>(baseType, propertyname, info, choiceType));

                    if (link.propertyInfo.PropertyType.IsGenericType)
                        //For instance AllergyIntolerance.Event, which is a List<Hl7.Fhir.Model.AllergyIntolerance.AllergyIntoleranceEventComponent>
                        baseType = link.propertyInfo.PropertyType.GetGenericArguments().First();
                    else if (link.AllowedPropertyType != null)
                        baseType = link.AllowedPropertyType;
                    else
                        baseType = link.propertyInfo.PropertyType;
                }

                return links;
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
                    (obj) => filterValue.Equals(obj.GetType().GetProperty(propertyName)?.GetValue(obj)?.ToString(), StringComparison.CurrentCultureIgnoreCase);

                return result;
            }

            private static bool IsFhirElement(MemberInfo m, object criterium)
            {
                string fhirElementName = (string)criterium;
                FhirElementAttribute feAtt = m.GetCustomAttribute<FhirElementAttribute>();

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
                            AllowedTypesAttribute atAtt = m.GetCustomAttribute<AllowedTypesAttribute>();
                            if (atAtt != null)
                            {
                                foreach (Type allowedType in atAtt.Types)
                                {
                                    var curTypeName = fhirElementName.Remove(0, feAtt.Name.Length);
                                    Type curType = ModelInfo.GetTypeForFhirType(curTypeName);
                                    if (allowedType.IsAssignableFrom(curType))
                                    //                                        if (link.propertyName.Equals(feAtt.Name + ModelInfo.FhirCsTypeToString[allowedType], StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        return true;
                                    }
                                    //if (fhirElementName.Equals(feAtt.Name + ModelInfo.FhirCsTypeToString[allowedType], StringComparison.InvariantCultureIgnoreCase))
                                    //    return true;
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
                Visit(field, this.links, action, null);
            }

            private void Visit(object field, IEnumerable<ChainLink> chain, Action<object> action, Predicate<object> predicate)
            {
                Type type = field.GetType();

                if (type.IsGenericType)
                {
                    var list = field as IEnumerable<object>;
                    if ((list != null) && (list.Count() > 0))
                    {
                        foreach (var subfield in list)
                        {
                            Visit(subfield, chain, action, predicate);
                        }
                    }
                    else
                    {
                        action(null);
                    }
                }
                else //single value
                { //Patient.address.city, current field is address
                    if (predicate == null || predicate(field))
                    {
                        if ((chain != null) && (chain.Count() > 0)) //not at the end of the chain, follow the next link in the chain
                        {
                            var nextLink = chain.First(); //{ FhirString, "city", (propertyInfo of city), AllowedTypes = null, Filter = null }
                            IEnumerable<ChainLink> subchain = chain.Skip(1); //subpath = <empty> (city is the last item)

                            object subfield = nextLink.propertyInfo.GetValue(field); //value of city

                            if (subfield != null && nextLink != null && nextLink.propertyInfo != null &&
                                (nextLink.AllowedPropertyType == null || nextLink.AllowedPropertyType.IsAssignableFrom(subfield.GetType()))
                               )
                            {
                                Visit(subfield, subchain, action, nextLink.Filter);
                            }
                            else
                                action(null);
                        }
                        else
                        {
                            action(field);
                        }
                    }
                }
            }

            /// <summary>
            /// Returns the property matching the propertyname, and if it is a FhirElement with DatatypeChoice or ResourceChoice, the allowed type as stated in the path.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="propertyname"></param>
            /// <returns></returns>

            //private Tuple<ChainLink, object> GetObjectProperty(object x, string propertyname)
            //{
            //    Type type = x.GetType();
            //    //var match = infoChain.Find(t => t.Item1 == type && t.Item2 == propertyname);
            //    var match = links.Find(t => t.FhirType == type && t.propertyName == propertyname);
            //    if (match != null && match.propertyInfo != null)
            //    {
            //        return Tuple.Create<ChainLink, object>(match, match.propertyInfo.GetValue(x));
            //    }
            //    else return null;
            //}

            //private static object GetObjectProperty(object x, string propertyname)
            //{
            //    Type type = x.GetType();
            //    PropertyInfo info;
            //    var matchingFhirElements = type.FindMembers(MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public, new MemberFilter(IsFhirElement), propertyname);
            //    if (matchingFhirElements.Count() > 0)
            //    {
            //        info = type.GetProperty(matchingFhirElements.First().Name);
            //    }
            //    else
            //    {
            //        info = type.GetProperty(propertyname);
            //    }
            //    if (info != null)
            //        return info.GetValue(x);
            //    else
            //        return null;

            //}

            public override string ToString()
            {
                return string.Join(".", chain);
            }

        }

        public override string ToString()
        {
            return string.Join(", ", chains.Select(chain => string.Join(".", chain)));
        }
    }

}