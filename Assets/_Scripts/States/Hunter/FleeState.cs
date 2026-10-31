using UnityEngine;

public class FleeState : State
{
    private readonly HunterAgent _agent;
    private readonly NavigationGraph _graph;
    private float _currentTime = 0;

    public FleeState(HunterAgent agent, NavigationGraph graph, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
        _graph = graph;
    }

    public override void Enter()
    {
        _currentTime = 0;
        NavigationNode destination;
        destination = _graph.GetClosestNodeInZone(_agent.transform.position, ZoneType.Safe);
        if (!_agent.SetDestination(destination.Position))
        {
            Debug.LogWarning($"No path found to {destination.Name}.");
        }
    }

    public override void Update()
    {
        bool arrived = _agent.FollowPath();
        if (!arrived)
            return;

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
