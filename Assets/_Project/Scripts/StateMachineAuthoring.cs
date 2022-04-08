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

        stateMachine.CurrentStateIndex = -1;

        // Add states in their polymorphic form to the buffer, and remember the index of each
        stateMachine.StateAIndex = statesBuffer.Length;
        statesBuffer.Add(StateA.ToMyState());
        stateMachine.StateBIndex = statesBuffer.Length;
        statesBuffer.Add(StateB.ToMyState());
        stateMachine.StateCIndex = statesBuffer.Length;
        statesBuffer.Add(StateC.ToMyState());

        dstManager.AddComponentData(entity, stateMachine);
    }
}
