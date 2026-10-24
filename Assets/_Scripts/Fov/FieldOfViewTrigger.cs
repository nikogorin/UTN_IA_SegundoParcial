using UnityEngine;

public class FieldOfViewTrigger : FieldOfViewSensor
{
    private bool _targetInRange;

    private void Awake()
    {
        SphereCollider sensor = gameObject.AddComponent<SphereCollider>();
        sensor.isTrigger = true;
        sensor.radius = viewRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidTarget(other.gameObject.layer, targetMask))
            return;

        _target = other.transform.root;
        _targetInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsValidTarget(other.gameObject.layer, targetMask))
            return;

        _target = null;
        _targetInRange = false;
    }

    private void Update()
    {
        CanSeeTarget = _targetInRange && _target != null && InViewAngle() && HasLineOfSight();
        _currentCheckTime += Time.deltaTime;
        if (_currentCheckTime >= _checkTime)
        {
            _currentCheckTime = 0;
            Debug.Log($"CanSeeTarget: {CanSeeTarget}");
        }
    }
}
