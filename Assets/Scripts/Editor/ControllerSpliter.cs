using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Editor
{
    public class ControllerSpliter
    {
        private static AnimatorController s_AnimatorController;
        private static AnimatorControllerLayer s_BaseLayer;
        private static AnimatorStateMachine s_StateMachine;
        private static ChildAnimatorState[] s_AllStates;
        private static string s_ControllerOutputPath = "Assets/Resources/";

        [MenuItem("Jobs/SplitController %w")]
        public static void SplitController()
        {
            Init();
            List<ChildAnimatorState> rootStates = FindRoots();
            foreach (ChildAnimatorState rootState in rootStates)
            {
                List<AnimatorState> states = GetConnectedStates(rootState.state);
                CreateController(rootState.state.name.Replace(" ", ""), rootState, states);
            }
        }

        private static void Init()
        {
            s_AnimatorController = GetAnimatorController();

            string ps = "Params:";
            foreach (AnimatorControllerParameter param in s_AnimatorController.parameters)
            {
                ps = ps + ' ' + param.name;
            }

            Debug.Log(ps);

            string layers = "Layers:";
            foreach (AnimatorControllerLayer layer in s_AnimatorController.layers)
            {
                layers = layers + ' ' + layer.name;
                if (layer.name == "Base Layer")
                {
                    s_BaseLayer = layer;
                }
            }

            Debug.Log(layers);

            if (s_BaseLayer == null)
            {
                Debug.LogError("Cannot find layer");
                return;
            }

            string statesOfLayer = "States of layer " + s_BaseLayer.name + ":";
            s_StateMachine = s_BaseLayer.stateMachine;
            s_AllStates = s_StateMachine.states;
            foreach (ChildAnimatorState state in s_AllStates)
            {
                statesOfLayer = statesOfLayer + ' ' + state.state.name;
            }

            Debug.Log(statesOfLayer);

            Debug.Log("========== Init Complete ===========");
        }

        private static AnimatorController GetAnimatorController()
        {
            EditorWindow window = EditorWindow.focusedWindow;
            Type windowType = window.GetType();

            AnimatorController controller = null;
            if (windowType.Name != "AnimatorControllerTool")
            {
                Debug.LogErrorFormat("[TriggerAdder] Animator window is not current active window");
            }

            PropertyInfo controllerProperty = windowType.GetProperty("animatorController",
                BindingFlags.NonPublic | BindingFlags.Public |
                BindingFlags.Instance);
            if (controllerProperty != null)
            {
                controller = controllerProperty.GetValue(window, null) as AnimatorController;
            }
            else
            {
                Debug.LogErrorFormat("[TriggerAdder] Cannot find animatorController");
            }

            return controller;
        }

        private static List<ChildAnimatorState> FindRoots()
        {
            List<ChildAnimatorState> results = new List<ChildAnimatorState>();
            foreach (ChildAnimatorState state in s_AllStates)
            {
                if (state.state.name.Equals("Old State 1") && HasAnyStateTransitionConditionOfParameter(state, "EnterState"))
                {
                    results.Add(state);
                }
                else if (state.state.name.Equals("New State 1"))
                {
                    results.Add(state);
                }
            }

            return results;
        }

        private static List<AnimatorState> GetConnectedStates(AnimatorState state)
        {
            var connectedStates = new List<AnimatorState>();
            var visitedStates = new HashSet<AnimatorState>();
            var statesToVisit = new Queue<AnimatorState>();

            visitedStates.Add(state);
            statesToVisit.Enqueue(state);

            while (statesToVisit.Count > 0)
            {
                var currentState = statesToVisit.Dequeue();
                connectedStates.Add(currentState);

                foreach (var transition in currentState.transitions)
                {
                    var nextState = transition.destinationState;

                    if (nextState != null && !visitedStates.Contains(nextState) && !nextState.name.Equals("Any State"))
                    {
                        visitedStates.Add(nextState);
                        statesToVisit.Enqueue(nextState);
                    }
                }
            }

            foreach (var s in connectedStates)
            {
                Debug.Log(s.name);
            }

            Debug.Log("================ Group Split ================");

            return connectedStates;
        }

        private static bool HasAnyStateTransitionConditionOfParameter(ChildAnimatorState state, string parameter)
        {
            AnimatorStateTransition toStateAnyStateTransition = null;
            foreach (var anyStateTransition in s_StateMachine.anyStateTransitions)
            {
                if (anyStateTransition.destinationState.name.Equals(state.state.name))
                {
                    toStateAnyStateTransition = anyStateTransition;
                    break;
                }
            }

            if (toStateAnyStateTransition == null)
            {
                throw new AssertionException("Cannot find any state transition to this state " + state.state.name);
            }

            foreach (var condition in toStateAnyStateTransition.conditions)
            {
                if (condition.parameter.Equals(parameter))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateController(string controllerName,
            ChildAnimatorState rootState, List<AnimatorState> states)
        {
            var newController =
                AnimatorController.CreateAnimatorControllerAtPath(s_ControllerOutputPath + controllerName + ".controller");
            foreach (var param in s_AnimatorController.parameters)
            {
                newController.AddParameter(param.name, param.type);
            }

            var newStateMachine = newController.layers[0].stateMachine;

            var nameToNewState = new Dictionary<string, AnimatorState>(states.Count);
            foreach (var state in states)
            {
                var newState = newStateMachine.AddState(state.name);
                nameToNewState.Add(state.name, newState);
            }

            foreach (var state in states)
            {
                var newState = nameToNewState[state.name];
                if (state.name.Equals(rootState.state.name))
                {
                    newStateMachine.AddAnyStateTransition(newState);
                }

                CopyState(state, newState);

                foreach (var transition in state.transitions)
                {
                    var newTransition =
                        newState.AddTransition(nameToNewState[transition.destinationState.name]);
                    CopyTransition(transition, newTransition);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static void CopyState(AnimatorState sourceState, AnimatorState targetState)
        {
            targetState.tag = sourceState.tag;
            targetState.motion = sourceState.motion;
            targetState.speed = sourceState.speed;
            targetState.speedParameterActive = sourceState.speedParameterActive;
            targetState.speedParameter = sourceState.speedParameter;
            targetState.timeParameterActive = sourceState.timeParameterActive;
            targetState.timeParameter = sourceState.timeParameter;
            targetState.mirror = sourceState.mirror;
            targetState.mirrorParameterActive = sourceState.mirrorParameterActive;
            targetState.mirrorParameter = sourceState.mirrorParameter;
            targetState.cycleOffset = sourceState.cycleOffset;
            targetState.cycleOffsetParameterActive = sourceState.cycleOffsetParameterActive;
            targetState.cycleOffsetParameter = sourceState.cycleOffsetParameter;
            targetState.iKOnFeet = sourceState.iKOnFeet;
            targetState.writeDefaultValues = sourceState.writeDefaultValues;
        }

        private static void CopyTransition(AnimatorStateTransition sourceTransition,
            AnimatorStateTransition targetTransition)
        {
            targetTransition.name = sourceTransition.name;
            targetTransition.hasExitTime = sourceTransition.hasExitTime;
            targetTransition.exitTime = sourceTransition.exitTime;
            targetTransition.hasFixedDuration = sourceTransition.hasFixedDuration;
            targetTransition.duration = sourceTransition.duration;
            targetTransition.offset = sourceTransition.offset;
            targetTransition.interruptionSource = sourceTransition.interruptionSource;
            targetTransition.orderedInterruption = sourceTransition.orderedInterruption;

            foreach (var condition in sourceTransition.conditions)
            {
                targetTransition.AddCondition(condition.mode, condition.threshold, condition.parameter);
            }
        }
    }
}