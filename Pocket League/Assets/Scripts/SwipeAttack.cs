using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeAttack : Attack
{
    [SerializeField]
    public Vector3 hitBoxScale;
    [SerializeField]
    public float startingYRotationOffset;
    [SerializeField]
    public Vector3 startingOffset;
    [SerializeField]
    public int framesToSwipe;
    [SerializeField]
    public int startupFrames;
    [SerializeField]
    public int hitboxLingerFrames;
    [SerializeField]
    public int attackLagFrames;
    [SerializeField]
    public int percentDealt;
}
