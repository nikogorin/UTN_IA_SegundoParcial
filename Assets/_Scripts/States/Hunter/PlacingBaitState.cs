using System;
using UnityEngine;

public class PlacingBaitState : State
{
    private float _currentTime = 0f;
    private readonly HunterAgent _agent;

    public event Action<float> ChannelProgressChanged;

    public PlacingBaitState(HunterAgent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _agent.StopMoving();
        _currentTime = 0f;
        ChannelProgressChanged?.Invoke(0f);
    }

    public override void Update()
    {
        _currentTime += Time.deltaTime;

        float progress = Mathf.Clamp01(_currentTime / _agent.PlacingBaitDelay);
        ChannelProgressChanged?.Invoke(progress);

        if (_currentTime >= _agent.PlacingBaitDelay)
        {
            if (!BaitManager.Instance.TrySpawnBait(_agent.transform.position))
                Debug.Log("Failed to spawn bait.");

            _agent.ResetBaitCooldown();
            _stateMachine.ChangeState(HunterState.Patrol);
            return;
        }
    }

    public override void Exit()
    {
        ChannelProgressChanged?.Invoke(0f);
    }
}
