using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PocketPlayerMachine : StateMachine
{
    protected override void Initialize()
    {
        State idle = CreateState(PlayerState.Idle, LegalTransitions(new List<Enum>() { PlayerState.Run, PlayerState.Projectile, PlayerState.Hitstun, PlayerState.Charge, PlayerState.Dead }));
        State run = CreateState(PlayerState.Run, LegalTransitions(new List<Enum>() { PlayerState.Charge, PlayerState.Projectile, PlayerState.Hitstun, PlayerState.Idle, PlayerState.Dead }));
        State charge = CreateState(PlayerState.Charge, LegalTransitions(new List<Enum>() { PlayerState.AttackRecovery, PlayerState.Projectile, PlayerState.Hitstun, PlayerState.Dead }));
        State attackRecovery = CreateState(PlayerState.AttackRecovery, LegalTransitions(new List<Enum>() { PlayerState.Actionable, PlayerState.Hitstun, PlayerState.Dead }));
        State projectile = CreateState(PlayerState.Projectile, LegalTransitions(new List<Enum>() { PlayerState.Idle, PlayerState.Hitstun, PlayerState.Charge, PlayerState.Dead }));
        State hitstun = CreateState(PlayerState.Hitstun, LegalTransitions(new List<Enum>() { PlayerState.Actionable, PlayerState.Tech, PlayerState.Hitstun, PlayerState.Dead }));
        State tech = CreateState(PlayerState.Tech, LegalTransitions(new List<Enum>() { PlayerState.Idle, PlayerState.Tech, PlayerState.Dead }));
        State dead = CreateState(PlayerState.Dead, LegalTransitions(new List<Enum>() { PlayerState.Actionable }));
        State actionable = CreateState(PlayerState.Actionable, LegalTransitions(new List<Enum>() { PlayerState.Idle }));


        StateGroup characterGroup = CreateGroup(GroupId.CharacterStates, new List<State> { idle, run, charge, attackRecovery, projectile, hitstun, tech, dead, actionable }, idle);

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
    AttackRecovery,
    Projectile,
    Hitstun,
    Tech,
    Dead,
    Actionable
}

