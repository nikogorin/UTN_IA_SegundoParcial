using UnityEngine;

public class EvadingState : State
{
    private readonly PreyAgent _agent;
    public EvadingState(StateMachine stateMachine, PreyAgent agent) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _agent.ReleaseBait();
    }

    public override void Update()
    {
        if (_agent.IsDead)
        {
            _stateMachine.ChangeState(PreyState.Dead);
            return;
        }

        if (!_agent.IsHunterDetected)
        {
            _stateMachine.ChangeState(PreyState.Flocking);
            return;
        }

        Vector3 steering = _agent.GetEvadeSteering();
        _agent.ApplySteering(steering);
        _agent.Move();
    }
}
