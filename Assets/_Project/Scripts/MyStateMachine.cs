using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[PolymorphicStruct]
public interface IMyState
{
    public void OnStateEnter(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData);
    public void OnStateExit(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData);
    public void OnStateUpdate(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData); 
}

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

[Serializable]
public struct StateElement : IBufferElementData
{
    public MyState Value;
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