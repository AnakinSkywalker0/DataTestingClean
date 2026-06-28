
using UnityEngine;
using VIVE.OpenXR.EyeTracker;
using TMPro;

namespace VIVE.OpenXR.Samples.EyeTracker
{
    public class EyeGazeObjectTimer : MonoBehaviour
    {
        public GameObject objectA;
        public GameObject objectB;

        public TMP_Text infoText;

        float objectATime = 0f;
        float objectBTime = 0f;

        void Update()
        {
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes);

            if (gazes == null || gazes.Length == 0) return;

            XrSingleEyeGazeDataHTC leftGaze = gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            XrSingleEyeGazeDataHTC rightGaze = gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

            if (!leftGaze.isValid || !rightGaze.isValid)
                return;

            Vector3 leftOrigin = leftGaze.gazePose.position.ToUnityVector();
            Vector3 leftDir = leftGaze.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;

            Vector3 rightOrigin = rightGaze.gazePose.position.ToUnityVector();
            Vector3 rightDir = rightGaze.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;

            Ray leftRay = new Ray(leftOrigin, leftDir);
            Ray rightRay = new Ray(rightOrigin, rightDir);

            RaycastHit leftHit;
            RaycastHit rightHit;

            bool leftHitSomething = Physics.Raycast(leftRay, out leftHit, 20f);
            bool rightHitSomething = Physics.Raycast(rightRay, out rightHit, 20f);

            if (leftHitSomething && rightHitSomething)
            {
                if (leftHit.collider.gameObject == objectA && rightHit.collider.gameObject == objectA)
                {
                    objectATime += Time.deltaTime;
                }

                if (leftHit.collider.gameObject == objectB && rightHit.collider.gameObject == objectB)
                {
                    objectBTime += Time.deltaTime;
                }
            }

            infoText.text =
                "Object A Look Time: " + objectATime.ToString("F2") + " sec\n" +
                "Object B Look Time: " + objectBTime.ToString("F2") + " sec";
        }
    }
}