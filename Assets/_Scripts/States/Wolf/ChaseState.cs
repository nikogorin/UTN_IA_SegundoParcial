using UnityEngine;

public class ChaseState : State
{
    private readonly WolfAgent _agent;

    public ChaseState(WolfAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        // Alertar otros lobos
    }

    public override void Update()
    {
        if (_agent.IsHunterInSafeZone())
        {
            _agent.AbandonHunterTrail();
            _stateMachine.ChangeState(WolfState.Patrol);
            return;
        }

        if (!_agent.CanSeeHunter)
        {
            _stateMachine.ChangeState(WolfState.Investigate);
            return;
        }

        if(_agent.IsReachedHunter())
        {
            _agent.KillHunter();
            _stateMachine.ChangeState(WolfState.Patrol);
            return;
        }

        Vector3 steering = _agent.GetPursuitSteering();
        _agent.ApplySteering(steering);
        _agent.Move();
    }
}
