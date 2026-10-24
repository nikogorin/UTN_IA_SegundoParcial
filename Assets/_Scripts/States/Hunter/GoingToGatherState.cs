using UnityEngine;

public class GoingToGatherState : State
{
    private readonly HunterAgent _agent;

    public GoingToGatherState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Update()
    {
        if (!_agent.HasTargetDead)
        {
            _stateMachine.ChangeState(HunterState.Patrol);
            return;
        }

        Vector3 steering = _agent.GetArriveSteering();
        _agent.ApplySteering(steering);
        _agent.Move();

        if (_agent.HasTargetDead && _agent.IsCloseToGather)
        {
            _stateMachine.ChangeState(HunterState.Gathering);
            return;
        }
    }
}
