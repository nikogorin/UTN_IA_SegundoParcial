using System.Collections.Generic;
using UnityEngine;

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

    [Header("Navigation")]
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private NavigationGraph graph;
    [SerializeField, Min(0.1f)] private float nodeReachDistance = 0.3f;

    [Header("Bait Stats")]
    [SerializeField, Min(0)] private float baitCooldown = 5f;
    [SerializeField, Min(0)] private float placingBaitDelay = 2f;

    [Header("Gather Stats")]
    [SerializeField] private float gatherDetectionRadius = 10f;
    [SerializeField] private float gatherDuration = 3f;
    [SerializeField, Range(0, 2)] private float gatheringDistance = 0.9f;

    [Header("Check Intervals")]
    [SerializeField] private float preyDetectionInterval = 0.10f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    private StateMachine _stateMachine;
    private HunterStateUI _stateUI;
    private PreyAgent _preyAgentAlive;
    private PreyAgent _preyAgentDead;
    private float _timeSinceLastAttack;
    private float _currentBaitCooldown;
    private PlacingBaitState _placingBaitState;
    private GatherState _gatherState;
    private double _nextPreyDetection;
    private List<NavigationNode> _currentPath = new();
    private int _currentPathIndex;

    private readonly Collider[] _preyBuffer = new Collider[16];

    public bool CanAttack => HasTargetAlive && IsReadyToAttack;
    public bool HasTargetAlive => _preyAgentAlive != null;
    public bool HasTargetDead => _preyAgentDead != null;
    public bool IsReadyToAttack => _timeSinceLastAttack >= timeBetweenAttacks;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public float DistanceToTargetAlive => HasTargetAlive ? Vector3.Distance(this.transform.position, _preyAgentAlive.transform.position) : float.PositiveInfinity;
    public float GatherDuration => gatherDuration;
    public bool IsCloseToGather => _preyAgentDead != null ? Vector3.Distance(transform.position, _preyAgentDead.transform.position) <= gatheringDistance : false;
    public float PlacingBaitDelay => placingBaitDelay;
    public bool CanPlaceBait => _currentBaitCooldown >= baitCooldown;

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
        _gatherState = new(this, _stateMachine);

        _placingBaitState.ChannelProgressChanged += _stateUI.SetChannelingSlide;
        _gatherState.ChannelProgressChanged += _stateUI.SetChannelingSlide;

        _stateMachine.RegisterState(HunterState.Search, searchState);
        _stateMachine.RegisterState(HunterState.PlacingBait, _placingBaitState);
        _stateMachine.RegisterState(HunterState.Attacking, attackState);
        _stateMachine.RegisterState(HunterState.GoingToGather, goingToGatherState);
        _stateMachine.RegisterState(HunterState.Gathering, _gatherState);

        _timeSinceLastAttack = 0f;
        _currentBaitCooldown = 0f;
    }

    private void Start()
    {
        _stateMachine.ChangeState(HunterState.Search);
    }
        

    void Update()
    {
        double now = Time.timeAsDouble;

        if (now >= _nextPreyDetection)
        {
            _nextPreyDetection = now + preyDetectionInterval;
            DetectPrey();
        }

        UpdateAttackTime();
        UpdateBaitCooldown();

        _stateMachine.Update();
    }

    private void OnDestroy()
    {
        if(_stateUI != null)
        {
            _stateMachine.OnStateChanged -= _stateUI.SetState;
            _placingBaitState.ChannelProgressChanged -= _stateUI.SetChannelingSlide;
            _gatherState.ChannelProgressChanged -= _stateUI.SetChannelingSlide;
        }
    }

    #endregion

    #region [Public Methods]

    public bool SetDestination(Vector3 destination)
    {
        _currentPath = pathfinder.FindPath(transform.position,destination);
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
        Vector3 steering = Seek(currentNode.Position);

        ApplySteering(steering);
        Move();

        return false;
    }

    public void MeleeAttack()
    {
        _preyAgentAlive.TakeDamage(_preyAgentAlive.CurrentHealth);
    }

    public void RangeAttack()
    {
        Vector3 targetPosition = CalculateProjectileTargetPosition(_preyAgentAlive, projectilePrefab.Speed);
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
    }

    public void ClearPath()
    {
        _currentPath.Clear();
        _currentPathIndex = 0;
        StopMoving();
    }

    #endregion

    #region [Private Methods]

    private void DetectPrey()
    {
        float detectionRadius = Mathf.Max(targetDetectionRadius, gatherDetectionRadius);

        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, _preyBuffer, preyLayer, QueryTriggerInteraction.Ignore);

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

        if (_preyAgentDead == null)
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

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetDetectionRadius);
    }

    #endregion
}
