// using UnityEngine;

// [RequireComponent(typeof(CharacterController))]
// public class KatXRPlayerRigController : MonoBehaviour
// {
//     [Header("XR Rig")]
//     [Tooltip("XR Origin / Rig root (usually this GameObject)")]
//     public Transform xrRigRoot;
//     [Tooltip("HMD camera (child of XR Rig)")]
//     public Transform xrCamera;

//     [Header("KAT Locomotion (live)")]
//     public bool   sdkConnected;
//     public string deviceName;
//     public Vector3 bodyEuler;          // from KAT bodyRotationRaw
//     public Vector3 leftFootSpeed;      // from ExtraData
//     public Vector3 rightFootSpeed;     // from ExtraData
//     public bool   isLeftGround, isRightGround, isLeftStatic, isRightStatic;
//     public int    motionType = -1;     // if available (C2/MiniS)

//     [Header("Movement Settings")]
//     [Tooltip("Use SDK's ws.moveSpeed instead of foot speeds")]
//     public bool useSdkMoveSpeed = false;
//     [Tooltip("Scales per-foot-speed derived speed")]
//     public float speedGain = 1.2f;
//     [Tooltip("Speed smoothing")]
//     public float smoothing = 6f;
//     [Tooltip("Lock rig yaw to KAT body yaw")]
//     public bool lockRigYawToBody = false;
//     [Tooltip("Gravity for CharacterController")]
//     public float gravity = 9.81f;

//     [Header("HUD")]
//     public bool showHud = true;
//     public int  hudFontSize = 14;

//     private CharacterController cc;
//     private float yawCorrection;     // body ↔ HMD alignment
//     private float smoothedSpeed;
//     private Vector3 velocity;

//     void Awake()
//     {
//         cc = GetComponent<CharacterController>();
//         if (!xrRigRoot) xrRigRoot = transform;
//         if (!xrCamera)
//         {
//             var cam = Camera.main;
//             if (cam) xrCamera = cam.transform;
//         }
//     }

//     void Update()
//     {
//         // ---- 1) Pull SDK status ----
//         var ws = KATNativeSDK.GetWalkStatus();
//         sdkConnected = ws.connected;
//         if (!sdkConnected) return;

//         deviceName = ws.deviceName;
//         bodyEuler  = ws.bodyRotationRaw.eulerAngles;

//         // KAT button or 'C' to calibrate yaw to HMD
//         bool katButton = false;
//         try { katButton = ws.deviceDatas != null && ws.deviceDatas.Length > 0 && ws.deviceDatas[0].btnPressed; } catch {}
//         if (katButton || Input.GetKeyDown(KeyCode.C))
//         {
//             if (xrCamera) yawCorrection = bodyEuler.y - xrCamera.eulerAngles.y;

//             // Optional: recenter rig under HMD on XZ (like your KATXRWalker)
//             if (xrRigRoot && xrCamera)
//             {
//                 var p = xrRigRoot.position;
//                 var eye = xrCamera.position;
//                 p.x = eye.x; p.z = eye.z;
//                 xrRigRoot.position = p;
//             }
//         }

//         // ---- 2) Parse per‑foot data (choose correct parser by device) ----
//         ResetFeet();
//         var nameL = (deviceName ?? "").ToLowerInvariant();

