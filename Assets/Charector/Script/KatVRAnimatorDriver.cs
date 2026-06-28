using UnityEngine;

    [RequireComponent(typeof(Animator))]
    public class KatVRAnimatorDriver : MonoBehaviour
    {
        private Animator animator;

        private bool lastLeftGround = false;
        private bool lastRightGround = false;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        void Update()
        {

            var data = KATNativeSDK.GetWalkStatus();

            var info = WalkC2ExtraData.GetExtraInfoC2(data);

            // LEFT FOOT: trigger when it touches ground again
            if (!lastLeftGround && info.isLeftGround)
            {
                animator.SetTrigger("leftFoot");
            }

            // RIGHT FOOT: trigger when it touches ground again
            if (!lastRightGround && info.isRightGround)
            {
                animator.SetTrigger("rightFoot");
            }

            // Store last frame state
            lastLeftGround = info.isLeftGround;
            lastRightGround = info.isRightGround;
        }
    }