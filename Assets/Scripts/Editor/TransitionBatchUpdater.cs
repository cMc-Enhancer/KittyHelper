using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Editor
{
    public class TransitionBatchUpdater
    {
        
        private const float kExitTime = 1.1f; 
        private const float kDuration = 1.2f; 
        private const float kOffset = 1.3f; 
        
        [MenuItem("Jobs/UpdateTransition %k")]
        public static void UpdateTransitions()
        {
            AnimatorController[] controllers = Selection.GetFiltered<AnimatorController>(SelectionMode.Editable);
            Debug.LogFormat("Updating transitions in controllers: {0}", 
                controllers.Select(s => s.name).Aggregate((a, b) => a + ", " + b));
            
            foreach (AnimatorController controller in controllers)
            {
                AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                foreach (AnimatorStateTransition transition in stateMachine.defaultState.transitions)
                {
                    UpdateTransition(transition);
                }
                
                foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
                {
                    UpdateTransition(transition);
                }
                
                foreach (ChildAnimatorState state in stateMachine.states)
                {
                    foreach (AnimatorStateTransition transition in state.state.transitions)
                    {
                        UpdateTransition(transition);
                    }
                }
                
            }
            
            AssetDatabase.SaveAssets();
        }

        private static void UpdateTransition(AnimatorStateTransition transition)
        {
            transition.exitTime = kExitTime;
            transition.duration = kDuration;
            transition.offset = kOffset;
        }
    }
}