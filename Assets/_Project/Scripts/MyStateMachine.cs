using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct MyStateMachine : IComponentData
{
    [HideInInspector]
    public MyState CurrentState;

    [HideInInspector]
    public float Speed;
    [HideInInspector]
    public float3 StartTranslation;
    [HideInInspector]
    public bool IsInitialized;
    [HideInInspector]
    public int TransitionToStateIndex;
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