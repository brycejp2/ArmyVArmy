using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArmyVArmy.UI
{
    // Lets a formation grid slot be dragged onto another slot to swap/move the unit there.
    // Attached at runtime by MetaHubController when it builds the grid. The slot itself doesn't
    // move during the drag (it's pinned by the GridLayoutGroup) - it just tints to show it's the
    // drag source, and the controller's refresh restores the real state once the gesture ends.
    public class FormationSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public int SlotIndex { get; set; }
        public MetaHubController Controller { get; set; }

        static readonly Color DraggingColor = new(0.9f, 0.85f, 0.3f);

        Image image;

        void Awake()
        {
            image = GetComponent<Image>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Controller.HasUnitAtSlot(SlotIndex))
                image.color = DraggingColor;
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Controller.RefreshGridVisuals();
        }

        public void OnDrop(PointerEventData eventData)
        {
            FormationSlotDragHandler source = eventData.pointerDrag?.GetComponent<FormationSlotDragHandler>();
            if (source != null)
                Controller.OnSlotDragDrop(source.SlotIndex, SlotIndex);
        }
    }
}
