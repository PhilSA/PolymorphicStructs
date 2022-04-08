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

// Using partial structs, we can declare that the generated "MyState" struct will also be a "IBufferElementData"
public partial struct MyState : IBufferElementData
{

}