using System;
using UnityEngine;

public class GatheringState : State
{
    private readonly HunterAgent _agent;
    private float _currentTime = 0;

    public event Action<float> ChannelProgressChanged;

    public GatheringState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
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
        if (_agent.ThreatDetected)
        {
            _stateMachine.ChangeState(HunterState.Flee);
            return;
        }

        if (!_agent.HasTargetDead)
        {
            _stateMachine.ChangeState(HunterState.Search);
            return;
        }

        _currentTime += Time.deltaTime;

        float progress = Mathf.Clamp01(_currentTime / _agent.GatherDuration);
        ChannelProgressChanged?.Invoke(progress);

        if (_currentTime >= _agent.GatherDuration)
        {
            _agent.GatherPreyAgentDead();
            var stateToChange = _agent.RequiredGatheredCompleted ? HunterState.Exit : HunterState.Search;
            _stateMachine.ChangeState(stateToChange);
            return;
        }
    }

    public override void Exit()
    {
        ChannelProgressChanged?.Invoke(0f);
    }

}