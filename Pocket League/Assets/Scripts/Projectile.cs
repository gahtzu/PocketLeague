using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    public GameObject projectileFab;
    [SerializeField]
    public float proejctileSpeed;
    [SerializeField]
    public int rechargeFrames;
    [SerializeField]
    public int stunFrames;
    [SerializeField]
    public int lagFrames;
}
