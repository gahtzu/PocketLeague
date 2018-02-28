using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PocketPlayerMachine : StateMachine
{

    public enum GroupId
    {
        Main
    }

    protected override void Initialize()
    {
        State idle = CreateState(StateId.Idle, LegalTransitions(new List<Enum>() { StateId.Run, StateId.Projectile, StateId.Hitstun, StateId.Charge }));
        State run = CreateState(StateId.Run, LegalTransitions(new List<Enum>() { StateId.Charge, StateId.Projectile, StateId.Hitstun, StateId.Idle }));
        State charge = CreateState(StateId.Charge, LegalTransitions(new List<Enum>() { StateId.AttackRecovery, StateId.Projectile }));
        State attackRecovery = CreateState(StateId.AttackRecovery, LegalTransitions(new List<Enum>() { StateId.Idle, StateId.Hitstun }));
        State projectile = CreateState(StateId.Projectile, LegalTransitions(new List<Enum>() { StateId.Idle, StateId.Hitstun, StateId.Charge }));
        State hitstun = CreateState(StateId.Hitstun, LegalTransitions(new List<Enum>() { StateId.Idle, StateId.Tech, StateId.Hitstun }));
        State tech = CreateState(StateId.Tech, LegalTransitions(new List<Enum>() { StateId.Idle, StateId.Tech }));

        StateGroup mainGroup = CreateGroup(GroupId.Main, new List<State> { idle, run, charge, attackRecovery, projectile, hitstun, tech }, idle);

        stateGroups = new List<StateGroup>() { mainGroup };
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
    Tech
}

