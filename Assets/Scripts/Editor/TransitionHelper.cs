using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class TransitionHelper : MonoBehaviour
{
    private static AnimatorState[] s_StartState = null;

    [MenuItem("Jobs/CreateTransition %g")]
    public static void CreateTransition()
    {
        AnimatorState[] states = Selection.GetFiltered<AnimatorState>(SelectionMode.Editable);
        if (states.Length == 0)
        {
            Debug.LogError("No animator states is selected");
        }
        else
        {
            if (s_StartState == null)
            {
                s_StartState = states;
                Debug.LogFormat("Start state marked");
            }
            else
            {
                if (states.Length == 1)
                {
                    AnimatorState targetState = states[0];
                    foreach (AnimatorState startState in s_StartState)
                    {
                        startState.AddTransition(targetState);
                    }
                }
                else
                {
                    if (s_StartState.Length == 1)
                    {
                        AnimatorState startState = s_StartState[0];
                        foreach (AnimatorState targetState in states)
                        {
                            startState.AddTransition(targetState);
                        }
                    }
                    else
                    {
                        Debug.LogError("Cannot create transition from multiple states to multiple states");
                    }
                }

                s_StartState = null;
            }
        }
    }

    [MenuItem("Jobs/Forget %f")]
    public static void Forget()
    {
        s_StartState = null;
    }
}