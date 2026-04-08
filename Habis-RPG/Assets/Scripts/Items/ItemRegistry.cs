// ============================================================================
// Habis RPG — Item Registry
// Global runtime lookup table: item Id → ItemData.
// Used by HabisCharacter.GetEffectiveStats() to resolve equipped items.
// Populated automatically by Inventory.AddItem().
// ============================================================================

using System.Collections.Generic;

namespace HabisRPG.Items
{
    public static class ItemRegistry
    {
        private static readonly Dictionary<string, ItemData> _map = new();

        public static void Register(ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) return;
            _map[item.Id] = item;
        }

        public static void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            _map.Remove(id);
        }

        public static ItemData Get(string id) =>
            string.IsNullOrEmpty(id) ? null :
            _map.TryGetValue(id, out var i) ? i : null;

        public static void Clear() => _map.Clear();

        public static int Count => _map.Count;
    }
}
