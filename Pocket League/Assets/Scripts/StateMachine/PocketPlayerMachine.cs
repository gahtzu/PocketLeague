using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PocketPlayerMachine : StateMachine
{
    protected override void Initialize()
    {
        CreateState(PlayerState.Idle, LegalTransitions(PlayerState.Run, PlayerState.Projectile, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Charge, PlayerState.Dead, PlayerState.Idle, PlayerState.SwipeAttack, PlayerState.Teleport ));
       CreateState(PlayerState.Run, LegalTransitions(PlayerState.Charge, PlayerState.Projectile, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Idle, PlayerState.Dead, PlayerState.SwipeAttack, PlayerState.Teleport ));
        CreateState(PlayerState.Charge, LegalTransitions(PlayerState.ChargeAttackRecovery, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead, PlayerState.Actionable ));
        CreateState(PlayerState.ChargeAttackRecovery, LegalTransitions(PlayerState.Teleport, PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead ));
       CreateState(PlayerState.Projectile, LegalTransitions(PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead ));
        CreateState(PlayerState.Hitstun, LegalTransitions(PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead ));
        CreateState(PlayerState.BulletHitstun, LegalTransitions(PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead ));

        CreateState(PlayerState.Dead, LegalTransitions(PlayerState.Actionable ));
       CreateState(PlayerState.Actionable, LegalTransitions(PlayerState.Idle ));

        CreateState(PlayerState.SwipeAttack, LegalTransitions(PlayerState.Teleport, PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead ));
        CreateState(PlayerState.Teleport, LegalTransitions(PlayerState.Actionable, PlayerState.Hitstun, PlayerState.BulletHitstun, PlayerState.Dead , PlayerState.Charge, PlayerState.SwipeAttack));
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

