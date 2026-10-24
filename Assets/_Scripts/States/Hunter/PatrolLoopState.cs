using UnityEngine;

//public class PatrolLoopState : State
//{
//    private readonly HunterAgent _agent;
//    private readonly PatrolData _patrolData;

//    private int _currentWaypointIndex = 0;

//    public PatrolLoopState(HunterAgent agent, PatrolData patrolData, StateMachine stateMachine) : base(stateMachine)
//    {
//        _agent = agent;
//        _patrolData = patrolData;
//    }

//    public override void Update()
//    {
//        if (_agent.HasTargetDead)
//        {
//            _stateMachine.ChangeState(HunterState.GoingToGather);
//            return;
//        }

//        if (_agent.CanPlaceBait)
//        {
//            _stateMachine.ChangeState(HunterState.PlacingBait);
//            return;
//        }

//        PatrolLoop();
//    }

//    private void PatrolLoop()
//    {
//        if (_agent.CanAttack)
//        {
//            _stateMachine.ChangeState(HunterState.Attacking);
//            return;
//        }

//        var currentWaypoint = _patrolData.Waypoints[_currentWaypointIndex];

//        if (Vector3.Distance(currentWaypoint.position, _patrolData.Transform.position) <= _patrolData.WaypointCheckDistance)
//        {
//            _currentWaypointIndex = _currentWaypointIndex + 1 < _patrolData.Waypoints.Count ? _currentWaypointIndex + 1 : 0;
//        }

//        var nextWaypoint = _patrolData.Waypoints[_currentWaypointIndex];
        
//        Vector3 steering = _agent.GetSeekSteering(nextWaypoint);
//        _agent.ApplySteering(steering);
//        _agent.Move();
//    }
//}