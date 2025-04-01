using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
//TODO basically go down through this and if its being controlled stop the AI stuff
public class SettlerNPC : HumanCharacterController
{
    public SettlerNPCScriptableObj nPCDataObj;
    private _TaskState currentState;

    // Dictionary that maps TaskType to TaskState
    Dictionary<TaskType, _TaskState> taskStates = new Dictionary<TaskType, _TaskState>();

    protected override void Awake()
    {
        base.Awake();

        // Get all TaskState components attached to the SettlerNPC GameObject
        _TaskState[] states = GetComponents<_TaskState>();

        // Populate the dictionary with TaskType -> TaskState mappings
        foreach (var state in states)
        {
            taskStates.Add(state.GetTaskType(), state);
            state.SetNPCReference(this);
        }
    }

    private void Start()
    {
        // Ensure NPC reference is set for each state component
        // Default to WanderState
        if (taskStates.ContainsKey(TaskType.WANDER))
        {
            ChangeState(taskStates[TaskType.WANDER]);
        }
    }

    private void Update()
    {
        //base.Update();        

        if (currentState != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude / 3.5f); //TODO have to work out this ratio a bit better
            currentState.UpdateState(); // Call UpdateState on the current state
        }
    }

    // Method to change states
    public void ChangeState(_TaskState newState)
    {
        if (currentState != null)
        {
            currentState.OnExitState(); // Exit the old state
        }

        currentState = newState;

        if (newState != null)
        {
            currentState.OnEnterState(); // Enter the new state

            // Adjust the agent's speed according to the new state's requirements
            agent.speed = currentState.MaxSpeed();
        }
    }

    internal void AssignWork(WorkTask newTask)
    {
        (taskStates[TaskType.WORK] as WorkState).AssignWork(newTask);
    }

    // Method to change task and update state
    public void ChangeTask(TaskType newTask)
    {
        if (taskStates.ContainsKey(newTask))
        {
            ChangeState(taskStates[newTask]);
        }
        else
        {
            Debug.LogWarning($"TaskType {newTask} does not exist in taskStates dictionary.");
        }
    }

    public NavMeshAgent GetAgent()
    {
        return agent; // Return the stored NavMeshAgent reference
    }

    public Animator GetAnimator()
    {
        return animator;
    }
}
