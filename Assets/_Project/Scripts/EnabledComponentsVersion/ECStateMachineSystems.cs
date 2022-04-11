using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[Serializable]
public struct ECStateMachine : IComponentData
{
    public Entity CurrentState;
    public float Speed;
}

[Serializable]
public struct ECStateUpdate : IComponentData
{
    public byte Execute;
}

[Serializable]
public struct ECStateEnter : IComponentData
{
    public byte Execute;
}

[Serializable]
public struct ECStateExit : IComponentData
{
    public byte Execute;
}

[Serializable]
public struct ECStateEntity : IBufferElementData
{
    public Entity Value;
}

[Serializable]
public struct ECState : IComponentData
{
    public Entity TransitionTo;
    public Entity StateMachine;
}

public partial class ECStateMachineSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!HasSingleton<StateMachineSettings>())
            return;

        StateMachineSettings smSettings = GetSingleton<StateMachineSettings>();
        float deltaTime = smSettings.UseFixedDeltaTime ? smSettings.FixedDeltaTime : Time.DeltaTime;

        // Updates
        Entities
            .ForEach((ref ECStateA state, ref ECState s, in ECStateUpdate stateUpdate) =>
            {
                if (stateUpdate.Execute == 1)
                {
                    float scaledDeltaTime = deltaTime * GetComponent<ECStateMachine>(s.StateMachine).Speed;

                    Translation translation = GetComponent<Translation>(s.StateMachine);
                    translation.Value += state.MovementSpeed * scaledDeltaTime;
                    SetComponent(s.StateMachine, translation);

                    state._durationCounter -= scaledDeltaTime;
                    if (state._durationCounter <= 0f)
                    {
                        DynamicBuffer<ECStateEntity> statesBuffer = GetBuffer<ECStateEntity>(s.StateMachine);
                        s.TransitionTo = statesBuffer[1].Value;
                    }
                }
            }).Schedule();

        Entities
            .ForEach((ref ECStateB state, ref ECState s, in ECStateUpdate stateUpdate) =>
            {
                if (stateUpdate.Execute == 1)
                {
                    float scaledDeltaTime = deltaTime * GetComponent<ECStateMachine>(s.StateMachine).Speed;

                    Rotation rotation = GetComponent<Rotation>(s.StateMachine);
                    rotation.Value = math.mul(quaternion.Euler(state.RotationSpeed * scaledDeltaTime), rotation.Value);
                    SetComponent(s.StateMachine, rotation);

                    state._durationCounter -= scaledDeltaTime;
                    if (state._durationCounter <= 0f)
                    {
                        DynamicBuffer<ECStateEntity> statesBuffer = GetBuffer<ECStateEntity>(s.StateMachine);
                        s.TransitionTo = statesBuffer[2].Value;
                    }
                }
            }).Schedule();

        Entities
            .ForEach((ref ECStateC state, ref ECState s, in ECStateUpdate stateUpdate) =>
            {
                if (stateUpdate.Execute == 1)
                {
                    float scaledDeltaTime = deltaTime * GetComponent<ECStateMachine>(s.StateMachine).Speed;

                    NonUniformScale scale = GetComponent<NonUniformScale>(s.StateMachine);
                    scale.Value += state.ScaleSpeed * scaledDeltaTime;
                    SetComponent(s.StateMachine, scale);

                    state._durationCounter -= scaledDeltaTime;
                    if (state._durationCounter <= 0f)
                    {
                        DynamicBuffer<ECStateEntity> statesBuffer = GetBuffer<ECStateEntity>(s.StateMachine);
                        s.TransitionTo = statesBuffer[0].Value;
                    }
                }
            }).Schedule();

        // StateMachine check transitions
        Entities
            .ForEach((ref ECStateMachine stateMachine, in DynamicBuffer<ECStateEntity> statesBuffer) =>
            {
                // Handle transition to first state
                if (stateMachine.CurrentState == Entity.Null)
                {
                    stateMachine.CurrentState = statesBuffer[0].Value;
                    SetComponent(stateMachine.CurrentState, new ECStateEnter { Execute = 1 });
                }
                else
                {
                    ECState currentState = GetComponent<ECState>(stateMachine.CurrentState);
                    if (currentState.TransitionTo != Entity.Null)
                    {
                        SetComponent(stateMachine.CurrentState, new ECStateExit { Execute = 1 });
                        stateMachine.CurrentState = currentState.TransitionTo;
                        SetComponent(stateMachine.CurrentState, new ECStateEnter { Execute = 1 });
                    }
                }
            }).Schedule();

        // Exits
        Entities
            .WithChangeFilter<ECStateExit>()
            .ForEach((ref ECStateA state, ref ECState s, ref ECStateUpdate stateUpdate, ref ECStateExit stateExit) =>
            {
                if (stateExit.Execute == 1)
                {
                    Translation translation = GetComponent<Translation>(s.StateMachine);
                    translation.Value = state._startTranslation;
                    SetComponent(s.StateMachine, translation);

                    s.TransitionTo = default;

                    stateExit.Execute = 0;
                    stateUpdate.Execute = 0;
                }
            }).Schedule();

        Entities
            .WithChangeFilter<ECStateExit>()
            .ForEach((ref ECStateB state, ref ECState s, ref ECStateUpdate stateUpdate, ref ECStateExit stateExit) =>
            {
                if (stateExit.Execute == 1)
                {
                    Rotation rotation = GetComponent<Rotation>(s.StateMachine);
                    rotation.Value = quaternion.identity;
                    SetComponent(s.StateMachine, rotation);

                    s.TransitionTo = default;

                    stateExit.Execute = 0;
                    stateUpdate.Execute = 0;
                }
            }).Schedule();

        Entities
            .WithChangeFilter<ECStateExit>()
            .ForEach((ref ECStateC state, ref ECState s, ref ECStateUpdate stateUpdate, ref ECStateExit stateExit) =>
            {
                if (stateExit.Execute == 1)
                {
                    NonUniformScale scale = GetComponent<NonUniformScale>(s.StateMachine);
                    scale.Value = 1f;
                    SetComponent(s.StateMachine, scale);

                    s.TransitionTo = default;

                    stateExit.Execute = 0;
                    stateUpdate.Execute = 0;
                }
            }).Schedule();

        // Enters
        Entities
            .WithChangeFilter<ECStateEnter>()
            .ForEach((ref ECStateA state, ref ECStateUpdate stateUpdate, ref ECStateEnter stateEnter, in ECState s) =>
            {
                if (stateEnter.Execute == 1)
                {
                    state._durationCounter = state.Duration;
                    state._startTranslation = GetComponent<Translation>(s.StateMachine).Value;

                    stateEnter.Execute = 0;
                    stateUpdate.Execute = 1;
                }
            }).Schedule();

        Entities
            .WithChangeFilter<ECStateEnter>()
            .ForEach((ref ECStateB state, ref ECStateUpdate stateUpdate, ref ECStateEnter stateEnter) =>
            {
                if (stateEnter.Execute == 1)
                {
                    state._durationCounter = state.Duration;

                    stateEnter.Execute = 0;
                    stateUpdate.Execute = 1;
                }
            }).Schedule();

        Entities
            .WithChangeFilter<ECStateEnter>()
            .ForEach((ref ECStateC state, ref ECStateUpdate stateUpdate, ref ECStateEnter stateEnter) =>
            {
                if (stateEnter.Execute == 1)
                {
                    state._durationCounter = state.Duration;

                    stateEnter.Execute = 0;
                    stateUpdate.Execute = 1;
                }
            }).Schedule();
    }
}
