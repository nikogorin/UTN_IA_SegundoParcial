using System.Collections.Generic;
using UnityEngine;

public class SteeringAgent : Agent
{
    public enum SteeringMode
    {
        Seek, Flee, Arrive, Pursuit, Evade, Flocking
    }

    [Header("Movement Stats")]
    [Tooltip("Maximum speed the agent can move.")]
    [SerializeField] private float speed = 5f;
    [Tooltip("Maximum acceleration the agent can apply to change its velocity.")]
    [SerializeField] private float maxAcceleration = 2f;

    [Header("Steering Settings")]
    [Tooltip("Distance at which the agent will consider itself to have arrived at the target.")]
    [SerializeField] private float arrivalThreshold = 0.2f;
    [Tooltip("Distance at which the agent will start to slow down when arriving at the target.")]
    [SerializeField] private float slowingDistance = 2f;
    [Tooltip("Distance at which the agent will maintain separation from other agents.")]
    [SerializeField] private float separationDistance = 2f;
    [Tooltip("Distance at which the agent will align its velocity with other agents.")]
    [SerializeField] private float alignmentDistance = 3f;
    [Tooltip("Distance at which the agent will move towards the center of mass of nearby agents.")]
    [SerializeField] private float cohesionDistance = 3f;
    [Tooltip("Weight applied to the separation steering behavior.")]
    [SerializeField, Range(0f, 3f)] private float separationWeight = 3f;
    [Tooltip("Weight applied to the alignment steering behavior.")]
    [SerializeField, Range(0f, 3f)] private float alignmentWeight = 2f;
    [Tooltip("Weight applied to the cohesion steering behavior.")]
    [SerializeField, Range(0f, 3f)] private float cohesionWeight = 1f;

    #region [Unity Events]

    protected virtual void Awake()
    {
        StartMoving();
    }

    #endregion

    #region [Public Methods]

    public void Move(bool constrainToBound = false)
    {
        transform.position += _velocity * Time.deltaTime;
        if (_velocity.sqrMagnitude > 0.001f)
            transform.forward = _velocity.normalized;

        if (constrainToBound)
            transform.position = Bounds.Instance.OutOfBounds(transform.position);
    }

    public void ApplySteering(Vector3 steering)
    {
        _velocity += steering;
        _velocity = Vector3.ClampMagnitude(_velocity, speed);
    }

    public void StartMoving()
    {
        if (_velocity.sqrMagnitude < 0.001f)
        {
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            _velocity = randomDirection * speed;
        }
    }

    public void StopMoving()
    {
        _velocity = Vector3.zero;
    }

    public void MoveTowardsPoint(Vector3 targetPosition)
    {
        Vector3 currentPosition = transform.position;

        targetPosition.y = currentPosition.y;

        Vector3 nextPosition = Vector3.MoveTowards(currentPosition,targetPosition,speed * Time.deltaTime);
        Vector3 movement = nextPosition - currentPosition;

        if (Time.deltaTime > 0f)
            _velocity = movement / Time.deltaTime;

        transform.position = nextPosition;

        if (movement.sqrMagnitude > 0.001f)
            transform.forward = movement.normalized;
    }

    #endregion

    #region [Private Methods]

    private Vector3 CalculateSeparation(IEnumerable<Agent> agents, float distance)
    {
        Vector3 desired = default;
        int count = 0;

        foreach (var agent in agents)
        {
            if (Vector3.Distance(agent.transform.position, transform.position) <= distance)
            {
                desired += transform.position - agent.transform.position;
                count++;
            }
        }
        if (count == 0)
            return Vector3.zero;

        desired /= count;

        return CalculateSteering(desired.normalized * speed);
    }

    private Vector3 CalculateAlignment(IEnumerable<Agent> agents, float distance)
    {
        Vector3 desired = default;
        int count = 0;
        foreach (var agent in agents)
        {
            if (Vector3.Distance(agent.transform.position, transform.position) <= distance)
            {
                desired += agent.Velocity;
                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        desired /= count;

        return CalculateSteering(desired.normalized * speed);
    }

    private Vector3 CalculateCohesion(IEnumerable<Agent> agents, float distance)
    {
        Vector3 desired = default;
        int count = 0;
        foreach (var agent in agents)
        {
            if (Vector3.Distance(agent.transform.position, transform.position) <= distance)
            {
                desired += agent.transform.position;
                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        desired /= count;

        return Seek(desired);
    }

    private Vector3 CalculateSteering(Vector3 desiredVelocity)
    {
        // Steering = Desired Velocity - Current Velocity
        Vector3 steering = desiredVelocity - _velocity;

        steering = Vector3.ClampMagnitude(steering, maxAcceleration * Time.deltaTime);

        return steering;
    }

    private Vector3 DesiredVelocity(Vector3 targetVector)
    {
        Vector3 direction = (targetVector - transform.position).normalized;

        Vector3 desiredVelocity = direction * speed;

        return desiredVelocity;
    }

    protected Vector3 Seek(Vector3 targetVector)
    {
        Vector3 desiredVelocity = DesiredVelocity(targetVector);

        return CalculateSteering(desiredVelocity);
    }

    protected Vector3 Flee(Vector3 targetVector)
    {
        Vector3 desiredVelocity = DesiredVelocity(targetVector);

        return CalculateSteering(-desiredVelocity);
    }

    protected Vector3 Arrive(Vector3 targetVector)
    {
        Vector3 direction = (targetVector - transform.position).normalized;
        float distance = (targetVector - transform.position).magnitude;

        if (distance < arrivalThreshold)
        {
            return CalculateSteering(Vector3.zero);
        }

        float targetSpeed = speed * (distance / slowingDistance);

        float desiredSpeed = Mathf.Min(targetSpeed, speed);

        Vector3 desiredVelocity = direction * desiredSpeed;
        Vector3 steering = CalculateSteering(desiredVelocity);

        return steering;
    }

    protected Vector3 Pursuit(Agent target)
    {
        Vector3 futurePosition = CalculateFuturePosition(target);

        return Seek(futurePosition);
    }

    protected Vector3 Evade(Agent target)
    {
        Vector3 futurePosition = CalculateFuturePosition(target);

        return Flee(futurePosition);
    }

    protected Vector3 Flocking(IEnumerable<Agent> agents)
    {
        return CalculateSeparation(agents, separationDistance) * separationWeight
            + CalculateAlignment(agents, alignmentDistance) * alignmentWeight
            + CalculateCohesion(agents, cohesionDistance) * cohesionWeight;
    }

    private Vector3 CalculateFuturePosition(Agent target)
    {
        float distance = (target.transform.position - transform.position).magnitude;

        var timePrediction = distance / (speed + target.Velocity.magnitude);

        Vector3 futurePosition = target.transform.position + (target.Velocity * timePrediction);

        return futurePosition;
    }

    #endregion
}
