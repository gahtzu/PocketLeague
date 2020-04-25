using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Tooltip("When attacked at 0%, how many frames should hitstun/knockback last?")]
    [SerializeField]
    public int minHitstunLength;
    [Tooltip("When attacked at 100%, how many frames should hitstun/knockback last?")]
    [SerializeField]
    public int maxHitstunLength;

    [Tooltip("How much EXTRA velocity is added when attacked at 0%")]
    [SerializeField]
    public float minKnockbackVelocityAdditionFromPercent;
    [Tooltip("How much EXTRA velocity is added when attacked at 100%")]
    [SerializeField]
    public float maxKnockbackVelocityAdditionFromPercent;
}
