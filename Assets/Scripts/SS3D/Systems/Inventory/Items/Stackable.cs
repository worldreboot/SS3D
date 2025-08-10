using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// Component to make an item stackable. Keeps counts synchronized and exposes merge/split helpers.
    /// </summary>
    [RequireComponent(typeof(Item))]
    public class Stackable : NetworkBehaviour
    {
        [SerializeField]
        [SyncVar]
        private int _maxStackSize = 10;

        [SerializeField]
        [SyncVar(OnChange = nameof(HandleCurrentStackSizeChanged))]
        private int _currentStackSize = 1;

        /// <summary>
        /// Optional list of visual duplicates to enable/disable based on count (index 0 corresponds to the 2nd item, etc.).
        /// If empty, visuals are ignored.
        /// </summary>
        [SerializeField]
        private List<GameObject> _visualCopies = new();

        public event Action<int> OnStackCountChanged;

        public int MaxStackSize => Mathf.Max(1, _maxStackSize);
        public int CurrentStackSize => Mathf.Max(0, _currentStackSize);
        public bool IsFull => CurrentStackSize >= MaxStackSize;

        private Item _item;

        private void Awake()
        {
            _item = GetComponent<Item>();
            _maxStackSize = Mathf.Max(1, _maxStackSize);
            _currentStackSize = Mathf.Clamp(_currentStackSize, 1, _maxStackSize);
            ApplyVisuals();
        }

        private void HandleCurrentStackSizeChanged(int oldValue, int newValue, bool asServer)
        {
            ApplyVisuals();
            OnStackCountChanged?.Invoke(newValue);
        }

        /// <summary>
        /// Returns true if this item can stack with the other item. Default: same Asset or same name.
        /// </summary>
        public bool CanStackWith(Item other)
        {
            if (other == null) return false;
            if (!other.TryGetComponent(out Stackable _)) return false;

            try
            {
                if (_item != null && other.Asset != null && _item.Asset != null)
                {
                    return Equals(_item.Asset, other.Asset);
                }
            }
            catch { }

            return string.Equals(_item?.name, other.name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Attempts to add up to amountToAdd units to this stack. Returns the number actually added.
        /// </summary>
        public int Add(int amountToAdd)
        {
            if (amountToAdd <= 0) return 0;
            int space = Mathf.Max(0, MaxStackSize - CurrentStackSize);
            int moved = Mathf.Min(space, amountToAdd);
            if (moved > 0)
            {
                _currentStackSize += moved;
            }
            return moved;
        }

        /// <summary>
        /// Removes up to amountToRemove units from this stack. Returns the number actually removed.
        /// </summary>
        public int Remove(int amountToRemove)
        {
            if (amountToRemove <= 0) return 0;
            int removed = Mathf.Min(amountToRemove, CurrentStackSize);
            if (removed > 0)
            {
                _currentStackSize -= removed;
            }
            return removed;
        }

        /// <summary>
        /// Moves up to maxTransfer units from source into this, returns how many were moved.
        /// </summary>
        public int AddFrom(Stackable source, int maxTransfer = int.MaxValue)
        {
            if (source == null || source == this || maxTransfer <= 0) return 0;
            if (!CanStackWith(source._item)) return 0;
            int transferable = Mathf.Min(maxTransfer, source.CurrentStackSize);
            if (transferable <= 0) return 0;
            int moved = Add(transferable);
            if (moved > 0)
            {
                source.Remove(moved);
            }
            return moved;
        }

        private void ApplyVisuals()
        {
            if (_visualCopies == null || _visualCopies.Count == 0) return;
            int enabledCount = Mathf.Clamp(CurrentStackSize - 1, 0, _visualCopies.Count);
            for (int i = 0; i < _visualCopies.Count; i++)
            {
                GameObject go = _visualCopies[i];
                if (go == null) continue;
                bool shouldEnable = i < enabledCount;
                if (go.activeSelf != shouldEnable)
                {
                    go.SetActive(shouldEnable);
                }
            }
        }
    }
}
