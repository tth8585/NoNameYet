using TTH.Combat.Attributes;
using TTH.Combat.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Player2DController : MonoBehaviour
{
    private static readonly int AttackStateHash = Animator.StringToHash("Base Layer.Pawn_Interact Knife_Blue");
    private static readonly int AttackShortHash = Animator.StringToHash("Pawn_Interact Knife_Blue");
    private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.Pawn_Idle Knife_Blue");
    private static readonly int IdleShortHash = Animator.StringToHash("Pawn_Idle Knife_Blue");
    private static readonly int RunStateHash = Animator.StringToHash("Base Layer.Pawn_Run Knife_Blue");
    private static readonly int RunShortHash = Animator.StringToHash("Pawn_Run Knife_Blue");

    [SerializeField] private PlayerRuntime playerRuntime;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private SpriteRenderer characterSprite;
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.8f;

    private Enemy2DRuntime[] enemies;
    private float nextAttackTime;
    private bool attackAnimationPlaying;
    private Color originalSpriteColor;

    private sealed class BasicAttackProc : ICombatProc
    {
        private readonly float damage;

        public BasicAttackProc(float damage)
        {
            this.damage = damage;
        }

        public void OnHit(ref DamageContext context, CombatEntity attacker, CombatEntity defender)
        {
            context.baseDamage = damage;
            context.hasBaseDamage = true;
        }

        public void AfterHit(ref DamageContext context, CombatEntity attacker, CombatEntity defender) { }
        public void OnTurnStart(CombatEntity actor) { }
        public void OnTurnEnd(CombatEntity actor) { }
    }

    private void Awake()
    {
        if (playerRuntime == null)
            playerRuntime = GetComponent<PlayerRuntime>();
        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<Animator>();
        if (characterSprite == null)
            characterSprite = GetComponentInChildren<SpriteRenderer>();
        if (characterSprite != null)
            originalSpriteColor = characterSprite.color;
    }

    private void Start()
    {
        enemies = FindObjectsByType<Enemy2DRuntime>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        var movement = MoveFromKeyboard();
        UpdateAnimation(movement);
        UpdateLowHealthWarning();
        TryAutoAttack();
    }

    private void UpdateLowHealthWarning()
    {
        if (characterSprite == null || playerRuntime?.Resources == null || playerRuntime.Attributes == null)
            return;

        float maxHealth = playerRuntime.Attributes.Get(AttributeId.HP);
        bool isLowHealth = maxHealth > 0f && playerRuntime.Resources.CurrentHP <= maxHealth * lowHealthThreshold;
        if (!isLowHealth)
        {
            characterSprite.color = originalSpriteColor;
            return;
        }

        float redPulse = Mathf.PingPong(Time.time * 4f, 1f);
        characterSprite.color = Color.Lerp(originalSpriteColor, Color.red, redPulse);
    }

    private Vector2 MoveFromKeyboard()
    {
        if (Keyboard.current == null)
            return Vector2.zero;

        var movement = Vector2.zero;
        if (Keyboard.current.aKey.isPressed) movement.x -= 1f;
        if (Keyboard.current.dKey.isPressed) movement.x += 1f;
        if (Keyboard.current.sKey.isPressed) movement.y -= 1f;
        if (Keyboard.current.wKey.isPressed) movement.y += 1f;

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        var moveSpeed = playerRuntime != null ? playerRuntime.ActualMoveSpeed : 4f;
        transform.position += (Vector3)(movement * moveSpeed * Time.deltaTime);
        return movement;
    }

    private void UpdateAnimation(Vector2 movement)
    {
        if (characterAnimator == null)
            return;

        var currentState = characterAnimator.GetCurrentAnimatorStateInfo(0);
        if (attackAnimationPlaying && currentState.shortNameHash == AttackShortHash && currentState.normalizedTime < 1f)
            return;

        attackAnimationPlaying = false;
        var desiredStateHash = movement.sqrMagnitude > 0f ? RunStateHash : IdleStateHash;
        var desiredShortHash = movement.sqrMagnitude > 0f ? RunShortHash : IdleShortHash;
        if (currentState.shortNameHash != desiredShortHash)
            characterAnimator.Play(desiredStateHash, 0, 0f);
    }

    private void PlayAttackAnimation()
    {
        if (characterAnimator == null)
            return;

        attackAnimationPlaying = true;
        characterAnimator.Play(AttackStateHash, 0, 0f);
    }

    private void TryAutoAttack()
    {
        if (playerRuntime?.Entity == null || playerRuntime.AttacksPerSecond <= 0f || Time.time < nextAttackTime)
            return;

        var range = playerRuntime.BasicAttackRange;
        Enemy2DRuntime target = null;
        var closestDistance = range;

        if (enemies == null)
            enemies = FindObjectsByType<Enemy2DRuntime>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || enemy.Entity == null)
                continue;

            var distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                target = enemy;
            }
        }

        if (target == null)
            return;

        var damage = playerRuntime.RollBasicAttackDamage();
        var combat = new CombatSystem(new CombatEvents(), new BasicAttackProc(damage));
        var result = combat.HandleHit(new HitEvent(
            playerRuntime.Entity,
            target.Entity,
            target.transform.position,
            this));

        if (!result.isValidHit)
            return;

        PlayAttackAnimation();
        nextAttackTime = Time.time + 1f / playerRuntime.AttacksPerSecond;
        Debug.Log($"Player hit {target.name} for {result.finalDamage:0.#}. HP: {result.defenderHPAfter:0.#}.", target);
        if (result.didKill)
            target.Defeat();
    }
}