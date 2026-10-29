using UnityEngine;

public class StartingState : State
{
    private readonly HunterAgent _agent;
    private readonly NavigationGraph _graph;

    public StartingState(HunterAgent agent, NavigationGraph graph, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
        _graph = graph;
    }

    public override void Enter()
    {
        NavigationNode destination = _graph.GetClosestWalkableNode(_agent.transform.position);
        _agent.SetDestination(destination.Position);
    }

    public override void Update()
    {
        bool arrived = _agent.FollowPath();

        if (!arrived)
            return;

        _agent.SetSearchPhase(SearchPhase.GoingToHuntingGround);
        _stateMachine.ChangeState(HunterState.Search);
    }
}
