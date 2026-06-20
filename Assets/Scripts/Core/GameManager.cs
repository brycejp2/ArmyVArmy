using UnityEngine;

namespace ArmyVArmy.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartNewRun()
        {
            SceneRouter.LoadMetaHub();
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
