using UnityEngine;
using TTH.Combat.Attributes;
using TTH.Combat.Runtime;
using TTH.Combat.Status;

public class PlayerRuntime : MonoBehaviour
{
    [SerializeField] private CharacterStatsSO characterStats;

    public AttributeSystem Attributes => characterAttributes;
    public CombatEntity Entity => characterEntity;
    public ResourcePool Resources => characterResources;
    public StatusSystem Statuses => characterStatus;

    private AttributeSystem characterAttributes;
    private CombatEntity characterEntity;
    private ResourcePool characterResources;
    private StatusSystem characterStatus;

    private StatBlock characterStatBlock; 

    private void Awake()
    {
        BuildRuntime();
    }

    private void BuildRuntime()
    {
        if (characterStats == null)
        {
            Debug.LogError("Assign Character Stats in the Inspector.");
            return;
        }

        characterStatBlock = new StatBlock();
        
        characterStatBlock.SetBase(AttributeId.ATK, characterStats.attack);
        characterStatBlock.SetBase(AttributeId.DEF, characterStats.defense);
        characterStatBlock.SetBase(AttributeId.DEX, characterStats.dexterity);
        characterStatBlock.SetBase(AttributeId.SPD, characterStats.speed);
        characterStatBlock.SetBase(AttributeId.VIT, characterStats.vitality);
        characterStatBlock.SetBase(AttributeId.WIS, characterStats.wisdom);
        characterStatBlock.SetBase(AttributeId.HP, characterStats.hitPoints);
        characterStatBlock.SetBase(AttributeId.MP, characterStats.magicPoints);

        characterAttributes = new AttributeSystem(characterStatBlock, null);
        characterResources = new ResourcePool(characterStats.hitPoints, characterStats.magicPoints);
        characterStatus = new StatusSystem();
        characterEntity = new CombatEntity(1, characterAttributes, characterStatus, null, characterResources);
    }
}
