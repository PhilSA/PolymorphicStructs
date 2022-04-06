using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public partial struct StateA : IMyState
{
    public float Duration;
    public float MovementAmplitude;

    public void OnStateEnter()
    {
    }

    public void OnStateExit()
    {
    }

    public void OnStateUpdate(ref StateUpdateData_ReadWrite refData, in StateUpdateData_Read inData)
    {
    }
}
