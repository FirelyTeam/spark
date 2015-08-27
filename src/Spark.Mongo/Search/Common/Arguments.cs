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

using System.Text.RegularExpressions;
using F = Hl7.Fhir.Model;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using Spark.Search.Mongo;

namespace Spark.Mongo.Search.Common
{
    public class Argument
    {
        public virtual string GroomElement(string value)
        {
            return value;
        }
        public virtual void ParseTermValue(string value, ITerm term)
        {
            term.Value = value;
        }
        public virtual string ValueToString(ITerm term)
        {
            return term.Value;
        }
        public virtual string FieldToString(ITerm term)
        {
            return term.Operator != null ? term.Field + ":" + term.Operator : term.Field;
        }
        public virtual bool Validate(string value)
        {
            return true;
        }
        public virtual IMongoQuery BuildQuery(ITerm term)
        {
            string field = FieldToInternalField(term.Field);
            return Query.EQ(field, term.Value);
        }
        private static string FieldToInternalField(string field)
        {
            if (Config.Equal(field, UniversalField.ID)) field = InternalField.JUSTID;
            return field;
        }
    }

    public class MetaArgument : Argument
    {
        private string field;
        public MetaArgument(string field)
        {
            this.field = field;
        }
        public override IMongoQuery BuildQuery(ITerm term)
        {
            return Query.EQ(term.Field, term.Value); 
        }
    }
   
    public class StringArgument : Argument
    {
        public override void ParseTermValue(string value, ITerm term)
        {
            term.Value = value;
        }

        public override string ValueToString(ITerm term)
        {
            return "\"" + term.Value + "\"";
        }

        public override IMongoQuery BuildQuery(ITerm term)
        {
            string m = (term.Operator ?? string.Empty).ToLower();
            switch (m)
            {
                case "exact": 
                    return Query.EQ(term.Field, term.Value);
                case "partial":
                    return Query.Matches(term.Field, new BsonRegularExpression("^"+term.Value, "i")); //
                case "phonetic":
                    return Query.Matches(term.Field+"soundex", "^"+term.Value);
                default:
                    return Query.Matches(term.Field, new BsonRegularExpression(term.Value, "i")); //full ci
            }
        }
    }
    public class TextArgument : Argument
    {
        public override IMongoQuery BuildQuery(ITerm term)
        {
            IEnumerable<IMongoQuery> queries = term.Value.Split(' ').Select(s => Query.Matches(term.Field, new BsonRegularExpression(s, "i")));
            return Query.And(queries);
        }
    }

    public class IntArgument : Argument
    {
        public override string GroomElement(string value)
        {
            if (value != null)
                return value.Trim();
            else
                return null;
        }
        public override void ParseTermValue(string value, ITerm term)
        {
            Match m;
            m = Regex.Match(value, "[<>=]+");
            if (!string.IsNullOrEmpty(m.Value))
                term.Operator = m.Value;
            else
                term.Operator = "=";

            m = Regex.Match(value, "[0-9]+");
            term.Value = m.Value;
        }

        public override string ValueToString(ITerm term)
        {
            return term.Operator + term.Value;
        }
        public override bool Validate(string value)
        {
            int i;
            return int.TryParse(value, out i);
        }
        public override IMongoQuery BuildQuery(ITerm term)
        {
            switch(term.Operator)
            {
                case "<":
                    return Query.LT(term.Field, term.Value);
                case ">":
                    return Query.GT(term.Field, term.Value);
                case "<=":
                    return Query.LTE(term.Field, term.Value);
                case ">=":
                    return Query.GTE(term.Field, term.Value);
                case null:
                case "=":
                case "":
                    return Query.EQ(term.Field, term.Value);
                default:
                    throw new SearchException(term.Operator+" is not a valid integer operator");

            }
        }
    }

    public class TokenArgument : Argument
    {
        /*
        name=namespace/code specifies matches on both the namespace and the code
        name=code matches a code that has no specified namespace
        name=code matches all codes irrespective of the namespace
        */
        public static void SplitToken(string token, out string system, out string code)
        {
            int index = token.LastIndexOf('|');
            if (index > 0)
            {
                code = token.Substring(index+1);
                system = token.Substring(0, index);
            }
            else
            {
                code = token;
                system = null;
            }


        }
        
        public override void ParseTermValue(string value, ITerm term)
        {
            if (value == null)
                term.Value = null;
            else
                term.Value = value;
        }
        enum TokenStyle { Partial, Text, Code, AnyNs }

        public override IMongoQuery BuildQuery(ITerm term)
        {
            TokenStyle style;
            string system, code;
            string systemfield = term.Field+".system";
            string codefield = term.Field+".code";
            string displayfield = term.Field+".display";
            string textfield = term.Field + "_text";

            SplitToken(term.Value, out system, out code);
            
            if (string.IsNullOrEmpty(term.Operator))
            {
                style = TokenStyle.Partial;
            }
            else if (term.Operator == Modifier.TEXT)
            {
                style = TokenStyle.Text;
            }
            else if (term.Operator == Modifier.CODE)
            {
                style = TokenStyle.Code;
            }
            else if (term.Operator == Modifier.ANYNAMESPACE)
            {
                style = TokenStyle.AnyNs;
            }
            else throw new SearchException("Invalid Modifier: " + term.Operator);

            switch(style)
            {
                case TokenStyle.Partial:
                    return Query.Or(
                        Query.Matches(codefield, new BsonRegularExpression(term.Value, "i")),
                        Query.Matches(textfield, new BsonRegularExpression(term.Value, "i")),
                        Query.Matches(displayfield, new BsonRegularExpression(term.Value, "i"))
                    );  

                case TokenStyle.Text:
                    return Query.Or(
                        Query.Matches(textfield, new BsonRegularExpression(term.Value, "i")),
                        Query.Matches(displayfield, new BsonRegularExpression(term.Value, "i"))
                    );

                case TokenStyle.Code:
                    if (system == null)
                    {
                        return Query.ElemMatch(term.Field, 
                                Query.And(
                                    Query.NotExists("system"),
                                    Query.EQ("code", code)
                                )
                        );  

                    }
                    else
                    {
                        return Query.ElemMatch(term.Field,
                            Query.And(
                                Query.EQ("system", system),
                                Query.EQ("code", code)
                            )
                        );  
                    }

                case TokenStyle.AnyNs:
                    return Query.EQ(codefield, term.Value);

                default:
                    throw new SearchException("Impossible to get here.");
            }
        }
    }

