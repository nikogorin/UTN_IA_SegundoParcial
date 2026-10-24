using System.Collections.Generic;
using UnityEngine;

public class BaitManager : MonoBehaviour
{
    public static BaitManager Instance { get; private set; }

    [SerializeField] private GameObject _baitPrefab;
    [SerializeField] private int _maxBaitCount = 5;

    private List<Bait> _baits = new();

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

    public bool TrySpawnBait(Vector3 position)
    {
        if (_baits.Count >= _maxBaitCount)
            return false;

        GameObject baitObject = Instantiate(_baitPrefab, position, Quaternion.identity);

        Bait bait = baitObject.GetComponent<Bait>();
        bait.Destroyed += OnBaitDestroyed;

        _baits.Add(bait);

        return true;
    }

    private void OnBaitDestroyed(Bait bait)
    {
        bait.Destroyed -= OnBaitDestroyed;
        _baits.Remove(bait);
    }
}
