using UnityEngine;

public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }

    [SerializeField] private float height = 10f;
    [SerializeField] private float width = 10f;
    [SerializeField] private bool drawGizmos = true;

    public float LimitHeight => height / 2;
    public float LimitWidth => width / 2;

    public void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector3 OutOfBounds(Vector3 position)
    {
        Vector3 newPosition = position;

        if (position.x > width / 2)
            newPosition.x = -width / 2;
        if (position.x < -width / 2)
            newPosition.x = width / 2;
        if (position.z > height / 2)
            newPosition.z = -height / 2;
        if (position.z < -height / 2)
            newPosition.z = height / 2;

        return newPosition;
    }

    public Vector3 GetRandomPosition()
    {
        Vector3 rndPosition = new(Random.Range(-LimitWidth, LimitWidth), 0f, Random.Range(-LimitHeight, LimitHeight));
        return rndPosition;
    }


    private void OnDrawGizmos()
    {
        if(!drawGizmos) return;

        Gizmos.color = Color.blueViolet;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, 0, height));
    }
}
