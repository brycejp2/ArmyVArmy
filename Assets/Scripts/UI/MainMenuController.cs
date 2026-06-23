using ArmyVArmy.Core;
using ArmyVArmy.Save;
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
            continueButton.interactable = SaveService.HasSave();
        }

        public void OnNewRun() => gameManager.StartNewRun();
        public void OnContinue() => gameManager.ContinueRun();
        public void OnOpenSettings() => settingsPanel.SetActive(true);
        public void OnQuit() => gameManager.QuitGame();
    }
}
