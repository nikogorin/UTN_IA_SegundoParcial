using UnityEngine;

public class InvestigateState : State
{
    private readonly WolfAgent _agent;
    private float _searchTimer;
    private const float SearchDuration = 2f;
    private bool _hasPath;
    private Vector3 _investigationTarget;

    public InvestigateState(WolfAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _searchTimer = 0f;
        UpdateInvestigationPath();
    }

    public override void Update()
    {
        if (_agent.HasLastKnownHunterPosition)
        {
            float differenceSqr = (_agent.LastKnownHunterPosition - _investigationTarget).sqrMagnitude;
            if (differenceSqr > 0.01f)
                UpdateInvestigationPath();
        }

        if (_agent.IsHunterInSafeZone())
        {
            _agent.AbandonHunterTrail();
            _stateMachine.ChangeState(WolfState.Patrol);
            return;
        }

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

    private void UpdateInvestigationPath()
    {
        if (!_agent.HasLastKnownHunterPosition)
        {
            _hasPath = false;
            return;
        }

        _investigationTarget = _agent.LastKnownHunterPosition;
        _hasPath = _agent.SetDestination(_investigationTarget);
    }
}
