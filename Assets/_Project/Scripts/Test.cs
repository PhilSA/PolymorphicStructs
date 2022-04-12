using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[PolymorphicStruct]
public interface IMyTest
{
    public bool TestProperty { get; set; }
    public void DoSomething(float a, ref ComponentDataFromEntity<Translation> translationFromEntity);
    public float DoSomethingWithReturnType(float a, ref ComponentDataFromEntity<Translation> translationFromEntity);
}

public partial struct TestA : IMyTest
{
    public float A;
    public float B;
    public float C;
    public float3 D;
    public Entity E;
    public Entity F;

    public bool TestProperty { get; set; }

    public void DoSomething(float a, ref ComponentDataFromEntity<Translation> translationFromEntity)
    {
        B += a * C;
    }

    public float DoSomethingWithReturnType(float a, ref ComponentDataFromEntity<Translation> translationFromEntity)
    {
        return A + B + math.length(D);
    }
}

public partial struct TestB : IMyTest
{
    public float A;
    public Entity B;

    public bool TestProperty { get; set; }

    public void DoSomething(float a, ref ComponentDataFromEntity<Translation> translationFromEntity)
    {
        A = math.length(translationFromEntity[B].Value);
    }

    public float DoSomethingWithReturnType(float a, ref ComponentDataFromEntity<Translation> translationFromEntity)
    {
        return A;
    }
}