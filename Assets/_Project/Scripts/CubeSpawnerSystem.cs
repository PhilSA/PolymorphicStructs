using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[AlwaysSynchronizeSystem]
public partial class CubeSpawnerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        Entities.ForEach((Entity entity, ref CubeSpawner spawner) => 
        {
            Random random = Random.CreateFromIndex(1);
            MyStateMachine stateMachine = GetComponent<MyStateMachine>(spawner.CubePrefab);
            int spawnResolution = (int)math.ceil(math.sqrt(spawner.SpawnCount));

            int spawnCounter = 0;
            for (int x = 0; x < spawnResolution; x++)
            {
                for (int y = 0; y < spawnResolution; y++)
                {
                    Entity spawnedPrefab = ecb.Instantiate(spawner.CubePrefab);
                    ecb.SetComponent(spawnedPrefab, new Translation { Value = new float3(x * spawner.Spacing, 0f, y * spawner.Spacing) });

                    stateMachine.Speed = random.NextFloat(0.3f, 1.5f);
                    ecb.SetComponent(spawnedPrefab, stateMachine);

                    spawnCounter++;
                    if(spawnCounter >= spawner.SpawnCount)
                    {
                        break;
                    }
                }

                if (spawnCounter >= spawner.SpawnCount)
                {
                    break;
                }
            }

            ecb.DestroyEntity(entity);
        }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
