using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    /// <summary>Runtime UI event probe. Uses panel hit testing; does not move the user's system pointer.</summary>
    internal static class PointerProbe
    {
        internal static Vector2 Center(VisualElement element)
        {
            if (element == null || element.panel == null || !element.enabledInHierarchy) throw new InvalidOperationException("Pointer target unavailable");
            Vector2 position = element.worldBound.center;
            var hit = element.panel.Pick(position);
            if (hit != element && (hit == null || !element.Contains(hit))) throw new InvalidOperationException("Pointer target clipped or covered: " + element.name);
            return position;
        }
        internal static void Click(VisualElement element)
        {
            var position = Center(element); Down(element, position); Up(element, position);
        }
        internal static void ClickAt(VisualElement element, Vector2 position)
        {
            var hit = element.panel.Pick(position);
            if (hit != element && (hit == null || !element.Contains(hit))) throw new InvalidOperationException("Pointer position outside target");
            Down(element, position); Up(element, position);
        }
        internal static void Down(VisualElement element, Vector2 position,int button=0,EventModifiers modifiers=EventModifiers.None)
        {
            using (var evt = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = button, modifiers=modifiers, mousePosition = position })) element.SendEvent(evt);
        }
        internal static void Move(VisualElement element, Vector2 position,int button=0,EventModifiers modifiers=EventModifiers.None)
        {
            using (var evt = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = button, modifiers=modifiers, mousePosition = position })) element.SendEvent(evt);
        }
        internal static void Up(VisualElement element, Vector2 position,int button=0,EventModifiers modifiers=EventModifiers.None)
        {
            using (var evt = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = button, modifiers=modifiers, mousePosition = position })) element.SendEvent(evt);
        }
    }
}
