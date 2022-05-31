using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace PolymorphicStructsSourceGeneratorsTests.Tests
{
[PolymorphicStruct]
public interface IInterfacesStruct
{
    int Foo();
}

public partial struct InterfacesStructA : IInterfacesStruct
{
    public int Foo()
    {
        return 42;
    }
}

[TestFixture]
[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public partial class SimpleInterfaceTest
{
    [Test]
    public void GeneratedStructureImplementsInterfaceSimple()
    {
        var myStruct = new InterfacesStructA();

        Assert.AreEqual(42, myStruct.Foo(), "Direct Call");
        Assert.AreEqual(42, myStruct.ToInterfacesStruct().Foo(), "Polymorphic Call");
        Assert.AreEqual(42, CallThroughInterface(myStruct), "Direct Call via template method");
        Assert.AreEqual(42, CallThroughInterface(myStruct.ToInterfacesStruct()),
            "Polymorphic Call via template method");
    }

    private static int CallThroughInterface<T>(T obj) where T : struct, IInterfacesStruct
    {
        return obj.Foo();
    }
}
}
