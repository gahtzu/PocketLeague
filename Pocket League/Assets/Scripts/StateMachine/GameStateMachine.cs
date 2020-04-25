using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateMachine : StateMachine
{
    protected override void Initialize()
    {
        State countdown = CreateState(GameStateId.Countdown, LegalTransitions(new List<Enum>() { GameStateId.Battle }));
        State battle = CreateState(GameStateId.Battle, LegalTransitions(new List<Enum>() { GameStateId.Death }));
        State death = CreateState(GameStateId.Death, LegalTransitions(new List<Enum>() { GameStateId.Countdown, GameStateId.Results }));
        State results = CreateState(GameStateId.Results, LegalTransitions(new List<Enum>() { }));
        StateGroup sceneGroup = CreateGroup(GroupId.SceneStates, new List<State> { countdown, battle, death, results }, countdown);
        stateGroups = new List<StateGroup>() { sceneGroup };
    }

    public enum GroupId
    {
        SceneStates = 0
    }
}


public enum GameStateId
{
    Countdown,
    Battle,
    Death,
    Results
}