    public class TagArgument : Argument
    {
        public override IMongoQuery BuildQuery(ITerm term)
        {
            IMongoQuery query;
            
            if (string.IsNullOrEmpty(term.Operator))
            {
                query = Query.EQ(InternalField.TAGTERM, term.Value);
            }
            else if (term.Operator == Modifier.PARTIAL)
            {
                query = Query.Matches(InternalField.TAGTERM, "^" + term.Value); // from the left
            }
            else if (term.Operator == Modifier.TEXT)
            {
                query = Query.EQ(InternalField.TAGLABEL, term.Value);

            }
            else
            {
                throw new SearchException("Invalid Modifier: " + term.Operator);
            }

            string scheme = "http://hl7.org/fhir/tag";
            
            return Query.ElemMatch(InternalField.TAG,
                    Query.And(
                        Query.EQ(InternalField.TAGSCHEME, scheme),
                        query
                    ));  
        }
    }

    public class ReferenceArgument : Argument
    {
        private string Groom(string value)
        {
            if (value != null)
            {
                //value = Regex.Replace(value, "/(?=[^@])", "/@"); // force include @ after "/", so "patient/10" becomes "patient/@10"
                return value.Trim();
            }
            else
            {
                return null;
            }
        }
        public override string GroomElement(string value)
        {
            return this.Groom(value);
                
        }
        public override void ParseTermValue(string value, ITerm term)
        {
            term.Value = this.Groom(value);
        }
        public override IMongoQuery BuildQuery(ITerm term)
        {
            //TODO: Find a real solution for this, because I don't think it will work for
            //a:Patient.b:Organization.c:Device=someid ?
            if(!String.IsNullOrEmpty(term.Operator))
                return Query.EQ(term.Field, term.Operator + "/" + term.Value);
            else
                return Query.Matches(term.Field, new BsonRegularExpression(".*" + term.Value));
            //return Query.EQ(term.Field, term.Value);
        }

    }

    public class DateArgument : Argument
    {
        private string Groom(string value)
        {
            if (value != null)
            {
                string s = Regex.Replace(value, @"[T\s:\-]", "");
                int i = s.IndexOf('+');
                if (i > 0) s = s.Remove(i);
                return s;
            }
            else
                return null;
        }
        public override string GroomElement(string value)
        {
            return Groom(value);
        }
        public override IMongoQuery BuildQuery(ITerm term)
        {
            string start = term.Field + ".start";
            string end = term.Field + ".end";
            string value = Groom(term.Value);
            if (string.IsNullOrEmpty(term.Operator))
            {
                // omdat date parameter zowel voor datums als voor Periods wordt gebruikt, kan je helaas niets anders doen dan hier een OR van maken.

                return 
                    Query.Or(
                        Query.Matches(term.Field, "^"+value),
                        Query.And(
                            Query.Or(Query.Exists(start), Query.Exists(end)),
                            Query.Or(Query.LTE(start, value), Query.NotExists(start)),
                            Query.Or(Query.GTE(end, value), Query.NotExists(end))
                        )
                    );
            }
            if (term.Operator == Modifier.AFTER)
            {
                return
                    Query.Or(
                        Query.GTE(term.Field, value),
                        Query.GTE(start, value)
                    );

            }
            else if (term.Operator == Modifier.BEFORE)
            {
                return
                    Query.Or(
                        Query.LTE(term.Field, value),
                        Query.LTE(end, value)
                    );
            }
            else
            {
                throw new SearchException(string.Format("The modifier [{0}] is invalid for date arguments", term.Operator));
            }

        }
    }

    public class FuzzyArgument : Argument
    {
        public override string GroomElement(string value)
        {
            return Soundex.For(value);
        }
        public override IMongoQuery BuildQuery(ITerm term)
        {
            string value = Soundex.For(term.Value);
            return Query.EQ(term.Field, value);
        }
    }

    public class MissingArgument : Argument
    {

        public override IMongoQuery BuildQuery(ITerm term)
        {
            switch (term.Value.ToLower())
            {
                case "true":
                    return Query.NotExists(term.Field);
                case "false":
                    return Query.Exists(term.Field);
                default:
                    throw new SearchException("Parameter ("+term.Field+") has a missing modifier and should have a value of 'true' or 'false'");
            }
        }

    }

    public static class ArgumentFactory
    {
        public static Argument Create(F.Conformance.SearchParamType type, bool fuzzy=false)
        {
            switch (type)
            {
                case F.Conformance.SearchParamType.Number:
                    return new IntArgument();
                case F.Conformance.SearchParamType.String:
                    return new StringArgument();
                case F.Conformance.SearchParamType.Date:
                    return new DateArgument();
                case F.Conformance.SearchParamType.Token:
                    return new TokenArgument();
                case F.Conformance.SearchParamType.Reference:
                    return new ReferenceArgument();
                case F.Conformance.SearchParamType.Composite:
                    //TODO: Implement Composite arguments
                    return new Argument();
                default:
                    return new Argument();
            }       
        }
    }


}