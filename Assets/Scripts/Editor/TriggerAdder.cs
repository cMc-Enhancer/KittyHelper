using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class TriggerAdder : MonoBehaviour
{
    private static AnimatorController s_Controller;
    private static readonly string[] s_Triggers = {"aTriggerParameter", "bTriggerParameter"};
    private static int s_Index;

    [MenuItem("Jobs/SetAnimatorController %t")]
    public static void SetAnimatorController()
    {
        AnimatorController[] controllers = Selection.GetFiltered<AnimatorController>(SelectionMode.Assets);
        if (controllers.Length == 1)
        {
            s_Controller = controllers[0];
            s_Index = 0;
            Debug.LogFormat("[TriggerAdder] Animator controller: {0}", s_Controller.name);
        }
        else
        {
            Debug.LogError("[TriggerAdder] Only 1 animator controller should be selected");
        }
    }

    [MenuItem("Jobs/AddParameters %y")]
    public static void AddParameters()
    {
        if (s_Controller == null)
        {
            Debug.LogError("[TriggerAdder] Animator controller not set");
            return;
        }
        
        foreach (string triggerName in s_Triggers)
        {
            s_Controller.AddParameter(triggerName, AnimatorControllerParameterType.Trigger);
        }
    }

    [MenuItem("Jobs/AddTrigger %g")]
    public static void AddTriggerToTransition()
    {
        if (s_Controller == null)
        {
            Debug.LogError("[TriggerAdder] Animator controller not set");
            return;
        }

        if (s_Index >= s_Triggers.Length)
        {
            Debug.LogError("[TriggerAdder] All triggers added, do nothing");
            return;
        }

        AnimatorStateTransition[] transitions = Selection.GetFiltered<AnimatorStateTransition>(SelectionMode.Editable);
        if (transitions.Length == 0)
        {
            Debug.LogError("[TriggerAdder] No transition is selected");
            return;
        }

        foreach (AnimatorStateTransition transition in transitions)
        {
            transition.AddCondition(AnimatorConditionMode.If, 0, s_Triggers[s_Index]);
        }

        s_Index++;

        Debug.LogFormat("[TriggerAdder] Trigger added");
    }

    [MenuItem("Jobs/Forget %j")]
    public static void Forget()
    {
        s_Controller = null;
        s_Index = 0;
        Debug.LogFormat("[TriggerAdder] Forgot");
    }
    
}