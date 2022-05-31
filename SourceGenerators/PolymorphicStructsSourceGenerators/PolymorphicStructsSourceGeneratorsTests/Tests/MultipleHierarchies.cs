using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace PolymorphicStructsSourceGeneratorsTests.Tests
{

public interface IAction
{
    int Act();
}

[PolymorphicStruct]
public interface IMelleEnemyAction : IAction
{
    
}

[PolymorphicStruct]
public interface IRangedEnemyAction : IAction
{
    
}

public partial struct MeleeIdleAction : IMelleEnemyAction
{
    public int Act()
    {
        return 1;
    }
}

public partial struct RangedIdleAction : IRangedEnemyAction
{
    public int Act()
    {
        return 2;
    }
}

[TestFixture]
[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public class MultipleHierarchiesTest
{
    [Test]
    public void CanHaveIndependentHierarchiesShareCommonCode()
    {
        Assert.AreEqual(1, ActionCommonCode(new MeleeIdleAction().ToMelleEnemyAction()));
        Assert.AreEqual(2, ActionCommonCode(new RangedIdleAction().ToRangedEnemyAction()));
    }

    private static int ActionCommonCode<T>(T action) where T : struct, IAction
    {
        return action.Act(); // here I can call anything that parent interface defines
    }
}
}
