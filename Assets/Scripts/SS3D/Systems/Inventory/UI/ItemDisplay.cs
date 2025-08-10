using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// ItemDisplay is in charge of displaying correctly an item sprite in the UI.
    /// It allows actions such as dragging the sprite around the screen.
    /// </summary>
    public class ItemDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler
    {
        public Image ItemImage;
        [NonSerialized] public bool ShouldDrop;
        [NonSerialized] public Vector3 OldPosition;

        protected InventoryDisplayElement InventoryDisplayElement;

        [SerializeField] private Item _item;
        private Transform _oldParent;
        private Vector2 _startMousePosition;
        private Vector3 _startPosition;
        private Image _slotImage;
        private Outline _outlineInner;
        private Outline _outlineOuter;


        public Item Item
        {
            get => _item;
            set
            {
                _item = value;
                UpdateDisplay();
                HookStackable();
            }
        }

        public void Start()
        {
            _slotImage = GetComponent<Image>();
            if (!_outlineInner)
            {
                _outlineInner = ItemImage.gameObject.AddComponent<Outline>();
                _outlineInner.effectColor = new Color(0, 0, 0, 0.4f);
                _outlineInner.effectDistance = new Vector2(0.6f, 0.6f);
            }
            if (!_outlineOuter)
            {
                _outlineOuter = ItemImage.gameObject.AddComponent<Outline>();
                _outlineOuter.effectColor = new Color(0, 0, 0, 0.2f);
                _outlineOuter.effectDistance = new Vector2(0.8f, 0.8f);
            }
            if (_item != null)
            {
                UpdateDisplay();
                HookStackable();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to avoid callbacks after UI was destroyed
            if (_stackable != null)
            {
                _stackable.OnStackCountChanged -= HandleStackCountChanged;
                _stackable = null;
            }
        }

        public virtual void OnDropAccepted() { }

        public void OnPointerDown(PointerEventData eventData)
        {
            _startPosition = transform.position;
            _startMousePosition = Mouse.current.position.ReadValue();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Somehow, itemdisplay hides the other IPointerClickHandler in it's parent, so the event OnpointerClick is never
            // called, for exemple in SingleItemContainerSlot. That's why we need to call the events on the parent from there.
            var pointerDownHandlers = transform.parent.GetComponentsInParent<IPointerClickHandler>();
            foreach (var pointerHandler in pointerDownHandlers)
            {
                pointerHandler?.OnPointerClick(eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Only allow to drag with a left click.
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _oldParent = transform.parent;
            if (InventoryDisplayElement == null)
            {
                InventoryDisplayElement = _oldParent.GetComponentInParent<InventoryDisplayElement>();
            }

            OldPosition = GetComponent<RectTransform>().localPosition;
            Vector3 tempPosition = transform.position;
            transform.SetParent(transform.root, false);
            transform.position = tempPosition;

            _slotImage.raycastTarget = false;
            ShouldDrop = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Only allow to drag with a left click.
            if (eventData.button != PointerEventData.InputButton.Left) return;

            Vector3 diff = Mouse.current.position.ReadValue() - _startMousePosition;
            transform.position = _startPosition + diff;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Only allow to drag with a left click.
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _slotImage.raycastTarget = true;

            transform.SetParent(_oldParent, false);
            GetComponent<RectTransform>().localPosition = OldPosition;

            if (ShouldDrop)
            {
                OnDropAccepted();
                return;
            }

            // If the raycast did not hit any element from the UI, drop the item out of the inventory.
            GameObject o = eventData.pointerCurrentRaycast.gameObject;
            if (o == null)
            {
                GetComponentInParent<InventoryDisplayElement>().DropItemOutside(Item);
            }
        }

        private void UpdateDisplay()
        {
            if(ItemImage == null)
            {
                return;
            }
            ItemImage.sprite = Item != null ? Item.ItemSprite : null;

            Color imageColor = ItemImage.color;
            imageColor.a = ItemImage.sprite != null ? 255 : 0;
            ItemImage.color = imageColor;

            UpdateCountBadge();
        }

		public void MakeVisible(bool visible)
		{
			Image[] images = GetComponentsInChildren<Image>();
			foreach (Image image in images)
			{
				image.enabled = visible;
			}
		}

        // -------- Stack UI badge --------
        [SerializeField] private Text _countText;
        private Stackable _stackable;

        private void HookStackable()
        {
            if (_stackable != null)
            {
                _stackable.OnStackCountChanged -= HandleStackCountChanged;
                _stackable = null;
            }
            if (_item != null && _item.TryGetComponent(out Stackable stack))
            {
                _stackable = stack;
                _stackable.OnStackCountChanged += HandleStackCountChanged;
                UpdateCountBadge();
            }
            else
            {
                UpdateCountBadge();
            }
        }

        private void HandleStackCountChanged(int newValue)
        {
            UpdateCountBadge();
        }

        private void UpdateCountBadge()
        {
            // Bail out if UI was destroyed or not set up
            if (this == null || ItemImage == null)
            {
                return;
            }

            if (_countText == null)
            {
                // Create a small text badge in bottom-right if not wired in prefab.
                var go = new GameObject("CountBadge", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(ItemImage.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = new Vector2(-4, 4);
                _countText = go.GetComponent<Text>();
                _countText.alignment = TextAnchor.LowerRight;
                _countText.color = Color.white;
                _countText.fontSize = 14;
                _countText.raycastTarget = false;
                // Try to inherit a font from existing Image canvas.
                _countText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                var outline = go.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1, 1);
            }

            int value = 1;
            if (_item != null && _item.TryGetComponent(out Stackable st))
            {
                value = Mathf.Max(1, st.CurrentStackSize);
            }

            bool show = value > 1;
            if (_countText != null)
            {
                _countText.text = show ? value.ToString() : string.Empty;
                _countText.enabled = show;
            }
        }

	}
}