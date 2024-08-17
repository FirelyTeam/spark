/* 
 * Copyright (c) 2016-2018, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

namespace Spark.Engine.Store.Interfaces
{
    public interface IExtendableWith<T>
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