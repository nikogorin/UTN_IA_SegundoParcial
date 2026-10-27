using UnityEngine;

public class NavigationGraphDebugger : MonoBehaviour
{
    [SerializeField] private NavigationGraph graph;
    [SerializeField] private Transform[] testPoints;

    [Header("Refresh")]
    [SerializeField, Min(0.05f)]
    private float refreshInterval = 0.25f;

    [Header("Gizmos")]
    [SerializeField] private Color testPointColor = Color.magenta;
    [SerializeField] private Color closestNodeColor = Color.green;
    [SerializeField, Min(0.05f)] private float pointRadius = 0.5f;

    private NavigationNode[] _closestNodes;
    private float _nextRefreshTime;

    private void Awake()
    {
        if (graph == null)
            graph = FindFirstObjectByType<NavigationGraph>();
    }

    private void Start()
    {
        RefreshNodes();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime =
            Time.unscaledTime + refreshInterval;

        RefreshNodes();
    }

    private void RefreshNodes()
    {
        if (graph == null || testPoints == null)
            return;

        if (_closestNodes == null ||
            _closestNodes.Length != testPoints.Length)
        {
            _closestNodes =
                new NavigationNode[testPoints.Length];
        }

        for (int i = 0; i < testPoints.Length; i++)
        {
            Transform testPoint = testPoints[i];

            if (testPoint == null)
            {
                _closestNodes[i] = null;
                continue;
            }

            NavigationNode previousNode =
                _closestNodes[i];

            NavigationNode closestNode =
                graph.GetClosestWalkableNode(
                    testPoint.position
                );

            _closestNodes[i] = closestNode;

            if (closestNode != null &&
                closestNode != previousNode)
            {
                Debug.Log(
                    $"{testPoint.name}: " +
                    $"{closestNode.Name}, " +
                    $"position {closestNode.Position}, " +
                    $"zone {closestNode.Zone}"
                );
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (testPoints == null)
            return;

        for (int i = 0; i < testPoints.Length; i++)
        {
            Transform testPoint = testPoints[i];

            if (testPoint == null)
                continue;

            Gizmos.color = testPointColor;
            Gizmos.DrawWireSphere(
                testPoint.position,
                pointRadius
            );

            if (_closestNodes == null ||
                i >= _closestNodes.Length)
            {
                continue;
            }

            NavigationNode closestNode =
                _closestNodes[i];

            if (closestNode == null)
                continue;

            Vector3 nodePosition =
                closestNode.Position
                + Vector3.up * 0.25f;

            Gizmos.color = closestNodeColor;
            Gizmos.DrawSphere(
                nodePosition,
                pointRadius * 0.5f
            );

            Gizmos.DrawLine(
                testPoint.position,
                nodePosition
            );
        }
    }
}
