using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionsStateMachine : StateMachine
{
    protected override void Initialize()
    {
        CreateState(Id.None, IllegalTransitions());
        CreateState(Id.Wall, IllegalTransitions());
        CreateState(Id.Hole, IllegalTransitions());
        CreateState(Id.Hitbox, IllegalTransitions());
    }

    public enum Id
    {
        None,
        Wall,
        Hole,
        Hitbox
    }
}




