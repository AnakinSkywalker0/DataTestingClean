    using System;
    using UnityEngine;
    using UnityEngine.XR;
    using VIVE.OpenXR.EyeTracker;

    namespace VIVE.OpenXR.Samples.EyeTracker
    {
        public class ObjectDetectedByEyeGazeValue : MonoBehaviour
        {

            [Header("Eye Transform")]
            public Transform leftEye = null;
            public Transform rightEye = null;

            [Header("Ray Settings")]
            public float rayDistance = 100f;
            public float closeDistance = .5f;
            public LayerMask visible; // Set this to the layer you want to detect (e.g., cars)

            [Header("Logger")]
            [SerializeField] private DataLogger logger;

            [Header("EEG Gaze Marker")]
            [SerializeField] private string markerTag = "EEGMarker";


            private Transform HeaderSet;
            string currentObject = "";
            string previousObject = "";

            float fixationStartTime = 0;

            Vector3 previousGazeDir;
            float previousFrameTime = 0;

            bool isBlinking = false;

            GameObject lastDetectedObject = null;
            float distanceToLookAheadCar = -1f;
            Vector3 lastGazeHitPoint = Vector3.zero;
            bool isGazingAtMarker = false;
            private int _gazeEpisode = 0;
            private bool _exitGracePending = false;
            private float _exitStartTime = -1f;
            private const float exitGracePeriod = 0.15f;

            float FixedTime =0f;
            float frequency= 0.1f;

            float logTimer = 0f;

            private void Start() {
                HeaderSet = Camera.main.transform; // Assuming the main camera is the head position reference
            }

            void Update()
            {
                logTimer += Time.deltaTime;

                if (logTimer >= frequency)
                {
                    
                    logTimer -= frequency;
                    CollectData();
                }
            }
            

            void CollectData()
            {
                if (!XRSettings.isDeviceActive) return;
                distanceToLookAheadCar = -1f;
                XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes);

                if (gazes == null || gazes.Length == 0) return;

                XrSingleEyeGazeDataHTC leftGaze =
                    gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];

                XrSingleEyeGazeDataHTC rightGaze =
                    gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

                // Is Blinked
                isBlinking = DidIBlink(leftGaze, rightGaze);

                // Direction
                Vector3 leftDir =
                    leftGaze.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;

                Vector3 rightDir =
                    rightGaze.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;


                Vector3 gazeDirection = (leftDir + rightDir) / 2f;
                Vector3 gazePosition = (leftEye.position + rightEye.position) / 2f;

                //Scaccade
                float saccadeSpeed = CalculateSaccadeSpeed(gazeDirection);
                
                //DetectObject
                GameObject detected = DetectObject(rightDir, leftDir, out Vector3 hitPoint);
                lastGazeHitPoint = hitPoint;

                HandleObject(detected, gazePosition, saccadeSpeed);

                // Debug ray from eye midpoint: red on marker, green otherwise
                Vector3 rayEnd = lastGazeHitPoint != Vector3.zero
                    ? lastGazeHitPoint
                    : gazePosition + gazeDirection * rayDistance;
                Debug.DrawLine(gazePosition, rayEnd, isGazingAtMarker ? Color.red : Color.green);

            }

            private bool DidIBlink(XrSingleEyeGazeDataHTC leftGaze, XrSingleEyeGazeDataHTC rightGaze)
            {
                return !leftGaze.isValid || !rightGaze.isValid;
            }

            float CalculateSaccadeSpeed(Vector3 gazeDir)
            {
                float speed = 0;

                if (previousFrameTime != 0)
                {
                    float deltaAngle = Vector3.Angle(previousGazeDir, gazeDir);
                    float deltaTime = Time.time - previousFrameTime;

                    if (deltaTime > 0)
                        speed = deltaAngle / deltaTime;
                }

                previousGazeDir = gazeDir;
                previousFrameTime = Time.time;

                return speed;
            }

            GameObject DetectObject(Vector3 rightEyeGazeRotation, Vector3 leftEyeGazeRotation, out Vector3 hitPoint)
            {
                RaycastHit leftHit;
                RaycastHit rightHit;

                bool leftDetect = Physics.Raycast(leftEye.position, leftEyeGazeRotation, out leftHit, rayDistance,visible);
                bool rightDetect = Physics.Raycast(rightEye.position, rightEyeGazeRotation, out rightHit, rayDistance,visible);

                if (leftDetect && rightDetect)
                {
                    if (leftHit.collider.gameObject == rightHit.collider.gameObject)
                    {
                        distanceToLookAheadCar = Vector3.Distance(leftHit.point, leftEye.position);
                        hitPoint = leftHit.point;
                        return leftHit.collider.gameObject;
                    }
                    else
                    {
                        if (leftHit.distance < closeDistance){
                            distanceToLookAheadCar = Vector3.Distance(leftHit.point, leftEye.position);
                            hitPoint = leftHit.point;
                            return leftHit.collider.gameObject;
                        }else if (rightHit.distance < closeDistance){
                            distanceToLookAheadCar = Vector3.Distance(rightHit.point, rightEye.position);
                            hitPoint = rightHit.point;
                            return rightHit.collider.gameObject;
                        }
                    }
                }
                else if (leftDetect)
                {
                    distanceToLookAheadCar = Vector3.Distance(leftHit.point, leftEye.position);
                    hitPoint = leftHit.point;
                    return leftHit.collider.gameObject;
                }
                else if (rightDetect)
                {
                    distanceToLookAheadCar = Vector3.Distance(rightHit.point, rightEye.position);
                    hitPoint = rightHit.point;
                    return rightHit.collider.gameObject;
                }

                hitPoint = Vector3.zero;
                return null;
            }

            public void ResetGazeEpisode()
            {
                _gazeEpisode = 0;
                _exitGracePending = false;
                _exitStartTime = -1f;
            }

            void HandleObject(GameObject obj, Vector3 gazePosition, float saccadeSpeed)
            {
                string currentAOI = obj != null ? obj.name : "None";
                bool nowOnMarker = obj != null && obj.CompareTag(markerTag);

                // Grace period check — runs every tick
                if (_exitGracePending)
                {
                    if (nowOnMarker)
                    {
                        // Gaze returned within grace period — cancel exit, no end_gaze fired
                        _exitGracePending = false;
                    }
                    else if (Time.time - _exitStartTime >= exitGracePeriod)
                    {
                        // Grace expired — confirm gaze end
                        GazeMarkerEvents.Invoke("end_gaze_" + _gazeEpisode, 0);
                        isGazingAtMarker = false;
                        _exitGracePending = false;
                    }
                }

                if (currentAOI != currentObject)
                {
                    Debug.Log($"[Gaze] Looking at: {currentAOI}");

                    // Gaze left confirmed marker — start grace period instead of firing immediately
                    if (isGazingAtMarker && !nowOnMarker && !_exitGracePending)
                    {
                        _exitGracePending = true;
                        _exitStartTime = Time.time;
                    }

                    // Gaze arrived on marker — fire start immediately
                    if (!isGazingAtMarker && nowOnMarker)
                    {
                        _exitGracePending = false;
                        _gazeEpisode++;
                        GazeMarkerEvents.Invoke("start_gaze_" + _gazeEpisode, 1);
                        isGazingAtMarker = true;
                    }

                    previousObject = currentObject;
                    currentObject = currentAOI;
                    lastDetectedObject = obj;
                    fixationStartTime = Time.time;
                }
                LogFrame(gazePosition, saccadeSpeed);
            }

            void LogFrame(Vector3 gazePos, float saccadeSpeed)
            {
                float exitTime = Time.time;
                float fixationDuration = exitTime - fixationStartTime;

                float timeMs = Time.time ;

                XR_HTC_eye_tracker.Interop.GetEyePupilData(out XrSingleEyePupilDataHTC[] pupils);
                if (pupils == null || pupils.Length == 0) return;

                XrSingleEyePupilDataHTC leftPupil =
                    pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];

                XrSingleEyePupilDataHTC rightPupil =
                    pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

                float pupilLeft = leftPupil.isDiameterValid ? leftPupil.pupilDiameter : -1f;
                float pupilRight = rightPupil.isDiameterValid ? rightPupil.pupilDiameter : -1f;

                float pupilCombined = (pupilLeft + pupilRight) / 2f;

                float headYaw = HeaderSet.eulerAngles.y;
                float headPitch = HeaderSet.eulerAngles.x;
                FixedTime += frequency;

                string line =
                    FixedTime.ToString("F2")+ "," +
                    StoreData.GetID()+","+
                    gazePos.x + "," + gazePos.y + "," + gazePos.z + "," +
                    currentObject + "," +
                    fixationDuration + "," +
                    previousObject + "," +
                    currentObject + "," +
                    pupilLeft + "," + pupilRight + "," +
                    pupilCombined + "," +
                    saccadeSpeed + "," +
                    headYaw + "," +
                    headPitch + "," +
                    isBlinking +","+
                    GetLookAheadCarData(lastDetectedObject) +"\n";
                
                logger.WriteEyeTrackingData(line);
                // currentObject = "";
            }

        private String GetLookAheadCarData(GameObject obj)
            {
                if (obj == null) return "None,None,,,,,";

                Vector3 pos = obj.transform.position;

                return string.Format("{0},{1},{2:F2},{3:F2},{4:F2},{5:f2},{6:f2}",
                    "",
                    "",
                    0f,
                    distanceToLookAheadCar,
                    pos.x,
                    pos.y,
                    pos.z
                    );
            }
        }
    }