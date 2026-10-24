using UnityEngine;

public class FlockingState : State
{
    private readonly PreyAgent _agent;

    public FlockingState(StateMachine stateMachine, PreyAgent agent) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _agent.StartMoving();
    }

    public override void Update()
    {
        Vector3 steering = _agent.GetFlockingSteering();
        _agent.ApplySteering(steering);
        _agent.Move();

        if (_agent.IsDead)
        {
            _stateMachine.ChangeState(PreyState.Dead);
            return;
        }
        else if (_agent.IsBaitAssigned)
        {
            _stateMachine.ChangeState(PreyState.GoingToBait);
            return;
        }
        else if(_agent.IsHunterDetected)
        {
            _stateMachine.ChangeState(PreyState.Evading);
            return;
        }
    }
}
