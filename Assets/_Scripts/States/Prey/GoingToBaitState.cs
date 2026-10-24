using UnityEngine;

public class GoingToBaitState : State
{
    private readonly PreyAgent _agent;

    public GoingToBaitState(StateMachine stateMachine, PreyAgent agent) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Update()
    {
        if (_agent.IsDead)
        {
            _stateMachine.ChangeState(PreyState.Dead);
            return;
        }

        if(_agent.IsBaitAssigned && _agent.IsHunterDetected)
        {
            _agent.ReleaseBait();
            _stateMachine.ChangeState(PreyState.Evading);
            return;
        }

        Vector3 steering = _agent.GetArriveSteering();
        _agent.ApplySteering(steering);
        _agent.Move();

        if(_agent.IsBaitAssigned && _agent.IsCloseToBait)
        {
            _stateMachine.ChangeState(PreyState.Eating);
            return;
        }
    }
}
