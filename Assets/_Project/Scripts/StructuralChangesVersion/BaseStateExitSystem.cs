using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(StateExitGroup))]
public partial class BaseStateExitSystem<T> : SystemBase where T : struct, IComponentData, IMyState
{
    private EntityCommandBufferSystem _ecbSystem;
    private EntityQuery _entityQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _ecbSystem = World.GetOrCreateSystem<StateExitECBSystem>();
        _entityQuery = GetEntityQuery(typeof(T), typeof(OnStateExit), typeof(MyStateMachine), typeof(Translation), typeof(Rotation), typeof(NonUniformScale));
    }

    protected override void OnUpdate()
    {
        if (!HasSingleton<StateMachineSettings>())
            return;

        StateMachineSettings smSettings = GetSingleton<StateMachineSettings>();
        float deltaTime = smSettings.UseFixedDeltaTime ? smSettings.FixedDeltaTime : Time.DeltaTime;

        if (!smSettings.UseStructuralChanges)
            return;

        EntityCommandBuffer ecb = _ecbSystem.CreateCommandBuffer();

        var job = new StateExitJob<T>
        {
            DeltaTime = deltaTime,
            ECB = ecb.AsParallelWriter(),

            EntityType = GetEntityTypeHandle(),
            StateType = GetComponentTypeHandle<T>(false),
            StateExitType = GetComponentTypeHandle<OnStateExit>(true),
            MyStateMachineType = GetComponentTypeHandle<MyStateMachine>(false),
            TranslationType = GetComponentTypeHandle<Translation>(false),
            RotationType = GetComponentTypeHandle<Rotation>(false),
            NonUniformScaleType = GetComponentTypeHandle<NonUniformScale>(false),
            MyStateType = GetBufferTypeHandle<MyState>(true),
        };

        if (smSettings.ScheduleParallel)
        {
            Dependency = job.ScheduleParallel(_entityQuery, Dependency);
        }
        else
        {
            Dependency = job.Schedule(_entityQuery);
        }

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

[BurstCompile]
public partial struct StateExitJob<T> : IJobEntityBatch where T : struct, IComponentData, IMyState
{
    public float DeltaTime;
    public EntityCommandBuffer.ParallelWriter ECB;

    [ReadOnly]
    public EntityTypeHandle EntityType;
    public ComponentTypeHandle<T> StateType;
    [ReadOnly]
    public ComponentTypeHandle<OnStateExit> StateExitType;
    public ComponentTypeHandle<MyStateMachine> MyStateMachineType;
    public ComponentTypeHandle<Translation> TranslationType;
    public ComponentTypeHandle<Rotation> RotationType;
    public ComponentTypeHandle<NonUniformScale> NonUniformScaleType;
    [ReadOnly]
    public BufferTypeHandle<MyState> MyStateType;

    public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
    {
        NativeArray<Entity> chunkEntities = batchInChunk.GetNativeArray(EntityType);
        NativeArray<T> chunkStates = batchInChunk.GetNativeArray(StateType);
        NativeArray<OnStateExit> chunkStateExits = batchInChunk.GetNativeArray(StateExitType);
        NativeArray<MyStateMachine> chunkMyStateMachines = batchInChunk.GetNativeArray(MyStateMachineType);
        NativeArray<Translation> chunkTranslations = batchInChunk.GetNativeArray(TranslationType);
        NativeArray<Rotation> chunkRotationTypes = batchInChunk.GetNativeArray(RotationType);
        NativeArray<NonUniformScale> chunkNonUniformScaleTypes = batchInChunk.GetNativeArray(NonUniformScaleType);
        BufferAccessor<MyState> chunkMyStateTypes = batchInChunk.GetBufferAccessor(MyStateType);

        for (int i = 0; i < batchInChunk.Count; i++)
        {
            Entity entity = chunkEntities[i];
            T state = chunkStates[i];
            OnStateExit stateExit = chunkStateExits[i];
            MyStateMachine stateMachine = chunkMyStateMachines[i];
            Translation translation = chunkTranslations[i];
            Rotation rotation = chunkRotationTypes[i];
            NonUniformScale scale = chunkNonUniformScaleTypes[i];
            DynamicBuffer<MyState> statesBuffer = chunkMyStateTypes[i];

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

            state.OnStateExit(ref refData, in inData);

            // Handle Transitions
            ECB.RemoveComponent<T>(batchIndex, entity);
            ECB.RemoveComponent<OnStateExit>(batchIndex, entity);
            switch (stateExit.NextState)
            {
                case MyState.TypeId.StateC:
                    ECB.AddComponent(batchIndex, entity, new StateC(statesBuffer[(int)MyState.TypeId.StateC]));
                    break;
                case MyState.TypeId.StateB:
                    ECB.AddComponent<StateB>(batchIndex, entity, new StateB(statesBuffer[(int)MyState.TypeId.StateB]));
                    break;
                case MyState.TypeId.StateA:
                    ECB.AddComponent<StateA>(batchIndex, entity, new StateA(statesBuffer[(int)MyState.TypeId.StateA]));
                    break;
            }
            ECB.AddComponent(batchIndex, entity, new OnStateEnter());

            // Write back data
            stateMachine = refData.StateMachine;
            translation.Value = refData.Translation;
            rotation.Value = refData.Rotation;
            scale.Value = refData.Scale;

            chunkStates[i] = state;
            chunkMyStateMachines[i] = stateMachine;
            chunkTranslations[i] = translation;
            chunkRotationTypes[i] = rotation;
            chunkNonUniformScaleTypes[i] = scale;
        }
    }
}