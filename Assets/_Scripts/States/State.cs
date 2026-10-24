public enum HunterState
{
    Idle,
    Patrol,
    PlacingBait,
    Attacking,
    GoingToGather,
    Gathering
}

public enum PreyState
{
    Flocking,
    GoingToBait,
    Eating,
    Evading,
    Dead
}

public abstract class State
{
    protected StateMachine _stateMachine;
    protected State(StateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public virtual void Enter() { }

    public virtual void Exit() { }

    public virtual void Update() { }
}