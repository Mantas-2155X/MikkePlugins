using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoveController 
{
    public static class ButtonManager 
    {
        private static Color HighlightColor = Color.blue;

        public static Button ClickButton(Button button, Action<BaseEventData> action) 
        {
            button.gameObject.AddComponent<EventTrigger>();
            var trigger = button.gameObject.GetComponent<EventTrigger>();
            trigger.triggers.Add(getScrollTrigger());

            //CLICK
            var entryClick = new EventTrigger.Entry {eventID = EventTriggerType.PointerClick};
            entryClick.callback.AddListener(data => 
            {
                pressColor(button);
                action(data);
            });
            trigger.triggers.Add(entryClick);

            //BUTTON UP
            var entryPointerUp = new EventTrigger.Entry {eventID = EventTriggerType.PointerUp};
            entryPointerUp.callback.AddListener(data => { liftColor(button); });
            trigger.triggers.Add(entryPointerUp);
            return button;
        }

        public static Button DragButton(Button button, DragButtonAction dba) 
        {
            var dragging = false;

            button.gameObject.AddComponent<EventTrigger>();
            var trigger = button.gameObject.GetComponent<EventTrigger>();
            trigger.triggers.Add(getScrollTrigger());

            //CLICK
            var entryClick = new EventTrigger.Entry {eventID = EventTriggerType.PointerClick};
            entryClick.callback.AddListener(data => 
            {
                pressColor(button);

                if (!dragging) 
                {
                    dba.Click(data);
                } 
                else 
                {
                    dragging = false;
                }
            });
            trigger.triggers.Add(entryClick);

            //INIT DRAG
            var potEntry = new EventTrigger.Entry {eventID = EventTriggerType.InitializePotentialDrag};
            potEntry.callback.AddListener(data => 
            {
                if (button.interactable == false) 
                {
                    return;
                }

                dba.StartDrag(data);

                pressColor(button);
            });
            trigger.triggers.Add(potEntry);

            //DRAG
            var entryDrag = new EventTrigger.Entry {eventID = EventTriggerType.Drag};
            entryDrag.callback.AddListener(data => 
            {
                if (button.interactable == false) 
                {
                    return;
                }

                dragging = true;
                ((PointerEventData) data).useDragThreshold = false;
                MoveCtrlPlugin.cameraControl.isCursorLock = false;
                if (Singleton<GameCursor>.IsInstance()) 
                {
                    Singleton<GameCursor>.Instance.SetCursorLock(true);
                }

                dba.Drag(data);
            });
            trigger.triggers.Add(entryDrag);

            //END DRAG
            var entryEndDrag = new EventTrigger.Entry {eventID = EventTriggerType.EndDrag};
            entryEndDrag.callback.AddListener(data => 
            {
                if (button.interactable == false) 
                {
                    return;
                }

                dba.EndDrag(data);
            });
            trigger.triggers.Add(entryEndDrag);

            //BUTTON UP
            var entryPointerUp = new EventTrigger.Entry {eventID = EventTriggerType.PointerUp};
            entryPointerUp.callback.AddListener(data => 
            {
                if (button.interactable == false) 
                {
                    return;
                }

                MoveCtrlPlugin.cameraControl.isCursorLock = true;
                if (Singleton<GameCursor>.IsInstance())
                    Singleton<GameCursor>.Instance.SetCursorLock(false);
                liftColor(button);
            });
            trigger.triggers.Add(entryPointerUp);

            return button;
        }

        private static void pressColor(Button button) 
        {
            var cb = button.colors;
            HighlightColor = cb.highlightedColor;
            cb.highlightedColor = cb.pressedColor;
            button.colors = cb;
        }

        private static void liftColor(Button button) 
        {
            var cb = button.colors;
            cb.highlightedColor = HighlightColor;
            button.colors = cb;
            EventSystem.current.SetSelectedGameObject(null);
        }

        public static Slider slider(Slider slider, Action<float> action) 
        {
            slider.onValueChanged.AddListener(x => { action(x); });
            return slider;
        }

        internal static EventTrigger.Entry getScrollTrigger() {
            var scroll = new EventTrigger.Entry {eventID = EventTriggerType.Scroll};
            scroll.callback.AddListener(data => 
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) 
                {
                    return; //CTRL just messes up selection, better to do nothing
                }

                var scrollRate = ((PointerEventData) data).scrollDelta.y;

                var shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                if (FkManagerService.ActiveBone == null) 
                    return;
                
                if (scrollRate > 0) 
                {
                    if (shiftDown)
                        FkManagerService.multiUp();
                    else if (altDown)
                        FkManagerService.up();
                    else
                        FkManagerService.slideUp();
                }

                if (scrollRate < 0) 
                {
                    if (shiftDown)
                        FkManagerService.multiDown();
                    else if (altDown)
                        FkManagerService.down();
                    else
                        FkManagerService.slideDown();
                }
            });
            
            return scroll;
        }
    }
}