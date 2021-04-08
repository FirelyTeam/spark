// /*
//  * Copyright (c) 2014, Furore (info@furore.com) and contributors
//  * See the file CONTRIBUTORS for details.
//  *
//  * This file is licensed under the BSD 3-Clause license
//  * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
//  */

namespace Spark.Engine.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Hl7.Fhir.Model;
    using Hl7.Fhir.Serialization;

    internal static class PatchExtensions
    {
        private enum Change
        {
            Delete,
            Replace,
            None
        }

        public static Parameters ToPatch<T>(this T current, T previous) where T : Resource
        {
            var operations = typeof(T).GetProperties()
                .Where(
                    p => p.SetMethod != null && typeof(DataType).IsAssignableFrom(p.PropertyType)
                         || p.PropertyType.IsGenericType
                         && typeof(List<>).IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())
                         && typeof(DataType).IsAssignableFrom(p.PropertyType.GenericTypeArguments[0]))
                .Select(p => CreateChangeTuple(current, previous, p))
                .Where(x => x.Item1 != Change.None)
                .SelectMany(x =>
                {
                    if (x.Item1 == Change.Delete)
                    {
                        return CreateDeleteParameter<T>(x.name);
                    }
                    if (x.Item3 is IEnumerable items)
                    {
                        return CreateDeleteParameter<T>(x.name)
                            .Concat(items.OfType<DataType>().Select(d => CreateSingleAddParameter<T>(x.name, d)));
                    }

                    return new[] { CreateSingleValueParameters<T>(x.name, x.Item3 as DataType) };
                });

            return new Parameters { Parameter = operations.ToList() };
        }

        public static Parameters ToPatch<T>(this T resource)
            where T : Resource
        {
            var operations = typeof(T).GetProperties()
                .Where(
                    p => typeof(DataType).IsAssignableFrom(p.PropertyType)
                         || p.PropertyType.IsGenericType
                         && typeof(List<>).IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())
                         && typeof(DataType).IsAssignableFrom(p.PropertyType.GenericTypeArguments[0]))
                .SelectMany(
                    p =>
                    {
                        var name = p.Name.Replace("Element", "");
                        name = char.ToLowerInvariant(name[0]) + name.Substring(1);
                        var value = p.GetValue(resource);
                        if (value is IEnumerable enumerable)
                        {
                            return CreateDeleteParameter<T>(name)
                                .Concat(
                                    enumerable.OfType<DataType>()
                                        .Select(v => CreateSingleAddParameter<T>(name, v)));
                        }

                        return new[]
                        {
                            CreateSingleValueParameters<T>(
                                name,
                                (DataType) value)
                        };
                    });

            return new Parameters { Parameter = operations.ToList() };
        }

        private static (Change, string name, object) CreateChangeTuple<T>(T current, T previous, PropertyInfo p)
            where T : Resource
        {
            var currentValue = p.GetValue(current);
            var previousValue = p.GetValue(previous);

            var name = p.Name.Replace("Element", "");
            name = char.ToLowerInvariant(name[0]) + name.Substring(1);

            if (currentValue == null)
            {
                return previousValue == null ? (Change.None, null, null) : (Change.Delete, name, (object)null);
            }

            if (currentValue is IEnumerable currentEnumerable)
            {
                var currentArray = new HashSet<string>(currentEnumerable.OfType<DataType>().Select(x => x.ToXml()));
                if (previousValue == null || !(previousValue is IEnumerable previousEnumerable))
                {
                    return (Change.Replace, name, currentValue);
                }

                var previousArray = new HashSet<string>(previousEnumerable.OfType<DataType>().Select(x => x.ToXml()));
                return currentArray.SetEquals(previousArray) ? (Change.None, name, null) : (Change.Replace, name, currentValue);
            }

            if (previousValue == null || ((DataType)previousValue).ToXml() != ((DataType)currentValue).ToXml())
            {
                return (Change.Replace, name, currentValue);
            }

            return (Change.None, null, null);
        }

        private static Parameters.ParameterComponent CreateSingleValueParameters<T>(string name, DataType value)
            where T : Resource
        {
            var operation = new Parameters.ParameterComponent { Name = "operation" };
            operation.Part.Add(
                new Parameters.ParameterComponent { Name = "path", Value = new FhirString(typeof(T).Name + "." + name) });
            if (value != null)
            {
                operation.Part.Add(new Parameters.ParameterComponent { Name = "type", Value = new Code("replace") });
                var item = value is PrimitiveType
                    ? new Parameters.ParameterComponent { Name = "value", Value = value }
                    : new Parameters.ParameterComponent { Name = "value", Part = { new Parameters.ParameterComponent { Value = value } } };
                operation.Part.Add(item);
            }
            else
            {
                operation.Part.Add(new Parameters.ParameterComponent { Name = "type", Value = new Code("delete") });
            }

            return operation;
        }

        private static Parameters.ParameterComponent CreateSingleAddParameter<T>(string name, DataType value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var operation = new Parameters.ParameterComponent { Name = "operation" };
            operation.Part.Add(
                new Parameters.ParameterComponent { Name = "path", Value = new FhirString(typeof(T).Name) });
            operation.Part.Add(new Parameters.ParameterComponent { Name = "type", Value = new Code("add") });
            operation.Part.Add(new Parameters.ParameterComponent { Name = "name", Value = new FhirString(name) });
            var item = value is PrimitiveType
                ? new Parameters.ParameterComponent { Name = "value", Value = value }
                : new Parameters.ParameterComponent { Name = "value", Part = { new Parameters.ParameterComponent { Value = value } } };
            operation.Part.Add(item);

            return operation;
        }

        private static IEnumerable<Parameters.ParameterComponent> CreateDeleteParameter<T>(string name)
        {
            var operation = new Parameters.ParameterComponent { Name = "operation" };
            operation.Part.Add(
                new Parameters.ParameterComponent { Name = "path", Value = new FhirString(typeof(T).Name + "." + name) });
            operation.Part.Add(new Parameters.ParameterComponent { Name = "type", Value = new Code("delete") });

            yield return operation;
        }
    }
}