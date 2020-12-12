using System.Collections.Generic;

namespace Spark.Engine.Search.Model
{
    public abstract class SearchParamType
    {
        public abstract Hl7.Fhir.Model.SearchParamType SupportsType
            { get; }

        public abstract bool ModifierIsAllowed(ActualModifier modifier);
    }

    public class SearchParamTypeNumber: SearchParamType
    {
        public override Hl7.Fhir.Model.SearchParamType SupportsType
        { get { return Hl7.Fhir.Model.SearchParamType.Number; } }

        protected static List<Modifier> allowedModifiers = new List<Modifier> { Modifier.NONE, Modifier.MISSING };
        public override bool ModifierIsAllowed(ActualModifier modifier)
        {
            return allowedModifiers.Contains(modifier.Modifier);
        }
    }

    public class SearchParamTypeDate : SearchParamType
    {
        public override Hl7.Fhir.Model.SearchParamType SupportsType
        { get { return Hl7.Fhir.Model.SearchParamType.Date; } }

        protected static List<Modifier> allowedModifiers = new List<Modifier> { Modifier.NONE, Modifier.MISSING };
        public override bool ModifierIsAllowed(ActualModifier modifier)
        {
            return allowedModifiers.Contains(modifier.Modifier);
        }
    }

    public class SearchParamTypeString : SearchParamType
    {
        public override Hl7.Fhir.Model.SearchParamType SupportsType
        { get { return Hl7.Fhir.Model.SearchParamType.String; } }

        protected static List<Modifier> allowedModifiers = new List<Modifier> { Modifier.NONE, Modifier.MISSING, Modifier.EXACT, Modifier.CONTAINS };
        public override bool ModifierIsAllowed(ActualModifier modifier)
        {
            return allowedModifiers.Contains(modifier.Modifier);
        }
    }

    public class SearchParamTypeToken : SearchParamType
    {
        public override Hl7.Fhir.Model.SearchParamType SupportsType
        { get { return Hl7.Fhir.Model.SearchParamType.Token; } }

        protected static List<Modifier> allowedModifiers = new List<Modifier> { Modifier.NONE, Modifier.MISSING, Modifier.TEXT, Modifier.IN, Modifier.BELOW, Modifier.ABOVE, Modifier.NOT_IN };
        public override bool ModifierIsAllowed(ActualModifier modifier)
        {
            return allowedModifiers.Contains(modifier.Modifier);
        }
    }

    public class SearchParamTypeReference : SearchParamType
    {
        public override Hl7.Fhir.Model.SearchParamType SupportsType
        { get { return Hl7.Fhir.Model.SearchParamType.Reference; } }

        protected static List<Modifier> allowedModifiers = new List<Modifier> { Modifier.NONE, Modifier.MISSING, Modifier.TYPE };
        public override bool ModifierIsAllowed(ActualModifier modifier)
        {
            return allowedModifiers.Contains(modifier.Modifier);
        }
    }

    public class SearchParamTypeComposite : SearchParamType
    {
        public override Hl7.Fhir.Model.SearchParamType SupportsType
        { get { return Hl7.Fhir.Model.SearchParamType.Composite; } }

        protected static List<Modifier> allowedModifiers = new List<Modifier> { Modifier.NONE };
        public override bool ModifierIsAllowed(ActualModifier modifier)
        {
            return allowedModifiers.Contains(modifier.Modifier);
        }
    }

    public class SearchParamTypeQuantity : SearchParamType
    {
        public override Hl7.Fhir.Model.SearchParamType SupportsType
        { get { return Hl7.Fhir.Model.SearchParamType.Quantity; } }

        protected static List<Modifier> allowedModifiers = new List<Modifier> { Modifier.NONE, Modifier.MISSING };
        public override bool ModifierIsAllowed(ActualModifier modifier)
        {
            return allowedModifiers.Contains(modifier.Modifier);
        }
    }

    public class SearchParamTypeUri : SearchParamType
    {
        public override Hl7.Fhir.Model.SearchParamType SupportsType
        { get { return Hl7.Fhir.Model.SearchParamType.Uri; } }

        protected static List<Modifier> allowedModifiers = new List<Modifier> { Modifier.NONE, Modifier.MISSING, Modifier.BELOW, Modifier.ABOVE };
        public override bool ModifierIsAllowed(ActualModifier modifier)
        {
            return allowedModifiers.Contains(modifier.Modifier);
        }
    }

}
