namespace Spark.Engine.Service.FhirServiceExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Hl7.Fhir.Language;
    using Hl7.Fhir.Model;
    using Hl7.FhirPath;
    using Expression = System.Linq.Expressions.Expression;
    using fhirExpression = Hl7.FhirPath.Expressions;

    public class PatchApplicationService : IPatchApplicationService
    {
        private readonly FhirPathCompiler _compiler;

        public PatchApplicationService()
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
                var value = component.Part.FirstOrDefault(x => x.Name == "value")?.Value ?? component.Part.FirstOrDefault(x => x.Name == "value")?.Part[0].Value;
                var valueExpression = System.Linq.Expressions.Expression.Constant(value);

                var parameterExpression = System.Linq.Expressions.Expression.Parameter(resource.GetType(), "x");
                var expression = operationType == "add" ? _compiler.Parse($"{path}.{name}") : _compiler.Parse(path);
                Expression result = expression.Accept(
                        new ResourceVisitor(parameterExpression),
                        new fhirExpression.SymbolTable());
                switch (operationType)
                {
                    case "add":
                        result = AddValue(result, valueExpression);
                        break;
                    case "insert":
                        result = InsertValue(result, valueExpression);
                        break;
                    case "replace":
                        result = System.Linq.Expressions.Expression.Assign(result, valueExpression);
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

                var compiled = System.Linq.Expressions.Expression.Lambda(result!, parameterExpression).Compile();
                compiled.DynamicInvoke(resource);
            }

            return resource;
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

        private static Expression InsertValue(Expression result, ConstantExpression valueExpression)
        {
            return result switch
            {
                IndexExpression indexExpression => Expression.Call(
                    indexExpression.Object,
                    GetMethod(indexExpression.Object!.Type, "Insert"),
                    new[] { indexExpression.Arguments[0], valueExpression }),
                _ => result
            };
        }

        private static Expression AddValue(Expression result, ConstantExpression value)
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
            private readonly ParameterExpression _parameter;

            public ResourceVisitor(ParameterExpression parameter)
            {
                _parameter = parameter;
            }

            /// <inheritdoc />
            public override System.Linq.Expressions.Expression VisitConstant(
                fhirExpression.ConstantExpression expression,
                fhirExpression.SymbolTable scope)
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
                        ? (Expression)_parameter
                        : System.Linq.Expressions.Expression.Property(_parameter, property);
                }

                return null;
            }

            /// <inheritdoc />
            public override System.Linq.Expressions.Expression VisitFunctionCall(
                fhirExpression.FunctionCallExpression expression,
                fhirExpression.SymbolTable scope)
            {
                switch (expression)
                {
                    case fhirExpression.IndexerExpression indexerExpression:
                        {
                            var index = indexerExpression.Index.Accept(this, scope);
                            var property = indexerExpression.Focus.Accept(this, scope);
                            var itemProperty = GetProperty(property.Type, "Item");
                            return Expression.MakeIndex(property, itemProperty, new[] { index });
                        }
                    case fhirExpression.ChildExpression child:
                        {
                            return child.Arguments.First().Accept(this, scope);
                        }
                    default:
                        return _parameter;
                }
            }

            /// <inheritdoc />
            public override System.Linq.Expressions.Expression VisitNewNodeListInit(
                fhirExpression.NewNodeListInitExpression expression,
                fhirExpression.SymbolTable scope)
            {
                return _parameter;
            }

            /// <inheritdoc />
            public override System.Linq.Expressions.Expression VisitVariableRef(fhirExpression.VariableRefExpression expression, fhirExpression.SymbolTable scope)
            {
                return _parameter;
            }
        }
    }
}
