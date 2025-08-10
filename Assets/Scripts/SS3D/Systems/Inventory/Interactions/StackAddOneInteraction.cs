using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Data.Generated;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    /// <summary>
    /// Add one unit from the item in hand into the target stack (if compatible and not full).
    /// </summary>
    public sealed class StackAddOneInteraction : Interaction
    {
        public override IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Add one";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            // Reuse a discard-style icon for adding (matches drop/drag visuals)
            return Icon != null ? Icon : InteractionIcons.Discard;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
                return false;

            if (interactionEvent.Source is Hand hand && interactionEvent.Target is Item target)
            {
                Item held = hand.ItemInHand;
                if (held == null) return false;
                if (!target.TryGetComponent(out Stackable targetStack)) return false;
                if (!held.TryGetComponent(out Stackable heldStack)) return false;
                if (!targetStack.CanStackWith(held)) return false;
                return !targetStack.IsFull && heldStack.CurrentStackSize >= 1;
            }
            return false;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is not Hand hand || interactionEvent.Target is not Item target)
                return false;

            Item held = hand.ItemInHand;
            if (held == null) return false;
            if (!target.TryGetComponent(out Stackable targetStack)) return false;
            if (!held.TryGetComponent(out Stackable heldStack)) return false;
            if (!targetStack.CanStackWith(held)) return false;

            int moved = targetStack.AddFrom(heldStack, 1);

            // If held stack becomes empty, remove from hand container and despawn
            if (held.TryGetComponent(out Stackable hs) && hs.CurrentStackSize <= 0)
            {
                if (hand.Container.FindItem(held, out int _))
                {
                    hand.Container.RemoveItem(held);
                }
                held.Despawn(held.GameObject);
            }
            // Instant; do not continue the interaction loop
            return false;
        }
    }
}


