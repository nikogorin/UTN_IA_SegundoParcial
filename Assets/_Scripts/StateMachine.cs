using System;
using System.Collections.Generic;

public class StateMachine
{
    private Dictionary<Enum, State> _states = new();
    public State CurrentState { get; private set; }
    public Action<Enum> OnStateChanged { get; set; }

    public void RegisterState(Enum key, State value)
    {
        _states[key] = value;
    }

    public void ChangeState(Enum key)
    {
        State newState = _states[key];

        if (newState == CurrentState)
            return;

        CurrentState?.Exit();
        
        CurrentState = newState;
        
        CurrentState?.Enter();

        OnStateChanged?.Invoke(key);
    }

    public void Update()
    {
        CurrentState?.Update();
    }
}