using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeAttack : Attack
{

    [Tooltip("With how much velocity are you knocked back when attacked with a NON-charged attack?")]
    [SerializeField]
    public float minKnockbackVelocity;
    [Tooltip("With how much velocity are you knocked back when attacked with a FULLY-charged attack?")]
    [SerializeField]
    public float maxKnockbackVelocity;
    [Tooltip("What should our speed be multiplied by while charging")]
    [SerializeField]
    public float speedMultiplierWhileCharging;
    [SerializeField]
    public bool rotationLockedDuringCharge;
    [Tooltip("How big is your hitbox when attacking (NON-CHARGED)")]
    [SerializeField]
    public Vector3 smallHitboxScale;
    [Tooltip("How far away from the player's center should their hitbox be placed when attacking? (NON-CHARGED)")]
    [SerializeField]
    public float smallHitboxOffset;
    [Tooltip("How big is your hitbox when attacking (FULLY-CHARGED)")]
    [SerializeField]
    public Vector3 bigHitboxScale;
    [Tooltip("How far away from the player's center should their hitbox be placed when attacking? (FULLY-CHARGED)")]
    [SerializeField]
    public float bigHitboxOffset;
    [Tooltip("Our quickest attack will still take this many frames to automatically \"charge\"")]
    [SerializeField]
    public int minChargeFrames;
    [Tooltip("What is the maximum number of frames that an attack can be charged for? (automatically releases attack after)")]
    [SerializeField]
    public int maxChargeFrames;
    [Tooltip("How many frames does of lag do our attacks have? (NON-CHARGED)")]
    [SerializeField]
    public float minAttackCooldownFrames;
    [Tooltip("How many frames does of lag do our attacks have? (FULLY-CHARGED)")]
    [SerializeField]
    public float maxAttackCooldownFrames;
    [Tooltip("How many frames is our hitbox active while we are in attack lag? (NON-CHARGED)")]
    [SerializeField]
    public float minAttackHitboxActivationFrames;
    [Tooltip("How many frames is our hitbox active while we are in attack lag? (FULLY-CHARGED)")]
    [SerializeField]
    public float maxAttackHitboxActivationFrames;
    [Tooltip("How much percent do we add to the opponent when attacking them? (NON-CHARGED)")]
    [SerializeField]
    public float minPercentDealt;
    [Tooltip("How much percent do we add to the opponent when attacking them? (FULLY-CHARGED)")]
    [SerializeField]
    public float maxPercentDealt;
}
