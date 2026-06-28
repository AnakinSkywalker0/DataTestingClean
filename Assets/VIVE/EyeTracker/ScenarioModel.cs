using System;

// DOE scenario set: 4 tasks x 5 scenarios (S1–S4 mandatory, S5 optional), plus the runtime pointers shared across scenes.
namespace ScenarioSeq
{
    public enum RoadType { Midblock, Intersection }
    public enum Distraction { None, StaticBillboard, AnimatedBillboard, Talking }
    public enum ITS { Off, On }
    public enum Traffic { Simple, Complex }

    [Serializable]
    public struct ScenarioDef
    {
        public RoadType road;
        public Distraction distraction;
        public ITS its;
        public Traffic traffic;

        public ScenarioDef(RoadType road, Distraction distraction, ITS its, Traffic traffic)
        {
            this.road = road;
            this.distraction = distraction;
            this.its = its;
            this.traffic = traffic;
        }

        // Unity scene loaded for this scenario. Must match Build Settings names exactly.
        public string EnvSceneName => road == RoadType.Midblock ? "MidBlock" : "INTERSECTION";

        // Factor breakdown shown on the scenario cards (matches the DOE table order).
        public string Summary =>
            $"{road} - {traffic} - {Label(distraction)} - ITS {(its == ITS.On ? "ON" : "OFF")}";

        private static string Label(Distraction d)
        {
            switch (d)
            {
                case Distraction.StaticBillboard: return "Static Billboard";
                case Distraction.AnimatedBillboard: return "Animated Billboard";
                default: return d.ToString(); // None, Talking
            }
        }
    }

    [Serializable]
    public class TaskDef
    {
        public int id;
        public string name;
        public ScenarioDef[] scenarios; // index order == S1..S5

        public TaskDef(int id, string name, ScenarioDef[] scenarios)
        {
            this.id = id;
            this.name = name;
            this.scenarios = scenarios;
        }

        public string MenuLabel => $"Task {id}\n({name})";
    }

    // 16 mandatory + 4 optional (S5) scenarios. NOTE: scenario factors follow the DOE tables; the task
    // display names are the experimenter-facing labels and may not match the DOE's balanced theme.
    public static class ScenarioCatalog
    {
        public static readonly TaskDef[] Tasks =
        {
            new TaskDef(1, "Baseline", new[]
            {
                new ScenarioDef(RoadType.Midblock,     Distraction.None,              ITS.Off, Traffic.Complex),
                new ScenarioDef(RoadType.Midblock,     Distraction.StaticBillboard,   ITS.Off, Traffic.Simple),
                new ScenarioDef(RoadType.Intersection, Distraction.AnimatedBillboard, ITS.On,  Traffic.Simple),
                new ScenarioDef(RoadType.Intersection, Distraction.Talking,           ITS.On,  Traffic.Complex),
                new ScenarioDef(RoadType.Midblock,     Distraction.None,              ITS.On,  Traffic.Simple),   // S5 optional
            }),
            new TaskDef(2, "Visual Distraction", new[]
            {
                new ScenarioDef(RoadType.Midblock,     Distraction.None,              ITS.On,  Traffic.Simple),
                new ScenarioDef(RoadType.Midblock,     Distraction.StaticBillboard,   ITS.On,  Traffic.Complex),
                new ScenarioDef(RoadType.Intersection, Distraction.AnimatedBillboard, ITS.Off, Traffic.Complex),
                new ScenarioDef(RoadType.Intersection, Distraction.Talking,           ITS.Off, Traffic.Simple),
                new ScenarioDef(RoadType.Intersection, Distraction.None,              ITS.Off, Traffic.Complex),  // S5 optional
            }),
            new TaskDef(3, "Complex Traffic", new[]
            {
                new ScenarioDef(RoadType.Midblock,     Distraction.AnimatedBillboard, ITS.Off, Traffic.Simple),
                new ScenarioDef(RoadType.Midblock,     Distraction.Talking,           ITS.Off, Traffic.Complex),
                new ScenarioDef(RoadType.Intersection, Distraction.None,              ITS.On,  Traffic.Complex),
                new ScenarioDef(RoadType.Intersection, Distraction.StaticBillboard,   ITS.On,  Traffic.Simple),
                new ScenarioDef(RoadType.Midblock,     Distraction.Talking,           ITS.On,  Traffic.Complex),  // S5 optional
            }),
            new TaskDef(4, "Highest Difficulty", new[]
            {
                new ScenarioDef(RoadType.Midblock,     Distraction.AnimatedBillboard, ITS.On,  Traffic.Complex),
                new ScenarioDef(RoadType.Midblock,     Distraction.Talking,           ITS.On,  Traffic.Simple),
                new ScenarioDef(RoadType.Intersection, Distraction.None,              ITS.Off, Traffic.Simple),
                new ScenarioDef(RoadType.Intersection, Distraction.StaticBillboard,   ITS.Off, Traffic.Complex),
                new ScenarioDef(RoadType.Intersection, Distraction.StaticBillboard,   ITS.Off, Traffic.Simple),  // S5 optional
            }),
        };

        public static TaskDef GetTask(int id)
        {
            foreach (var t in Tasks)
                if (t.id == id) return t;
            return null;
        }
    }

    // Task picked on the Task menu; read by the Scenario menu (persists across the scene load).
    public static class ScenarioSelection
    {
        public static int SelectedTaskId = 1;
    }

    // Runtime pointer to the scenario currently running; read by DataLogger for file tagging.
    public static class ScenarioContext
    {
        public static int TaskId;       // 1..4 (0 = no run active)
        public static int SceneId;      // 1..4 (0 = no run active)
        public static int GlobalIndex;  // position within the chosen order

        public static string Label => $"T{TaskId}_S{SceneId}";

        public static void Clear()
        {
            TaskId = 0;
            SceneId = 0;
            GlobalIndex = 0;
        }
    }
}
