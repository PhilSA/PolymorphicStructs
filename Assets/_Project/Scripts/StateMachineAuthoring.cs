using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class StateMachineAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public StateA StateA;
    public StateB StateB;
    public StateC StateC;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        MyStateMachine stateMachine = new MyStateMachine();
        DynamicBuffer<MyState> statesBuffer = dstManager.AddBuffer<MyState>(entity);

        // Add states in their polymorphic form to the buffer.
        // Each state's index in the buffer will correspond to the int value of their "TypeId" enum
        AddStateToStatesBuffer(StateA.ToMyState(), ref statesBuffer);
        AddStateToStatesBuffer(StateB.ToMyState(), ref statesBuffer);
        AddStateToStatesBuffer(StateC.ToMyState(), ref statesBuffer);

        stateMachine.CurrentState = statesBuffer[(int)MyState.TypeId.StateA];
        stateMachine.IsInitialized = false;
        stateMachine.TransitionToStateIndex = -1;

        dstManager.AddComponentData(entity, stateMachine);
    }

    private void AddStateToStatesBuffer(MyState state, ref DynamicBuffer<MyState> statesBuffer)
    {
        int stateIndex = (int)state.CurrentTypeId;

        // Resize buffer if needed
        if(stateIndex >= statesBuffer.Length)
        {
            int stateTypesCount = Enum.GetValues(typeof(MyState.TypeId)).Length;
            for (int i = statesBuffer.Length; i < stateTypesCount; i++)
            {
                statesBuffer.Add(default);
            }
        }

        statesBuffer[stateIndex] = state;
    }
}
