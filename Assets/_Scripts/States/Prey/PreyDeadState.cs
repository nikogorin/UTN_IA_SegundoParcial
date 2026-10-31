public class PreyDeadState : State
{
    private PreyAgent _agent;
    public PreyDeadState(StateMachine stateMachine, PreyAgent agent) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _agent.StopMoving();
        _agent.ReleaseBait();
    }

    public override void Update()
    {
        if (_agent.IsGathered)
            _agent.gameObject.SetActive(false);
    }
}
