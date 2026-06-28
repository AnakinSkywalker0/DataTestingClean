// using System.IO;
// using UnityEngine;
// using System;

// namespace VIVE.OpenXR.Samples.EyeTracker
// {
//     public class LoggerEyeGazeData : MonoBehaviour
//     {
//         string filePath;
//         public string EyeTrackingfolderPath,MovementfolderPath;
//         int TestCounter = 0;


//         void Awake()
//         {
//             EyeTrackingfolderPath = "DataSet/EyeTrackingData/"+StoreData.GetFolderName();
//             MovementfolderPath = "DataSet/MovementData/"+StoreData.GetFolderName();
    

//             // folderPath = Path.Combine("DataSet",");
//             filePath = Path.Combine(folderPath, StoreData.GetFileName());

//             // ✅ Create directory if it doesn't exist
//             string directory = Path.GetDirectoryName(filePath);
//             if (!Directory.Exists(directory))
//             {
//                 Directory.CreateDirectory(directory);
//                 Debug.Log("Created directory: " + directory);
//             }

//             if (!File.Exists(filePath))
//             {   
//                 File.WriteAllText(filePath,
//                 "Time(ms),ID,Gaze_X,Gaze_Y,Gaze_Z,TrackingObject,FixationDuration,PreviousAOI,CurrentAOI,PupilLeft,PupilRight,PupilCombined,SaccadeSpeed,HeadYaw,HeadPitch,Blink,LookAheadCarID,LookAheadCarType, LookAheadCarSpeed,Distance,LookAheadCar_PosX,LookAheadCar_PosY,LookAheadCar_PosZ\n");
//             }

//             Debug.Log("Eye Tracking Log Path: " + filePath);
//         }

//         public void WriteEyeTrackingData(string dataStream)
//         {
//             File.AppendAllText(filePath, dataStream);
//         }

//         public void WriteMovementData(string dataStream)
//         {
//             string movementFilePath = Path.Combine(MovementfolderPath, StoreData.GetFileName());
//             string directory = Path.GetDirectoryName(movementFilePath);
//             if (!Directory.Exists(directory))
//             {
//                 Directory.CreateDirectory(directory);
//                 Debug.Log("Created directory: " + directory);
//             }

//             if (!File.Exists(movementFilePath))
//             {   
//                 File.WriteAllText(movementFilePath,
//                 "Time(ms),ID,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ\n");
//             }

//             File.AppendAllText(movementFilePath, dataStream);
//         }

//         public void WriteToFile(string dataStream)
//         {
//             File.AppendAllText(filePath, dataStream);

//         }
//     }
// }


using System;
using System.IO;
using UnityEngine;

    public class DataLogger : MonoBehaviour
    {
        private string eyeTrackingFilePath;
        private string movementDataFilePath;

        public string eyeTrackingFolderPath;
        public string movementFolderPath;

        private const string eyeTrackingHeader =
            "Time(ms),ID,Gaze_X,Gaze_Y,Gaze_Z,TrackingObject,FixationDuration,PreviousAOI,CurrentAOI,PupilLeft,PupilRight,PupilCombined,SaccadeSpeed,HeadYaw,HeadPitch,Blink,LookAheadCarID,LookAheadCarType,LookAheadCarSpeed,Distance,LookAheadCar_PosX,LookAheadCar_PosY,LookAheadCar_PosZ\n";

        private const string movementDataHeader =
             "Time(ms),Zone,Speed(M/s),ID,Status,Step,Yaw,Pitch,PlayerPos_X,PlayerPos_Y,PlayerPos_Z,NearestCarId,NearestCarType,NearestCarDistance,NearestCar_Speed,NearestCarPos_X,NearestCarPos_Y,NearestCarPos_Z\n";
       
        private int testCounter = 0;

        private void Awake()
        {
            eyeTrackingFolderPath = SessionPaths.EyeTracking;
            movementFolderPath = SessionPaths.Movement;

            CreateFiles();
        }

        public void CreateFiles()
        {
            // Tag files by scenario (T#_S#) when a sequenced run is active; fall back to the legacy counter otherwise.
            string tag = ScenarioSeq.ScenarioContext.SceneId > 0
                ? ScenarioSeq.ScenarioContext.Label
                : $"task{testCounter}";
            CreateCSVFile(out eyeTrackingFilePath, eyeTrackingFolderPath, $"EyeTracking_{tag}.csv", eyeTrackingHeader);
            CreateCSVFile(out movementDataFilePath, movementFolderPath, $"Movement_{tag}.csv", movementDataHeader);
            testCounter++;
        }

        public void WriteEyeTrackingData(string dataStream)
        {
            WriteToFile(eyeTrackingFilePath, dataStream);
        }

        public void WriteMovementData(string dataStream)
        {
            WriteToFile(movementDataFilePath, dataStream);
        }

        private void WriteToFile(string filePath, string dataStream)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("File path is empty. CSV file was not created properly.");
                return;
            }

            File.AppendAllText(filePath, dataStream);
        }

        private void CreateCSVFile(out string filePath, string folderPath, string fileName, string headerLine)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Debug.Log("Created directory: " + folderPath);
            }

            filePath = Path.Combine(folderPath, fileName);

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, headerLine);
                Debug.Log("Created CSV file: " + filePath);
            }
        }
    }
