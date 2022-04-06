using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public partial struct StateB : IMyState
{
    public float Duration;
    public float RotationSpeed;

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
