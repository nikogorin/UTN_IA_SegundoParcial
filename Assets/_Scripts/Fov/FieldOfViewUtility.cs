using UnityEngine;

public class FieldOfViewUtility
{
    public static bool CanSee(Transform observer, Transform target, float radius, float angle, float eyeHeight, LayerMask obstacleMask)
    {
        if (observer == null || target == null)
            return false;

        Vector3 observerEyePos = observer.position + Vector3.up * eyeHeight;
        Vector3 targetEyePos = target.position + Vector3.up * eyeHeight;
        Vector3 direction = targetEyePos - observerEyePos;

        float sqrDistance = direction.sqrMagnitude;
        if(sqrDistance > radius * radius)
            return false; // out of reach

        Vector3 horizontalDirection = direction;
        horizontalDirection.y = 0f;

        if (angle < 360f && horizontalDirection.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Vector3.Angle(observer.forward, horizontalDirection);
            if (targetAngle > angle * 0.5f)
                return false; //out of angle
        }

        float distance = direction.magnitude;
        bool blocked = Physics.Raycast(observerEyePos, direction.normalized, distance, obstacleMask, QueryTriggerInteraction.Ignore); //is blocked if the raycast hit a obstacleMask

        return !blocked;
    }
}