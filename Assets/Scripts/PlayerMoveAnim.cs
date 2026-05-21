using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveAnim : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    Animator animator;
    float horizontalInput;

    void Start()
    {
        animator = GetComponentInChildren<Animator>(true);
    }

    void Update()
    {
        
        
    }
}