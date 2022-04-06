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

        // Add states in their polymorphic form to the buffer, and remember the index of each
        //stateMachine.StateAIndex = statesBuffer.Length;
        //statesBuffer.Add(new StateElement { State = StateA.ToMyState() });
        //stateMachine.StateBIndex = statesBuffer.Length;
        //statesBuffer.Add(new StateElement { State = StateB.ToMyState() });
        //stateMachine.StateCIndex = statesBuffer.Length;
        //statesBuffer.Add(new StateElement { State = StateC.ToMyState() });

        dstManager.AddComponentData(entity, stateMachine);
    }
}
