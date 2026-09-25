using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TTH.Combat.Attributes;
using TTH.Game.Inventory;
using TTH.Game.Inventory.Tests;

namespace TTH.Game.Inventory.Tests.Editor
{
    public static class InventoryAffixTestSceneSetup
    {
        private const string FolderPath = "Assets/_Dev/Tests/InventoryAffixTest";
        private const string AssetPath = FolderPath + "/Item_rusty_sword.asset";
        private const string PoolPath = FolderPath + "/Weapon_AffixPool.asset";
        private const string ScenePath = FolderPath + "/InventoryAffixTest.unity";

        [MenuItem("TTH/Tests/Create Inventory Affix Test Scene")]
        public static void CreateTestScene()
        {
            EnsureFolder();
            RandomAffixPoolSO pool = CreateWeaponAffixPool();
            ItemDefinitionSO sword = CreateRustySwordAsset(pool);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var testObject = new GameObject("InventoryAffixTest");
            var runner = testObject.AddComponent<InventoryAffixTestRunner>();
            runner.rustySword = sword;
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = testObject;
            Debug.Log($"[InventoryAffixTest] Created scene: {ScenePath}");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Dev/Tests/InventoryAffixTest"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Dev/Tests"))
                    AssetDatabase.CreateFolder("Assets/_Dev", "Tests");
                AssetDatabase.CreateFolder("Assets/_Dev/Tests", "InventoryAffixTest");
            }
        }

        private static RandomAffixPoolSO CreateWeaponAffixPool()
        {
            var pool = AssetDatabase.LoadAssetAtPath<RandomAffixPoolSO>(PoolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<RandomAffixPoolSO>();
                AssetDatabase.CreateAsset(pool, PoolPath);
            }

            pool.prefixAffixAssets = new[]
            {
                CreateAffixAsset("hp", Prefix("hp", "HP", false, 10f, 20f, 30f)),
                CreateAffixAsset("mp", Prefix("mp", "MP", false, 10f, 20f, 30f)),
                CreateAffixAsset("def", Prefix("def", "DEF", false, 5f, 10f, 15f)),
                CreateAffixAsset("atk", Prefix("atk", "ATK", false, 5f, 10f, 15f))
            };
            pool.suffixAffixAssets = new[]
            {
                CreateAffixAsset("wis", Suffix("wis", "WIS", false, 5f, 10f, 15f)),
                CreateAffixAsset("vit", Suffix("vit", "VIT", false, 5f, 10f, 15f)),
                CreateAffixAsset("dex", Suffix("dex", "DEX", false, 5f, 10f, 15f)),
                CreateAffixAsset("spd", Suffix("spd", "SPD", false, 5f, 10f, 15f))
            };
            EditorUtility.SetDirty(pool);
            AssetDatabase.SaveAssets();
            return pool;
        }

        private static AffixDefinitionSO CreateAffixAsset(string id, RandomAffixDefinition definition)
        {
            string path = $"{FolderPath}/Affix_{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<AffixDefinitionSO>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AffixDefinitionSO>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.affixId = definition.affixId;
            asset.affixFamilyId = definition.affixFamilyId;
            asset.displayName = definition.displayName;
            asset.slotType = definition.slotType;
            asset.isPercent = definition.isPercent;
            asset.effectType = definition.effectType;
            asset.attribute = definition.attribute;
            asset.operation = definition.operation;
            asset.tiers = definition.tiers;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ItemDefinitionSO CreateRustySwordAsset(RandomAffixPoolSO pool)
        {
            var sword = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>(AssetPath);
            if (sword == null)
            {
                sword = ScriptableObject.CreateInstance<ItemDefinitionSO>();
                AssetDatabase.CreateAsset(sword, AssetPath);
            }

            sword.itemId = "Item_rusty_sword";
            sword.displayName = "Rusty Sword";
            sword.description = "Test weapon with weighted prefix and suffix affixes.";
            sword.itemType = ItemType.Equipment;
            sword.rarity = ItemRarity.Common;
            sword.maxStack = 1;
            sword.tags = new[] { "Weapon", "Sword", "Melee" };
            sword.equipmentSlotId = "Weapon";
            sword.randomAffixRules = new RandomAffixRules
            {
                minRandomAffixes = 1,
                maxRandomAffixes = 4,
                minPrefixAffixes = 0,
                maxPrefixAffixes = 2,
                minSuffixAffixes = 0,
                maxSuffixAffixes = 2
            };
            sword.randomAffixPool = pool;

            EditorUtility.SetDirty(sword);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sword;
        }

        private static RandomAffixDefinition Prefix(string id, string displayName, bool isPercent,
            float t1, float t2, float t3)
        {
            return Affix(id, displayName, AffixSlotType.Prefix, isPercent, t1, t2, t3);
        }

        private static RandomAffixDefinition Suffix(string id, string displayName, bool isPercent,
            float t1, float t2, float t3)
        {
            return Affix(id, displayName, AffixSlotType.Suffix, isPercent, t1, t2, t3);
        }

        private static RandomAffixDefinition Affix(string id, string displayName, AffixSlotType slot,
            bool isPercent, float t1, float t2, float t3)
        {
            return new RandomAffixDefinition
            {
                affixId = id,
                affixFamilyId = id,
                displayName = displayName,
                slotType = slot,
                isPercent = isPercent,
                effectType = AttributeFor(id).HasValue ? AffixEffectType.Attribute : AffixEffectType.Gameplay,
                attribute = AttributeFor(id) ?? AttributeId.ATK,
                operation = OperationFor(id),
                gameplayModifier = GameplayFor(id),
                tiers = new[]
                {
                    new AffixTierDefinition { tierId = "T1", value = t1, weight = 50f },
                    new AffixTierDefinition { tierId = "T2", value = t2, weight = 40f },
                    new AffixTierDefinition { tierId = "T3", value = t3, weight = 10f }
                }
            };
        }

        private static AttributeId? AttributeFor(string id)
        {
            switch (id)
            {
                case "hp": return AttributeId.HP;
                case "mp": return AttributeId.MP;
                case "atk": return AttributeId.ATK;
                case "def": return AttributeId.DEF;
                case "spd": return AttributeId.SPD;
                case "wis": return AttributeId.WIS;
                case "vit": return AttributeId.VIT;
                case "dex": return AttributeId.DEX;
                default: return null;
            }
        }

        private static ModifierOp OperationFor(string id)
        {
            return ModifierOp.Add;
        }

        private static GameplayModifierType GameplayFor(string id)
        {
            return GameplayModifierType.none;
        }
    }
}