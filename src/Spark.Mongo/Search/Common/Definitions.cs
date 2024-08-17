/* 
 * Copyright (c) 2014-2018, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Mongo.Search.Common
{
    /* 
    Ik heb deze class losgetrokken van SearchParamDefinition,
    omdat Definition onafhankelijk van Spark zou moeten kunnen bestaan.
    Er komt dus een converter voor in de plaats. -mh
    */

    public class Definition
    {
        public Argument Argument { get; set; }
        public string Resource { get; set; }
        public string ParamName { get; set; }
        public string Description { get; set; }
        public SearchParamType ParamType { get; set; }
        public ElementQuery Query { get; set; }

        public bool Matches(object x)
        {
            string objectname = x.GetType().Name;
            return string.Equals(objectname, Resource, StringComparison.OrdinalIgnoreCase);
        }
        public bool Matches(string resource, string field)
        {
            if (resource != null && field != null)
            {
                return (Resource.ToLower() == resource.ToLower())
                   &&
                   (ParamName.ToLower() == field.ToLower());
            }
            else return false;

        }
        public override string ToString()
        {
            _ = Query.ToString();
            return string.Format("{0}.{1}->{2}", Resource.ToLower(), ParamName.ToLower(), Query.ToString());
        }
        public void Harvest(object item, Action<Definition, object> harvest)
        {
            Query.Visit(item, (element) => harvest(this, element));
        }
    }

    public class Definitions
    {
        private List<Definition> _definitions = new List<Definition>();

        public void Add(Definition definition)
        {
            _definitions.Add(definition);
        }
        public void Replace(Definition definition)
        {
            _definitions.RemoveAll(d => (d.Resource == definition.Resource) && (d.ParamName == definition.ParamName));
            _definitions.Add(definition);
        }

        public IEnumerable<Definition> MatchesFor(object x)
        {
            string objectname = x.GetType().Name;

            return MatchesFor(objectname);
        }

        public IEnumerable<Definition> MatchesFor(string resourceType)
        {
            return _definitions.Where(d => d.Resource == resourceType);
        }

        public Definition Find(string resource, string field)
        {
            return _definitions.Find(d => d.Matches(resource, field));
        }

        public Argument FindArgument(string resource, string field)
        {
            Definition definition = Find(resource, field);
            if (definition != null)
                return definition.Argument;
            else
                return null;
        }

        public Argument DetermineUniversalArgument(string field)
        {
            if (InternalField.All.Contains(field))
                return new Argument();

            switch (field.ToLower())
            {
                case UniversalField.ID:
                    return new Argument();
                case UniversalField.TAG:
                    return new TagArgument();

                case MetaField.COUNT:
                    return new Argument();
                case MetaField.INCLUDE:
                    return new Argument();
                case MetaField.LIMIT:
                    return new Argument();

                default: 
                    return null;
            }
        }

        public Argument GuessArgument(string field)
        {
            var query =
                from d in _definitions
                where (d.ParamName == field)
                group d by d.ParamType into pgroup
                let count = pgroup.Count()
                orderby count descending
                select new { ParamType = pgroup.Key, Count = count };

            //for testing: 
            var pg = query.ToList();

            Argument argument = (pg.Any()) ? ArgumentFactory.Create(pg.First().ParamType) : null;
            return argument;
        }
    }
}