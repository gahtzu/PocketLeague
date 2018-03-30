/*Jason Diehl
This script is for categorizing states into groups of states that
will each override the other when called (if it is a legal transition)

States not in the same group do not override each other and 
thus may be active at the same time

State groups will be placed in the state hierarchy of the state machine
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class StateGroup
{
    //constructor used for setting the states that should be considered a group and the starting state (optional parameter)
    public StateGroup(Enum groupId, List<State> groupedStates, State startingState = null)
    {
        id = groupId;
        states = groupedStates;
        currentState = startingState;
    }

    //used to trigger a state change within this state group
    public void ChangeState(State state, bool on = true, bool forceTransition = false)
    {
        //if user is turning a state off then return right after
        if (!on)
        {
            state.Set(currentState.id, false);
            if (currentState == state)
            {
                currentState = null;
            }
            return;
        }

        //check to see if the current state can legally transition to the incoming state, if not then return
        if (currentState != null && !forceTransition)
        {
            if (!currentState.CanTransition(state.id))
            {
                Debug.LogWarning("Tried to call an illegal transition!");
                Debug.LogWarning("Current state ' " + currentState.name + " ' cannot transition into state ' " + state.name + " '");
                return;
            }
        }

        //check is state 'isTransitional', if not then override the currentState of the group
        if (!state.isTransitional || forceTransition)
        {
            State previousCurrentState = currentState;
            currentState = state;
            if (previousCurrentState != null)
            {
                //set previous state to 'false'
                previousCurrentState.Set(state.id, false);
            }
        }

        //set incoming state to 'true'
        state.Set(state.id, true);

    }

    #region Variables

    //houses the states within this state group
    public List<State> states;

    //houses the current state of this state group
    public State currentState;

    //houses the stateMachine used to control this state group
    public StateMachine stateMachine
    {
        get
        {
            return _stateMachine;
        }

        set
        {
            if (_stateMachine == null)
            {
                _stateMachine = value;
            }
        }
    }

    //used to guard the setting of the stateMachine
    private StateMachine _stateMachine;

    public Enum id
    {
        get;
        private set;
    }

    #endregion

}
