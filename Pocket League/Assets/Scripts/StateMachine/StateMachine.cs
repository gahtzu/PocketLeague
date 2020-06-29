using System;
using System.Collections.Generic;
using UnityEngine;


public abstract class StateMachine
{
    protected abstract void Initialize();
    public StateMachine() { Initialize(); }

    public void ChangeState(Enum stateId, bool on = true, bool forceChange = false)
    {
        State state = FindState(stateId);
        if (state != null)
        {
            StateGroup stateGroup = stateGroups[state.groupIndex];
            if (stateGroup != null)
                stateGroup.ChangeState(state, on, forceChange);
        }
    }

    public void Subscribe(Action callback, Enum stateId = null, bool onEntry = true)
    {
        State state = null;

        if (stateId == null)
        {
            foreach (PlayerState s in Enum.GetValues(typeof(PlayerState)))
            {
                state = FindState(s);
                state.Subscribe(callback, onEntry);
            }
        }

        else
        {
            state = FindState(stateId);
            if (state != null)
                state.Subscribe(callback, onEntry);
        }

    }

    public bool IsActive(Enum stateId)
    {
        bool active = false;
        State state = FindState(stateId);
        if (state != null)
            active = state.active;
        return active;
    }

    #region LookupFunctions

    public Enum GetCurrentStateEnum()
    {
        return (Enum)stateGroups[0].currentState.id;
    }

    public int GetCurrentStateId()
    {
        return Convert.ToInt32(stateGroups[0].currentState.id);
    }

    public int GetTotalStateCount()
    {
        return stateGroups[0].states.Count;
    }

    #endregion

    #region PrivateFunctions

    private State FindState(Enum stateId)
    {
        State state = null;
        int id = Convert.ToInt32(stateId);
        if (id < allStates.Count)
            state = allStates[id];
        return state;
    }

    private StateGroup FindStateGroup(Enum stateGroupId)
    {
        int id = Convert.ToInt32(stateGroupId);
        StateGroup group = null;
        if (id < stateGroups.Count)
            group = stateGroups[id];
        return group;
    }

    private void SortAllStates()
    {
        stateGroups.Sort((g1, g2) => g1.id.CompareTo(g2.id));

        for (int i = 0; i < stateGroups.Count; i++)
        {
            StateGroup group = stateGroups[i];
            for (int j = 0; j < group.states.Count; j++)
            {
                State state = group.states[j];
                state.groupIndex = i;
                state.selfIndex = j;

                if (!allStates.Contains(state))
                    allStates.Add(state);
            }
        }

        allStates.Sort((s1, s2) => s1.id.CompareTo(s2.id));
    }

    protected State CreateState(Enum stateId, State.Transitions transitions = null)
    {
        return new State(stateId, transitions);
    }

    protected State CreateTransitionState(Enum stateId, State.Transitions stateTransitions = null)
    {
        return new State(stateId, stateTransitions, true);
    }

    protected State.Transitions LegalTransitions(List<Enum> stateTransitions)
    {
        return new State.Transitions(stateTransitions, true);
    }

    protected State.Transitions IllegalTransitions(List<Enum> stateTransitions)
    {
        return new State.Transitions(stateTransitions, false);
    }

    protected StateGroup CreateGroup(Enum groupId, List<State> groupedStates, State startingState = null)
    {
        return new StateGroup(groupId, groupedStates, startingState);
    }

    #endregion

    #region Variables

    //enum type we want the developer to define inside 'howToDefineTransitions'
    public enum Initialization
    {
        Define_LegalTransitions,
        Define_IllegalTransitions
    }

    //houses the list of the state groups
    protected List<StateGroup> stateGroups
    {
        get
        {
            return _stateGroups;
        }

        set
        {
            if (_stateGroups != null && value != null)
            {

                _stateGroups = value;
                SortAllStates();
            }

        }
    }

    //houses list of ALL states for this state machine, mostly used for fast lookups
    protected List<State> allStates = new List<State>();

    //used to guard the setting of the stateGroups
    private List<StateGroup> _stateGroups = new List<StateGroup>();

    #endregion

}
