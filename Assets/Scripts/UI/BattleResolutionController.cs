using ArmyVArmy.Core;
using ArmyVArmy.Sim;
using TMPro;
using UnityEngine;

namespace ArmyVArmy.UI
{
    public class BattleResolutionController : MonoBehaviour
    {
        [SerializeField] BattleSimulationRunner runner;
        [SerializeField] GameObject resolutionPanel;
        [SerializeField] TextMeshProUGUI resultLabel;

        bool shown;

        void Update()
        {
            if (shown || !runner.BattleEnded)
                return;

            shown = true;
            resultLabel.text = runner.PlayerWon ? "Victory!" : "Defeat";
            resolutionPanel.SetActive(true);
        }

        public void OnContinueButton()
        {
            if (runner.PlayerWon)
                SceneRouter.LoadMetaHub();
            else
                SceneRouter.LoadMainMenu();
        }
    }
}
