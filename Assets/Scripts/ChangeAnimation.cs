using UnityEngine;

public class FlexibleAnimationController : MonoBehaviour
{
    [System.Serializable]
    public struct AnimationTriggerInput
    {
        public string description;
        public KeyCode key;
        public string animName;
    }

    private Animator animator;

    [Header("Настройки анимаций")]
    public AnimationTriggerInput action1;
    public AnimationTriggerInput action2;
    public AnimationTriggerInput action3;
    public AnimationTriggerInput action4;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator == null) return;

        if (Input.GetKeyDown(action1.key) && !string.IsNullOrEmpty(action1.animName))
        {
            animator.Play(action1.animName);
        }

        if (Input.GetKeyDown(action2.key) && !string.IsNullOrEmpty(action2.animName))
        {
            animator.Play(action2.animName);
        }

        if (Input.GetKeyDown(action3.key) && !string.IsNullOrEmpty(action3.animName))
        {
            animator.Play(action3.animName);
        }

        if (Input.GetKeyDown(action4.key) && !string.IsNullOrEmpty(action4.animName))
        {
            animator.Play(action4.animName);
        }
    }
}