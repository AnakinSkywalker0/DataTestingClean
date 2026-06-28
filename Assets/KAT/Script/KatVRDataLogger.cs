using UnityEngine;


public class MovementDataLogger : MonoBehaviour
{
    public Transform player; // Assign your camera / XR rig / player
    public Vector3 respawnPoint; // Set this to your desired respawn location
    public LayerMask carLayer; // Set this to the layer your cars are on
    private float detectionRadius = 100f;



    

    [Header("Logger Settings")]
    public DataLogger logger;
    public GameObject resetTasbtn;

    private string NearestCarId = "None";
    private Vector3 NearestCarPos;
    public Transform reference = null;
    private string filePath;
    string currentZone = "Unknown";
    private int FootStepCount = -2;
    private bool lastLeftGround = false;
    private bool lastRightGround = false;
    float FixedTime =0f;
    float frequency= 0.1f;
    float logTimer = 0f;

    int testCounter = 4;

    void Start()
    {
        respawnPoint = player.position; // Initialize respawn point to player's starting position
    }

    void Update() {
        logTimer += Time.deltaTime;

        if (logTimer >= frequency)
        {
            
            logTimer -= frequency;
            CollectData();
        }
    }


    void CollectData()
    {
        var data = KATNativeSDK.GetWalkStatus();

        // StepCount ---------------------------
        var info = WalkC2ExtraData.GetExtraInfoC2(data);
        
        if (!lastLeftGround && info.isLeftGround)
        {
            FootStepCount++;
        }

        // Right foot step
        if (!lastRightGround && info.isRightGround)
        {
            FootStepCount++;
        }

        lastLeftGround = info.isLeftGround;
        lastRightGround = info.isRightGround;
        

        float speed = data.moveSpeed.z;

        // Rotation
        Vector3 rotation = data.bodyRotationRaw.eulerAngles;
        float yaw = rotation.y;
        float pitch = rotation.x;

        // Position
        Vector3 pos = player.position;

        string status = GetStatus(speed);
        string zone = currentZone;

        FixedTime +=frequency;

        string line =
            FixedTime.ToString("F2") + "," +
            zone + "," +
            speed.ToString("F3") + "," +
            StoreData.GetID()+","+
            status + "," +
            FootStepCount.ToString("F2")+","+
            yaw.ToString("F2") + "," +
            pitch.ToString("F2") + "," +
            pos.x.ToString("F3") + "," +
            pos.y.ToString("F3") + "," +
            pos.z.ToString("F3") + ","+
            GetClosestCardData() +"\n";
            /*Time(ms),
            Zone,Speed(M/s),ID,Status,Step
            ,Yaw,Pitch,Pos_X,Pos_Y,Pos_Z,
            NearestCarId,NearestCarType,NearestCarDistance,NearestCarPos_X,NearestCarPos_Y,NearestCarPos_Z,NearestCar_Speed\n");

            */

        // Write to CSV
        logger.WriteMovementData(line);
        // File.AppendAllText(filePath, line);
    }
    string GetClosestCardData()
    {
        Collider[] hits = Physics.OverlapSphere(
            reference.position,
            detectionRadius,
            carLayer
        );

        Transform closestCar = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Transform car = hit.transform;

            // CAR perspective check
            Vector3 dirToPlayer = (reference.position - car.position).normalized;
            float dot = Vector3.Dot(car.forward, dirToPlayer);

            if (dot <= 0)
                continue;

            //  Horizontal distance (better for vehicles)
            Vector3 a = reference.position;
            Vector3 b = car.position;
            a.y = 0;
            b.y = 0;

            float distance = Vector3.Distance(a, b);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestCar = car;
            }
        }



        if (closestCar != null)
        {
            Vector3 pos = closestCar.position;

            string vehicleId = "";
            string vehicleType = "";
            float vehicleSpeed = 0f;

            string result = string.Format("{0:F2},{1:F2},{2:F2},{3:F2},{4:F2},{5:f2},{6:f2}",
                vehicleId,
                vehicleType,
                minDistance,
                vehicleSpeed,
                pos.x,
                pos.y,
                pos.z
            );

            return result;
        }
        else
        {
            return "None,None,,,,,";
        }
    }
    private string GetStatus(float speed)
    {
        if (speed < 0.1f) return "Stop";
        if (speed < 1.5f) return "Walking";
        return "Running";
    }


    public void ResetTask()
    {
        if(!resetTasbtn.activeSelf) return;
        logger.CreateFiles();
        FootStepCount = 0;  
        player.position = respawnPoint;
        testCounter--; // Teleport player to respawn point
        resetTasbtn.SetActive(false);
    }
    void OnTriggerEnter(Collider other){
        if(other.CompareTag("Respawn")){
            resetTasbtn.SetActive(true);
            if(testCounter == 0){
                other.gameObject.SetActive(false);
            }
            Invoke("ResetTask",5f);
            return;// Teleport player to respawn point
        }
        currentZone = other.tag;
    }
}