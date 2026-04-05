// ============================================================================
// Habis RPG — Core Game Enums & Data Structures
// Used across all systems. Import via: using HabisRPG.Core;
// ============================================================================

namespace HabisRPG.Core
{
    // ── CLASS & SUBCLASS ──
    public enum CharacterClass
    {
        Warrior,
        Mage,
        Rogue
    }

    public enum HybridClass
    {
        None,
        Spellknight,  // Warrior + Mage
        Duelist,       // Warrior + Rogue
        Trickster      // Mage + Rogue
    }

    // ── STATS ──
    public enum StatType
    {
        STR,  // Strength — Physical damage
        INT,  // Intelligence — Magical damage
        DEX,  // Dexterity — Accuracy & Evasion
        VIT,  // Vitality — HP pool
        SPD,  // Speed — Turn order
        CRI,  // Critical — Crit chance
        MAG,  // Magic — Spell power
        DEF,  // Defense — Damage reduction
        ACC,  // Accuracy — Hit chance
        EVA   // Evasion — Dodge chance
    }

    // ── ITEMS ──
    public enum ItemRarity
    {
        Common,     // Gray  — 60% drop
        Rare,       // Blue  — 25% drop
        Epic,       // Purple — 12% drop
        Legendary   // Gold  — 3% drop
    }

    public enum ItemType
    {
        Weapon,
        Helmet,
        Chest,
        Legs,
        Feet,
        Gloves,
        Ring,
        Amulet,
        Bracelet,
        Anklet,
        Consumable,
        CraftingMaterial
    }

    public enum WeaponType
    {
        Sword,
        Axe,
        Bow,
        Staff,
        SpellOrb,
        Daggers
    }

    public enum EquipSlot
    {
        MainHand,
        Helmet,
        Chest,
        Legs,
        Feet,
        Gloves,
        Ring1,
        Ring2,
        Amulet,
        Bracelet,
        Anklet
    }

    // ── COMBAT ──
    public enum CombatAction
    {
        BasicAttack,
        UseSkill,
        UseItem,
        Defend,
        Flee
    }

    public enum DamageType
    {
        Physical,
        Magical,
        True  // Ignores defense
    }

    public enum ElementType
    {
        None,
        Fire,
        Ice,
        Lightning,
        Poison,
        Void
    }

    // ── STATUS EFFECTS ──
    public enum StatusEffectType
    {
        // Buffs
        Haste,
        Barrier,
        Berserk,
        Regen,
        ArcaneShield,
        CriticalBoost,

        // Debuffs
        Poison,
        Burn,
        Stun,
        Slow,
        Weaken,
        Blind,
        Curse,
        Confuse
    }

    public enum StatusCategory
    {
        Buff,
        Debuff
    }

    // ── ENERGY (Unified Resource) ──
    public enum EnergyFlavor
    {
        Rage,        // Warrior — builds on hit
        Arcana,      // Mage — passive regen + crit boost
        ComboPoints  // Rogue — earned by attacks
    }

    // ── WORLD ──
    public enum Region
    {
        CursedForest,
        RottingSwamp,
        ShatteredMountain,
        AbyssalDungeon,
        AncientRuins,
        VoidAbyss
    }

    public enum EnemyTier
    {
        Basic,
        Advanced,
        Elite,
        Boss,
        SecretBoss
    }

    // ── COMPANION ──
    public enum CompanionAIPreset
    {
        Aggressive,
        Defensive,
        Balanced,
        Support
    }

    // ── ECONOMY ──
    public enum CurrencyType
    {
        Gold,
        VoidShards,
        SigilTokens  // Premium
    }

    // ── SKILL TREE ──
    public enum SkillTreeBranch
    {
        // Warrior
        MeleeMastery,
        TankDefensive,
        BerserkAggressive,

        // Mage
        Elemental,
        Support,
        ArcaneArtillery,

        // Rogue
        Assassination,
        Evasion,
        Control
    }
}
