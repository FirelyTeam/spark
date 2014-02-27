using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Spark.Support
{
    public static class ResourceInspector
    {
        //public static IEnumerable<Element> GetByPath(object item, string path)
        //{
        //    var qry = new ElementQuery(path);

        //    return GetByPath(item, qry);
        //}

        //public static IEnumerable<Element> GetByPath(object item, ElementQuery path)
        //{
        //    var result = new List<Element>();

        //    VisitByPath(item, path, (e,p) => result.Add(e));

        //    return result;
        //}

        //public static void VisitByPath(object item, string path, Action<Element, string> action)
        //{
        //    var chain = new ElementQuery(path);

        //    VisitByPath(item, chain, action);
        //}

        //public static void VisitByPath(object item, ElementQuery qry, Action<Element, string> action)
        //{
        //    // This filter returns true if the path of the property matches the filter given
        //    // in qry: scan() will return all Elements in item for which its path matches the
        //    // given query.
        //    scan(item, (pInfo, path, elem) => { if (qry.Matches(path)) action(elem, path); });
        //}

        //public static IEnumerable<Element> GetByType(object item, params Type[] filter)
        //{
        //    //typeof(Element).IsAssignableFrom(filter.GetType().GetGenericArguments()[0])

        //    //filter must contain subclasses of Element

        //    var result = new List<Element>();
        //    VisitByType(item, (elem,path) => result.Add(elem), filter);

        //    return result;
        //}

        public static void VisitByType(object item, Action<Element,string> action, params Type[] filter)
        {
            // This is a filter that returns true if the property in pInfo is a subtype
            // of one of the types given in the filter. Because of this, scan() returns
            // all Elements in item that are of the types in filter, or subclasses.
            Action<Type,string,Element> visitor = (type, path, elem) =>
                {
                    foreach (var t in filter)
                        if (t.IsAssignableFrom(type))
                            action(elem, path);
                };

            scan(item, visitor);
        }


        private static bool propertyFilter(MemberInfo mem, object arg)
        {
            // We prefilter on properties, so this cast is always valid
            PropertyInfo prop = (PropertyInfo)mem;

            // Return true if the property is either an Element or an
            // IEnumerable<Element>.
            bool isElementProperty = typeof(Element).IsAssignableFrom(prop.PropertyType);
            var collectionInterface = prop.PropertyType.GetInterface("IEnumerable`1");
            bool isElementCollection = false;

            if (collectionInterface != null)
            {
                var firstGenericArg = collectionInterface.GetGenericArguments()[0];
                isElementCollection = typeof(Element).IsAssignableFrom(firstGenericArg);
            }

            return isElementProperty || isElementCollection;
        }



        private static string joinPath(string old, string part)
        {
            if (!String.IsNullOrEmpty(old))
                return old + "." + part;
            else
                return part;
        }

        private static void scan(object item, Action<Type,string, Element> visitor, string path = null)
        {
            if (item == null) return;

            if (path == null) path = String.Empty;

            // Scan the object 'item' and find all properties of type Element of IEnumerable<Element>
            var result = item.GetType().FindMembers(MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public,
                             new MemberFilter(propertyFilter), null);
            
            // Do a depth-first traversal of the properties and their contents
            foreach (PropertyInfo property in result)
            {
                // If this member is an IEnumerable<Element>, go inside and recurse
                if (property.PropertyType.GetInterface("IEnumerable`1") != null)
                {
                    // Since we filter for Properties of Element or IEnumerable<Element>
                    // this cast should always work
                    var list = (IEnumerable<Element>)property.GetValue(item, null);

                    if (list != null)
                    {
                        int index = 0;
                        foreach (var elem in list)
                        {
                            var propertyPath = joinPath(path,property.Name + "[" + index.ToString() + "]");

                            if (elem != null)
                            {
                                visitor(elem.GetType(), propertyPath, elem);
                                scan(elem, visitor, propertyPath);
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
                        visitor(property.PropertyType, propertyPath, propValue);
                        scan(propValue, visitor, propertyPath);
                    }
                }
            }
        }
    }
}