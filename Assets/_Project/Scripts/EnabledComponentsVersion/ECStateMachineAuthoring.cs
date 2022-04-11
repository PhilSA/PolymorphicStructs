using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class ECStateMachineAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public ECStateA StateA;
    public ECStateB StateB;
    public ECStateC StateC;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Entity stateAEntity = CreateStateEntity(entity, dstManager, conversionSystem, StateA);
        Entity stateBEntity = CreateStateEntity(entity, dstManager, conversionSystem, StateB);
        Entity stateCEntity = CreateStateEntity(entity, dstManager, conversionSystem, StateC);

        DynamicBuffer<ECStateEntity> statesBuffer = dstManager.AddBuffer<ECStateEntity>(entity);
        statesBuffer.Add(new ECStateEntity { Value = stateAEntity });
        statesBuffer.Add(new ECStateEntity { Value = stateBEntity });
        statesBuffer.Add(new ECStateEntity { Value = stateCEntity });

        dstManager.AddComponentData(entity, new ECStateMachine { Speed = 1f });
    }

    public Entity CreateStateEntity<T>(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem, T state) where T : struct, IComponentData
    {
        Entity stateEntity = conversionSystem.CreateAdditionalEntity(transform);

        dstManager.AddComponentData(stateEntity, new ECState { StateMachine = entity });
        dstManager.AddComponentData(stateEntity, new ECStateUpdate());
        dstManager.AddComponentData(stateEntity, new ECStateEnter());
        dstManager.AddComponentData(stateEntity, new ECStateExit());
        dstManager.AddComponentData(stateEntity, state);

        return stateEntity;
    }
}
