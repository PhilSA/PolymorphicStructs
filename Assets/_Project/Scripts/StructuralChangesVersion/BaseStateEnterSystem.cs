using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;


[UpdateInGroup(typeof(StateEnterGroup))]
public partial class BaseStateEnterSystem<T> : SystemBase where T : struct, IComponentData, IMyState
{
    private EntityCommandBufferSystem _ecbSystem;
    private EntityQuery _entityQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        _ecbSystem = World.GetOrCreateSystem<StateEnterECBSystem>();
        _entityQuery = GetEntityQuery(typeof(T), typeof(OnStateEnter), typeof(MyStateMachine), typeof(Translation), typeof(Rotation), typeof(NonUniformScale));
    }

    protected override void OnUpdate()
    {
        if (!HasSingleton<StateMachineSettings>())
            return;

        float deltaTime = Time.DeltaTime;
        StateMachineSettings smSettings = GetSingleton<StateMachineSettings>();

        if (!smSettings.UseStructuralChanges)
            return;

        EntityCommandBuffer ecb = _ecbSystem.CreateCommandBuffer();

        var job = new StateEnterJob<T>
        {
            DeltaTime = deltaTime,
            ECB = ecb.AsParallelWriter(),

            EntityType = GetEntityTypeHandle(),
            StateType = GetComponentTypeHandle<T>(false),
            MyStateMachineType = GetComponentTypeHandle<MyStateMachine>(false),
            TranslationType = GetComponentTypeHandle<Translation>(false),
            RotationType = GetComponentTypeHandle<Rotation>(false),
            NonUniformScaleType = GetComponentTypeHandle<NonUniformScale>(false),
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
public partial struct StateEnterJob<T> : IJobEntityBatch where T : struct, IComponentData, IMyState
{
    public float DeltaTime;
    public EntityCommandBuffer.ParallelWriter ECB;

    [ReadOnly]
    public EntityTypeHandle EntityType;
    public ComponentTypeHandle<T> StateType;
    public ComponentTypeHandle<MyStateMachine> MyStateMachineType;
    public ComponentTypeHandle<Translation> TranslationType;
    public ComponentTypeHandle<Rotation> RotationType;
    public ComponentTypeHandle<NonUniformScale> NonUniformScaleType;

    public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
    {
        NativeArray<Entity> chunkEntities = batchInChunk.GetNativeArray(EntityType);
        NativeArray<T> chunkStates = batchInChunk.GetNativeArray(StateType);
        NativeArray<MyStateMachine> chunkMyStateMachines = batchInChunk.GetNativeArray(MyStateMachineType);
        NativeArray<Translation> chunkTranslations = batchInChunk.GetNativeArray(TranslationType);
        NativeArray<Rotation> chunkRotationTypes = batchInChunk.GetNativeArray(RotationType);
        NativeArray<NonUniformScale> chunkNonUniformScaleTypes = batchInChunk.GetNativeArray(NonUniformScaleType);

        for (int i = 0; i < batchInChunk.Count; i++)
        {
            Entity entity = chunkEntities[i];
            T state = chunkStates[i];
            MyStateMachine stateMachine = chunkMyStateMachines[i];
            Translation translation = chunkTranslations[i];
            Rotation rotation = chunkRotationTypes[i];
            NonUniformScale scale = chunkNonUniformScaleTypes[i];

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

            state.OnStateEnter(ref refData, in inData);

            ECB.RemoveComponent<OnStateEnter>(batchIndex, entity);

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