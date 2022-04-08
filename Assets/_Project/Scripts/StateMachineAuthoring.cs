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
        DynamicBuffer<StateElement> statesBuffer = dstManager.AddBuffer<StateElement>(entity);

        stateMachine.CurrentStateIndex = -1;

        // Add states in their polymorphic form to the buffer, and remember the index of each
        stateMachine.StateAIndex = statesBuffer.Length;
        statesBuffer.Add(new StateElement { Value = StateA.ToMyState() });
        stateMachine.StateBIndex = statesBuffer.Length;
        statesBuffer.Add(new StateElement { Value = StateB.ToMyState() });
        stateMachine.StateCIndex = statesBuffer.Length;
        statesBuffer.Add(new StateElement { Value = StateC.ToMyState() });

        dstManager.AddComponentData(entity, stateMachine);
    }
}
