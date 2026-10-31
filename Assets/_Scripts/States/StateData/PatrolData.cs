using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PatrolData
{
    public List<Transform> Waypoints;
    public float WaypointCheckDistance;
}
