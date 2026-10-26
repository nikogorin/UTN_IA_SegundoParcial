using UnityEngine;

public enum ZoneType
{
    Regular,
    Initial,
    HuntingGround,
    Safe,
    Exit
}

public class FloorZone : MonoBehaviour
{
    [SerializeField] private ZoneType zoneType = ZoneType.Regular;

    public ZoneType ZoneType => zoneType;
}
