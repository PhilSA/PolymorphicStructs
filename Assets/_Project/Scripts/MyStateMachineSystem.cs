using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class MyStateMachineSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities.ForEach((
            ref MyStateMachine stateMachine, 
            ref DynamicBuffer<MyState> statesBuffer,
            ref Translation translation, 
            ref Rotation rotation,
            ref NonUniformScale scale) =>
        {
            float scaledDeltaTime = deltaTime * stateMachine.Speed;

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
            if(!refData.StateMachine.IsInitialized)
            {
                refData.StateMachine.IsInitialized = true;
                refData.StateMachine.TransitionToStateIndex = -1;
                refData.StateMachine.StartTranslation = translation.Value;

                refData.StateMachine.CurrentState = statesBuffer[(int)MyState.TypeId.StateA];
                refData.StateMachine.CurrentState.OnStateEnter(ref refData, in inData);
            }

            // State update
            int stateIndexBeforeUpdate = (int)refData.StateMachine.CurrentState.CurrentTypeId;
            refData.StateMachine.CurrentState.OnStateUpdate(ref refData, in inData);

            // Handle Transitions
            if(refData.StateMachine.TransitionToStateIndex >= 0 && refData.StateMachine.TransitionToStateIndex != stateIndexBeforeUpdate)
            {
                // Exit current state
                refData.StateMachine.CurrentState.OnStateExit(ref refData, in inData);

                // Write current state data back into states buffer
                statesBuffer[(int)refData.StateMachine.CurrentState.CurrentTypeId] = refData.StateMachine.CurrentState;

                // Enter next state
                refData.StateMachine.CurrentState = statesBuffer[refData.StateMachine.TransitionToStateIndex];
                refData.StateMachine.CurrentState.OnStateEnter(ref refData, in inData);
            }

            // Write back data
            stateMachine = refData.StateMachine;
            translation.Value = refData.Translation;
            rotation.Value = refData.Rotation;
            scale.Value = refData.Scale;
        }).Schedule();
        //}).ScheduleParallel();
    }
}
