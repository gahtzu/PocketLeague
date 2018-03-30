using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneMachine : StateMachine
{
    protected override void Initialize()
    {
        State countdown = CreateState(SceneStateId.Countdown, LegalTransitions(new List<Enum>() { SceneStateId.Battle }));
        State battle = CreateState(SceneStateId.Battle, LegalTransitions(new List<Enum>() { SceneStateId.Death }));
        State death = CreateState(SceneStateId.Death, LegalTransitions(new List<Enum>() { SceneStateId.Countdown, SceneStateId.Results }));
        State results = CreateState(SceneStateId.Results);
        StateGroup sceneGroup = CreateGroup(_GroupId.SceneStates, new List<State> { countdown, battle, death, results }, countdown);
        stateGroups = new List<StateGroup>() { sceneGroup };
    }

    public enum _GroupId
    {
        SceneStates = 0
    }
}


public enum SceneStateId
{
  Countdown,
    Battle,
    Death,
    Results
}

