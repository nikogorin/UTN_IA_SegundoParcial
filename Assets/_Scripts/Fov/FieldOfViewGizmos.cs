using System.Collections.Generic;
using UnityEngine;

public class FieldOfViewGizmos : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform observer;
    [SerializeField] private List<Transform> targets = new();

    [Header("Perception")]
    [SerializeField, Min(0f)] private float radius = 10f;
    [SerializeField, Range(0f, 360f)] private float angle = 90f;
    [SerializeField, Min(0f)] private float eyeHeight = 1f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Drawing")]
    [SerializeField, Min(3)] private int arcSegments = 32;
    [SerializeField] private Color fieldColor = Color.violetRed;
    [SerializeField] private Color visibleColor = Color.green;
    [SerializeField] private Color blockedColor = Color.red;

    private void Reset()
    {
        observer = transform;
    }

    private void OnDrawGizmosSelected()
    {
        Transform currentObserver = observer != null ? observer : transform;

        Vector3 eyePosition = currentObserver.position + Vector3.up * eyeHeight;

        DrawEyePosition(eyePosition);
        DrawViewRadius(eyePosition);
        DrawViewArc(currentObserver, eyePosition);
        DrawTargets(currentObserver, eyePosition);
    }

    private void DrawEyePosition(Vector3 eyePosition)
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(eyePosition, 0.1f);
    }

    private void DrawViewRadius(Vector3 eyePosition)
    {
        Gizmos.color = fieldColor;
        Gizmos.DrawWireSphere(eyePosition, radius);
    }

    private void DrawViewArc(Transform currentObserver, Vector3 eyePosition)
    {
        Vector3 forward = Vector3.ProjectOnPlane(currentObserver.forward, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        float startAngle = -angle * 0.5f;
        float step = angle / arcSegments;

        Vector3 previousDirection = Quaternion.AngleAxis(startAngle, Vector3.up) * forward;

        Gizmos.color = fieldColor;

        for (int i = 1; i <= arcSegments; i++)
        {
            float currentAngle = startAngle + step * i;

            Vector3 currentDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * forward;

            Gizmos.DrawLine(eyePosition + previousDirection * radius, eyePosition + currentDirection * radius);

            previousDirection = currentDirection;
        }

        if (angle < 360f)
        {
            Vector3 leftDirection = Quaternion.AngleAxis(-angle * 0.5f, Vector3.up) * forward;
            Vector3 rightDirection = Quaternion.AngleAxis(angle * 0.5f, Vector3.up) * forward;

            Gizmos.DrawLine(eyePosition, eyePosition + leftDirection * radius);
            Gizmos.DrawLine(eyePosition, eyePosition + rightDirection * radius);
        }
    }

    private void DrawTargets(Transform currentObserver, Vector3 eyePosition)
    {
        if (targets == null)
            return;

        foreach (Transform target in targets)
        {
            if (target == null)
                continue;

            bool canSee = FieldOfViewUtility.CanSee(currentObserver, target, radius, angle, eyeHeight, obstacleMask);

            Vector3 targetPosition = target.position + Vector3.up * eyeHeight;
            Gizmos.color = canSee ? visibleColor : blockedColor;
            Gizmos.DrawLine(eyePosition, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.25f);
        }
    }

    [ContextMenu("Find All Prey")]
    private void FindAllPrey()
    {
        targets.Clear();
        PreyAgent[] preyAgents = FindObjectsByType<PreyAgent>(FindObjectsSortMode.None);
        foreach (PreyAgent prey in preyAgents)
        {
            if (prey != null)
                targets.Add(prey.transform);
        }
    }
}
