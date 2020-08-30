using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateMachine : StateMachine
{
    protected override void Initialize()
    {
        CreateState(GameStateId.Countdown, LegalTransitions(GameStateId.Battle));
        CreateState(GameStateId.Battle, LegalTransitions(GameStateId.Death));
        CreateState(GameStateId.Death, LegalTransitions(GameStateId.Countdown, GameStateId.Results));
        CreateState(GameStateId.Results, IllegalTransitions());
    }

}


public enum GameStateId
{
    Countdown,
    Battle,
    Death,
    Results
}

