using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BlendTreeInput : MonoBehaviour
{
    private Animator animator;

    [Header("Settings")]
    public float stopDelay = 1.0f; // Time to wait before returning to 0
    public float smoothStopSpeed = 5f; // How fast it Lerps to 0 after the delay
    public float deadZone = 0.1f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

        void Update()
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            // LEFT FOOT
            if (x > 0.5f && !state.IsName("rightfoot"))
            {
                animator.SetTrigger("leftFoot");
            }

            // RIGHT FOOT
            if (y > 0.5f && !state.IsName("leftfoot"))
            {
                animator.SetTrigger("rightFoot");
            }
        }
}