using System;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class StateMachine
{
    #region Variables

    // enum type we want the developer to define inside 'howToDefineTransitions'
    public enum Initialization
    {
        Define_LegalTransitions,
        Define_IllegalTransitions
    }

    // current state
    protected State currentState;

    // houses list of ALL states for this state machine, mostly used for fast lookups
    protected Dictionary<Enum, State> states = new Dictionary<Enum, State>();

    #endregion

    protected abstract void Initialize();
    public StateMachine() { Initialize(); }

    public void ChangeState(Enum stateId, bool on = true)
    {
        if (states.ContainsKey(stateId))
        {
            State state = states[stateId];

            // immediately set the current state if one doesn't exist
            if(currentState == null)
            {
                currentState = state;
                state.Set(currentState.id, true);
                return;
            }

            // check to see if the current state can legally transition to the incoming state
            if (currentState.CanTransition(state.id))
            {
                State previousCurrentState = currentState;
                if (previousCurrentState != null)
                {
                    // set previous state to 'false'
                    previousCurrentState.Set(state.id, false);
                }

                // set current state
                currentState = state;

                // set incoming state to 'true'
                state.Set(previousCurrentState.id, true);
            }
        }
    }

    public Enum GetCurrentState()
    {
        Enum state = null;
        if(currentState != null)
        {
            state = currentState.id;
        }

        return state;
    }

    public bool IsStateActive(Enum stateId)
    {
        bool active = false;
        if (states.ContainsKey(stateId))
        {
            State state = states[stateId];
            active = state.active;
        }

        return active;
    }

    public void Subscribe(Action<Enum> callback, Enum stateId, bool onEntry = true)
    {
        if (states.ContainsKey(stateId))
        {
            State state = states[stateId];
            state.Subscribe(callback, onEntry);
        }
    }

    #region PrivateFunctions

    protected void CreateState(Enum stateId, State.Transitions transitions = null)
    {
        if(!states.ContainsKey(stateId))
        {
            State state = new State(stateId, transitions);
            states.Add(stateId, state);
        }
    }

    protected State.Transitions LegalTransitions(params Enum[] stateTransitions)
    {
        return new State.Transitions(stateTransitions, true);
    }

    protected State.Transitions IllegalTransitions(params Enum[] stateTransitions)
    {
        return new State.Transitions(stateTransitions, false);
    }

    #endregion
}
