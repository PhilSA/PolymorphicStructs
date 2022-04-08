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
            ref DynamicBuffer<StateElement> statesBuffer,
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
            if(refData.StateMachine.CurrentStateIndex < 0)
            {
                refData.StateMachine.CurrentStateIndex = 0;
                refData.StateMachine.StartTranslation = translation.Value;

                int enterStateIndex = refData.StateMachine.CurrentStateIndex;
                StateElement enterStateElement = statesBuffer[enterStateIndex];
                enterStateElement.Value.OnStateEnter(ref refData, in inData);
                statesBuffer[enterStateIndex] = enterStateElement;
            }

            // State update
            int updateStateIndex = refData.StateMachine.CurrentStateIndex;
            StateElement updateStateElement = statesBuffer[updateStateIndex];
            updateStateElement.Value.OnStateUpdate(ref refData, in inData);
            statesBuffer[updateStateIndex] = updateStateElement;

            // Handle Transitions
            if(refData.StateMachine.CurrentStateIndex != updateStateIndex)
            {
                int exitStateIndex = updateStateIndex;
                StateElement exitStateElement = statesBuffer[exitStateIndex];
                exitStateElement.Value.OnStateExit(ref refData, in inData);
                statesBuffer[exitStateIndex] = exitStateElement;

                int enterStateIndex = refData.StateMachine.CurrentStateIndex;
                StateElement enterStateElement = statesBuffer[enterStateIndex];
                enterStateElement.Value.OnStateEnter(ref refData, in inData); 
                statesBuffer[enterStateIndex] = enterStateElement;
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
