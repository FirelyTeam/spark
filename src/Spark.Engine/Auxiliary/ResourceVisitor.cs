/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Spark.Engine.Auxiliary
{
    public delegate void Visitor(Element element, string path);
    
    public static class ResourceVisitor
    {
        public static void VisitByType(object item, Visitor action, params Type[] filter)
        {
            // This is a filter that returns true if the property in pInfo is a subtype
            // of one of the types given in the filter. Because of this, scan() returns
            // all Elements in item that are of the types in filter, or subclasses.
            Visitor visitor = (elem, path) =>
                {
                    foreach (var t in filter)
                    {
                        Type type = elem.GetType();
                        if (t.IsAssignableFrom(type))
                           action(elem, path);
                    }
                };

            scan(item, null, visitor);
        }

        private static bool propertyFilter(MemberInfo mem, object arg)
        {
            // We prefilter on properties, so this cast is always valid
            PropertyInfo prop = (PropertyInfo)mem;

            // Return true if the property is either an Element or an IEnumerable<Element>.
            bool hasIndexParameters = prop.GetIndexParameters().Length > 0;

            return (isElementProperty(prop) || isElementCollection(prop)) && hasIndexParameters == false;
        }

        private static bool isElementProperty(PropertyInfo prop) =>
            typeof(Element).IsAssignableFrom(prop.PropertyType);

        private static bool isElementCollection(PropertyInfo prop)
        {
            var collectionInterface = prop.PropertyType.GetInterface("IEnumerable`1");
            return collectionInterface != null && typeof(Element).IsAssignableFrom(
                    collectionInterface.GetGenericArguments()[0]);
        }

        private static string joinPath(string old, string part)
        {
            if (!string.IsNullOrEmpty(old))
                return old + "." + part;
            else
                return part;
        }

        private static void scan(object item, string path, Visitor visitor)
        {
            if (item == null) return;

            if (path == null) path = string.Empty;

            // Scan the object 'item' and find all properties of type Element of IEnumerable<Element>
            var result = item.GetType().FindMembers(MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public,
                             new MemberFilter(propertyFilter), null);
            
            // Do a depth-first traversal of the properties and their contents
            foreach (PropertyInfo property in result)
            {
                // If this member is an IEnumerable<Element>, go inside and recurse
                if (isElementCollection(property))
                {
                    // Since we filter for Properties of Element or IEnumerable<Element>
                    // this cast should always work
                    var list = (IEnumerable<Element>)property.GetValue(item, null);

                    if (list != null)
                    {
                        int index = 0;
                        foreach (var element in list)
                        {
                            var propertyPath = joinPath(path,property.Name + "[" + index.ToString() + "]");

                            if (element != null)
                            {
                                visitor(element, propertyPath);
                                scan(element, propertyPath, visitor);
                            }
                        }
                    }
                }

                // If this member is an Element, go inside and recurse
                else
                {
                    var propertyPath = joinPath(path,property.Name);

                    Element propValue = (Element)property.GetValue(item);
                   
                    // Look into the property to find nested elements
                    if (propValue != null)
                    {
                        visitor(propValue, propertyPath);
                        scan(propValue, propertyPath, visitor);
                    }
                }
            }
        }
    }
}