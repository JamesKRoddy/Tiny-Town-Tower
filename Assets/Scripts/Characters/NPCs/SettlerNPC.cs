using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]

public class SettlerNPC : HumanCharacterController
{
    public SettlerNPCScriptableObj nPCDataObj;
    private _TaskState currentState;
    private NavMeshAgent agent; // Reference to NavMeshAgent

    // Dictionary that maps TaskType to TaskState
    Dictionary<TaskType, _TaskState> taskStates = new Dictionary<TaskType, _TaskState>();

    private void Awake()
    {
        // Store the reference to NavMeshAgent once
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Get all TaskState components attached to the SettlerNPC GameObject
        _TaskState[] states = GetComponents<_TaskState>();

        // Populate the dictionary with TaskType -> TaskState mappings
        foreach (var state in states)
        {
            taskStates.Add(state.GetTaskType(), state);
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

        if (PlayerController.Instance._possessedNPC != gameObject)
            ToggleNPCComponents(true, gameObject);

    }

    private void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude / 3.5f); //TODO have to work out this ratio a bit better

        if (currentState != null)
        {
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
        currentState.OnEnterState(); // Enter the new state

        // Adjust the agent's speed according to the new state's requirements
        agent.speed = currentState.MaxSpeed();
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
