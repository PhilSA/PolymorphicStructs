# PolymorphicStructs

See the forum thread for more information
https://forum.unity.com/threads/sources-included-polymorphism-in-dots-now-with-source-generators.1264616/

## Installing into your project
1. Import the `PolymorphicStructs.unitypackage` into your project (find it in the [Releases](https://github.com/PhilSA/PolymorphicStructs/releases) section)
2. Make sure all of the code that's going to use PolymorphicStructs is under an .asmdef and references the PolymorphicStructs.asmdef

## How to use
1. Create an interface that will represent the various functions your polymorphic structs will share. This interface must have the `[PolymorphicStruct]`  attribute on it
```cs
[PolymorphicStruct]
public interface IMyState
{
    public void OnStateEnter(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData);
    public void OnStateExit(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData);
    public void OnStateUpdate(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData); 
}
```
2. Create the structs that implement that interface, representing all the different forms that the polymorphic stuct can assume. Make sure these structs are `partial`
```cs

[Serializable]
public partial struct StateA : IMyState
{
    public float Duration;
    public float3 MovementSpeed;

    private float _durationCounter;

    public void OnStateEnter(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        _durationCounter = Duration;
    }

    public void OnStateExit(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        refData.Translation = refData.StateMachine.StartTranslation;
    }

    public void OnStateUpdate(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        refData.Translation += MovementSpeed * inData.DeltaTime;

        _durationCounter -= inData.DeltaTime;
        if (_durationCounter <= 0f)
        {
            refData.StateMachine.CurrentStateIndex = refData.StateMachine.StateBIndex;
        }
    }
}


[Serializable]
public partial struct StateB : IMyState
{
    public float Duration;
    public float3 RotationSpeed;

    private float _durationCounter;

    public void OnStateEnter(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        _durationCounter = Duration;
    }

    public void OnStateExit(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        refData.Rotation = quaternion.identity;
    }

    public void OnStateUpdate(ref StateUpdateData_ReadWrite refData, in StateUpdateData_ReadOnly inData)
    {
        refData.Rotation = math.mul(quaternion.Euler(RotationSpeed * inData.DeltaTime), refData.Rotation);

        _durationCounter -= inData.DeltaTime;
        if (_durationCounter <= 0f)
        {
            refData.StateMachine.CurrentStateIndex = refData.StateMachine.StateCIndex;
        }
    }
}
```
3. This will generate a `MyState` struct (takes the name of the interface, minus the first character "I") that will have all the functions of the `IMyState` interface. You can now call polymorphic functions on MyState like this and it will call the implementation of whichever version of the struct was assigned to `MyState`:
```cs
myState.OnStateUpdate(...)
``` 
4. You can also create a `MyState` from the various specific structs and vice-versa like this:
```cs
// Creating a MyState from a StateA
StateA stateA = new StateA();
MyState state = stateA.ToMyState();

// Creating a StateA from a MyState
StateA stateA = new StateA(state);
```

## How the sample works
1. `TestScene` is the sample scene
2. `CubeSpawnerSystem` initializes the scene by spawning thousands of cube prefabs (`Cube`)
3. The `Cube` prefab has a `StateMachineAuthoring` component
4. The `StateMachineAuthoring` component adds a `MyStateMachine` as well as a `DynamicBuffer<MyState>`. This buffer is the place where we store all of our polymorphic states. You can see how we create a polymorphic `MyState` from individual state structs in the `StateMachineAuthoring.AddStateToStatesBuffer()` method
5. `MyStateMachineSystem` iterates over all state machine entities, and for each one, handles calling `OnStateUpdate()` on whichever state is set as the `CurrentState`. It also handles transitions
