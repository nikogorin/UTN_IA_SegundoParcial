public class ExitState : State
{
    private readonly HunterAgent _agent;
    private readonly NavigationGraph _graph;
    private bool _hasPath;

    public ExitState(HunterAgent hunterAgent, NavigationGraph graph, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = hunterAgent;
        _graph = graph;
    }

    public override void Enter()
    {
        NavigationNode destination = _graph.GetClosestNodeInZone(_agent.transform.position, ZoneType.Exit);
        _hasPath = destination != null && _agent.SetDestination(destination.Position);
    }

    public override void Update()
    {
        if (_agent.ThreatDetected)
        {
            _stateMachine.ChangeState(HunterState.Flee);
            return;
        }

        if (!_hasPath)
            return;

        if (!_agent.FollowPath())
            return;

        NavigationNode initialNode = _graph.GetRandomNodeInZone(ZoneType.Initial);
        if (initialNode == null)
            return;

        _agent.ResetSimulationCycle(initialNode.Position);
        _stateMachine.ChangeState(HunterState.Search);
    }

    public override void Exit()
    {
        _agent.ClearPath();
    }
}
