using System.IO;
using UnityEngine;

public static class CSVWriter
{
    // private static string fileName = "TrainingData.csv";
    private static string path = "/Training.csv";

    public static void Save(string datastream)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "Name,ID,Age,Height\n");
        }

        string line = datastream + "\n";

        File.AppendAllText(path, line);

        Debug.Log("Saved to: " + path);
    }

    // public static bool DoesIDExist(string id)
    // {
    //     // string path = GetPath();

    //     if (!File.Exists(path))
    //         return false;

    //     string[] lines = File.ReadAllLines(path);

    //     // Skip header (start from 1)
    //     for (int i = 1; i < lines.Length; i++)
    //     {
    //         string[] columns = lines[i].Split(',');

    //         if (columns.Length < 2) continue;

    //         if (columns[1] == id)
    //             return true;
    //     }

    //     return false;
    // }

}
