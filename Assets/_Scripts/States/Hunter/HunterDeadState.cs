using UnityEngine;

public class HunterDeadState : State
{
    private HunterAgent _agent;
    private NavigationGraph _graph;
    private float _currentDeadTime;

    public HunterDeadState(HunterAgent agent, NavigationGraph graph, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
        _graph = graph;
    }

    public override void Enter()
    {
        _agent.SetDeadPresentation(true);
        _agent.StopMoving();
        _currentDeadTime = 0f;
    }

    public override void Update()
    {
        _currentDeadTime += Time.deltaTime;
        if(_currentDeadTime < _agent.RespawnTime)
            return;

        NavigationNode initialNode = _graph.GetRandomNodeInZone(ZoneType.Initial);
        if (initialNode == null)
        {
            Debug.LogError("No Initial zone node was found.");
            return;
        }
        _agent.ResetSimulationCycle(initialNode.Position);
        _agent.SetDeadPresentation(false);
        _stateMachine.ChangeState(HunterState.Search);
    }
}
