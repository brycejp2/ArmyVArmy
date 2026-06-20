using ArmyVArmy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArmyVArmy.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] GameManager gameManager;
        [SerializeField] Button continueButton;
        [SerializeField] GameObject settingsPanel;

        void Start()
        {
            // No save system until the meta loop ships (Phase 5), so there is never a run to continue yet.
            continueButton.interactable = false;
        }

        public void OnNewRun() => gameManager.StartNewRun();
        public void OnOpenSettings() => settingsPanel.SetActive(true);
        public void OnQuit() => gameManager.QuitGame();
    }
}
