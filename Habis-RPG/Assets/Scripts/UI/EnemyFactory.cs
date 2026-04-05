using System.Collections.Generic;
using UnityEngine;
using HabisRPG.Core;
using HabisRPG.Character;

namespace HabisRPG.UI
{
    /// <summary>
    /// Generates enemy encounters based on region and player level.
    /// </summary>
    public static class EnemyFactory
    {
        private static readonly Dictionary<Region, string[]> RegionEnemies = new()
        {
            [Region.CursedForest] = new[] { "Cursed Wolf", "Shadow Sprite", "Rot Treant", "Dark Beetle" },
            [Region.RottingSwamp] = new[] { "Swamp Lurker", "Plague Frog", "Mire Crawler", "Bog Witch" },
            [Region.ShatteredMountain] = new[] { "Stone Golem", "Iron Eagle", "Frost Giant", "Cave Troll" },
            [Region.AbyssalDungeon] = new[] { "Void Wraith", "Shadow Knight", "Abyssal Serpent", "Dark Mage" },
            [Region.AncientRuins] = new[] { "Ancient Guardian", "Ruin Sentinel", "Arcane Construct", "Phantom" },
            [Region.VoidAbyss] = new[] { "Void Titan", "Abyss Stalker", "Null Weaver", "Entropy Fiend" },
        };

        public static List<HabisCharacter> GenerateEncounter(Region region, int playerLevel)
        {
            var enemies = new List<HabisCharacter>();
            int count = Random.Range(1, 4); // 1-3 enemies

            string[] names = RegionEnemies.ContainsKey(region)
                ? RegionEnemies[region]
                : new[] { "Unknown Creature" };

            for (int i = 0; i < count; i++)
            {
                string name = names[Random.Range(0, names.Length)];
                var enemyClass = (CharacterClass)Random.Range(0, 3);
                var enemy = new HabisCharacter(name, enemyClass);

                // Scale enemy to player level (+-2)
                int targetLevel = Mathf.Max(1, playerLevel + Random.Range(-2, 3));
                while (enemy.Data.Level < targetLevel)
                {
                    enemy.AddExperience(enemy.GetXPRequiredForLevel(enemy.Data.Level));
                }

                enemies.Add(enemy);
            }

            return enemies;
        }
    }
}
