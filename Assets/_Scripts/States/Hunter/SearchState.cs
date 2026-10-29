using UnityEngine;

public class SearchState : State
{
    private readonly HunterAgent _agent;
    private readonly NavigationGraph _graph;
    private readonly float _minDistance = 15f;
    public SearchState(HunterAgent hunterAgent, NavigationGraph graph, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = hunterAgent;
        _graph = graph;
    }
    public override void Enter()
    {
        NavigationNode destination;
        if (!_agent.IsOnHunterGround)
        {
            destination = _graph.GetClosestNodeInZone(_agent.transform.position, ZoneType.HuntingGround);
        }
        else
        {
            destination = _graph.GetRandomNodeInZone(_agent.transform.position, _minDistance, ZoneType.HuntingGround);
        }

        TrySetDestination(destination);
    }

    public override void Update()
    {
        if (_agent.IsOnHunterGround)
        {
            if (_agent.HasTargetDead)
            {
                _stateMachine.ChangeState(HunterState.GoingToGather);
                return;
            }

            if(_agent.CanAttack)
            {
                _stateMachine.ChangeState(HunterState.Attacking);
                return;
            }

            if (_agent.CanPlaceBait)
            {
                _stateMachine.ChangeState(HunterState.PlacingBait);
                return;
            }
        }

        bool arrived = _agent.FollowPath();

        if (!arrived)
            return;

        if (!_agent.IsOnHunterGround)
        {
            _agent.SetSearchPhase(SearchPhase.ExploringHuntingGround);
        }

        NavigationNode nextDestination = _graph.GetRandomNodeInZone(_agent.transform.position, _minDistance, ZoneType.HuntingGround);
        TrySetDestination(nextDestination);
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