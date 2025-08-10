using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Core;
using SS3D.Data.Generated;
using UnityEngine.UI;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    /// <summary>
    /// Take one unit from a stack into the active hand. If the hand holds a compatible stack, merge into it.
    /// </summary>
    public sealed class StackTakeOneInteraction : Interaction
    {
        public override IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            // Instant interaction; no client-side lifecycle needed
            return null;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Take one";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            // Reuse the same icon as "Take" used elsewhere (e.g., Pickup)
            return Icon != null ? Icon : InteractionIcons.Take;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
                return false;

            // Target must be a stack with at least 2
            if (interactionEvent.Target is Item target && target.TryGetComponent(out Stackable stack))
            {
                if (stack.CurrentStackSize <= 1)
                    return false;

                // Source must be a hand
                if (interactionEvent.Source is Hand hand)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is not Hand hand || interactionEvent.Target is not Item target)
                return false;

            if (!target.TryGetComponent(out Stackable targetStack))
                return false;

            // If hand has a compatible stack, merge 1 unit into it
            Item held = hand.ItemInHand;
            if (held != null && held.TryGetComponent(out Stackable heldStack) && heldStack.CanStackWith(target))
            {
                int moved = heldStack.AddFrom(targetStack, 1);
                // Instant; do not continue the interaction loop
                if (moved > 0 && targetStack.CurrentStackSize <= 0)
                {
                    if (target.Container != null)
                    {
                        target.Container.RemoveItem(target);
                    }
                    target.Despawn(target.GameObject);
                }
                return false;
            }

            // Else if hand is empty, spawn a single item into the hand container
            if (hand.IsEmpty())
            {
                if (targetStack.Remove(1) <= 0)
                    return false;

                // Spawn a new single copy of the same item type into the hand container
                ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
                Item spawned = itemSystem.SpawnItemInContainer(target.Prefab.GameObject, hand.Container);
                // Ensure it is a single-unit stack if stackable
                if (spawned != null && spawned.TryGetComponent(out Stackable spawnedStack))
                {
                    // Set to 1 in case prefab default isn't 1
                    while (spawnedStack.CurrentStackSize > 1)
                    {
                        spawnedStack.Remove(spawnedStack.CurrentStackSize - 1);
                    }
                }
                // Cleanup source if emptied by the take
                if (targetStack.CurrentStackSize <= 0)
                {
                    if (target.Container != null)
                    {
                        target.Container.RemoveItem(target);
                    }
                    target.Despawn(target.GameObject);
                }
                // Instant; do not continue the interaction loop
                return false;
            }

            return false;
        }
    }
}


