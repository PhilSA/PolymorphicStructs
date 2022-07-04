using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace PolymorphicStructsSourceGeneratorsTests.Tests
{
public interface IParentStruct
{
    public int Foo(int a, bool b);
}

public interface IParentStruct2
{
    public int Bar();
}

[PolymorphicStruct]
public interface IChildStruct : IParentStruct, IParentStruct2
{ }

public partial struct ChildStruct : IChildStruct
    // an ability to explicitly define IChildStruct here is redundant, if generated ChildStruct will be always implementing IChildStruct.
    // But you might still want to do that and also define other interfaces, which should not affect generation
    // Also, if you did that by mistake, now generator won't be including it in the TypeId
{
}

public partial struct InheritedStruct : IChildStruct
{
    public int Foo(int a, bool b)
    {
        return 42 + a * (b ? 1 : 0);
    }

    public int Bar()
    {
        return 13;
    }
}

[TestFixture]
[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public partial class InheritanceHierarchyTest
{
    [Test]
    public void GeneratedStructureContainsMethodsFromBaseInterfaces()
    {
        var childStruct = new InheritedStruct().ToChildStruct();
        Assert.AreEqual(13, childStruct.Bar());
        Assert.AreEqual(42, childStruct.Foo(1, false));
        Assert.AreEqual(43, childStruct.Foo(1, true));
    }
}
}
