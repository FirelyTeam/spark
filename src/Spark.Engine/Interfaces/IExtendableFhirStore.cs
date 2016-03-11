using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Core;

namespace Spark.Engine.Interfaces
{
    public interface IExtensibleObject<T>
    {
        void AddExtension<TV>(TV extension) where TV : T;
        void RemoveExtension<TV>() where TV : T;
        TV FindExtension<TV>() where TV : T;
    }

    public interface IExtension<in T>
    {
        void OnExtensionAdded(T extensibleObject);
    }
}