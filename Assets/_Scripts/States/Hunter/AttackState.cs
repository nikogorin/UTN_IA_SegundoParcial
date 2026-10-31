using UnityEngine;

public class AttackState : State
{
    private readonly HunterAgent _agent;

    public AttackState(HunterAgent hunterAgent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = hunterAgent;
    }

    public override void Enter()
    {
        _agent.TryLockAttackTarget();
    }

    public override void Update()
    {
        if (_agent.ThreatDetected)
        {
            _stateMachine.ChangeState(HunterState.Flee);
            return;
        }

        if (_agent.HasTargetDead)
        {
            _stateMachine.ChangeState(HunterState.GoingToGather);
            return;
        }

        if(!_agent.HasAttackTarget || !_agent.CanSeeAttackTarget())
        {
            _stateMachine.ChangeState(HunterState.Search);
            return;
        }

        float distance = _agent.DistanceToAttackTarget;

        if(distance <= _agent.MeleeAttackRadius)
        {
            _agent.StopMoving();
            _agent.MeleeAttack();
            _agent.ResetAttackCooldown();

            _stateMachine.ChangeState(HunterState.Search);
            return;
        }
        else if(distance <= _agent.RangeAttackRadius)
        {
            _agent.StopMoving();
            _agent.RangeAttack();
            _agent.ResetAttackCooldown();

            _stateMachine.ChangeState(HunterState.Search);
            return;
        }

        Vector3 steering = _agent.GetAttackPursuitSteering();

        _agent.ApplySteering(steering);
        _agent.Move();
    }

    public override void Exit()
    {
        _agent.ClearAttackTarget();
    }
}