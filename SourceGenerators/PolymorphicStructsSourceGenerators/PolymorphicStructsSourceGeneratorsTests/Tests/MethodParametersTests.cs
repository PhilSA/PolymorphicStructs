using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace PolymorphicStructsSourceGeneratorsTests.Tests
{
public struct MethodParamTestStruct
{
    public int Field;
}

[PolymorphicStruct]
public interface IMethodParametersStruct
{
    public struct MyInnerStruct
    {
        public int Field;
    }

    public void Foo1(); // void no arg
    public int Foo2(); // return value, no arg
    public int Foo3(int a); // One arg

    public void Foo4(ref IMethodParametersStruct.MyInnerStruct a, in MethodParamTestStruct b,
        out MethodParamTestStruct c); // yolo
}

public partial struct MethodParametersStructA : IMethodParametersStruct
{
    public void Foo1()
    {
    }

    public int Foo2()
    {
        return 1;
    }

    public int Foo3(int a)
    {
        return a;
    }

    public void Foo4(ref IMethodParametersStruct.MyInnerStruct a, in MethodParamTestStruct b, out MethodParamTestStruct c)
    {
        a.Field = b.Field;
        c = new MethodParamTestStruct { Field = 42 };
    }
}

[TestFixture]
[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public partial class MethodParametersTests
{
    [Test]
    public void GeneratedStructureImplementsInterfaceSimple()
    {
        var myStruct = new MethodParametersStructA().ToMethodParametersStruct();

        Assert.DoesNotThrow(() => myStruct.Foo1());
        Assert.AreEqual(1, myStruct.Foo2());
        Assert.AreEqual(5, myStruct.Foo3(5));
        var innerStruct = new IMethodParametersStruct.MyInnerStruct { Field = 5 };
        myStruct.Foo4(ref innerStruct, new MethodParamTestStruct { Field = 43 }, out var outStruct);
        Assert.AreEqual(43, innerStruct.Field, "ref works");
        Assert.AreEqual(42, outStruct.Field, "out works");
    }
}
}
