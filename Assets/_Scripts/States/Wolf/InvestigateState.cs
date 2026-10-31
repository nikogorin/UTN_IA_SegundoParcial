using UnityEngine;

public class InvestigateState : State
{
    private readonly WolfAgent _agent;
    private float _searchTimer;
    private const float SearchDuration = 2f;
    private bool _hasPath;

    public InvestigateState(WolfAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _searchTimer = 0f;
        _hasPath = _agent.HasLastKnownHunterPosition && _agent.SetDestination(_agent.LastKnownHunterPosition);
    }

    public override void Update()
    {
        if (_agent.CanSeeHunter)
        {
            _stateMachine.ChangeState(WolfState.Chase);
            return;
        }

        if (!_hasPath)
        {
            FinishInvestigation();
            return;
        }

        bool arrived = _agent.FollowPath();
        if (!arrived)
            return;

        _searchTimer += Time.deltaTime;
        if (_searchTimer >= SearchDuration)
            FinishInvestigation();
    }

    public override void Exit()
    {
        _agent.ClearPath();
    }

    private void FinishInvestigation()
    {
        _agent.ForgetLastKnownHunterPosition();
        _stateMachine.ChangeState(WolfState.Patrol);
    }
}
