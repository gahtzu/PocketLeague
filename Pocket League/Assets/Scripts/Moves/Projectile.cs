using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    public GameObject projectileFab;
    [SerializeField]
    public float projectileSpeed;

    [SerializeField]
    public float percentDealt;

    [SerializeField]
    public int rechargeFrames;
    [SerializeField]
    public int projectileLifeFrames;

    [SerializeField]
    public float stunSpeed;
    [SerializeField]
    public int stunFrames;
    [SerializeField]
    public int lagFrames;
    [SerializeField]
    public int startupFrames;
    [SerializeField]
    public Vector3 spawnOffset;


}
