// IEnemyState.cs
// Contract every FSM state must fulfil.
// Adding a new state = implement this interface, nothing else changes.

public interface IEnemyState
{
    void Enter(EnemyBase enemy);
    void Execute(EnemyBase enemy);
    void Exit(EnemyBase enemy);
}
