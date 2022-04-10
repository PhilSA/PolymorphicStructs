using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public struct StructuralStateMachineInit : IComponentData
{ }

public struct OnStateEnter : IComponentData
{ }

public struct OnStateExit : IComponentData
{
    public MyState.TypeId NextState;
}

public class StructuralStateMachineGroup : ComponentSystemGroup
{ }

[UpdateInGroup(typeof(StructuralStateMachineGroup))]
public class StateUpdateGroup : ComponentSystemGroup
{ }

[UpdateInGroup(typeof(StructuralStateMachineGroup))]
[UpdateAfter(typeof(StateUpdateGroup))]
public class StateUpdateECBSystem : EntityCommandBufferSystem
{ }

[UpdateInGroup(typeof(StructuralStateMachineGroup))]
[UpdateAfter(typeof(StateUpdateECBSystem))]
public class StateExitGroup : ComponentSystemGroup
{ }

[UpdateInGroup(typeof(StructuralStateMachineGroup))]
[UpdateAfter(typeof(StateExitGroup))]
public class StateExitECBSystem : EntityCommandBufferSystem
{ }

[UpdateInGroup(typeof(StructuralStateMachineGroup))]
[UpdateAfter(typeof(StateExitECBSystem))]
public class StateEnterGroup : ComponentSystemGroup
{ }

[UpdateInGroup(typeof(StructuralStateMachineGroup))]
[UpdateAfter(typeof(StateEnterGroup))]
public class StateEnterECBSystem : EntityCommandBufferSystem
{ }

[UpdateInGroup(typeof(StructuralStateMachineGroup))]
[UpdateAfter(typeof(StateExitGroup))]
[UpdateBefore(typeof(StateExitECBSystem))]
public partial class AssignInitialStateComponentSystem : SystemBase
{
    private StateExitECBSystem StateExitECBSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        StateExitECBSystem = World.GetOrCreateSystem<StateExitECBSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = StateExitECBSystem.CreateCommandBuffer();

        Entities
            .WithNone<StructuralStateMachineInit>()
            .ForEach((Entity entity, ref MyStateMachine stateMachine, in Translation translation, in DynamicBuffer<MyState> statesBuffer) =>
            {
                stateMachine.IsInitialized = true;
                stateMachine.StartTranslation = translation.Value;

                ecb.AddComponent(entity, new StateA(statesBuffer[(int)MyState.TypeId.StateA]));
                ecb.AddComponent(entity, new OnStateEnter());
                ecb.AddComponent(entity, new StructuralStateMachineInit());
            }).Schedule();

        StateExitECBSystem.AddJobHandleForProducer(Dependency);
    }
}