// EnemyStateMachine.cs
// Pure state-machine driver. Knows nothing about what states do.
// EnemyBase owns one of these and calls Tick() every frame.
//
// Adding a new state never requires touching this file.

using UnityEngine;

public class EnemyStateMachine
{
    public IEnemyState CurrentState { get; private set; }
    private EnemyBase _owner;

    public EnemyStateMachine(EnemyBase owner)
    {
        _owner = owner;
    }

    /// <summary>Set the initial state without calling Enter (use for spawn init).</summary>
    public void Initialise(IEnemyState startState)
    {
        CurrentState = startState;
        CurrentState.Enter(_owner);
    }

    /// <summary>Transition to a new state. Calls Exit on old, Enter on new.</summary>
    public void ChangeState(IEnemyState newState)
    {
        if (newState == null)
        {
            Debug.LogError("[EnemyStateMachine] Tried to change to a null state.");
            return;
        }
        if (newState == CurrentState) return;

        CurrentState?.Exit(_owner);
        CurrentState = newState;
        CurrentState.Enter(_owner);
    }

    /// <summary>Called every frame by EnemyBase.Update().</summary>
    public void Tick()
    {
        CurrentState?.Execute(_owner);
    }
}
