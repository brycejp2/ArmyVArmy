using UnityEngine.SceneManagement;

namespace ArmyVArmy.Core
{
    public static class SceneRouter
    {
        public const string MainMenu = "MainMenu";
        public const string MetaHub = "MetaHub";
        public const string Battle = "Battle";

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenu);
        public static void LoadMetaHub() => SceneManager.LoadScene(MetaHub);
        public static void LoadBattle() => SceneManager.LoadScene(Battle);
    }
}
