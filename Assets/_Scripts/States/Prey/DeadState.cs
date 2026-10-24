public class DeadState : State
{
    private PreyAgent _agent;
    public DeadState(StateMachine stateMachine, PreyAgent agent) : base(stateMachine)
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
