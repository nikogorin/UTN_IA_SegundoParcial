
// Exit State is like Search State, I created it only to fulfill the exam requirements
using UnityEngine;

public class ExitState : State
{
    private readonly HunterAgent _agent;
    private readonly NavigationGraph _graph;
    private float _currentTime = 0;
    public ExitState(HunterAgent hunterAgent, NavigationGraph graph, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = hunterAgent;
        _graph = graph;
    }

    public override void Enter()
    {
        NavigationNode destination = _graph.GetClosestNodeInZone(_agent.transform.position, ZoneType.Exit);
        TrySetDestination(destination);
        _currentTime = 0f;
    }

    public override void Update()
    {
        bool arrived = _agent.FollowPath();

        if (!arrived)
            return;

        _agent.StopMoving();
        _currentTime += Time.deltaTime;

        if (_currentTime > _agent.SafeZoneWaitTime )
        {
            NavigationNode initialNode = _graph.GetRandomNodeInZone(ZoneType.Initial);
            if (initialNode == null)
            {
                Debug.LogError("No Initial zone node was found.");
                return;
            }

            _agent.ResetSimulationCycle(initialNode.Position);
            _stateMachine.ChangeState(HunterState.Search);
        }
    }

    public override void Exit()
    {
        _agent.ClearPath();
    }

    private void TrySetDestination(NavigationNode destination)
    {
        if (destination == null)
        {
            Debug.LogWarning("SearchState couldn't select a destination.");
            return;
        }

        if (!_agent.SetDestination(destination.Position))
        {
            Debug.LogWarning($"No path found to {destination.Name}.");
        }
    }
}
