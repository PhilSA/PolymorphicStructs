using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct MyStateMachine : IComponentData
{
    public int CurrentStateIndex;

    public int StateAIndex;
    public int StateBIndex;
    public int StateCIndex;

    public float Speed;
    public float3 StartTranslation;
}

public struct StateUpdateData_ReadWrite
{
    public MyStateMachine StateMachine;

    public float3 Translation;
    public quaternion Rotation;
    public float3 Scale;
}

public struct StateUpdateData_ReadOnly
{
    public float DeltaTime;
}