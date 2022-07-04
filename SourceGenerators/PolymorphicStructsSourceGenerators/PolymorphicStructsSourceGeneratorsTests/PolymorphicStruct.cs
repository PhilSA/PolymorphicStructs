using System;

namespace PolymorphicStructsSourceGeneratorsTests
{
/// <summary>
/// We can use our own polymorphic struct argument so we don't have to reference anything else
/// Generator resolves attribute by name, so as long as it matches, we don't care where it's coming from
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class PolymorphicStruct : System.Attribute
{
    public string ImplementedInterfaces;

    public PolymorphicStruct(string implementedInterfaces = "")
    {
        ImplementedInterfaces = implementedInterfaces;
    }
}
}
