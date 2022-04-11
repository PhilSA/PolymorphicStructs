using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ECStateA : IComponentData
{
    public float Duration;
    public float3 MovementSpeed;
    public float _durationCounter;
    public float3 _startTranslation;
}

[Serializable]
public struct ECStateB : IComponentData
{
    public float Duration;
    public float3 RotationSpeed;
    public float _durationCounter;
}

[Serializable]
public struct ECStateC : IComponentData
{
    public float Duration;
    public float ScaleSpeed;
    public float _durationCounter;
}
