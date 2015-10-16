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

    }

    public class TagArgument : Argument
    {
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
    }

    public class FuzzyArgument : Argument
    {
        public override string GroomElement(string value)
        {
            return Soundex.For(value);
        }
    }

    public static class ArgumentFactory
    {
        public static Argument Create(F.SearchParamType type, bool fuzzy=false)
        {
            switch (type)
            {
                case F.SearchParamType.Number:
                    return new IntArgument();
                case F.SearchParamType.String:
                    return new StringArgument();
                case F.SearchParamType.Date:
                    return new DateArgument();
                case F.SearchParamType.Token:
                    return new TokenArgument();
                case F.SearchParamType.Reference:
                    return new ReferenceArgument();
                case F.SearchParamType.Composite:
                    //TODO: Implement Composite arguments
                    return new Argument();
                default:
                    return new Argument();
            }       
        }
    }


}