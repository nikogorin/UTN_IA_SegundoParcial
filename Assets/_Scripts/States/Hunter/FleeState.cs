using UnityEngine;

public class FleeState : State
{
    private readonly HunterAgent _agent;
    private float _currentTime = 0;
    private bool _hasPath;
    private bool _safeArrivalNotified;

    public FleeState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _currentTime = 0;
        _safeArrivalNotified = false;

        _agent.SetSearchPhase(SearchPhase.GoingToHuntingGround);

        _hasPath = _agent.SetDestinationToClosestZone(ZoneType.Safe);
        if (!_hasPath)
            Debug.LogWarning("No path found to a Safe zone.");
    }

    public override void Update()
    {
        if (!_hasPath)
        {
            _hasPath = _agent.SetDestinationToClosestZone(ZoneType.Safe);
            return;
        }

        bool arrived = _hasPath && _agent.FollowPath();
        if (!arrived)
            return;

        if (!_safeArrivalNotified)
        {
            _safeArrivalNotified = true;
            _agent.ArrivedToSafe();
        }

        _currentTime += Time.deltaTime;
        if (_currentTime >= _agent.SafeZoneWaitTime)
        {
            var stateToChange = _agent.RequiredGatheredCompleted ? HunterState.Exit : HunterState.Search;
            _stateMachine.ChangeState(stateToChange);
            return;
        }
    }

    public override void Exit()
    {
        _agent.ClearPath();
    }
}
