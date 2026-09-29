using UnityEngine;
using TTH.Combat.Attributes;
using TTH.Combat.Runtime;
using TTH.Combat.Status;

public sealed class Enemy2DRuntime : MonoBehaviour
{
    [SerializeField] private EnemyStatsSO enemyStats;

    public EnemyStatsSO Stats => enemyStats;
    public CombatEntity Entity { get; private set; }
    public float CurrentHP => Entity?.Resources?.CurrentHP ?? 0f;
    public bool IsDead => Entity == null || Entity.Resources == null || Entity.Resources.IsDead;

    private void Awake()
    {
        if (enemyStats == null)
        {
            Debug.LogError("Assign Enemy Stats to Enemy2D.", this);
            return;
        }

        var statBlock = new StatBlock();
        statBlock.SetBase(AttributeId.ATK, enemyStats.attack);
        statBlock.SetBase(AttributeId.DEF, enemyStats.defense);
        statBlock.SetBase(AttributeId.DEX, enemyStats.dexterity);
        statBlock.SetBase(AttributeId.HP, enemyStats.maxHealth);
        statBlock.SetBase(AttributeId.MP, enemyStats.maxMana);

        var attributes = new AttributeSystem(statBlock, null);
        var resources = new ResourcePool(enemyStats.maxHealth, enemyStats.maxMana);
        Entity = new CombatEntity(GetInstanceID(), attributes, new StatusSystem(), null, resources);
    }

    public void Defeat()
    {
        if (!IsDead)
            return;

        gameObject.SetActive(false);
    }
}