using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public partial struct StateA : IMyState, IComponentData // IComponentData is only there for the "Structural changes" version
{
    public float Duration;
    public float3 MovementSpeed;

    private float _durationCounter;

    public void OnStateEnter(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        _durationCounter = Duration;
    }

    public void OnStateExit(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        refData.Translation = refData.StateMachine.StartTranslation;
    }

    public void OnStateUpdate(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        refData.Translation += MovementSpeed * inData.DeltaTime;

        _durationCounter -= inData.DeltaTime;
        if (_durationCounter <= 0f)
        {
            refData.StateMachine.TransitionToStateIndex = (int)MyState.TypeId.StateB;
        }
    }
}
