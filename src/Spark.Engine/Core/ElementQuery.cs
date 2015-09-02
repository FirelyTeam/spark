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
            public Chain(string path)
            {
                path = path.Replace("[x]", "");
                path = Regex.Replace(path, @"\b(\w)", match => match.Value.ToUpper());

                this.chain = path.Split('.').Skip(1).ToList();  // Skip(1), dat is nl. de class-naam zelf.
                fillInfoChain(path.Split('.').First());  // Dat is nl. de class-naam zelf.
            }

            //Cache for PropertyInfo for every link in the chain. Required to cache this for performance.
            // <Fhir type, property name, info of that property, specific type in case of a ChoiceType.DatatypeChoice>
            // Example: ClinicalImpression.trigger: <ClinicalImpression, "trigger", (propertyinfo of property Trigger), CodeableConcept>
            private List<Tuple<Type, string, PropertyInfo, Type>> infoChain = new List<Tuple<Type, string, PropertyInfo, Type>>();

            private void fillInfoChain(string classname)
            {
                Type baseType = ModelInfo.FhirTypeToCsType[classname];
                foreach (string propertyname in chain)
                {
                    PropertyInfo info;
                    Type choiceType = null;
                    var matchingFhirElements = baseType.FindMembers(MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public, new MemberFilter(IsFhirElement), propertyname);
                    if (matchingFhirElements.Count() > 0)
                    {
                        info = baseType.GetProperty(matchingFhirElements.First().Name);
                        //TODO: Ugly repetitive code from IsFhirElement(), since that method can only return a boolean...
                        FhirElementAttribute feAtt = info.GetCustomAttribute<FhirElementAttribute>();
                        if (feAtt != null)
                        {
                            if (feAtt.Choice == ChoiceType.DatatypeChoice || feAtt.Choice == ChoiceType.ResourceChoice)
                            {
                                AllowedTypesAttribute atAtt = info.GetCustomAttribute<AllowedTypesAttribute>();
                                if (atAtt != null)
                                {
                                    foreach (Type allowedType in atAtt.Types)
                                    {
                                        if (propertyname.Equals(feAtt.Name + ModelInfo.FhirCsTypeToString[allowedType], StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            choiceType = allowedType;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        info = baseType.GetProperty(propertyname);
                    }
                    if (info == null)
                        break;

                    infoChain.Add(Tuple.Create<Type, string, PropertyInfo, Type>(baseType, propertyname, info, choiceType));

                    if (info.PropertyType.IsGenericType)
                        //For instance AllergyIntolerance.Event, which is a List<Hl7.Fhir.Model.AllergyIntolerance.AllergyIntoleranceEventComponent>
                        baseType = info.PropertyType.GetGenericArguments().First();
                    else if (choiceType != null)
                        baseType = choiceType;
                    else
                        baseType = info.PropertyType;
                }
            }

            private static bool IsFhirElement(MemberInfo m, object criterium)
            {
                string fhirElementName = (string)criterium;
                FhirElementAttribute feAtt = m.GetCustomAttribute<FhirElementAttribute>();
                if (feAtt != null)
                {
                    if (feAtt.Choice == ChoiceType.DatatypeChoice || feAtt.Choice == ChoiceType.ResourceChoice)
                    {
                        AllowedTypesAttribute atAtt = m.GetCustomAttribute<AllowedTypesAttribute>();
                        if (atAtt != null)
                        {
                            foreach (Type allowedType in atAtt.Types)
                            {
                                if (fhirElementName.Equals(feAtt.Name + ModelInfo.FhirCsTypeToString[allowedType], StringComparison.InvariantCultureIgnoreCase))
                                    return true;
                            }
                        }
                    }
                    //else: normal fhir element, not a choice.
                    return fhirElementName.Equals(feAtt.Name, StringComparison.InvariantCultureIgnoreCase);
                }
                //if it has no FhirElementAttribute, it is not a FhirElement...
                return false;
            }

            public void Visit(object field, Action<object> action)
            {
                Visit(field, this.chain, action);
            }
            public void Visit(object field, IEnumerable<string> chain, Action<object> action)
            {
                Type type = field.GetType();

                if (type.IsGenericType)
                {
                    var list = field as IEnumerable<object>;
                    if ((list != null) && (list.Count() > 0))
                    {
                        foreach (var subfield in list)
                        {
                            Visit(subfield, chain, action);
                        }
                    }
                    else
                    {
                        action(null);
                    }
                }
                else
                {
                    if ((chain != null) && (chain.Count() > 0))
                    {
                        string name = chain.First();
                        IEnumerable<string> subpath = chain.Skip(1);

                        Tuple<object, Type> subProperty = GetObjectProperty(field, name);
                        object subfield = subProperty?.Item1;
                        Type allowedType = subProperty?.Item2;

                        if (subfield != null)
                        {
                            if(allowedType == null || allowedType.Equals(subfield.GetType()))
                                Visit(subfield, subpath, action);
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
            /// <summary>
            /// Returns the property matching the propertyname, and if it is a FhirElement with DatatypeChoice or ResourceChoice, the allowed type as stated in the path.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="propertyname"></param>
            /// <returns></returns>
            private Tuple<object, Type> GetObjectProperty(object x, string propertyname)
            {
                Type type = x.GetType();
                var match = infoChain.Find(t => t.Item1 == type && t.Item2 == propertyname);
                if (match != null)
                {
                    PropertyInfo info = match.Item3;
                    Type allowedType = match.Item4;
                    return Tuple.Create<object, Type>(info.GetValue(x), match.Item4);
                }
                else return null;
            }
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