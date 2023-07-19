using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Editor
{
    public class ControllerSpliter
    {
        private static AnimatorController s_AnimatorController;
        private static AnimatorControllerLayer s_BaseLayer;
        private static AnimatorControllerLayer s_CarryLayer;
        private static AnimatorStateMachine s_StateMachine;
        private static ChildAnimatorState[] s_AllStates;
        private const string kCarryLayerName = "EyeBlink";
        private const string kControllerOutputPath = "Assets/ArtAssets/MyAnimators/CharShow/";

        [MenuItem("Jobs/SplitController %w")]
        public static void SplitController()
        {
            Init();
            List<List<AnimatorState>> groups = GroupRootStates();
            for (var index = 0; index < groups.Count; index++)
            {
                var groupId = index + 1;
                Debug.LogFormat("Processing group {0}", groupId);

                List<AnimatorState> rootStatesOfGroup = groups[index];
                HashSet<AnimatorState> allStates = new HashSet<AnimatorState>();
                foreach (var rootState in rootStatesOfGroup)
                {
                    List<AnimatorState> states = GetConnectedStatesFromRoot(rootState);
                    allStates.UnionWith(states);
                }

                Debug.LogFormat("Found {0} states in group {1}", allStates.Count, groupId);

                CreateController(s_AnimatorController.name + index, allStates);
                DeleteStates(allStates);

                Debug.LogFormat("====================== Group {0} process completed ======================", groupId);
            }
        }

        private static void Init()
        {
            s_AnimatorController = GetAnimatorController();

            Debug.LogFormat("Params: {0}",
                s_AnimatorController.parameters.Select(s => s.name).Aggregate((a, b) => a + ", " + b));

            foreach (AnimatorControllerLayer layer in s_AnimatorController.layers)
            {
                if (layer.name == "Base Layer")
                {
                    s_BaseLayer = layer;
                }
                else if (layer.name == kCarryLayerName)
                {
                    s_CarryLayer = layer;
                }
            }

            Debug.LogFormat("Layers: {0}",
                s_AnimatorController.layers.Select(s => s.name).Aggregate((a, b) => a + ", " + b));

            if (s_BaseLayer == null)
            {
                Debug.LogError("Cannot find layer");
                return;
            }

            s_StateMachine = s_BaseLayer.stateMachine;
            s_AllStates = s_StateMachine.states;

            Debug.LogFormat("States of layer {0}: {1}", s_BaseLayer.name,
                s_AllStates.Select(s => s.state.name).Aggregate((a, b) => a + ", " + b));

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

        private static List<List<AnimatorState>> GroupRootStates()
        {
            List<AnimatorState> group1 = new List<AnimatorState>();
            List<AnimatorState> group2 = new List<AnimatorState>();
            foreach (ChildAnimatorState state in s_AllStates)
            {
                if (HasAnyStateTransitionConditionOfParameter(state.state, "EnterState"))
                {
                    group1.Add(state.state);
                }
                else if (state.state.name.StartsWith("Team"))
                {
                    group2.Add(state.state);
                }
            }

            List<List<AnimatorState>> results = new List<List<AnimatorState>>
            {
                group1, group2
            };

            for (int i = 0; i < results.Count; i++)
            {
                var group = results[i];
                if (group.Count == 0)
                {
                    Debug.LogErrorFormat("Found no root state for group {0}", i);
                }
                else
                {
                    Debug.LogFormat("Found root state for group {0}: {1}", i,
                        @group.Select(s => s.name).Aggregate((a, b) => a + ", " + b));
                }
            }

            Debug.Log("========== Group Complete ===========");

            return results;
        }

        private static List<AnimatorState> GetConnectedStatesFromRoot(AnimatorState state)
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

            Debug.LogFormat("Found {0} states connected to root state {1}: {2}",
                connectedStates.Count,
                state.name,
                connectedStates.Select(s => s.name).Aggregate((a, b) => a + ", " + b));

            return connectedStates;
        }

        private static bool HasAnyStateTransitionConditionOfParameter(AnimatorState state, string parameter)
        {
            bool found = false;
            foreach (var param in s_AnimatorController.parameters)
            {
                if (param.name == parameter)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogErrorFormat("This controller does not have parameter named {0}", parameter);
            }

            var anyStateTransition = FindAnyStateTransitionToRootState(state);

            if (anyStateTransition == null)
            {
                return false;
            }

            foreach (var condition in anyStateTransition.conditions)
            {
                if (condition.parameter.Equals(parameter))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateController(string controllerName,
            HashSet<AnimatorState> states)
        {
            var controllerOutputPath = kControllerOutputPath + controllerName + ".controller";
            
            Debug.LogFormat("Creating new controller at: {0}", controllerOutputPath);
            
            var newController = AnimatorController.CreateAnimatorControllerAtPath(controllerOutputPath);
            foreach (var param in s_AnimatorController.parameters)
            {
                newController.AddParameter(param.name, param.type);
            }
            
            Debug.Log("Copying base layer");

            var newBaseLayerStateMachine = newController.layers[0].stateMachine;
            CopyLayer(s_BaseLayer, newController.layers[0], false);
            CopyStatesAndTransitionsBetweenThem(s_StateMachine, newBaseLayerStateMachine, states);
            
            Debug.Log("Copying carry layer");

            newController.AddLayer(kCarryLayerName);
            AnimatorControllerLayer newCarryLayer = newController.layers.First(c => c.name == kCarryLayerName);
            CopyLayer(s_CarryLayer, newCarryLayer, true);

            AssetDatabase.SaveAssets();
        }

        private static void DeleteStates(HashSet<AnimatorState> animatorStates)
        {
            Debug.LogFormat("Deleting {0} states", animatorStates.Count);
            foreach (var state in animatorStates)
            {
                s_StateMachine.RemoveState(state);
            }
        }

        private static AnimatorStateTransition FindAnyStateTransitionToRootState(AnimatorState state)
        {
            foreach (var layer in s_AnimatorController.layers)
            {
                foreach (var transition in layer.stateMachine.anyStateTransitions)
                {
                    if (transition.destinationState.Equals(state))
                    {
                        return transition;
                    }
                }
            }

            return null;
        }

        private static void CopyStatesAndTransitionsBetweenThem(AnimatorStateMachine sourceStateMachine,
            AnimatorStateMachine targetStateMachine,
            ICollection<AnimatorState> toCopyStates)
        {
            var nameToNewState = new Dictionary<string, AnimatorState>(toCopyStates.Count);
            foreach (var state in toCopyStates)
            {
                var newState = targetStateMachine.AddState(state.name);
                CopyState(state, newState);
                if (sourceStateMachine.defaultState.Equals(state))
                {
                    targetStateMachine.defaultState = newState;
                }
                nameToNewState.Add(state.name, newState);
            }

            foreach (var transition in sourceStateMachine.anyStateTransitions)
            {
                AnimatorState state = transition.destinationState;
                if (toCopyStates.Contains(state))
                {
                    var newState = nameToNewState[state.name];
                    var newAnyStateTransition = targetStateMachine.AddAnyStateTransition(newState);
                    CopyTransition(transition, newAnyStateTransition);
                }
            }

            foreach (var state in toCopyStates)
            {
                var newState = nameToNewState[state.name];
                foreach (var transition in state.transitions)
                {
                    var newTransition =
                        newState.AddTransition(nameToNewState[transition.destinationState.name]);
                    CopyTransition(transition, newTransition);
                }
            }
        }

        private static void CopyLayer(AnimatorControllerLayer sourceLayer, AnimatorControllerLayer targetLayer,
            bool copyStatesAndTransition)
        {
            targetLayer.name = sourceLayer.name;
            targetLayer.defaultWeight = sourceLayer.defaultWeight;
            targetLayer.avatarMask = sourceLayer.avatarMask;
            targetLayer.blendingMode = sourceLayer.blendingMode;
            targetLayer.iKPass = sourceLayer.iKPass;
            targetLayer.syncedLayerIndex = sourceLayer.syncedLayerIndex;
            targetLayer.syncedLayerAffectsTiming = sourceLayer.syncedLayerAffectsTiming;

            if (copyStatesAndTransition)
            {
                var sourceStates = sourceLayer.stateMachine.states.Select(s => s.state).ToList();
                CopyStatesAndTransitionsBetweenThem(sourceLayer.stateMachine, targetLayer.stateMachine,
                    sourceStates);
            }
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

            foreach (StateMachineBehaviour behaviour in sourceState.behaviours)
            {
                targetState.AddStateMachineBehaviour(behaviour.GetType());
            }
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