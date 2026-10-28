using System;
using System.Collections.Generic;
using UnityEngine;

public class PreyAgent : SteeringAgent
{
    [Header("Life Stats")]
    [SerializeField] private float _maxHealth = 5f;

    [Header("Flocking Settings")]
    [Tooltip("Layer mask to filter which objects are considered part of the flock.")]
    [SerializeField] private LayerMask flockLayer;
    [Tooltip("Radius within which the agent will detect other flock agents.")]
    [SerializeField] private float flockDetectionRadius = 10;

    [Header("Target Settings")]
    [Tooltip("Layer mask to filter which objects are considered hunters.")]
    [SerializeField] private LayerMask hunterLayer;
    [Tooltip("Radius within which the agent will detect hunter agents.")]
    [SerializeField] private float hunterDetectionRadius = 15f;
    [Tooltip("Layer mask to filter which objects are considered bait.")]
    [SerializeField] private LayerMask baitLayer;
    [Tooltip("Radius within which the agent will detect bait.")]
    [SerializeField] private float baitDetectionRadius = 15f;

    [Header("Eat Settings")]
    [SerializeField, Range(0, 2)] private float eatingDistance = 0.7f;
    [SerializeField, Min(0)] private float eatingTime = 2f;

    [Header("Check Intervals")]
    [SerializeField] private float flockCheckInterval = 0.10f;
    [SerializeField] private float hunterCheckInterval = 0.075f;
    [SerializeField] private float baitCheckInterval = 0.30f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = false;

    private readonly List<Agent> _flockAgents = new();
    private Agent _hunterAgent;
    private Bait _targetBait;
    private PreyStateUI _stateUI;
    private StateMachine _stateMachine;
    private bool _gathered;
    private EatingState _eatingState;
    private float _nextFlockCheck;
    private float _nextHunterCheck;
    private float _nextBaitCheck;

    private readonly Collider[] _flockBuffer = new Collider[16];
    private readonly Collider[] _hunterBuffer = new Collider[4];
    private readonly Collider[] _baitBuffer = new Collider[8];

    public float CurrentHealth { get; private set; }
    public PreyState CurrentState { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;
    public bool IsBaitAssigned => _targetBait != null;
    public bool IsHunterDetected => _hunterAgent != null;
    public bool IsCloseToBait => _targetBait != null ? Vector3.Distance(transform.position, _targetBait.transform.position) <= eatingDistance : false;
    public float EatingTime => eatingTime;
    public bool IsGathered => _gathered;
    public bool CanTakeBait => CurrentState == PreyState.Flocking;

    #region [Unity Events]

    protected override void Awake()
    {
        base.Awake();

        _nextFlockCheck = Time.time + UnityEngine.Random.Range(0f, flockCheckInterval);
        _nextHunterCheck = Time.time + UnityEngine.Random.Range(0f, hunterCheckInterval);
        _nextBaitCheck = Time.time + UnityEngine.Random.Range(0f, baitCheckInterval);

        _stateMachine = new StateMachine();
        _stateUI = GetComponent<PreyStateUI>();
        _stateMachine.OnStateChanged += _stateUI.SetState;
        _stateMachine.OnStateChanged += SetStateChange;
        _stateUI.SetChannelingSlide(0f);

        FlockingState flockingState = new(_stateMachine, this);
        EvadingState evadingState = new(_stateMachine, this);
        GoingToBaitState goingToBaitState = new(_stateMachine, this);
        _eatingState = new(_stateMachine, this);
        DeadState deadState = new(_stateMachine, this);

        _eatingState.ChannelProgressChanged += _stateUI.SetChannelingSlide;

        _stateMachine.RegisterState(PreyState.Flocking, flockingState);
        _stateMachine.RegisterState(PreyState.Evading, evadingState);
        _stateMachine.RegisterState(PreyState.GoingToBait, goingToBaitState);
        _stateMachine.RegisterState(PreyState.Eating, _eatingState);
        _stateMachine.RegisterState(PreyState.Dead, deadState);

        _stateMachine.ChangeState(PreyState.Flocking);

        CurrentHealth = _maxHealth;
    }

    private void Update()
    {
        float now = Time.time;

        if (now >= _nextHunterCheck)
        {
            DetectHunter();
            _nextHunterCheck = now + hunterCheckInterval;
        }

        if (now >= _nextFlockCheck)
        {
            DetectFlock();
            _nextFlockCheck = now + flockCheckInterval;
        }

        if (now >= _nextBaitCheck)
        {
            DetectBait();
            _nextBaitCheck = now + baitCheckInterval;
        }

        _stateMachine.Update();
    }

    private void OnDestroy()
    {
        if (_stateUI != null)
        {
            _stateMachine.OnStateChanged -= _stateUI.SetState;
            _eatingState.ChannelProgressChanged -= _stateUI.SetChannelingSlide;
        }

        _stateMachine.OnStateChanged -= SetStateChange;
    }

    #endregion

    #region [Public Methods]

    public Vector3 GetFlockingSteering()
    {
        return Flocking(_flockAgents);
    }

    public Vector3 GetArriveSteering()
    {
        return Arrive(_targetBait.transform.position);
    }

    public Vector3 GetEvadeSteering()
    {
        return Evade(_hunterAgent);
    }

    public void ConsumeTargetBait()
    {
        if (_targetBait == null)
            return;

        _targetBait.Consume();
        _targetBait = null;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, _maxHealth);
    }

    public void ReleaseBait()
    {
        if (_targetBait == null)
            return;

        _targetBait.ReleaseAgent(this);
        _targetBait = null;
    }

    #endregion

    #region [Private Methods]

    private void SetStateChange(Enum state)
    {
        CurrentState = (PreyState)state;
    }

    private void DetectFlock()
    {
        _flockAgents.Clear();
        int count = Physics.OverlapSphereNonAlloc(transform.position, flockDetectionRadius, _flockBuffer, flockLayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (!_flockBuffer[i].TryGetComponent(out PreyAgent prey))
                continue;

            if (prey == this || prey.CurrentState != PreyState.Flocking)
                continue;

            _flockAgents.Add(prey);
        }
    }

    private void DetectHunter()
    {
        _hunterAgent = null;

        int count = Physics.OverlapSphereNonAlloc(transform.position, hunterDetectionRadius, _hunterBuffer, hunterLayer, QueryTriggerInteraction.Ignore);

        float closestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            if (!_hunterBuffer[i].TryGetComponent(out HunterAgent hunter))
                continue;

            float sqrDistance = (hunter.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                _hunterAgent = hunter;
            }
        }
    }

    private void DetectBait()
    {
        if (IsBaitAssigned || !CanTakeBait)
            return;

        int count = Physics.OverlapSphereNonAlloc(transform.position, baitDetectionRadius, _baitBuffer, baitLayer, QueryTriggerInteraction.Ignore);

        Bait closestBait = null;
        float closestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            if (!_baitBuffer[i].TryGetComponent(out Bait bait))
                continue;

            float sqrDistance = (bait.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestBait = bait;
            }
        }

        if (closestBait != null && closestBait.TryAssignAgent(this))
            _targetBait = closestBait;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.lightGreen;
        Gizmos.DrawWireSphere(transform.position, flockDetectionRadius);

        Gizmos.color = Color.lightPink;
        Gizmos.DrawWireSphere(transform.position, hunterDetectionRadius);

        Gizmos.color = Color.lightBlue;
        Gizmos.DrawWireSphere(transform.position, baitDetectionRadius);
    }

    #endregion
}
