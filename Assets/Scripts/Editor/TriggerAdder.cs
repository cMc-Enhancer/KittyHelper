using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class TriggerAdder : MonoBehaviour
{
    private static readonly string[] s_Triggers = {"aTriggerParameter", "bTriggerParameter"};
    private static int s_Index;

    [MenuItem("Jobs/AddParameters %y")]
    public static void AddParameters()
    {
        AnimatorController controller = GetAnimatorController();
        if (controller == null)
        {
            Debug.LogError("[TriggerAdder] Animator controller not set");
            return;
        }

        foreach (string triggerName in s_Triggers)
        {
            controller.AddParameter(triggerName, AnimatorControllerParameterType.Trigger);
        }
    }

    [MenuItem("Jobs/AddTrigger %g")]
    public static void AddTriggerToTransition()
    {
        if (s_Index >= s_Triggers.Length)
        {
            Debug.LogErrorFormat("[TriggerAdder] All triggers added, do nothing");
            return;
        }

        string triggerToAdd = s_Triggers[s_Index];
        if (!IsTriggerExists(triggerToAdd))
        {
            Debug.LogErrorFormat("[TriggerAdder] Trigger {0} does not exist", triggerToAdd);
            return;
        }

        AnimatorStateTransition[] transitions =
            Selection.GetFiltered<AnimatorStateTransition>(SelectionMode.Editable);
        if (transitions.Length == 0)
        {
            Debug.LogErrorFormat("[TriggerAdder] No transition is selected");
            return;
        }

        foreach (AnimatorStateTransition transition in transitions)
        {
            transition.AddCondition(AnimatorConditionMode.If, 0, triggerToAdd);
        }

        s_Index++;

        Debug.LogFormat("[TriggerAdder] Trigger added");
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

    private static bool IsTriggerExists(string name)
    {
        AnimatorController controller = GetAnimatorController();
        if (controller == null)
        {
            Debug.LogError("[TriggerAdder] Animator controller not set");
            return false;
        }

        HashSet<string> triggers = new HashSet<string>(controller.parameters.Length);
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                triggers.Add(parameter.name);
            }
        }

        return triggers.Contains(name);
    }

    [MenuItem("Jobs/Forget %j")]
    public static void Forget()
    {
        s_Index = 0;
        Debug.LogFormat("[TriggerAdder] Forgot");
    }
}