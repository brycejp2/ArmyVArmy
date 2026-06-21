using ArmyVArmy.Data;
using ArmyVArmy.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArmyVArmy.UI
{
    public class BattleHud : MonoBehaviour
    {
        [SerializeField] BattleSimulationRunner runner;
        [SerializeField] AbilityDef damageAbility;
        [SerializeField] AbilityDef healAbility;
        [SerializeField] CommandDef chargeCommand;
        [SerializeField] CommandDef braceCommand;

        [SerializeField] Button damageAbilityButton;
        [SerializeField] TextMeshProUGUI damageAbilityLabel;
        [SerializeField] Button healAbilityButton;
        [SerializeField] TextMeshProUGUI healAbilityLabel;
        [SerializeField] Button pauseButton;
        [SerializeField] TextMeshProUGUI pauseLabel;
        [SerializeField] Button speedButton;
        [SerializeField] TextMeshProUGUI speedLabel;

        AbilityDef pendingAbility;
        bool paused;
        float baseSpeed = 1f;

        public void OnDamageAbilityButton() => pendingAbility = damageAbility;
        public void OnHealAbilityButton() => pendingAbility = healAbility;
        public void OnChargeButton() => runner.IssueCommand(chargeCommand);
        public void OnBraceButton() => runner.IssueCommand(braceCommand);

        public void OnPauseButton()
        {
            paused = !paused;
            ApplyTimeScale();
        }

        public void OnSpeedButton()
        {
            baseSpeed = Mathf.Approximately(baseSpeed, 1f) ? 2f : 1f;
            ApplyTimeScale();
        }

        void ApplyTimeScale()
        {
            runner.TimeScale = paused ? 0f : baseSpeed;
            pauseLabel.text = paused ? "Resume" : "Pause";
            speedLabel.text = Mathf.Approximately(baseSpeed, 1f) ? "1x" : "2x";
        }

        void Update()
        {
            UpdateTargeting();
            UpdateCooldownDisplay();
        }

        void UpdateTargeting()
        {
            if (pendingAbility == null)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return;

            Vector2 screenPos = pointer.position.ReadValue();
            float depth = -runner.BattleCamera.transform.position.z;
            Vector3 worldPos = runner.BattleCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));

            runner.TryCastAbility(pendingAbility, worldPos);
            pendingAbility = null;
        }

        void UpdateCooldownDisplay()
        {
            SetAbilityButtonState(damageAbilityButton, damageAbilityLabel, damageAbility);
            SetAbilityButtonState(healAbilityButton, healAbilityLabel, healAbility);
        }

        void SetAbilityButtonState(Button button, TextMeshProUGUI label, AbilityDef ability)
        {
            float remaining = runner.GetAbilityCooldownRemaining(ability);
            button.interactable = remaining <= 0f;
            label.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : ability.DisplayName;
        }
    }
}
