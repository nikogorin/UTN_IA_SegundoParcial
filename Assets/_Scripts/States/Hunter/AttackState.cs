using UnityEngine;

public class AttackState : State
{
    private readonly HunterAgent _agent;

    public AttackState(HunterAgent hunterAgent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = hunterAgent;
    }

    //Revisar comportamientos de estados hacia HunterState.Search
    public override void Update()
    {
        if(_agent.HasTargetDead)
        {
            _stateMachine.ChangeState(HunterState.GoingToGather);
            return;
        }

        if(!_agent.HasTargetAlive)
        {
            _stateMachine.ChangeState(HunterState.Search);
            return;
        }

        float distance = _agent.DistanceToTargetAlive;

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

        Vector3 steering = _agent.GetPursuitSteering();
        _agent.ApplySteering(steering);
        _agent.Move();
    }
}