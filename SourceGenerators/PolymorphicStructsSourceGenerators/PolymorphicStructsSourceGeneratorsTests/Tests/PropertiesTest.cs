using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace PolymorphicStructsSourceGeneratorsTests.Tests
{
[PolymorphicStruct]
public interface IPropertiesStruct
{
    public int Prop1 { get; set; } // to be used with backing field
    public int Prop2 { get; set; } // to be used as auto property
    public int GetOnlyProp { get; }
    public int SetOnlyProp { set; }
}

public partial struct PropertiesStructA : IPropertiesStruct
{
    public int _prop1;
    public int Prop1
    {
        get => _prop1;
        set => _prop1 = value;
    }

    public int Prop2 { get; set; }
    public int GetOnlyProp
    {
        get => Prop1 * Prop2;
    }

    public int SetOnlyProp {
        set => _prop1 = value * 2;
    }
}

[TestFixture]
[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public class PropertiesTest
{
    [Test]
    public void PropertiesWork()
    {
        var propertiesStructA = new PropertiesStructA().ToPropertiesStruct();
        Assert.AreEqual(0, propertiesStructA.Prop1, "Read default value");
        propertiesStructA.Prop1 = 12; // set backing property
        Assert.AreEqual(12, propertiesStructA.Prop1, "Read value that is set");
        propertiesStructA.Prop2 = 10; // Set AutoProperty
        Assert.AreEqual(10, propertiesStructA.Prop2, "Read value that is set");
        Assert.AreEqual(120, propertiesStructA.GetOnlyProp, "Read Get Only Prop");
        propertiesStructA.SetOnlyProp = 21; // this mutates Prop1 backing field
        Assert.AreEqual(42, propertiesStructA.Prop1, "Access mutated value");
    }
}
}
