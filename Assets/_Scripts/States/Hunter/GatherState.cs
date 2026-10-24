using System;
using UnityEngine;

public class GatherState : State
{
    private readonly HunterAgent _agent;
    private float _currentTime = 0;

    public event Action<float> ChannelProgressChanged;

    public GatherState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        ChannelProgressChanged?.Invoke(0f);
        _currentTime = 0;
        _agent.StopMoving();
    }

    public override void Update()
    {
        if (!_agent.HasTargetDead)
        {
            _stateMachine.ChangeState(HunterState.Patrol);
            return;
        }

        _currentTime += Time.deltaTime;

        float progress = Mathf.Clamp01(_currentTime / _agent.GatherDuration);
        ChannelProgressChanged?.Invoke(progress);

        if (_currentTime >= _agent.GatherDuration)
        {
            _agent.GatherPreyAgentDead();
            _stateMachine.ChangeState(HunterState.Patrol);
            return;
        }
    }

    public override void Exit()
    {
        ChannelProgressChanged?.Invoke(0f);
    }

}