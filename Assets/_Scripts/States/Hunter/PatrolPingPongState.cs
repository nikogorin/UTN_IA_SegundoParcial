using UnityEngine;

//public class PatrolPingPongState : State
//{
//    private readonly HunterAgent _agent;
//    private readonly PatrolData _patrolData;

//    private int _currentWaypointIndex = 0;
//    private int _patrolDirection = 1;

//    public PatrolPingPongState(HunterAgent agent, PatrolData patrolData, StateMachine stateMachine) : base(stateMachine)
//    {
//        _agent = agent;
//        _patrolData = patrolData;
//    }

//    public override void Update()
//    {
//        PatrolPingPong();
//        // Is not use, need to be added the changeState logic
//        // _stateMachine.ChangeState(HunterState.Idle);
//    }

//    private void PatrolPingPong()
//    {
//        var nextWaypoint = _patrolData.Waypoints[_currentWaypointIndex];

//        if (Vector3.Distance(nextWaypoint.position, _patrolData.Transform.position) <= _patrolData.WaypointCheckDistance)
//        {
//            _currentWaypointIndex += _patrolDirection;

//            if (_currentWaypointIndex >= _patrolData.Waypoints.Count)
//            {
//                _currentWaypointIndex = _patrolData.Waypoints.Count - 1;
//                _patrolDirection = -1;
//            }
//            else if (_currentWaypointIndex < 0)
//            {
//                _currentWaypointIndex = 0;
//                _patrolDirection = 1;
//            }
//        }

//        Vector3 steering = _agent.GetSeekSteering(nextWaypoint);
//        _agent.ApplySteering(steering);
//        _agent.Move();
//    }
//}