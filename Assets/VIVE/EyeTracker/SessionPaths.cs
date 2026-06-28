using System.IO;
using UnityEngine;

// Single source of truth for this session's recording folders — one directory per data stream.
public static class SessionPaths
{
    // Anchor to the project root (parent of Assets/) so every path is absolute. External
    // processes such as Emotiv Cortex don't share Unity's working directory and require an
    // absolute export folder.
    private static string ParticipantRoot =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Data", StoreData.GetFolderName());

    public static string EyeTracking => Ensure(Path.Combine(ParticipantRoot, "EyeTracking"));
    public static string Movement    => Ensure(Path.Combine(ParticipantRoot, "Movement"));
    public static string EEG         => Ensure(Path.Combine(ParticipantRoot, "EEG"));
    public static string HeartRate   => Ensure(Path.Combine(ParticipantRoot, "HeartRate"));
    public static string Events      => Ensure(Path.Combine(ParticipantRoot, "Events"));

    private static string Ensure(string dir)
    {
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }
}
