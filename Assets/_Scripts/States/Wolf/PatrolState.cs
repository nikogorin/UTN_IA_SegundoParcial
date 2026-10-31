using UnityEngine;

public class PatrolState : State
{
    private readonly WolfAgent _agent;
    private readonly PatrolData _patrolData;
    private int _currentWaypointIndex = 0;
    private bool _hasPath;

    public PatrolState(WolfAgent agent, PatrolData patrolData, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
        _patrolData = patrolData;
    }
    public override void Enter()
    {
        _currentWaypointIndex = 0;
        SetPathToCurrentWaypoint();
    }

    public override void Update()
    {
        if (_agent.CanSeeHunter)
        {
            _stateMachine.ChangeState(WolfState.Chase);
            return;
        }

        if (_agent.HasLastKnownHunterPosition)
        {
            _stateMachine.ChangeState(WolfState.Investigate);
            return;
        }

        if (!HasValidPatrol())
            return;

        if (!_hasPath)
        {
            SetPathToCurrentWaypoint();
            return;
        }

        bool arrived = _agent.FollowPath();
        if (!arrived)
            return;

        SelectNextWaypoint();
        SetPathToCurrentWaypoint();
    }

    public override void Exit()
    {
        _agent.ClearPath();
    }

    private void SelectNextWaypoint()
    {
        _currentWaypointIndex++;

        if (_currentWaypointIndex >= _patrolData.Waypoints.Count)
            _currentWaypointIndex = 0;
    }

    private void SetPathToCurrentWaypoint()
    {
        if (!HasValidPatrol())
        {
            _hasPath = false;
            return;
        }

        Transform waypoint = _patrolData.Waypoints[_currentWaypointIndex];
        if (waypoint == null)
        {
            Debug.LogWarning($"Wolf waypoint {_currentWaypointIndex} is null.");
            _hasPath = false;
            return;
        }

        _hasPath = _agent.SetDestination(waypoint.position);
        if (!_hasPath)
        {
            Debug.LogWarning($"No path found to wolf waypoint " + $"{_currentWaypointIndex}.");
        }
    }

    private bool HasValidPatrol()
    {
        return _patrolData != null && _patrolData.Waypoints != null && _patrolData.Waypoints.Count > 0;
    }
}
