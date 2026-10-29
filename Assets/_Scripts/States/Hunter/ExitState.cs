
// Exit State is like Search State, I created it only to fulfill the exam requirements
using UnityEngine;

public class ExitState : State
{
    private readonly HunterAgent _agent;
    private readonly NavigationGraph _graph;

    public ExitState(HunterAgent hunterAgent, NavigationGraph graph, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = hunterAgent;
        _graph = graph;
    }

    public override void Enter()
    {
        NavigationNode destination = _graph.GetClosestNodeInZone(_agent.transform.position, ZoneType.Exit);
        TrySetDestination(destination);
    }

    public override void Update()
    {
        bool arrived = _agent.FollowPath();

        if (!arrived)
            return;

        Debug.Log("Arrived to Exit Zone... Wait to Exit (?)");
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
