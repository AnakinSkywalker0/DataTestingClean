using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class KatPerFootBodyController : MonoBehaviour
{
    [Header("References")]
    public Transform eye; // HMD/camera (for yaw calibration)

    [Header("Live Readout (Inspector)")]
    public bool   sdkConnected;
    public string deviceName;
    public Vector3 bodyEuler;          // from ws.bodyRotationRaw
    public Vector3 leftFootSpeed;      // from ExtraData parsers
    public Vector3 rightFootSpeed;     // from ExtraData parsers
    public bool   isLeftGround;
    public bool   isRightGround;
    public bool   isLeftStatic;
    public bool   isRightStatic;
    public int    motionType = -1;     // if available (C2/MiniS)

    [Header("Locomotion")]
    [Tooltip("Scale for foot-speed-derived walking speed")]
    public float speedGain = 1.2f;
    [Tooltip("Movement smoothing")]
    public float smoothing = 6f;
    public float gravity = 9.81f;

    [Header("HUD")]
    public bool showHud = true;
    public int  hudFontSize = 14;

    private CharacterController cc;
    private float yawCorrection;   // body ↔ HMD alignment
    private float smoothedSpeed;
    private Vector3 velocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1) Pull the current treadmill data (struct) directly
        var ws = KATNativeSDK.GetWalkStatus();
        sdkConnected = ws.connected;
        if (!sdkConnected) return;

        deviceName = ws.deviceName;
        bodyEuler  = ws.bodyRotationRaw.eulerAngles;

        // 2) Calibration: if the KAT button is pressed or user hits C
        bool buttonPressed = false;
        try
        {
            // deviceDatas is a fixed-size array of 3 in SDK; guard for null
            if (ws.deviceDatas != null && ws.deviceDatas.Length > 0)
                buttonPressed = ws.deviceDatas[0].btnPressed;
        }
        catch { /* ignore if marshaling differs */ }

        if (buttonPressed || Input.GetKeyDown(KeyCode.C))
        {
            if (eye != null) yawCorrection = bodyEuler.y - eye.eulerAngles.y;
        }

        // 3) Parse per-foot extra data depending on device
        ResetFeet(); // clear previous frame values

        var nameLower = (deviceName ?? "").ToLowerInvariant();
        if (nameLower.Contains("c2"))
        {
            var info = WalkC2ExtraData.GetExtraInfoC2(ws);
            isLeftGround  = info.isLeftGround;
            isRightGround = info.isRightGround;
            isLeftStatic  = info.isLeftStatic;
            isRightStatic = info.isRightStatic;
            motionType    = info.motionType;
            leftFootSpeed = info.lFootSpeed;
            rightFootSpeed= info.rFootSpeed;
        }
        else if (nameLower.Contains("minis") || nameLower.Contains("mini s"))
        {
            var info = MiniSExtraData.GetExtraInfoMiniS(ws);
            isLeftGround  = info.isLeftGround;
            isRightGround = info.isRightGround;
            isLeftStatic  = info.isLeftStatic;
            isRightStatic = info.isRightStatic;
            motionType    = info.motionType;
            leftFootSpeed = info.lFootSpeed;
            rightFootSpeed= info.rFootSpeed;
        }
        else if (nameLower.Contains("loco"))
        {
            // Loco/LocoS does not provide foot speeds; it provides pitch/roll per foot
            // We'll expose ground/static=false and speed=zero in that case.
            var info = LocoSExtraData.GetExtraInfoLoco(ws);
            // You can read info.L_Pitch / info.R_Pitch here if you need phase detection.
            isLeftGround  = info.baseSDKinfo.isLeftGround;
            isRightGround = info.baseSDKinfo.isRightGround;
            isLeftStatic  = info.baseSDKinfo.isLeftStatic;
            isRightStatic = info.baseSDKinfo.isRightStatic;
            // left/right speeds not used for Loco; leave zero.
        }
        else if (nameLower.Contains("walkc"))
        {
            var info = WalkCExtraData.GetExtraInfoC(ws);
            isLeftGround  = info.baseSDKinfo.isLeftGround;
            isRightGround = info.baseSDKinfo.isRightGround;
            isLeftStatic  = info.baseSDKinfo.isLeftStatic;
            isRightStatic = info.baseSDKinfo.isRightStatic;
            // Legacy WalkC has pitch/roll, not per-foot speed; speeds remain zero.
        }
        else
        {
            // Unknown model: attempt C2 parser as a best guess (many C2 devices use that layout)
            var info = WalkC2ExtraData.GetExtraInfoC2(ws);
            isLeftGround  = info.isLeftGround;
            isRightGround = info.isRightGround;
            isLeftStatic  = info.isLeftStatic;
            isRightStatic = info.isRightStatic;
            motionType    = info.motionType;
            leftFootSpeed = info.lFootSpeed;
            rightFootSpeed= info.rFootSpeed;
        }

        // 4) Heading from body yaw (minus correction)
        var heading = Quaternion.Euler(0f, bodyEuler.y - yawCorrection, 0f);
        Vector3 forward = heading * Vector3.forward;

        // 5) Convert per-foot speeds into a scalar walking speed
        float targetSpeed = FootSpeedToScalar(leftFootSpeed, rightFootSpeed);

        // Smooth and move
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        Vector3 horiz = forward * smoothedSpeed;
        velocity.x = horiz.x;
        velocity.z = horiz.z;
        velocity.y += -gravity * Time.deltaTime;

        cc.Move(velocity * Time.deltaTime);
    }

    float FootSpeedToScalar(Vector3 l, Vector3 r)
    {
        // Use horizontal components; average magnitude, bias toward the swinging foot
        var lh = new Vector3(l.x, 0f, l.z);
        var rh = new Vector3(r.x, 0f, r.z);

        float baseMag = 0.5f * (lh.magnitude + rh.magnitude);

        if (isLeftGround && !isRightGround) baseMag = rh.magnitude * 0.9f + baseMag * 0.1f;
        if (isRightGround && !isLeftGround) baseMag = lh.magnitude * 0.9f + baseMag * 0.1f;

        return baseMag * speedGain;
    }

    void ResetFeet()
    {
        leftFootSpeed = rightFootSpeed = Vector3.zero;
        isLeftGround = isRightGround = isLeftStatic = isRightStatic = false;
        motionType = -1;
    }

    void OnGUI()
    {
        if (!showHud) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = hudFontSize, richText = true };

        GUILayout.BeginArea(new Rect(12, 12, 640, 300));
        GUILayout.Label("<b>KAT Per-Foot + Body Live</b>", style);
        GUILayout.Label($"Connected: {sdkConnected} | Device: {deviceName}", style);
        GUILayout.Label($"Body Euler (deg) YPR: {bodyEuler.y:0.0}, {bodyEuler.x:0.0}, {bodyEuler.z:0.0}", style);
        GUILayout.Label($"Left  foot v: {leftFootSpeed.x:0.00}, {leftFootSpeed.y:0.00}, {leftFootSpeed.z:0.00} | Ground:{isLeftGround} Static:{isLeftStatic}", style);
        GUILayout.Label($"Right foot v: {rightFootSpeed.x:0.00}, {rightFootSpeed.y:0.00}, {rightFootSpeed.z:0.00} | Ground:{isRightGround} Static:{isRightStatic}", style);
        GUILayout.Label($"motionType: {motionType}   |  Calibrate yaw: press <b>C</b>", style);
        GUILayout.EndArea();
    }
}
