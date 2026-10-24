using System.Collections;
using UnityEngine;

public class PreyManager : MonoBehaviour
{
    [SerializeField] private PreyAgent preyPrefab;
    [SerializeField] private float respawnTime = 20f;

    public static PreyManager Instance { get; private set; }

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

    public void DespawnAndRespawn(PreyAgent prey)
    {
        Destroy(prey.gameObject);
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        Vector3 spawnPosition = Bounds.Instance.GetRandomPosition();

        Instantiate(preyPrefab, spawnPosition, Quaternion.identity);
    }
}
