using UnityEngine;
using UnityEngine.EventSystems;

namespace Uninvited
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        public Vector2 Direction { get; private set; }
        RectTransform rect;
        int pointer = int.MinValue;
        void Awake() => rect = (RectTransform)transform;
        public void OnPointerDown(PointerEventData data)
        {
            if (pointer != int.MinValue) return;
            pointer = data.pointerId;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != pointer) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out var local)) return;
            var radius = rect.rect.width * .33f;
            Direction = Vector2.ClampMagnitude(local / radius, 1);
            Knob.anchoredPosition = Direction * radius;
        }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointer) Clear(); }
        public void Clear() { pointer = int.MinValue; Direction = Vector2.zero; if (Knob != null) Knob.anchoredPosition = Vector2.zero; }
        void OnDisable() => Clear();
    }
}
