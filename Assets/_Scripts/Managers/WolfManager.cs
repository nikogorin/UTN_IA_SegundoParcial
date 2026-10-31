using System;
using System.Collections.Generic;
using UnityEngine;

public class WolfManager : MonoBehaviour
{
    public static WolfManager Instance { get; private set; }
    public event Action HunterCaptured;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void RaiseHunterCaptured()
    {
        HunterCaptured?.Invoke();
    }
}
