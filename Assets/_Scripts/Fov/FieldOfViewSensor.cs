using UnityEngine;

public abstract class FieldOfViewSensor : MonoBehaviour
{
    [Header("Field of view")]
    [SerializeField] protected float viewRadius = 8f;
    [SerializeField] protected float eyeHeight = 1f;
    [SerializeField, Range(0f, 360f)] protected float viewAngle = 90f;

    [Header("Filtros por layer")]
    [SerializeField] protected LayerMask targetMask = ~0;
    [SerializeField] protected LayerMask obstacleMask = ~0;


    protected float _checkTime = 1f;
    protected Transform _target;
    protected float _currentCheckTime = 0f;

    public bool CanSeeTarget { get; protected set; }
    public Transform Target => _target;
    public float ViewRadius => viewRadius;
    public float ViewAngle => viewAngle;
    public float EyeHeight => eyeHeight;

    protected static bool IsValidTarget(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    protected bool InViewAngle()
    {
        Vector3 dirToTarget = (_target.position - transform.position).normalized;
        return Vector3.Angle(transform.forward, dirToTarget) <= viewAngle * 0.5f;
    }

    protected bool HasLineOfSight()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = _target.position + Vector3.up * eyeHeight;
        Vector3 direction = targetPos - eyePos;

        if (Physics.Raycast(eyePos, direction.normalized, out RaycastHit hit, direction.magnitude, obstacleMask))
            return hit.transform == _target || hit.transform.IsChildOf(_target);

        return true;
    }
}
