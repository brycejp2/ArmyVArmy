using System.Collections.Generic;
using ArmyVArmy.Core;
using ArmyVArmy.Data;
using ArmyVArmy.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmyVArmy.UI
{
    // Roster list and formation grid are built at runtime from RunState (variable size, and need
    // to reflect roster changes from later phases), so only the static panels/buttons are wired
    // by hand in the scene. Selecting a unit (from either side) highlights it in both the roster
    // and the grid; grid slots can also be dragged onto each other to swap/move a unit directly.
    public class MetaHubController : MonoBehaviour
    {
        [SerializeField] UnitDef swordsmanDef;
        [SerializeField] UnitDef spearmanDef;
        [SerializeField] UnitDef archerDef;

        [SerializeField] UpgradeDef swordsmanArmorUpgrade;
        [SerializeField] UpgradeDef spearmanArmorUpgrade;
        [SerializeField] UpgradeDef archerArmorUpgrade;

        [SerializeField] Transform rosterListParent;
        [SerializeField] Transform formationGridParent;
        [SerializeField] TextMeshProUGUI goldLabel;
        [SerializeField] TextMeshProUGUI nodeLabel;
        [SerializeField] Button healButton;
        [SerializeField] TextMeshProUGUI healButtonLabel;
        [SerializeField] Button swordsmanUpgradeButton;
        [SerializeField] TextMeshProUGUI swordsmanUpgradeLabel;
        [SerializeField] Button spearmanUpgradeButton;
        [SerializeField] TextMeshProUGUI spearmanUpgradeLabel;
        [SerializeField] Button archerUpgradeButton;
        [SerializeField] TextMeshProUGUI archerUpgradeLabel;

        const int HealCost = 20;

        static readonly Color SelectedColor = new(0.4f, 0.7f, 0.3f);
        static readonly Color OccupiedColor = new(0.25f, 0.35f, 0.5f);
        static readonly Color EmptyColor = new(0.15f, 0.15f, 0.18f);
        static readonly Color UnselectedRosterColor = new(0.18f, 0.18f, 0.22f);

        RunState run;
        int selectedRosterIndex = -1;
        readonly List<Button> rosterButtons = new();
        readonly List<Button> slotButtons = new();

        void Start()
        {
            run = GameManager.Instance.CurrentRun;
            BuildFormationGrid();
            RefreshUI();
        }

        void BuildFormationGrid()
        {
            for (int slot = 0; slot < FormationGrid.SlotCount; slot++)
            {
                int capturedSlot = slot;
                Button button = CreateButton(formationGridParent, $"Slot{slot}", "", out _);
                button.onClick.AddListener(() => OnSlotClicked(capturedSlot));

                var dragHandler = button.gameObject.AddComponent<FormationSlotDragHandler>();
                dragHandler.SlotIndex = slot;
                dragHandler.Controller = this;

                slotButtons.Add(button);
            }
        }

        void RefreshUI()
        {
            goldLabel.text = $"Gold: {run.Gold}";
            nodeLabel.text = $"Node: {run.NodeIndex + 1}";
            RefreshRosterList();
            RefreshFormationGrid();
            RefreshShop();
        }

        void RefreshRosterList()
        {
            foreach (Button b in rosterButtons)
                Destroy(b.gameObject);
            rosterButtons.Clear();

            for (int i = 0; i < run.Roster.Count; i++)
            {
                RosterUnit unit = run.Roster[i];
                int capturedIndex = i;
                UnitDef def = FindDef(unit.UnitDefName);
                string label = $"{unit.UnitDefName}\n{Mathf.CeilToInt(unit.CurrentHealth)}/{Mathf.CeilToInt(def.Health)}";

                Button button = CreateButton(rosterListParent, $"Roster{i}", label, out _);
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
                button.onClick.AddListener(() => OnRosterClicked(capturedIndex));
                button.GetComponent<Image>().color = i == selectedRosterIndex ? SelectedColor : UnselectedRosterColor;

                rosterButtons.Add(button);
            }
        }

        void RefreshFormationGrid()
        {
            var occupied = new string[FormationGrid.SlotCount];
            foreach (RosterUnit unit in run.Roster)
                if (unit.FormationSlot >= 0 && unit.FormationSlot < FormationGrid.SlotCount)
                    occupied[unit.FormationSlot] = unit.UnitDefName;

            int selectedSlot = selectedRosterIndex >= 0 ? run.Roster[selectedRosterIndex].FormationSlot : -1;

            for (int slot = 0; slot < FormationGrid.SlotCount; slot++)
            {
                Button button = slotButtons[slot];
                string occupant = occupied[slot];
                button.GetComponentInChildren<TextMeshProUGUI>().text = occupant != null ? occupant[..Mathf.Min(3, occupant.Length)] : "";
                button.GetComponent<Image>().color = slot == selectedSlot ? SelectedColor : occupant != null ? OccupiedColor : EmptyColor;
            }
        }

        void RefreshShop()
        {
            SetUpgradeButton(swordsmanUpgradeButton, swordsmanUpgradeLabel, swordsmanArmorUpgrade);
            SetUpgradeButton(spearmanUpgradeButton, spearmanUpgradeLabel, spearmanArmorUpgrade);
            SetUpgradeButton(archerUpgradeButton, archerUpgradeLabel, archerArmorUpgrade);

            bool hasSelection = selectedRosterIndex >= 0;
            bool isWounded = hasSelection &&
                run.Roster[selectedRosterIndex].CurrentHealth < FindDef(run.Roster[selectedRosterIndex].UnitDefName).Health - 0.01f;
            healButton.interactable = hasSelection && isWounded && run.Gold >= HealCost;
            healButtonLabel.text = $"Heal ({HealCost}g)";
        }

        void SetUpgradeButton(Button button, TextMeshProUGUI label, UpgradeDef upgrade)
        {
            int level = run.GetUpgradeLevel(upgrade.DisplayName);
            int cost = upgrade.CostForNextLevel(level);
            label.text = $"{upgrade.TargetUnit.DisplayName} Armor+\n{cost}g";
            button.interactable = run.Gold >= cost;
        }

        void OnRosterClicked(int index)
        {
            selectedRosterIndex = selectedRosterIndex == index ? -1 : index;
            RefreshUI();
        }

        void OnSlotClicked(int slot)
        {
            RosterUnit occupant = FindUnitAtSlot(slot);

            if (selectedRosterIndex < 0)
            {
                if (occupant != null)
                    SelectUnit(occupant);
                return;
            }

            RosterUnit selected = run.Roster[selectedRosterIndex];
            if (selected == occupant)
            {
                selectedRosterIndex = -1;
                RefreshUI();
                return;
            }

            MoveUnitToSlot(selected, slot);
        }

        public void OnSlotDragDrop(int sourceSlot, int targetSlot)
        {
            if (sourceSlot == targetSlot)
                return;

            RosterUnit dragged = FindUnitAtSlot(sourceSlot);
            if (dragged != null)
                MoveUnitToSlot(dragged, targetSlot);
        }

        public void RefreshGridVisuals() => RefreshFormationGrid();

        public bool HasUnitAtSlot(int slot) => FindUnitAtSlot(slot) != null;

        void SelectUnit(RosterUnit unit)
        {
            selectedRosterIndex = run.Roster.IndexOf(unit);
            RefreshUI();
        }

        void MoveUnitToSlot(RosterUnit unit, int targetSlot)
        {
            int oldSlot = unit.FormationSlot;
            RosterUnit occupant = FindUnitAtSlot(targetSlot);

            unit.FormationSlot = targetSlot;
            if (occupant != null && occupant != unit)
                occupant.FormationSlot = oldSlot;

            selectedRosterIndex = run.Roster.IndexOf(unit);

            SaveService.Save(run);
            RefreshUI();
        }

        public void OnHealButton()
        {
            if (selectedRosterIndex < 0)
                return;

            RosterUnit unit = run.Roster[selectedRosterIndex];
            run.Gold -= HealCost;
            unit.CurrentHealth = FindDef(unit.UnitDefName).Health;

            SaveService.Save(run);
            RefreshUI();
        }

        public void OnSwordsmanUpgradeButton() => PurchaseUpgrade(swordsmanArmorUpgrade);
        public void OnSpearmanUpgradeButton() => PurchaseUpgrade(spearmanArmorUpgrade);
        public void OnArcherUpgradeButton() => PurchaseUpgrade(archerArmorUpgrade);

        void PurchaseUpgrade(UpgradeDef upgrade)
        {
            int level = run.GetUpgradeLevel(upgrade.DisplayName);
            int cost = upgrade.CostForNextLevel(level);

            if (run.Gold < cost)
                return;

            run.Gold -= cost;
            run.IncrementUpgradeLevel(upgrade.DisplayName);

            SaveService.Save(run);
            RefreshUI();
        }

        public void OnStartBattleButton() => SceneRouter.LoadBattle();

        RosterUnit FindUnitAtSlot(int slot)
        {
            foreach (RosterUnit unit in run.Roster)
                if (unit.FormationSlot == slot)
                    return unit;
            return null;
        }

        UnitDef FindDef(string unitDefName)
        {
            if (swordsmanDef.DisplayName == unitDefName) return swordsmanDef;
            if (spearmanDef.DisplayName == unitDefName) return spearmanDef;
            if (archerDef.DisplayName == unitDefName) return archerDef;
            return null;
        }

        static Button CreateButton(Transform parent, string name, string label, out TextMeshProUGUI labelComponent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.18f, 0.22f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            labelComponent = textGo.AddComponent<TextMeshProUGUI>();
            labelComponent.text = label;
            labelComponent.fontSize = 20;
            labelComponent.alignment = TextAlignmentOptions.Center;
            labelComponent.color = Color.white;

            return button;
        }
    }
}
