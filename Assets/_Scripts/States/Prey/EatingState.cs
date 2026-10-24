using System;
using UnityEngine;

public class EatingState : State
{
    private readonly PreyAgent _agent;
    private float _currentEatingTime;

    public event Action<float> ChannelProgressChanged;

    public EatingState(StateMachine stateMachine, PreyAgent agent) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _currentEatingTime = 0;
        ChannelProgressChanged?.Invoke(0f);
        _agent.StopMoving();
    }

    public override void Update()
    {
        if (_agent.IsDead)
        {
            _stateMachine.ChangeState(PreyState.Dead);
            return;
        }

        _currentEatingTime += Time.deltaTime;

        float progress = Mathf.Clamp01(_currentEatingTime / _agent.EatingTime);
        ChannelProgressChanged?.Invoke(progress);

        if (_currentEatingTime >= _agent.EatingTime)
        {
            _agent.ConsumeTargetBait();
            _currentEatingTime = 0;
            _stateMachine.ChangeState(PreyState.Flocking);
            return;
        }
    }

    public override void Exit()
    {
        ChannelProgressChanged?.Invoke(0f);
    }
}
