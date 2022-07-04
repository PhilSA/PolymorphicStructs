using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace PolymorphicStructsSourceGeneratorsTests.Tests
{
[PolymorphicStruct]
public interface IFieldTestStruct
{
    public int Foo();
}

public partial struct FieldTestStruct {
}


public partial struct FieldTestStructA : IFieldTestStruct
{
    public int Field;

    public int Foo()
    {
        return Field;
    }
}

public partial struct FieldTestStructB : IFieldTestStruct
{
    public int Field;
    public int Field2;

    public int Foo()
    {
        return Field + Field2;
    }
}


[TestFixture]
[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public partial class FieldsTest
{
    [Test]
    public void PolymoprphicStructWorks()
    {
        var baseStructA = new FieldTestStructA { Field = 1 }.ToFieldTestStruct();
        var baseStructB = new FieldTestStructB { Field = 2, Field2 = 3 }.ToFieldTestStruct();

        Assert.AreEqual(FieldTestStruct.TypeId.FieldTestStructA, baseStructA.CurrentTypeId);
        Assert.AreEqual(FieldTestStruct.TypeId.FieldTestStructB, baseStructB.CurrentTypeId);
        
        Assert.AreEqual(1, baseStructA.Foo());
        Assert.AreEqual(5, baseStructB.Foo());
    }
}
}
