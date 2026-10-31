using System;
using UnityEngine;

public class WolfManager : MonoBehaviour
{
    public static WolfManager Instance { get; private set; }
    public event Action HunterKilled;
    public event Action<Vector3, int> HunterDetected;
    public event Action HunterEnteredSafeZone;

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

    public void RaiseHunterKilled()
    {
        HunterKilled?.Invoke();
    }

    public void RaiseHunterAlertDetection(Vector3 hunterPosition, int detectingWolfId)
    {
        HunterDetected?.Invoke(hunterPosition, detectingWolfId);
    }

    public void RaiseHunterEnteredSafeZone()
    {
        HunterEnteredSafeZone?.Invoke();
    }
}
