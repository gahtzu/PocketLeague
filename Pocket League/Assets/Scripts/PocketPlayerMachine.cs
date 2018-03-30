using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PocketPlayerMachine : StateMachine
{
    protected override void Initialize()
    {
        State idle = CreateState(StateId.Idle, LegalTransitions(new List<Enum>() { StateId.Run, StateId.Projectile, StateId.Hitstun, StateId.Charge, StateId.Dead }));
        State run = CreateState(StateId.Run, LegalTransitions(new List<Enum>() { StateId.Charge, StateId.Projectile, StateId.Hitstun, StateId.Idle, StateId.Dead }));
        State charge = CreateState(StateId.Charge, LegalTransitions(new List<Enum>() { StateId.AttackRecovery, StateId.Projectile, StateId.Hitstun, StateId.Dead }));
        State attackRecovery = CreateState(StateId.AttackRecovery, LegalTransitions(new List<Enum>() { StateId.Hitstun, StateId.Dead }));
        State projectile = CreateState(StateId.Projectile, LegalTransitions(new List<Enum>() { StateId.Idle, StateId.Hitstun, StateId.Charge, StateId.Dead }));
        State hitstun = CreateState(StateId.Hitstun, LegalTransitions(new List<Enum>() { StateId.Tech, StateId.Hitstun, StateId.Dead }));
        State tech = CreateState(StateId.Tech, LegalTransitions(new List<Enum>() { StateId.Idle, StateId.Tech, StateId.Dead }));
        State dead = CreateState(StateId.Dead);
        StateGroup characterGroup = CreateGroup(GroupId.CharacterStates, new List<State> { idle, run, charge, attackRecovery, projectile, hitstun, tech, dead }, idle);

        stateGroups = new List<StateGroup>() { characterGroup };
    }

    public enum GroupId
    {
        CharacterStates = 0
    }
}


public enum StateId
{
    Idle,
    Run,
    Charge,
    AttackRecovery,
    Projectile,
    Hitstun,
    Tech,
    Dead
}

