/*
 * Copyright (c) 2022-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Hl7.Fhir.Language;
using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Hl7.Fhir.Utility;
using Hl7.Fhir.Introspection;
using Expression = System.Linq.Expressions.Expression;
using fhirExpression = Hl7.FhirPath.Expressions;

namespace Spark.Engine.Service.FhirServiceExtensions;

public class PatchService : IPatchService
{
    private readonly FhirPathCompiler _compiler;

    public PatchService()
    {
        _compiler = new FhirPathCompiler();
    }

    public Resource Apply(Resource resource, Parameters patch)
    {
        foreach (var component in patch.Parameter.Where(x => x.Name == "operation"))
        {
            var operationType = component.Part.First(x => x.Name == "type").Value.ToString();
            var path = component.Part.First(x => x.Name == "path").Value.ToString();
            var name = component.Part.FirstOrDefault(x => x.Name == "name")?.Value.ToString();
            var valuePart = component.Part.FirstOrDefault(x => x.Name == "value") 
                            ?? component.Part.FirstOrDefault(x => x.Name == "value");

            var parameterExpression = Expression.Parameter(resource.GetType(), "x");
            var expression = operationType == "add" ? _compiler.Parse($"{path}.{name}") : _compiler.Parse(path);
            var result = expression.Accept(new ResourceVisitor(parameterExpression));
            switch (operationType)
            {
                case "add":
                    result = AddValue(result, CreateValueExpression(valuePart, result.Type));
                    break;
                case "insert":
                    var insertIndex = int.Parse(component.Part.First(x => x.Name == "index").Value.ToString()!);
                    result = InsertValue(result, CreateValueExpression(valuePart, result.Type), insertIndex);
                    break;
                case "replace":
                    result = Expression.Assign(result, CreateValueExpression(valuePart, result.Type));
                    break;
                case "delete":
                    result = DeleteValue(result);
                    break;
                case "move":
                    var source = int.Parse(component.Part.First(x => x.Name == "source").Value.ToString()!);
                    var destination = int.Parse(component.Part.First(x => x.Name == "destination").Value.ToString()!);
                    result = MoveItem(result, source, destination);
                    break;
            }

            var compiled = Expression.Lambda(result!, parameterExpression).Compile();
            compiled.DynamicInvoke(resource);
        }

        return resource;
    }
        
    private static Expression CreateValueExpression(Parameters.ParameterComponent part, Type resultType)
    {
        resultType = part.Value == null && resultType.IsGenericType ? resultType.GenericTypeArguments[0] : resultType;
        return part.Value == null
            ? Expression.MemberInit(
                Expression.New(resultType.GetConstructor(Array.Empty<Type>())),
                GetPartsBindings(part.Part, resultType))
            : GetConstantExpression(part.Value, resultType);
    }
        
    private static Expression GetConstantExpression(DataType value, Type valueType)
    {
        Expression FromString(string str, Type targetType)
        {
            return targetType.CanBeTreatedAsType(typeof(DataType))
                ? (Expression) Expression.MemberInit(
                    Expression.New(targetType.GetConstructor(Array.Empty<Type>())),
                    Expression.Bind(
                        targetType.GetProperty("ObjectValue"),
                        Expression.Constant(str)))
                : Expression.Constant(str);
        }
            
        return value switch
        {
            Code code => FromString(code.Value, valueType == typeof(DataType) ? typeof(Code) : valueType),
            FhirUri uri => FromString(uri.Value, valueType == typeof(DataType) ? typeof(FhirUri) : valueType),
            FhirString s => FromString(s.Value, valueType == typeof(DataType) ? typeof(FhirString) : valueType),
            _ => Expression.Constant(value)
        };
    }

    private static IEnumerable<MemberBinding> GetPartsBindings(List<Parameters.ParameterComponent> parts, Type resultType)
    {
        foreach (var partGroup in parts.GroupBy(x => x.Name))
        {
            var property = resultType.GetProperties().Single(
                p => p.GetCustomAttribute<FhirElementAttribute>()?.Name == partGroup.Key);
            if (property.PropertyType.IsGenericType)
            {
                var listExpression = GetCollectionExpression(property, partGroup);
                yield return Expression.Bind(property, listExpression);
            }
            else
            {
                var propertyValue = CreateValueExpression(partGroup.Single(), property.PropertyType);
                yield return Expression.Bind(property, propertyValue);
            }
        }
    }

    private static Expression GetCollectionExpression(PropertyInfo property, IEnumerable<Parameters.ParameterComponent> parts)
    {
        var variableExpr = Expression.Variable(property.PropertyType);
        return Expression.Block(new [] {variableExpr}, GetCollectionCreationExpressions(variableExpr, property, parts));
    }

    private static IEnumerable<Expression> GetCollectionCreationExpressions(ParameterExpression variableExpr, PropertyInfo property, IEnumerable<Parameters.ParameterComponent> parts)
    {
        LabelTarget returnTarget = Expression.Label(property.PropertyType);
            
        GotoExpression returnExpression = Expression.Return(returnTarget, 
            variableExpr, property.PropertyType);

        LabelExpression returnLabel = Expression.Label(returnTarget, Expression.New(property.PropertyType.GetConstructor(Array.Empty<Type>())));

        yield return Expression.Assign(variableExpr, Expression.New(property.PropertyType.GetConstructor(Array.Empty<Type>())));
        foreach (var part in parts)
        {
            yield return Expression.Call(variableExpr, GetMethod(variableExpr.Type, "Add"),
                CreateValueExpression(part, property.PropertyType));
        }

        yield return returnExpression;
        yield return returnLabel;
    }

    private static Expression MoveItem(Expression result, int source, int destination)
    {
        var propertyInfo = GetProperty(result.Type, "Item");
        var variable = Expression.Variable(propertyInfo.PropertyType, "item");
        var block = Expression.Block(
            new[] { variable },
            Expression.Assign(
                variable,
                Expression.MakeIndex(result, propertyInfo, new[] { Expression.Constant(source) })),
            Expression.Call(result, GetMethod(result.Type, "RemoveAt"), Expression.Constant(source)),
            Expression.Call(
                result,
                GetMethod(result.Type, "Insert"),
                Expression.Constant(Math.Max(0, destination - 1)),
                variable));
        return block;
    }

    private static Expression InsertValue(Expression result, Expression valueExpression, int insertIndex)
    {
        return result switch
        {
            MemberExpression me when me.Type.IsGenericType
                                     && GetMethod(me.Type, "Insert") != null =>
                Expression.Block(
                    Expression.IfThen(
                        Expression.Equal(me, Expression.Default(result.Type)),
                        Expression.Throw(Expression.New(typeof(InvalidOperationException)))),
                    Expression.Call(me, GetMethod(me.Type, "Insert"), Expression.Constant(insertIndex),
                        valueExpression)),
            _ => result
        };
    }

    private static Expression AddValue(Expression result, Expression value)
    {
        return result switch
        {
            MemberExpression me when me.Type.IsGenericType
                                     && GetMethod(me.Type, "Add") != null =>
                Expression.Block(
                    Expression.IfThen(
                        Expression.Equal(me, Expression.Default(result.Type)),
                        Expression.Throw(Expression.New(typeof(InvalidOperationException)))),
                    Expression.Call(me, GetMethod(me.Type, "Add"), value)),

            MemberExpression me => Expression.Block(
                Expression.IfThen(
                    Expression.NotEqual(me, Expression.Default(result.Type)),
                    Expression.Throw(Expression.New(typeof(InvalidOperationException)))),
                Expression.Assign(me, value)),

            _ => result
        };
    }

    private static Expression DeleteValue(Expression result)
    {
        return result switch
        {
            IndexExpression indexExpression => Expression.Call(
                indexExpression.Object,
                GetMethod(indexExpression.Object!.Type, "RemoveAt"),
                indexExpression.Arguments),
            MemberExpression me when me.Type.IsGenericType
                                     && typeof(List<>).IsAssignableFrom(me.Type.GetGenericTypeDefinition()) =>
                Expression.Call(me, GetMethod(me.Type, "Clear")),
            MemberExpression me => Expression.Assign(me, Expression.Default(me.Type)),
            _ => result
        };
    }

    private static MethodInfo GetMethod(Type constantType, string methodName)
    {
        var propertyInfos = constantType.GetMethods();
        var property =
            propertyInfos.FirstOrDefault(p => p.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));

        return property;
    }

    private static PropertyInfo GetProperty(Type constantType, string propertyName)
    {
        var propertyInfos = constantType.GetProperties();
        var property =
            propertyInfos.FirstOrDefault(p => p.Name.Equals(propertyName + "Element", StringComparison.OrdinalIgnoreCase))
            ?? propertyInfos.FirstOrDefault(x => x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

        return property;
    }

    private class ResourceVisitor : fhirExpression.ExpressionVisitor<Expression>
    {
        private readonly Expression _parameter;

        public ResourceVisitor(Expression parameter)
        {
            _parameter = parameter;
        }

        /// <inheritdoc />
        public override Expression VisitConstant(fhirExpression.ConstantExpression expression)
        {
            if (expression.ExpressionType == TypeSpecifier.Integer)
            {
                return Expression.Constant((int)expression.Value);
            }

            if (expression.ExpressionType == TypeSpecifier.String)
            {
                var propertyName = expression.Value.ToString();
                var property = GetProperty(_parameter.Type, propertyName);
                return property == null
                    ? _parameter
                    : Expression.Property(_parameter, property);
            }

            return null;
        }

        /// <inheritdoc />
        public override Expression VisitFunctionCall(fhirExpression.FunctionCallExpression expression)
        {
            switch (expression)
            {
                case fhirExpression.IndexerExpression indexerExpression:
                    {
                        var index = indexerExpression.Index.Accept(this);
                        var property = indexerExpression.Focus.Accept(this);
                        var itemProperty = GetProperty(property.Type, "Item");
                        return Expression.MakeIndex(property, itemProperty, new[] { index });
                    }
                case fhirExpression.ChildExpression child:
                    {
                        var focus = child.Focus?.Accept(this);
                        return child.Arguments.First().Accept(new ResourceVisitor(focus));
                    }
                default:
                    return _parameter;
            }
        }

        /// <inheritdoc />
        public override Expression VisitNewNodeListInit(fhirExpression.NewNodeListInitExpression expression)
        {
            return _parameter;
        }

        /// <inheritdoc />
        public override Expression VisitVariableRef(fhirExpression.VariableRefExpression expression)
        {
            return _parameter;
        }
    }
}