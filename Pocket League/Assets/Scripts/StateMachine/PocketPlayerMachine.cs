using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PocketPlayerMachine : StateMachine
{
    protected override void Initialize()
    {
        State idle = CreateState(PlayerState.Idle, LegalTransitions(new List<Enum>() { PlayerState.Run, PlayerState.Projectile, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Charge, PlayerState.Dead, PlayerState.Idle, PlayerState.SwipeAttack, PlayerState.Teleport }));
        State run = CreateState(PlayerState.Run, LegalTransitions(new List<Enum>() { PlayerState.Charge, PlayerState.Projectile, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Idle, PlayerState.Dead, PlayerState.SwipeAttack, PlayerState.Teleport }));
        State charge = CreateState(PlayerState.Charge, LegalTransitions(new List<Enum>() { PlayerState.ChargeAttackRecovery, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead, PlayerState.Actionable }));
        State chargeAttackRecovery = CreateState(PlayerState.ChargeAttackRecovery, LegalTransitions(new List<Enum>() { PlayerState.Teleport, PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead }));
        State projectile = CreateState(PlayerState.Projectile, LegalTransitions(new List<Enum>() { PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead }));
        State hitstun = CreateState(PlayerState.Hitstun, LegalTransitions(new List<Enum>() { PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead }));
        State bulletHitstun = CreateState(PlayerState.BulletHitstun, LegalTransitions(new List<Enum>() { PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead }));

        State dead = CreateState(PlayerState.Dead, LegalTransitions(new List<Enum>() { PlayerState.Actionable }));
        State actionable = CreateState(PlayerState.Actionable, LegalTransitions(new List<Enum>() { PlayerState.Idle }));

        State swipeAttack = CreateState(PlayerState.SwipeAttack, LegalTransitions(new List<Enum>() { PlayerState.Teleport, PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead }));
        State teleport = CreateState(PlayerState.Teleport, LegalTransitions(new List<Enum>() { PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead , PlayerState.Charge, PlayerState.SwipeAttack}));

        StateGroup characterGroup = CreateGroup(GroupId.CharacterStates, new List<State> { idle, run, charge, chargeAttackRecovery, projectile, hitstun, dead, actionable, swipeAttack, teleport, bulletHitstun }, idle);

        stateGroups = new List<StateGroup>() { characterGroup };
    }

    public enum GroupId
    {
        CharacterStates = 0
    }
}


public enum PlayerState
{
    Idle,
    Run,
    Charge,
    ChargeAttackRecovery,
    Projectile,
    Hitstun,
    BulletHitstun,
    Dead,
    Actionable,
    SwipeAttack,
    Teleport
}

