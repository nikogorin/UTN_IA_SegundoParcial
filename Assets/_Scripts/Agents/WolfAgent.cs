using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(WolfStateUI))]
public class WolfAgent : SteeringAgent
{
    [Header("Attack stats")]
    [SerializeField, Min(0.1f)] private float attackRange = 0.9f;

    [Header("Patrol")]
    [SerializeField] private PatrolData patrolData;

    [Header("Navigation")]
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private NavigationGraph graph;
    [SerializeField, Min(0.1f)] private float nodeReachDistance = 0.3f;

    [Header("Field of view")]
    [SerializeField] private float targetDetectionRadius = 10f;
    [SerializeField] protected float eyeHeight = 1f;
    [SerializeField, Range(0f, 360f)] protected float viewAngle = 120f;
    [SerializeField] protected LayerMask targetMask = ~0;
    [SerializeField] protected LayerMask obstacleMask = ~0;
    [SerializeField] private float detectionInterval = 0.10f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawCurrentPath = true;
    [SerializeField] private Color pathColor = Color.magenta;
    [SerializeField] private Color currentNodeColor = Color.yellow;
    [SerializeField] private Color destinationNodeColor = Color.green;
    [SerializeField, Min(0.01f)] private float pathNodeSize = 0.35f;
    [SerializeField, Min(0f)] private float pathHeightOffset = 0.3f;

    public Vector3 LastKnownHunterPosition => _lastKnownHunterPosition;
    public bool HasLastKnownHunterPosition => _hasLastKnownHunterPosition;
    public bool CanSeeHunter => _visibleHunter != null;

    private StateMachine _stateMachine;
    private InvestigateState _investigateState;
    private WolfStateUI _stateUI;
    private List<NavigationNode> _currentPath = new();
    private int _currentPathIndex;
    private HunterAgent _hunterInRange;
    private HunterAgent _visibleHunter;
    private SphereCollider _trigger;
    private double _nextDetection;
    private Vector3 _lastKnownHunterPosition = default;
    private bool _hasLastKnownHunterPosition;

    #region [Unity Events]
    protected override void Awake()
    {
        base.Awake();

        _trigger = GetComponent<SphereCollider>();
        ConfigureTrigger();

        _stateMachine = new StateMachine();
        
        _stateUI = GetComponent<WolfStateUI>();
        _stateMachine.OnStateChanged += _stateUI.SetState;
        _stateUI.SetChannelingSlide(0f);

        PatrolState patrolState = new(this, patrolData, _stateMachine);
        ChaseState chaseState = new(this, _stateMachine);
        _investigateState = new(this, _stateMachine);

        _investigateState.ChannelProgressChanged += _stateUI.SetChannelingSlide;

        _stateMachine.RegisterState(WolfState.Patrol, patrolState);
        _stateMachine.RegisterState(WolfState.Chase, chaseState);
        _stateMachine.RegisterState(WolfState.Investigate, _investigateState);

    }

    private void Start()
    {
        _stateMachine.ChangeState(WolfState.Patrol);

        if(WolfManager.Instance != null)
        {
            WolfManager.Instance.HunterDetected += HunterDetected;
            WolfManager.Instance.HunterKilled += HunterKilled;
            WolfManager.Instance.HunterEnteredSafeZone += HunterEnteredSafeZone;
        }
    }

    void Update()
    {
        double now = Time.timeAsDouble;

        if (now >= _nextDetection)
        {
            _nextDetection = now + detectionInterval;
            PerformHunterDetection();
        }

        _stateMachine.Update();
    }

    private void OnDestroy()
    {
        if (WolfManager.Instance != null)
        {
            WolfManager.Instance.HunterDetected -= HunterDetected;
            WolfManager.Instance.HunterKilled -= HunterKilled;
            WolfManager.Instance.HunterEnteredSafeZone -= HunterEnteredSafeZone;
        }

        _investigateState.ChannelProgressChanged -= _stateUI.SetChannelingSlide;
    }

    private void OnValidate()
    {
        if (_trigger == null)
            _trigger = GetComponent<SphereCollider>();

        ConfigureTrigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetHunter(other, out HunterAgent hunter))
            return;

