/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using Spark.Mongo.Search.Common;

namespace Spark.Search.Mongo
{
    public class ParameterFactory
    {
        private Definitions definitions;
        string resource;
        public ParameterFactory(Definitions definitions, string resource)
        {
            this.definitions = definitions;
            this.resource = resource;
        }

        private static void split(string s, char glue, out string left, out string right)
        {
            string[] parts = s.Split(glue);
            left = parts[0];
            right = (parts.Count() > 1) ? parts[1] : null;
        }
        public IParameter ResourceParameter(string name)
        {
            return CreateParameter(null, InternalField.RESOURCE, name);
        }
        public IParameter ResourceParameter()
        {
            return this.ResourceParameter(this.resource);
        }
       
        private Argument DetermineArgument(ITerm term)
        {
            Argument argument = null;

            if (term.Operator == Modifier.MISSING)
                argument = new MissingArgument();

            if (argument == null)
                argument = definitions.DetermineUniversalArgument(term.Field);

            if (argument == null) 
                argument = definitions.FindArgument(term.Resource, term.Field);

            if (argument == null)
                argument = definitions.GuessArgument(term.Field);

            return argument;

        }
        private Strain DetermineStrain(IParameter parameter)
        {
            // Fall through order here is relevant!
            if (parameter is ChainedParameter)
            {
                return Strain.Chained;
            }
            else if (parameter is Parameter)
            {
                Parameter p = (Parameter)parameter;
                if (p.Value == null)
                {
                    return Strain.Empty;
                }
                if (InternalField.All.Contains(p.Field))
                {
                    return Strain.Internal;
                }

                if (UniversalField.All.Contains(p.Field))
                {
                    return Strain.Universal;
                }

                if (MetaField.All.Contains(p.Field))
                {
                    return Strain.Meta;
                }

                if (p.Argument != null)
                {
                    return Strain.Simple;
                }
                return Strain.Undefined;
            }
            else return Strain.Undefined;
            
        }
        
        public void ParseKey(string key, Parameter parameter)
        {
            string field, _operator;
            split(key, ':', out field, out _operator);

            parameter.Field = field;
            parameter.Operator = _operator;
        }

        private void ParseValueToTerm(string value, ITerm term)
        {
            if (term.Argument != null)
                term.Argument.ParseTermValue(value, term);
            else
                term.Value = value;
        }

        private void ParseJoins(string key, ChainedParameter parameter)
        {
            

        }
        public ChainedParameter CreateChainedParameter(string resource, string key, string value)
        {
            ChainedParameter parameter = new ChainedParameter();
            parameter.Strain = Strain.Chained;

            string[] segments = key.Split('.');
            string field, modifier;

            int last = segments.Count() - 1;
            for (int i = 0; i < last; i++)
            {
                Join join = new Join();
                split(segments[i], ':', out field, out modifier);
                
                join.Field = field;
                join.Resource = resource;
                join.Argument = this.DetermineArgument(join);
                
                resource = modifier;
                parameter.Joins.Add(join);
            }

            string _key = segments[last];// field+modifier;
            parameter.Parameter = this.ExtractParameter(resource, _key, value);

            return parameter;
        }
        public IncludeParameter CreateIncludeParameter(string resource, string key, string value)
        {
            IncludeParameter p = new IncludeParameter();
            p.Strain = Strain.Meta;
            p.TargetResource = value.Split('.').First();
            p.TargetField = value.Split('.').Last();
            return p;
        }
        public Parameter CreateSimpleParameter(string resource, string key, string value)
        {
            Parameter parameter = new Parameter();
            parameter.Resource = resource;
            ParseKey(key, parameter);
            parameter.Argument = DetermineArgument(parameter);
            ParseValueToTerm(value, parameter);
            parameter.Strain = DetermineStrain(parameter);
            
            return parameter;
        }

        public IEnumerable<string> ExtractValues(string input, string separator)
        {
            string pattern = "(\"[^\"]+\"|[^"+separator+"])+";
            MatchCollection matches = Regex.Matches(input, pattern);
            foreach (var item in matches)
            {
                yield return item.ToString().Trim().Trim('"');
            }
        }

