using System;
using System.Collections.Generic;
using UnityEngine;

public enum SearchPhase
{
    GoingToHuntingGround,
    ExploringHuntingGround
}

public class HunterAgent : SteeringAgent
{
    [Header("Attack Stats")]
    [SerializeField] private float timeBetweenAttacks = 20f;
    [SerializeField] private float targetDetectionRadius = 10f;
    [SerializeField] private LayerMask preyLayer;
    [SerializeField, Min(0)] private float meleeAttackRadius = 1f;
    [SerializeField, Min(0)] private float rangeAttackRadius = 7f;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Bait Stats")]
    [SerializeField, Min(0)] private float baitCooldown = 5f;
    [SerializeField, Min(0)] private float placingBaitDelay = 2f;

    [Header("Gather Stats")]
    [SerializeField] private float gatherDetectionRadius = 10f;
    [SerializeField] private float gatherDuration = 3f;
    [SerializeField, Range(0, 2)] private float gatheringDistance = 0.9f;
    [SerializeField] private int RequiredGathers = 3;

    [Header("Navigation")]
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private NavigationGraph graph;
    [SerializeField, Min(0.1f)] private float nodeReachDistance = 0.3f;

    [Header("Check Intervals")]
    [SerializeField] private float detectionInterval = 0.10f;
    [SerializeField] private LayerMask wolfLayer;

    [Header("Respawn & Safe Zone (sec)")]
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private float safeZoneWaitTime = 10f;

    [Header("Field of view")]
    [SerializeField] protected float eyeHeight = 1f;
    [SerializeField, Range(0f, 360f)] protected float viewAngle = 360f;
    [SerializeField] protected LayerMask targetMask = ~0;
    [SerializeField] protected LayerMask obstacleMask = ~0;
    [SerializeField] private GameObject visualRoot;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawCurrentPath = true;
    [SerializeField] private Color pathColor = Color.magenta;
    [SerializeField] private Color currentNodeColor = Color.yellow;
    [SerializeField] private Color destinationNodeColor = Color.green;
    [SerializeField, Min(0.01f)] private float pathNodeSize = 0.35f;
    [SerializeField, Min(0f)] private float pathHeightOffset = 0.3f;

    private StateMachine _stateMachine;
    private HunterStateUI _stateUI;
    private PreyAgent _preyAgentAlive;
    private PreyAgent _attackTarget;
    private PreyAgent _preyAgentDead;
    private float _timeSinceLastAttack;
    private float _currentBaitCooldown;
    private PlacingBaitState _placingBaitState;
    private GatheringState _gatheringState;
    private double _nextDetection;
    private List<NavigationNode> _currentPath = new();
    private int _currentPathIndex;
    private int _currentPreyGathered;
    private readonly Collider[] _preyBuffer = new Collider[16];
    private readonly Collider[] _wolfBuffer = new Collider[16];
    private SearchPhase _phase;
    private WolfAgent _wolfAgent;
    private bool _isdead;

    public bool CanAttack => HasTargetAlive && IsReadyToAttack;
    public bool HasTargetAlive => _preyAgentAlive != null;
    public bool HasTargetDead => _preyAgentDead != null;
    public bool HasAttackTarget => _attackTarget != null && !_attackTarget.IsDead;
    public float DistanceToAttackTarget => HasAttackTarget ? Vector3.Distance(transform.position, _attackTarget.transform.position) : float.PositiveInfinity;
    public bool IsReadyToAttack => _timeSinceLastAttack >= timeBetweenAttacks;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public float GatherDuration => gatherDuration;
    public bool IsCloseToGather => _preyAgentDead != null ? Vector3.Distance(transform.position, _preyAgentDead.transform.position) <= gatheringDistance : false;
    public float PlacingBaitDelay => placingBaitDelay;
    public bool CanPlaceBait => IsOnHunterGround && _currentBaitCooldown >= baitCooldown;
    public bool IsOnHunterGround => _phase == SearchPhase.ExploringHuntingGround;
    public bool RequiredGatheredCompleted => _currentPreyGathered >= RequiredGathers;
    public float RespawnTime => respawnTime;
    public float SafeZoneWaitTime => safeZoneWaitTime;
    public bool ThreatDetected => _wolfAgent != null;

    #region [Unity Events]

