using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public partial struct StateB : IMyState
{
    public float Duration;
    public float3 RotationSpeed;

    private float _durationCounter;

    public void OnStateEnter(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        _durationCounter = Duration;
    }

    public void OnStateExit(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        refData.Rotation = quaternion.identity;
    }

    public void OnStateUpdate(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        refData.Rotation = math.mul(quaternion.Euler(RotationSpeed * inData.DeltaTime), refData.Rotation);

        _durationCounter -= inData.DeltaTime;
        if (_durationCounter <= 0f)
        {
            refData.StateMachine.CurrentStateIndex = refData.StateMachine.StateCIndex;
        }
    }
}
