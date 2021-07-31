using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoveController {
    public class ButtonManager {
        private FkManagerService _fkManagerService;

        Color HighlightColor = Color.blue;
        Color PressedColor = Color.red;

        internal ButtonManager(FkManagerService fkManagerService) {
            _fkManagerService = fkManagerService;
        }

        public Button ClickButton(Button button, Action<BaseEventData> action) {
            button.gameObject.AddComponent<EventTrigger>();
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            trigger.triggers.Add(getScrollTrigger());

            //CLICK
            EventTrigger.Entry entryClick = new EventTrigger.Entry();
            entryClick.eventID = EventTriggerType.PointerClick;
            entryClick.callback.AddListener((data) => {
                pressColor(button);
                action(data);
            });
            trigger.triggers.Add(entryClick);

            //BUTTON UP
            EventTrigger.Entry entryPointerUp = new EventTrigger.Entry();
            entryPointerUp.eventID = EventTriggerType.PointerUp;
            entryPointerUp.callback.AddListener((data) => { liftColor(button); });
            trigger.triggers.Add(entryPointerUp);
            return button;
        }

        public Button DragButton(Button button, DragButtonAction dba) {
            bool dragging = false;

            button.gameObject.AddComponent<EventTrigger>();
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            trigger.triggers.Add(getScrollTrigger());

            //CLICK
            EventTrigger.Entry entryClick = new EventTrigger.Entry();
            entryClick.eventID = EventTriggerType.PointerClick;
            entryClick.callback.AddListener((data) => {
                pressColor(button);

                if (!dragging) {
                    dba.Click(data);
                } else {
                    dragging = false;
                }
            });
            trigger.triggers.Add(entryClick);

            //INIT DRAG
            EventTrigger.Entry potEntry = new EventTrigger.Entry();
            potEntry.eventID = EventTriggerType.InitializePotentialDrag;
            potEntry.callback.AddListener((data) => {
                if (button.interactable == false) {
                    return;
                }

                dba.StartDrag(data);

                pressColor(button);
            });
            trigger.triggers.Add(potEntry);

            //DRAG
            EventTrigger.Entry entryDrag = new EventTrigger.Entry();
            entryDrag.eventID = EventTriggerType.Drag;
            entryDrag.callback.AddListener((data) => {
                if (button.interactable == false) {
                    return;
                }

                dragging = true;
                ((PointerEventData) data).useDragThreshold = false;
                MoveCtrlPlugin.cameraControl.isCursorLock = false;
                if (Singleton<GameCursor>.IsInstance()) {
                    Singleton<GameCursor>.Instance.SetCursorLock(true);
                }

                dba.Drag(data);
            });
            trigger.triggers.Add(entryDrag);

            //END DRAG
            EventTrigger.Entry entryEndDrag = new EventTrigger.Entry();
            entryEndDrag.eventID = EventTriggerType.EndDrag;
            entryEndDrag.callback.AddListener((data) => {
                if (button.interactable == false) {
                    return;
                }

                dba.EndDrag(data);
            });
            trigger.triggers.Add(entryEndDrag);

            //BUTTON UP
            EventTrigger.Entry entryPointerUp = new EventTrigger.Entry();
            entryPointerUp.eventID = EventTriggerType.PointerUp;
            entryPointerUp.callback.AddListener((data) => {
                if (button.interactable == false) {
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

        private void pressColor(Button button) {
            ColorBlock cb = button.colors;
            HighlightColor = cb.highlightedColor;
            cb.highlightedColor = cb.pressedColor;
            button.colors = cb;
        }

        private void liftColor(Button button) {
            ColorBlock cb = button.colors;
            cb.highlightedColor = HighlightColor;
            button.colors = cb;
            EventSystem.current.SetSelectedGameObject(null);
        }

        public Slider slider(Slider slider, Action<float> action) {
            slider.onValueChanged.AddListener((x) => { action(x); });
            return slider;
        }

        internal EventTrigger.Entry getScrollTrigger() {
            EventTrigger.Entry scroll = new EventTrigger.Entry();
            scroll.eventID = EventTriggerType.Scroll;
            scroll.callback.AddListener((data) => {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                    return; //CTRL just messes up selection, better to do nothing
                }

                float scrollRate = ((PointerEventData) data).scrollDelta.y;

                bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                if (_fkManagerService.ActiveBone != null) {
                    if (scrollRate > 0) {
                        if (shiftDown)
                            _fkManagerService.multiUp();
                        else if (altDown)
                            _fkManagerService.up();
                        else
                            _fkManagerService.slideUp();
                    }

                    if (scrollRate < 0) {
                        if (shiftDown)
                            _fkManagerService.multiDown();
                        else if (altDown)
                            _fkManagerService.down();
                        else
                            _fkManagerService.slideDown();
                    }
                }
            });
            return scroll;
        }
    }
}