    protected override void Awake()
    {
        base.Awake();

        _stateMachine = new StateMachine();
        _stateUI = GetComponent<HunterStateUI>();
        _stateMachine.OnStateChanged += _stateUI.SetState;
        _stateUI.SetChannelingSlide(0f);

        SearchState searchState = new(this, graph, _stateMachine);
        _placingBaitState = new(this, _stateMachine);
        AttackState attackState = new(this, _stateMachine);
        GoingToGatherState goingToGatherState = new(this, _stateMachine);
        _gatheringState = new(this, _stateMachine);
        FleeState fleeState = new(this, _stateMachine);
        HunterDeadState deadState = new(this, graph, _stateMachine);
        ExitState exitState = new(this, graph, _stateMachine);

        _stateMachine.RegisterState(HunterState.Search, searchState);
        _stateMachine.RegisterState(HunterState.PlacingBait, _placingBaitState);
        _stateMachine.RegisterState(HunterState.Attacking, attackState);
        _stateMachine.RegisterState(HunterState.GoingToGather, goingToGatherState);
        _stateMachine.RegisterState(HunterState.Gathering, _gatheringState);
        _stateMachine.RegisterState(HunterState.Flee, fleeState);
        _stateMachine.RegisterState(HunterState.Dead, deadState);
        _stateMachine.RegisterState(HunterState.Exit, exitState);

        _placingBaitState.ChannelProgressChanged += _stateUI.SetChannelingSlide;
        _gatheringState.ChannelProgressChanged += _stateUI.SetChannelingSlide;

        _timeSinceLastAttack = 0f;
        _currentBaitCooldown = 0f;
    }

    private void Start()
    {
        SetSearchPhase(SearchPhase.GoingToHuntingGround);
        _stateMachine.ChangeState(HunterState.Search);

        WolfManager.Instance.HunterKilled += HandleKilled;
    }
        

    void Update()
    {
        if (!_isdead)
        {
            double now = Time.timeAsDouble;

            if (now >= _nextDetection)
            {
                _nextDetection = now + detectionInterval;
                PerformDetection();
            }

            UpdateAttackTime();
            UpdateBaitCooldown();
        }
        _stateMachine.Update();
    }

    private void OnDestroy()
    {
        if(_stateUI != null)
        {
            _stateMachine.OnStateChanged -= _stateUI.SetState;
            _placingBaitState.ChannelProgressChanged -= _stateUI.SetChannelingSlide;
            _gatheringState.ChannelProgressChanged -= _stateUI.SetChannelingSlide;
        }
    }

    private void OnDisable()
    {
        if (WolfManager.Instance != null)
            WolfManager.Instance.HunterKilled -= HandleKilled;
    }

    #endregion

    #region [Public Methods]

    public bool SetDestination(Vector3 destination)
    {
        _currentPath = pathfinder.FindPath(transform.position, destination, false);
        _currentPathIndex = 0;

        return _currentPath != null && _currentPath.Count > 0;
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

    public void MeleeAttack()
    {
        if (!HasAttackTarget)
            return;

        _attackTarget.TakeDamage(_attackTarget.CurrentHealth);
    }

    public Vector3 GetAttackPursuitSteering()
    {
        return HasAttackTarget ? Pursuit(_attackTarget) : Vector3.zero;
    }

    public void RangeAttack()
    {
        if (!HasAttackTarget)
            return;

        Vector3 targetPosition = CalculateProjectileTargetPosition(_attackTarget, projectilePrefab.Speed);
        Projectile projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);

        projectile.Initialize(targetPosition);
    }

    public void ResetAttackCooldown()
    {
        _timeSinceLastAttack = 0;
    }

    public void ResetBaitCooldown()
    {
        _currentBaitCooldown = 0;
    }

    public Vector3 GetPursuitSteering()
    {
        return _preyAgentAlive != null ? Pursuit(_preyAgentAlive) : Vector3.zero;
    }

    public Vector3 GetArriveSteering()
    {
        return _preyAgentDead != null ? Arrive(_preyAgentDead.transform.position) : Vector3.zero;
    }

    public void GatherPreyAgentDead()
    {
        if (_preyAgentDead == null)
            return;

        PreyManager.Instance.DespawnAndRespawn(_preyAgentDead);
        _preyAgentDead = null;
        _currentPreyGathered++;
        _stateUI.SetGatherCount(_currentPreyGathered);
    }

    public void ClearPath()
    {
        _currentPath.Clear();
        _currentPathIndex = 0;
        StopMoving();
    }

    public void SetSearchPhase(SearchPhase searchPhase)
    {
        _phase = searchPhase;
    }

    public void ResetSimulationCycle(Vector3 spawnPosition)
    {
        _isdead = false;
        ClearPath();
        ClearAttackTarget();

        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;

        _preyAgentAlive = null;
        _preyAgentDead = null;

        _currentPreyGathered = 0;
        _timeSinceLastAttack = 0f;
        _currentBaitCooldown = 0f;

        _nextDetection = Time.timeAsDouble + detectionInterval;
        _wolfAgent = null;

        SetSearchPhase(SearchPhase.GoingToHuntingGround);
        _stateUI.SetGatherCount(_currentPreyGathered);
    }

    public void SetDeadPresentation(bool dead)
    {
        visualRoot.SetActive(!dead);

        foreach (Collider currentCollider in GetComponentsInChildren<Collider>())
            currentCollider.enabled = !dead;
    }

    public void ArrivedToSafe()
    {
        if (WolfManager.Instance != null)
            WolfManager.Instance.RaiseHunterEnteredSafeZone();
    }

    private bool SetPath(List<NavigationNode> path)
    {
        ClearPath();
        if (path == null || path.Count == 0)
            return false;

        _currentPath = path;
        _currentPathIndex = 0;

        return true;
    }

