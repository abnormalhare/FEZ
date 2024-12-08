namespace FezEngine.Structure;

public interface ISimplePhysicsEntity : IPhysicsEntity
{
	bool IgnoreCollision { get; }
}