        _hunterInRange = hunter;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryGetHunter(other, out HunterAgent hunter))
            return;

        if (_hunterInRange != hunter)
            return;

        ForgetHunter();
    }

    #endregion

    #region [Public Methods]
    public void ForgetLastKnownHunterPosition()
    {
        _lastKnownHunterPosition = default;
        _hasLastKnownHunterPosition = false;
        _stateUI.SetNotificationImage(false, isAlert: false);
    }

    public void ForgetHunter()
    {
        _hunterInRange = null;
        _visibleHunter = null;
    }

    public void KillHunter()
    {
        if (WolfManager.Instance == null)
            return;

        WolfManager.Instance.RaiseHunterKilled();
    }

    public bool FollowPath()
    {
        if (_currentPath == null || _currentPath.Count == 0)
            return true;

        SkipReachedNodes();

        if (_currentPathIndex >= _currentPath.Count)
        {
            StopMoving();
            return true;
        }

        NavigationNode currentNode = _currentPath[_currentPathIndex];

        Vector3 steering = Arrive(currentNode.Position); // Or Seek depend of the orbit effect
        ApplySteering(steering);
        Move();

        return false;
    }

    public Vector3 GetPursuitSteering()
    {
        return _hunterInRange != null ? Pursuit(_hunterInRange) : Vector3.zero;
    }

    public bool IsReachedHunter()
    {
        if(_visibleHunter == null)
            return false;
        
        return Vector3.SqrMagnitude(_visibleHunter.transform.position - transform.position) <= (attackRange * attackRange);
    }

    public bool IsHunterInSafeZone()
    {
        if (_hunterInRange == null)
            return false;

        NavigationNode hunterNode = graph.GetClosestWalkableNode(_hunterInRange.transform.position);

        return hunterNode != null && hunterNode.Zone == ZoneType.Safe;
    }

    public bool SetDestination(Vector3 destination)
    {
        _currentPath = pathfinder.FindPath(transform.position, destination, true);
        _currentPathIndex = 0;

        return _currentPath != null && _currentPath.Count > 0;
    }

    public void ClearPath()
    {
        _currentPath.Clear();
        _currentPathIndex = 0;
        StopMoving();
    }

    public void AbandonHunterTrail()
    {
        _visibleHunter = null;
        ForgetLastKnownHunterPosition();
        ClearPath();
    }

    #endregion

    #region [Private Methods]
    private void ConfigureTrigger()
    {
        if (_trigger == null)
            return;

        _trigger.isTrigger = true;
        _trigger.radius = targetDetectionRadius;
    }

    private void SkipReachedNodes()
    {
        while (_currentPathIndex < _currentPath.Count)
        {
            Vector3 difference = _currentPath[_currentPathIndex].Position - transform.position;

            difference.y = 0f;

            if (difference.sqrMagnitude >
                nodeReachDistance * nodeReachDistance)
            {
                break;
            }

            _currentPathIndex++;
        }
    }

    private void PerformHunterDetection()
    {
        bool wasVisible = _visibleHunter != null;
        _visibleHunter = null;

        if (_hunterInRange == null)
            return;

        if (IsHunterInSafeZone())
        {
            ForgetLastKnownHunterPosition();
            return;
        }

        bool canSee = FieldOfViewUtility.CanSee(transform, _hunterInRange.transform, targetDetectionRadius, viewAngle, eyeHeight, obstacleMask);
        if (!canSee)
        {
            if (wasVisible && HasLastKnownHunterPosition && WolfManager.Instance != null)
            {
                WolfManager.Instance.RaiseHunterAlertDetection(LastKnownHunterPosition, GetInstanceID());
            }
            return;
        }
            

        _visibleHunter = _hunterInRange;
        _lastKnownHunterPosition = _hunterInRange.transform.position;
        _hasLastKnownHunterPosition = true;

        if (!wasVisible && WolfManager.Instance != null)
            WolfManager.Instance.RaiseHunterAlertDetection(_hunterInRange.transform.position, GetInstanceID());
    }

    private bool TryGetHunter(Collider other, out HunterAgent hunter)
    {
        hunter = null;
        if (!IsInTargetMask(other.gameObject.layer))
            return false;

        hunter = other.GetComponentInParent<HunterAgent>();
        return hunter != null;
    }

    private bool IsInTargetMask(int layer)
    {
        return (targetMask.value & (1 << layer)) != 0;
    }

    private void HunterDetected(Vector3 hunterPosition, int detectingWolfId)
    {
        if (detectingWolfId == GetInstanceID())
        {
            _stateUI.SetNotificationImage(true, isAlert: false);
            return;
        }

        _lastKnownHunterPosition = hunterPosition;
        _hasLastKnownHunterPosition = true;
        _stateUI.SetNotificationImage(true, isAlert: true);
    }

    private void HunterKilled()
    {
        StopMoving();
        ForgetHunter();
        ForgetLastKnownHunterPosition();

        _stateMachine.ChangeState(WolfState.Patrol);
    }

    private void HunterEnteredSafeZone()
    {
        StopMoving();
        ForgetHunter();
        ForgetLastKnownHunterPosition();
        
        _stateMachine.ChangeState(WolfState.Patrol);
    }
    #endregion

    #region [Gizmos]
    private void OnDrawGizmos()
    {
        if (!drawGizmos) 
            return;

        Gizmos.color = Color.brown;
        Gizmos.DrawWireSphere(transform.position, targetDetectionRadius);

        if (HasLastKnownHunterPosition)
        {
            Gizmos.color = Color.cornsilk;
            Gizmos.DrawWireSphere(LastKnownHunterPosition, 0.6f);

            Gizmos.DrawLine(transform.position, LastKnownHunterPosition);
        }

        if (drawCurrentPath)
            DrawCurrentPath();
    }

    private void DrawCurrentPath()
    {
        if (_currentPath == null || _currentPath.Count == 0)
        {
            return;
        }

        if (_currentPathIndex >= _currentPath.Count)
            return;

        Vector3 offset = Vector3.up * pathHeightOffset;
        Vector3 previousPosition = transform.position + offset;

        for (int i = _currentPathIndex; i < _currentPath.Count; i++)
        {
            NavigationNode node = _currentPath[i];

            if (node == null)
                continue;

            Vector3 nodePosition = node.Position + offset;

            Gizmos.color = pathColor;
            Gizmos.DrawLine(previousPosition, nodePosition);

            if (i == _currentPathIndex)
            {
                Gizmos.color = currentNodeColor;
            }
            else if (i == _currentPath.Count - 1)
            {
                Gizmos.color = destinationNodeColor;
            }
            else
            {
                Gizmos.color = pathColor;
            }

            Gizmos.DrawCube(nodePosition, Vector3.one * pathNodeSize);
            previousPosition = nodePosition;
        }
    }

    #endregion
}
