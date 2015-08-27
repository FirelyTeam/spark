/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

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
            }
            public void Visit(object field, Action<object> action)
            {
                Visit(field, this.chain, action);
            }
            public static void Visit(object field, IEnumerable<string> chain, Action<object> action)
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

                        object subfield = GetObjectProperty(field, name);
                        if (subfield != null)
                            Visit(subfield, subpath, action);
                        else
                            action(null);
                    }
                    else
                    {
                        action(field);
                    }
                }
            }
            private static object GetObjectProperty(object x, string propertyname)
            {
                Type type = x.GetType();
                PropertyInfo info = type.GetProperty(propertyname);
                if (info != null)
                    return info.GetValue(x);
                else
                    return null;

            }
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