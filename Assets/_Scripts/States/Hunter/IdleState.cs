using UnityEngine;

public class IdleState : State
{
    private float _idleTime = 0f;
    private readonly float _timerToChange = 3f;
    private HunterAgent _agent;

    public IdleState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _idleTime = 0f;
    }

    public override void Update()
    {
        if(_agent.CanAttack)
        {
            _stateMachine.ChangeState(HunterState.Attacking);
            return;
        }

        _idleTime += Time.deltaTime;

        if (_timerToChange <= _idleTime)
        {
            _stateMachine.ChangeState(HunterState.PlacingBait);
            return;
        }
    }
}