    public bool SetDestinationToClosestZone(ZoneType zone)
    {
        List<NavigationNode> path = pathfinder.FindPathToClosestZone(transform.position, zone, false);

        return SetPath(path);
    }

    public bool TryLockAttackTarget()
    {
        if (_preyAgentAlive == null || _preyAgentAlive.IsDead)
            return false;

        _attackTarget = _preyAgentAlive;
        return true;
    }

    public void ClearAttackTarget()
    {
        _attackTarget = null;
    }

    public bool CanSeeAttackTarget()
    {
        if (!HasAttackTarget)
            return false;

        return FieldOfViewUtility.CanSee(transform, _attackTarget.transform, targetDetectionRadius, viewAngle, eyeHeight, obstacleMask);
    }

    #endregion

    #region [Private Methods]

    private void PerformDetection()
    {
        ThreatDetection();

        if (IsOnHunterGround)
            HunterGroundDetection();
    }

    private void ThreatDetection()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, targetDetectionRadius, _wolfBuffer, wolfLayer, QueryTriggerInteraction.Ignore);

        WolfAgent closestWolf = null;
        float closestWolfSqr = float.PositiveInfinity;
        float wolfRadiusSqr = targetDetectionRadius * targetDetectionRadius;

        for (int i = 0; i < count; i++)
        {
            var wolf = _wolfBuffer[i].GetComponentInParent<WolfAgent>();
            if (wolf == null)
                continue;

            bool canSee = FieldOfViewUtility.CanSee(transform, wolf.transform, targetDetectionRadius, viewAngle, eyeHeight, obstacleMask);
            if (!canSee)
                continue;

            float sqrDistance = (wolf.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= wolfRadiusSqr && sqrDistance < closestWolfSqr)
            {
                closestWolf = wolf;
                closestWolfSqr = sqrDistance;
            }
        }

        _wolfAgent = closestWolf;
    }

    private void HunterGroundDetection()
    {
        float maxDetectionRadius = Mathf.Max(targetDetectionRadius, gatherDetectionRadius);
        int count = Physics.OverlapSphereNonAlloc(transform.position, maxDetectionRadius, _preyBuffer, preyLayer, QueryTriggerInteraction.Ignore);

        PreyAgent closestAlive = null;
        PreyAgent closestDead = null;

        float closestAliveSqr = float.PositiveInfinity;
        float closestDeadSqr = float.PositiveInfinity;

        float attackRadiusSqr = targetDetectionRadius * targetDetectionRadius;
        float gatherRadiusSqr = gatherDetectionRadius * gatherDetectionRadius;

        for (int i = 0; i < count; i++)
        {
            if (!_preyBuffer[i].TryGetComponent(out PreyAgent prey))
                continue;

            float perceptionRadius = prey.IsDead ? gatherDetectionRadius : targetDetectionRadius;
            bool canSee = FieldOfViewUtility.CanSee(transform, prey.transform, perceptionRadius, viewAngle, eyeHeight, obstacleMask);
            if (!canSee)
                continue;

            float sqrDistance = (prey.transform.position - transform.position).sqrMagnitude;

            if (prey.IsDead)
            {
                if (sqrDistance <= gatherRadiusSqr && sqrDistance < closestDeadSqr)
                {
                    closestDead = prey;
                    closestDeadSqr = sqrDistance;
                }
            }
            else
            {
                if (sqrDistance <= attackRadiusSqr && sqrDistance < closestAliveSqr)
                {
                    closestAlive = prey;
                    closestAliveSqr = sqrDistance;
                }
            }
        }

        _preyAgentAlive = closestAlive;
        _preyAgentDead = closestDead;
    }

    private void UpdateAttackTime()
    {
        if(_timeSinceLastAttack <= timeBetweenAttacks)
        {
            _timeSinceLastAttack += Time.deltaTime;
            _stateUI.SetAttackCooldown(Mathf.Clamp01(_timeSinceLastAttack / timeBetweenAttacks));
        }
    }

    private void UpdateBaitCooldown()
    {
        if (_currentBaitCooldown <= baitCooldown)
        {
            _currentBaitCooldown += Time.deltaTime;
            _stateUI.SetBaitCooldown(Mathf.Clamp01(_currentBaitCooldown / baitCooldown));
        }       
    }

    private Vector3 CalculateProjectileTargetPosition(Agent target, float projectileSpeed)
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);

        float predictionTime = distance / projectileSpeed;

        return target.transform.position + target.Velocity * predictionTime;
    }

    private void SkipReachedNodes()
    {
        while (_currentPathIndex < _currentPath.Count)
        {
            Vector3 difference = _currentPath[_currentPathIndex].Position - transform.position;
            difference.y = 0f;

            if (difference.sqrMagnitude > nodeReachDistance * nodeReachDistance)
            {
                break;
            }

            _currentPathIndex++;
        }
    }

    private void HandleKilled()
    {
        ClearPath();
        StopMoving();
        _stateMachine.ChangeState(HunterState.Dead);
        _isdead = true;
    }

    #endregion

    #region [Gizmos]
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetDetectionRadius);

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
