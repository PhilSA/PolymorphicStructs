using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class PolymorphicStateMachineSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!HasSingleton<StateMachineSettings>())
            return;

        StateMachineSettings smSettings = GetSingleton<StateMachineSettings>();
        float deltaTime = smSettings.UseFixedDeltaTime ? smSettings.FixedDeltaTime : Time.DeltaTime;

        if (smSettings.UseStructuralChanges)
            return;

        var job = new PolymorphicStateMachineJob { DeltaTime = deltaTime };
        if (smSettings.ScheduleParallel)
        {
            job.ScheduleParallel();
        }
        else
        {
            job.Schedule();
        }
    }
}

[BurstCompile]
public partial struct PolymorphicStateMachineJob : IJobEntity
{
    public float DeltaTime;

    public void Execute(Entity entity, ref DynamicBuffer<MyState> statesBuffer, ref MyStateMachine stateMachine, ref Translation translation, ref Rotation rotation, ref NonUniformScale scale)
    {
        float scaledDeltaTime = DeltaTime * stateMachine.Speed;

        // Prepare state update data
        StateUpdateData_ReadOnly inData = new StateUpdateData_ReadOnly
        {
            DeltaTime = scaledDeltaTime,
        };
        StateUpdateData_ReadWrite refData = new StateUpdateData_ReadWrite
        {
            StateMachine = stateMachine,

            Translation = translation.Value,
            Rotation = rotation.Value,
            Scale = scale.Value,
        };

        // Handle entering the first state
        if (!refData.StateMachine.IsInitialized)
        {
            refData.StateMachine.IsInitialized = true;
            refData.StateMachine.StartTranslation = translation.Value;
            refData.StateMachine.CurrentState.OnStateEnter(ref refData, in inData);
        }

        // State update
        int stateIndexBeforeUpdate = (int)refData.StateMachine.CurrentState.CurrentTypeId;
        refData.StateMachine.CurrentState.OnStateUpdate(ref refData, in inData);

        // Handle Transitions
        if (refData.StateMachine.TransitionToStateIndex >= 0)
        {
            // Exit current state
            refData.StateMachine.CurrentState.OnStateExit(ref refData, in inData);

            // Write current state data back into states buffer
            statesBuffer[(int)refData.StateMachine.CurrentState.CurrentTypeId] = refData.StateMachine.CurrentState;

            // Enter next state
            refData.StateMachine.CurrentState = statesBuffer[refData.StateMachine.TransitionToStateIndex];
            refData.StateMachine.CurrentState.OnStateEnter(ref refData, in inData);

            refData.StateMachine.TransitionToStateIndex = -1;
        }

        // Write back data
        stateMachine = refData.StateMachine;
        translation.Value = refData.Translation;
        rotation.Value = refData.Rotation;
        scale.Value = refData.Scale;
    }
}