//         if (nameL.Contains("c2"))
//         {
//             var info = WalkC2ExtraData.GetExtraInfoC2(ws);
//             isLeftGround  = info.isLeftGround;  isRightGround = info.isRightGround;
//             isLeftStatic  = info.isLeftStatic;  isRightStatic = info.isRightStatic;
//             motionType    = info.motionType;
//             leftFootSpeed = info.lFootSpeed;    rightFootSpeed = info.rFootSpeed;
//         }
//         else if (nameL.Contains("minis") || nameL.Contains("mini s"))
//         {
//             var info = MiniSExtraData.GetExtraInfoMiniS(ws);
//             isLeftGround  = info.isLeftGround;  isRightGround = info.isRightGround;
//             isLeftStatic  = info.isLeftStatic;  isRightStatic = info.isRightStatic;
//             motionType    = info.motionType;
//             leftFootSpeed = info.lFootSpeed;    rightFootSpeed = info.rFootSpeed;
//         }
//         else if (nameL.Contains("loco"))
//         {
//             // Loco/LocoS give shoe pitch/roll; speeds remain zero here (you can extend if needed)
//             var info = LocoSExtraData.GetExtraInfoLoco(ws);
//             isLeftGround  = info.baseSDKinfo.isLeftGround;  isRightGround = info.baseSDKinfo.isRightGround;
//             isLeftStatic  = info.baseSDKinfo.isLeftStatic;  isRightStatic = info.baseSDKinfo.isRightStatic;
//         }
//         else if (nameL.Contains("walkc"))
//         {
//             var info = WalkCExtraData.GetExtraInfoC(ws);
//             isLeftGround  = info.baseSDKinfo.isLeftGround;  isRightGround = info.baseSDKinfo.isRightGround;
//             isLeftStatic  = info.baseSDKinfo.isLeftStatic;  isRightStatic = info.baseSDKinfo.isRightStatic;
//         }
//         else
//         {
//             // Default to C2 layout if unknown
//             var info = WalkC2ExtraData.GetExtraInfoC2(ws);
//             isLeftGround  = info.isLeftGround;  isRightGround = info.isRightGround;
//             isLeftStatic  = info.isLeftStatic;  isRightStatic = info.isRightStatic;
//             motionType    = info.motionType;
//             leftFootSpeed = info.lFootSpeed;    rightFootSpeed = info.rFootSpeed;
//         }

//         // ---- 3) Compute heading from body yaw (minus correction) ----
//         var desiredYaw = bodyEuler.y - yawCorrection;
//         if (lockRigYawToBody && xrRigRoot)
//             xrRigRoot.rotation = Quaternion.Euler(0f, desiredYaw, 0f);

//         // Move direction always in body heading (don’t depend on HMD)
//         var heading = Quaternion.Euler(0f, desiredYaw, 0f);
//         Vector3 forward = heading * Vector3.forward;

//         // ---- 4) Choose speed source ----
//         float targetSpeed;
//         if (useSdkMoveSpeed)
//         {
//             // Project SDK's ws.moveSpeed into body-forward plane
//             var bodySpeed = heading * ws.moveSpeed;
//             targetSpeed = new Vector3(bodySpeed.x, 0f, bodySpeed.z).magnitude;
//         }
//         else
//         {
//             // Derive from left/right foot horizontal speeds (works on C2/MiniS)
//             var lh = new Vector3(leftFootSpeed.x, 0f, leftFootSpeed.z);
//             var rh = new Vector3(rightFootSpeed.x, 0f, rightFootSpeed.z);
//             float baseMag = 0.5f * (lh.magnitude + rh.magnitude);
//             if (isLeftGround && !isRightGround)  baseMag = rh.magnitude * 0.9f + baseMag * 0.1f;
//             if (isRightGround && !isLeftGround)  baseMag = lh.magnitude * 0.9f + baseMag * 0.1f;
//             targetSpeed = baseMag * speedGain;
//         }

//         // ---- 5) Height-fit the capsule to the HMD & move ----
//         AdjustCapsuleToCamera();

//         smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
//         Vector3 horiz = forward * smoothedSpeed;

//         velocity.x = horiz.x;
//         velocity.z = horiz.z;
//         velocity.y += -gravity * Time.deltaTime;

//         cc.Move(velocity * Time.deltaTime);
//     }

//     void AdjustCapsuleToCamera()
//     {
//         if (!xrRigRoot || !xrCamera) return;

//         // height/center in rig space (standard XR height fit pattern)
//         var headLocal = xrRigRoot.InverseTransformPoint(xrCamera.position);
//         float headHeight = Mathf.Clamp(headLocal.y, 1.0f, 2.2f);
//         cc.height = headHeight + cc.skinWidth;
//         cc.center = new Vector3(headLocal.x, cc.height / 2f, headLocal.z);
//     }

//     void ResetFeet()
//     {
//         leftFootSpeed = rightFootSpeed = Vector3.zero;
//         isLeftGround = isRightGround = isLeftStatic = isRightStatic = false;
//         motionType = -1;
//     }

//     void OnGUI()
//     {
//         if (!showHud) return;
//         var style = new GUIStyle(GUI.skin.label) { fontSize = hudFontSize, richText = true };

