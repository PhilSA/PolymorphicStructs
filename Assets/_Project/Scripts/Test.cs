using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[PolymorphicStruct]
public interface IMyTest
{
    void DoSomething(float a, ref ComponentDataFromEntity<Translation> translationFromEntity);
}

public partial struct TestA : IMyTest
{
    public float A;
    public float B;
    public float C;
    public float3 D;
    public Entity E;
    public Entity F;

    public void DoSomething(float a, ref ComponentDataFromEntity<Translation> translationFromEntity)
    {
        B += a * C;
    }
}

public partial struct TestB : IMyTest
{
    public float A;
    public Entity B;

    public void DoSomething(float a, ref ComponentDataFromEntity<Translation> translationFromEntity)
    {
        A = math.length(translationFromEntity[B].Value);
    }
}