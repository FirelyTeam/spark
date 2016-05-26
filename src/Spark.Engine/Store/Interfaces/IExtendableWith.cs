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