//         GUILayout.BeginArea(new Rect(12, 12, 680, 320));
//         GUILayout.Label("<b>KAT XR Rig Player</b>", style);
//         GUILayout.Label($"Connected: {sdkConnected} | Device: {deviceName}", style);
//         GUILayout.Label($"Body YPR (deg): {bodyEuler.y:0.0}, {bodyEuler.x:0.0}, {bodyEuler.z:0.0}", style);
//         GUILayout.Label($"Left  v: {leftFootSpeed.x:0.00}, {leftFootSpeed.y:0.00}, {leftFootSpeed.z:0.00} | Ground:{isLeftGround} Static:{isLeftStatic}", style);
//         GUILayout.Label($"Right v: {rightFootSpeed.x:0.00}, {rightFootSpeed.y:0.00}, {rightFootSpeed.z:0.00} | Ground:{isRightGround} Static:{isRightStatic}", style);
//         GUILayout.Label($"motionType: {motionType} | Calibrate yaw: press <b>C</b> (or KAT button)", style);
//         GUILayout.EndArea();
//     }
// }



using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class KatXRPlayerRigController : MonoBehaviour
{
    [Header("XR Rig")]
    [Tooltip("XR Origin / Rig root (usually this GameObject)")]
    public Transform xrRigRoot;
    [Tooltip("HMD camera (child of XR Rig)")]
    public Transform xrCamera;

    [Header("KAT Locomotion (live)")]
    public bool   sdkConnected;
    public string deviceName;
    public Vector3 bodyEuler;          // from KAT bodyRotationRaw
    public Vector3 leftFootSpeed;      // from ExtraData
    public Vector3 rightFootSpeed;     // from ExtraData
    public bool   isLeftGround, isRightGround, isLeftStatic, isRightStatic;
    public int    motionType = -1;     // if available (C2/MiniS)
    public int    isForwardFlag = 0;   // MiniS supports this (0/1)

    [Header("Movement Settings")]
    [Tooltip("Use SDK's ws.moveSpeed instead of foot speeds")]
    public bool useSdkMoveSpeed = false;

    [Tooltip("Multiply raw KAT speed to calibrate to meters/second (if needed)")]
    public float speedGain = 1.0f;

    [Tooltip("Clamp max forward walking speed (m/s)")]
    public float maxWalkSpeed = 1.5f;      // ~5.4 km/h realistic walk
    [Tooltip("Clamp max backward walking speed (m/s)")]
    public float maxBackSpeed = 1.2f;      // slower backward
    [Tooltip("Optional higher cap if you allow jogging (m/s)")]
    public float maxRunSpeed = 3.0f;

    [Tooltip("Time to ramp up to target speed (s)")]
    public float accelTime = 0.25f;
    [Tooltip("Time to ramp down to target speed (s)")]
    public float decelTime = 0.20f;

    [Tooltip("Lock rig yaw to KAT body yaw")]
    public bool lockRigYawToBody = false;

    [Tooltip("Extra rotation degrees to apply on top of KAT body yaw")]
    public float yawOffsetDeg = 0f;

    [Tooltip("Gravity for CharacterController")]
    public float gravity = 9.81f;

    [Header("HUD")]
    public bool showHud = true;
    public int  hudFontSize = 14;

    private CharacterController cc;
    private float yawCorrection;     // body ↔ HMD alignment
    private float currentSpeed;      // signed m/s after smoothing
    private Vector3 velocity;        // world-space velocity

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!xrRigRoot) xrRigRoot = transform;
        if (!xrCamera)
        {
            var cam = Camera.main;
            if (cam) xrCamera = cam.transform;
        }
    }

    void Update()
    {
        // ---- 1) Pull SDK status ----
        var ws = KATNativeSDK.GetWalkStatus();
        sdkConnected = ws.connected;
        if (!sdkConnected) return;

        deviceName = ws.deviceName;
        bodyEuler  = ws.bodyRotationRaw.eulerAngles;

        // KAT button or 'C' to calibrate yaw to HMD
        bool katButton = false;
        try { katButton = ws.deviceDatas != null && ws.deviceDatas.Length > 0 && ws.deviceDatas[0].btnPressed; } catch {}
        if (katButton || Input.GetKeyDown(KeyCode.C))
        {
            if (xrCamera) yawCorrection = bodyEuler.y - xrCamera.eulerAngles.y;

            // recenter rig under HMD on XZ (optional)
            if (xrRigRoot && xrCamera)
            {
                var p = xrRigRoot.position;
                var eye = xrCamera.position;
                p.x = eye.x; p.z = eye.z;
                xrRigRoot.position = p;
            }
        }

        // ---- 2) Parse per‑foot data (choose correct parser by device) ----
        ResetFeet();
        var nameL = (deviceName ?? "").ToLowerInvariant();

        if (nameL.Contains("c2"))
        {
            var info = WalkC2ExtraData.GetExtraInfoC2(ws);
            isLeftGround  = info.isLeftGround;  isRightGround = info.isRightGround;
            isLeftStatic  = info.isLeftStatic;  isRightStatic = info.isRightStatic;
            motionType    = info.motionType;
            leftFootSpeed = info.lFootSpeed;    rightFootSpeed = info.rFootSpeed;
            isForwardFlag = 0; // not provided on C2
        }
        else if (nameL.Contains("minis") || nameL.Contains("mini s"))
        {
            var info = MiniSExtraData.GetExtraInfoMiniS(ws);
            isLeftGround  = info.isLeftGround;  isRightGround = info.isRightGround;
            isLeftStatic  = info.isLeftStatic;  isRightStatic = info.isRightStatic;
            motionType    = info.motionType;
            leftFootSpeed = info.lFootSpeed;    rightFootSpeed = info.rFootSpeed;
            isForwardFlag = info.isForward;     // 0/1 (MiniS supports this)
        }
        else if (nameL.Contains("loco"))
        {
            var info = LocoSExtraData.GetExtraInfoLoco(ws);
            isLeftGround  = info.baseSDKinfo.isLeftGround;  isRightGround = info.baseSDKinfo.isRightGround;
            isLeftStatic  = info.baseSDKinfo.isLeftStatic;  isRightStatic = info.baseSDKinfo.isRightStatic;
            // Loco gives pitch/roll; speeds remain zero (extend if you want gait-phase based speed)
            isForwardFlag = 0;
        }
        else if (nameL.Contains("walkc"))
        {
            var info = WalkCExtraData.GetExtraInfoC(ws);
            isLeftGround  = info.baseSDKinfo.isLeftGround;  isRightGround = info.baseSDKinfo.isRightGround;
            isLeftStatic  = info.baseSDKinfo.isLeftStatic;  isRightStatic = info.baseSDKinfo.isRightStatic;
            isForwardFlag = 0;
        }
        else
        {
            // Default to C2 layout if unknown
            var info = WalkC2ExtraData.GetExtraInfoC2(ws);
            isLeftGround  = info.isLeftGround;  isRightGround = info.isRightGround;
            isLeftStatic  = info.isLeftStatic;  isRightStatic = info.isRightStatic;
            motionType    = info.motionType;
            leftFootSpeed = info.lFootSpeed;    rightFootSpeed = info.rFootSpeed;
            isForwardFlag = 0;
        }

        // ---- 3) Compute heading from body yaw (minus correction + offset) ----
        var desiredYaw = bodyEuler.y - yawCorrection + yawOffsetDeg;
        if (lockRigYawToBody && xrRigRoot)
            xrRigRoot.rotation = Quaternion.Euler(0f, desiredYaw, 0f);

        var headingRot = Quaternion.Euler(0f, desiredYaw, 0f);
        Vector3 forward = headingRot * Vector3.forward; // world forward of the body

        // ---- 4) Compute a SIGNED target speed in m/s (negative = backward) ----
        float targetSignedSpeed = 0f;

        if (useSdkMoveSpeed)
        {
            // Rotate SDK's moveSpeed into world and project onto forward (keeps sign)
            Vector3 worldSpeed = headingRot * ws.moveSpeed;
            targetSignedSpeed = Vector3.Dot(new Vector3(worldSpeed.x, 0f, worldSpeed.z), forward.normalized) * speedGain;
        }
        else
        {
            // Combine feet (body space), rotate to world, then sign via dot with forward
            Vector3 l = new Vector3(leftFootSpeed.x, 0f, leftFootSpeed.z);
            Vector3 r = new Vector3(rightFootSpeed.x, 0f, rightFootSpeed.z);
            Vector3 avgFeetBody = 0.5f * (l + r);

            // Bias toward the swinging foot for stability
            if (isLeftGround && !isRightGround)  avgFeetBody = 0.8f * r + 0.2f * avgFeetBody;
            if (isRightGround && !isLeftGround)  avgFeetBody = 0.8f * l + 0.2f * avgFeetBody;

            Vector3 avgFeetWorld = headingRot * avgFeetBody;

            // SIGN comes from projection onto body-forward (Dot can be negative)
            targetSignedSpeed = Vector3.Dot(avgFeetWorld, forward.normalized) * speedGain;

            // If MiniS exposes isForward (0/1), use it to reinforce sign when near zero
            if (Mathf.Abs(targetSignedSpeed) < 0.05f && (nameL.Contains("minis") || nameL.Contains("mini s")))
            {
                targetSignedSpeed = (isForwardFlag == 1 ? +1f : -1f) * avgFeetWorld.magnitude * speedGain;
            }
        }

        // ---- 5) Clamp to realistic speeds (forward/backward) ----
        float cap = targetSignedSpeed >= 0f ? Mathf.Max(maxWalkSpeed, Mathf.Min(maxRunSpeed, maxWalkSpeed)) : maxBackSpeed;
        targetSignedSpeed = Mathf.Clamp(targetSignedSpeed, -maxBackSpeed, cap);

        // ---- 6) Smooth acceleration/deceleration (critical for comfort) ----
        float tau = (Mathf.Abs(targetSignedSpeed) > Mathf.Abs(currentSpeed)) ? Mathf.Max(0.001f, accelTime) : Mathf.Max(0.001f, decelTime);
        float alpha = 1f - Mathf.Exp(-Time.deltaTime / tau);
        currentSpeed = Mathf.Lerp(currentSpeed, targetSignedSpeed, alpha);  // signed m/s

        // ---- 7) Fit capsule to HMD height and move ----
        AdjustCapsuleToCamera();

        Vector3 horiz = forward * currentSpeed;   // signed world velocity (m/s)
        velocity.x = horiz.x;
        velocity.z = horiz.z;
        velocity.y += -gravity * Time.deltaTime;  // manual gravity

        cc.Move(velocity * Time.deltaTime);
    }

    void AdjustCapsuleToCamera()
    {
        if (!xrRigRoot || !xrCamera) return;

        var headLocal = xrRigRoot.InverseTransformPoint(xrCamera.position);
        float headHeight = Mathf.Clamp(headLocal.y, 1.2f, 2.2f);
        cc.height = headHeight + cc.skinWidth;
        // Keep center under the head XZ so small offsets don’t cause collider drag
        cc.center = new Vector3(headLocal.x, cc.height * 0.5f, headLocal.z);
    }

    void ResetFeet()
    {
        leftFootSpeed = rightFootSpeed = Vector3.zero;
        isLeftGround = isRightGround = isLeftStatic = isRightStatic = false;
        motionType = -1;
        isForwardFlag = 0;
    }

    void OnGUI()
    {
        if (!showHud) return;
        var style = new GUIStyle(GUI.skin.label) { fontSize = hudFontSize, richText = true };

        GUILayout.BeginArea(new Rect(12, 12, 740, 360));
        GUILayout.Label("<b>KAT XR Rig Player</b>", style);
        GUILayout.Label($"Connected: {sdkConnected} | Device: {deviceName}", style);
        GUILayout.Label($"Body YPR (deg): {bodyEuler.y:0.0}, {bodyEuler.x:0.0}, {bodyEuler.z:0.0}", style);
        GUILayout.Label($"Left  v: {leftFootSpeed.x:0.00}, {leftFootSpeed.y:0.00}, {leftFootSpeed.z:0.00} | Ground:{isLeftGround} Static:{isLeftStatic}", style);
        GUILayout.Label($"Right v: {rightFootSpeed.x:0.00}, {rightFootSpeed.y:0.00}, {rightFootSpeed.z:0.00} | Ground:{isRightGround} Static:{isRightStatic}", style);
        if (deviceName != null && deviceName.ToLower().Contains("mini")) GUILayout.Label($"isForward (MiniS): {isForwardFlag}", style);
        GUILayout.Label($"Signed speed (m/s): <b>{currentSpeed:0.00}</b>", style);
        GUILayout.Label($"Calibrate yaw: press <b>C</b> (or KAT button)", style);
        GUILayout.EndArea();
    }
}
