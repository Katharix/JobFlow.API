namespace JobFlow.Business.DI;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ScopedServiceAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SingletonServiceAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TransientServiceAttribute : Attribute
{
}