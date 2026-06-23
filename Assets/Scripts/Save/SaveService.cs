using System.IO;
using ArmyVArmy.Core;
using UnityEngine;

namespace ArmyVArmy.Save
{
    public static class SaveService
    {
        const string FileName = "run.json";

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave() => File.Exists(FilePath);

        public static void Save(RunState state)
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(state));
        }

        public static RunState Load()
        {
            return HasSave() ? JsonUtility.FromJson<RunState>(File.ReadAllText(FilePath)) : null;
        }

        public static void DeleteSave()
        {
            if (HasSave())
                File.Delete(FilePath);
        }
    }
}