        public IParameter CreateParameter(string resource, string key, string value)
        {

            if (key == null) key = ""; // for parameters that have no key. url?value
            if (key.Contains('.'))
                return CreateChainedParameter(resource, key, value);
            else if (key == MetaField.INCLUDE)
                return CreateIncludeParameter(resource, key, value);
            else
                return CreateSimpleParameter(resource, key, value);
        }
        public CompositeParameter CreateComposite(string resource, string key, IEnumerable<string> values)
        {
            CompositeParameter composite = new CompositeParameter();
            composite.Logic = Logic.Or;
            //List<IParameter> parameters = new List<IParameter>();
            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    IParameter p = CreateParameter(resource, key, value);
                    composite.parameters.Add(p as Parameter);
                }
            }
            return composite;
        }
        public IParameter ExtractParameter(string resource, string key, string valuesstring)
        {
            List<string> values = ExtractValues(valuesstring, ",").ToList();
            
            if (key.Contains('.'))
                return CreateChainedParameter(resource, key, valuesstring); //omdat een chained parameter ZELF weer een Composite kan bevatten, moet dit vooraf!

            switch (values.Count())
            {
                case 0:
                    return CreateParameter(resource, key, null);
                case 1:
                    return CreateParameter(resource, key, values[0]);
                default:
                    return CreateComposite(resource, key, values);
            }
        }
        
        public List<IParameter> CreateParameters(string resource, NameValueCollection collection)
        {
            List<IParameter> parameters = new List<IParameter>();
            foreach (string key in collection.AllKeys)
            {
                IParameter p = ExtractParameter(resource, key, collection[key]);
                parameters.Add(p);
            }
            return parameters;
        }
        public List<IParameter> CreateParametersFromHttpQuery(string resource, string query)
        {
            List<Tuple<string, string>> tuples = new List<Tuple<string, string>>();
            List<string> parameters = query.Split('&').ToList();
            foreach(string p in parameters)
            {
                string[] s = p.Split('=');
                if (s.Count() == 2)
                {
                    tuples.Add(new Tuple<string, string>(s[0], s[1]));
                }
            }
            //NameValueCollection collection = System.Web.HttpUtility.ParseQueryString(query);

            return CreateParameters(resource, tuples);
        }
        public List<IParameter> CreateParameters(string resource, IEnumerable<Tuple<string, string>> collection)
        {
            List<IParameter> parameters = new List<IParameter>();
            foreach (Tuple<string, string> tuple in collection)
            {
                IParameter p = ExtractParameter(resource, tuple.Item1, tuple.Item2 ?? string.Empty);
                parameters.Add(p);
            }
            return parameters;
        }
        
        public static Parameters Parameters(Definitions definitions, string resource)
        {
            ParameterFactory factory = new ParameterFactory(definitions, resource);
            Parameters parameters = new Parameters();
            parameters += factory.ResourceParameter();
            return parameters;
        }
        public static Parameters Parameters(Definitions definitions, string resource, string query)
        {
            ParameterFactory factory = new ParameterFactory(definitions, resource);
            Parameters parameters = new Parameters();
            parameters += factory.ResourceParameter();
            parameters += factory.CreateParametersFromHttpQuery(resource, query);
            return parameters;
        }
        public static Parameters Parameters(Definitions definitions, string resource, IEnumerable<Tuple<string, string>> query)
        {
            ParameterFactory factory = new ParameterFactory(definitions, resource);
            Parameters parameters = new Parameters();
            parameters += factory.ResourceParameter();
            parameters += factory.CreateParameters(resource, query);
            return parameters;
        }


        public static Search.Mongo.Parameters Parameters(Definitions definitions, string resource, IEnumerable<Criterium> criteria)
        {
            ParameterFactory factory = new ParameterFactory(definitions, resource);
            Parameters parameters = new Parameters();
            parameters += factory.ResourceParameter();
            parameters += factory.CreateParameters(resource, criteria);
            return parameters;
        }

        private Search.Mongo.Parameters CreateParameters(string resource, IEnumerable<Criterium> criteria)
        {
            throw new NotImplementedException();
        }
    }
}