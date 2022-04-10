using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct StateMachineSettings : IComponentData
{
    public bool UseStructuralChanges;
    public bool ScheduleParallel;

    public bool UseFixedDeltaTime;
    public float FixedDeltaTime;

    public float2 RandomStateMachineSpeed;
}
