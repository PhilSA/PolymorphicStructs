using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[assembly: RegisterGenericJobType(typeof(StateUpdateJob<StateA>))]
[assembly: RegisterGenericJobType(typeof(StateUpdateJob<StateB>))]
[assembly: RegisterGenericJobType(typeof(StateUpdateJob<StateC>))]
[assembly: RegisterGenericJobType(typeof(StateExitJob<StateA>))]
[assembly: RegisterGenericJobType(typeof(StateExitJob<StateB>))]
[assembly: RegisterGenericJobType(typeof(StateExitJob<StateC>))]
[assembly: RegisterGenericJobType(typeof(StateEnterJob<StateA>))]
[assembly: RegisterGenericJobType(typeof(StateEnterJob<StateB>))]
[assembly: RegisterGenericJobType(typeof(StateEnterJob<StateC>))]

public partial class StateAUpdateSystem : BaseStateUpdateSystem<StateA>
{ }
public partial class StateBUpdateSystem : BaseStateUpdateSystem<StateB>
{ }
public partial class StateCUpdateSystem : BaseStateUpdateSystem<StateC>
{ }
public partial class StateAExitSystem : BaseStateExitSystem<StateA>
{ }
public partial class StateBExitSystem : BaseStateExitSystem<StateB>
{ }
public partial class StateCExitSystem : BaseStateExitSystem<StateC>
{ }
public partial class StateAEnterSystem : BaseStateEnterSystem<StateA>
{ }
public partial class StateBEnterSystem : BaseStateEnterSystem<StateB>
{ }
public partial class StateCEnterSystem : BaseStateEnterSystem<StateC>
{ }