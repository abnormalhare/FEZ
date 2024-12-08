namespace FezEngine.Effects;

public interface IShaderInstantiatableEffect<T>
{
	void SetInstanceData(T[] instances, int start, int batchInstances);
}
