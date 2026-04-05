// ============================================================================
// Habis RPG — Save Manager
// Handles file I/O for save/load operations
// ============================================================================

using System;
using System.IO;
using UnityEngine;

namespace HabisRPG.Managers
{
    public static class SaveManager
    {
        private const string SAVE_FOLDER = "HabisRPG_Saves";
        private const string SAVE_EXTENSION = ".habis";
        private const int MAX_SAVE_SLOTS = 3;

        private static string SaveDirectory =>
            Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

        /// <summary>
        /// Ensure save directory exists
        /// </summary>
        public static void Initialize()
        {
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);
        }

        /// <summary>
        /// Save game to a specific slot (1-3)
        /// </summary>
        public static bool SaveToSlot(int slot, string jsonData)
        {
            if (slot < 1 || slot > MAX_SAVE_SLOTS) return false;

            try
            {
                Initialize();
                string path = GetSlotPath(slot);
                File.WriteAllText(path, jsonData);
                Debug.Log($"[SaveManager] Saved to slot {slot}: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load game from a specific slot
        /// </summary>
        public static string LoadFromSlot(int slot)
        {
            if (slot < 1 || slot > MAX_SAVE_SLOTS) return null;

            try
            {
                string path = GetSlotPath(slot);
                if (!File.Exists(path)) return null;

                string json = File.ReadAllText(path);
                Debug.Log($"[SaveManager] Loaded from slot {slot}");
                return json;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if a save slot has data
        /// </summary>
        public static bool SlotExists(int slot)
        {
            return File.Exists(GetSlotPath(slot));
        }

        /// <summary>
        /// Get save info without loading full data
        /// </summary>
        public static SaveSlotInfo GetSlotInfo(int slot)
        {
            string path = GetSlotPath(slot);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                var save = JsonUtility.FromJson<GameManager.SaveData>(json);

                return new SaveSlotInfo
                {
                    Slot = slot,
                    PlayerName = save.PlayerData.Name,
                    PlayerLevel = save.PlayerData.Level,
                    PlayerClass = save.PlayerData.PrimaryClass.ToString(),
                    Region = save.CurrentRegion.ToString(),
                    SaveTime = save.SaveTime,
                    FileSize = new FileInfo(path).Length
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Delete a save slot
        /// </summary>
        public static bool DeleteSlot(int slot)
        {
            try
            {
                string path = GetSlotPath(slot);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Delete failed: {ex.Message}");
                return false;
            }
        }

        private static string GetSlotPath(int slot)
        {
            return Path.Combine(SaveDirectory, $"save_slot_{slot}{SAVE_EXTENSION}");
        }
    }

    [Serializable]
    public class SaveSlotInfo
    {
        public int Slot;
        public string PlayerName;
        public int PlayerLevel;
        public string PlayerClass;
        public string Region;
        public string SaveTime;
        public long FileSize;
    }
}
