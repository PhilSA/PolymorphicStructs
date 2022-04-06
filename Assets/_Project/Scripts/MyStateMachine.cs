using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PolymorphicStructs;

[PolymorphicStruct]
public interface IMyState
{
    public void OnStateEnter();
    public void OnStateExit();
    public void OnStateUpdate(ref StateUpdateData_ReadWrite refData, in StateUpdateData_Read inData); 
}

[Serializable]
public struct MyStateMachine : IComponentData
{
    public int CurrentStateIndex;

    public int StateAIndex;
    public int StateBIndex;
    public int StateCIndex;
}

[Serializable]
public struct StateElement : IBufferElementData
{
}

public struct StateUpdateData_ReadWrite
{
    public float3 Translation;
    public quaternion Rotation;
    public float3 Scale;
}

public struct StateUpdateData_Read
{

}