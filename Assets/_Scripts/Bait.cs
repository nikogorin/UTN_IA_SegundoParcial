using System;
using UnityEngine;

public class Bait : MonoBehaviour
{
    public Agent AssignedAgent { get; private set; }
    public event Action<Bait> Destroyed;

    public void Consume()
    {

        Destroyed?.Invoke(this);
        Destroy(gameObject);
    }

    public bool TryAssignAgent(PreyAgent agent)
    {
        if (AssignedAgent != null) 
            return false;

        if (!agent.CanTakeBait)
            return false;

        AssignedAgent = agent;
        
        return true;
    }

    public void ReleaseAgent(PreyAgent agent)
    {
        if (AssignedAgent == agent)
            AssignedAgent = null;
    }
}
