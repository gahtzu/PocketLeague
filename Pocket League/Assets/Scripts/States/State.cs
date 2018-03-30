/*Jason Diehl
This script is for defining what a state is, it's identifiable components,
and what legal transitions exist

When a state is changed, it calls subscribed callback functions
for both the state entry and exit

A state that is a TransitionState is one that does not override 
the currentState within it's state group, this type of state has isTransitional = true

Any class may subscribe to any state's entry or exit events
 */                                                              

using System;
using System.Collections.Generic;
using UnityEngine;


public class State
{
    //parameterless constructor needed
    public State()
    {

    }

    //base constructor for setting a state's identifiable properties and it's legal transitions
    public State(Enum stateId, Transitions stateTransitions, bool transitional = false)
    {
        id = stateId;
        name = stateId.ToString();
        isTransitional = transitional;
        transitions = stateTransitions;
        hasNoTransitions = transitions == null;
    }

    //used to check if a state has a legal transition with the given stateId
    public bool CanTransition(Enum stateId)
    {
        //if no transitions object is set, then this state can freely transition to any other state within it's group
        bool canTransition = false;
        if(transitions != null)
        {
            List<Enum> stateTransitions = transitions.states;
            bool exists = stateTransitions.Exists((Enum stateTransitionId) => stateTransitionId.CompareTo(stateId) == 0);
            canTransition = transitions.areLegal ? exists : !exists;
        }
        return canTransition;
    }

    //used to set the state's status event, whether it is entering or exiting
    public void Set(Enum previousState, bool enteringState)
    {
        if (enteringState)
        {
            Entry(previousState);
        }
        else
        {
            Exit(previousState);
        }
    }

    //used to subscribe to a state's entry or exit event
    public void Subscribe(Action callback, bool onEntry, int priorityValue = 0)
    {
        if(callback != null)
        {
            Subscriber newSub = new Subscriber(callback, priorityValue);
            if (onEntry)
            {
                entrySubscribers.Add(newSub);
                entrySubscribers.Sort((sub1, sub2) => sub2.priority.CompareTo(sub1.priority));
            }
            else
            {
                exitSubscribers.Add(newSub);
                exitSubscribers.Sort((sub1, sub2) => sub2.priority.CompareTo(sub1.priority));
            }
        }
    }

    #region PrivateFunctions

    //function called when the state is entered and calls every subscribed callback inside a try catch for uninterrupted execution, but still logs the error
    private void Entry(Enum previousState)
    {
        active = true;
        foreach (Subscriber subscriber in entrySubscribers)
        {
            try
            {
                subscriber.callback();
            }

            catch (Exception e)
            {
                Debug.LogError("Function subscribed to State: ' " + name + " ' , With exception: " + e);
            }
        }
    }

    //function called when the state is exited and calls every subscribed callback inside a try catch for uninterrupted execution, but still logs the error
    private void Exit(Enum previousState)
    {
        active = false;
        foreach (Subscriber subscriber in exitSubscribers)
        {
            try
            {
                subscriber.callback();
            }

            catch (Exception e)
            {
                Debug.LogError("Function subscribed to State: ' " + name + " ' , With exception: " + e);
            }
        }
    }

    #endregion

    #region Variables
    //houses name of state
    public string name
    {
        get;
        protected set;
    }

    //houses the state id
    public Enum id
    {
        get;
        protected set;
    }

    //describes if this state is a TransitionState or not without having to check object type
    public bool isTransitional;

    //bool to determine whether a state is currently active or not
    public bool active { get; private set; }

    //list of legal OR illegal states this state may transition to
    protected Transitions transitions;

    //houses list of callback functions to call on state entry
    private List<Subscriber> entrySubscribers = new List<Subscriber>();

    //houses list of callback functions to call on state exit
    private List<Subscriber> exitSubscribers = new List<Subscriber>();

    public bool hasNoTransitions { get; private set; }

    public int groupIndex
    {
        get
        {
            return _groupIndex;
        }

        set
        {
            if(_groupIndex == -1)
            {
                _groupIndex = value;
            }
        }
    }
    public int selfIndex
    {
        get
        {
            return _selfIndex;
        }

        set
        {
            if (_selfIndex == -1)
            {
                _selfIndex = value;
            }
        }
    }

    private int _groupIndex = -1;
    private int _selfIndex = -1;

    #endregion

    #region HelperClasses
    //class used to house the list of legal OR illegal states this state may transition to
    public class Transitions
    {
        public Transitions(List<Enum> listOfStates, bool areLegalTranstions)
        {
            states = listOfStates;
            areLegal = areLegalTranstions;
        }

        //list of states, the bool 'areLegal' determines whether these states are LEGAL transitions or ILLEGAL transitions
        public List<Enum> states;
        public bool areLegal = true;
    }

    //class used to a subscriber's callback function and it's priority
    public class Subscriber
    {
        public Subscriber(Action callbackFoo, int priorityValue)
        {
            callback = callbackFoo;
            priority = priorityValue;
        }

        //callback function the subscriber will call when an entry or exit occurs
        public Action callback;

        //priority for what subscribers should run first or last, higher number = higher priority.
        public int priority ;
    }
    #endregion
}


