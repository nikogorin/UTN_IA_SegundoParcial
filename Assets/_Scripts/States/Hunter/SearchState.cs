using UnityEngine;

public class SearchState : State
{
    private readonly HunterAgent _agent;
    private readonly NavigationGraph _graph;

    public SearchState(HunterAgent hunterAgent, NavigationGraph graph, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = hunterAgent;
        _graph = graph;
    }

    public override void Enter()
    {
        NavigationNode destination = _graph.GetClosestNodeInZone(_agent.transform.position, ZoneType.HuntingGround);
        _agent.SetDestination(destination.Position);
    }

    public override void Update()
    {
        bool arrived = _agent.FollowPath();

        if (!arrived)
            return;

        NavigationNode nextDestination = _graph.GetRandomNodeInZone(ZoneType.HuntingGround);

        if (nextDestination != null)
            _agent.SetDestination(nextDestination.Position);
        // Change state or search another destination
    }
}