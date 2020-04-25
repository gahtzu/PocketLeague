using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    [SerializeField]
    public int startupFrames;
    [SerializeField]
    public float teleportDistance;
    [SerializeField]
    public int framesToTeleport;
    [SerializeField]
    public bool invincibleWhileTeleporting;
}
