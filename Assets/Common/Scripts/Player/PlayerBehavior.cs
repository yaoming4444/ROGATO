using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    public class PlayerBehavior : MonoBehaviour
    {
        protected static readonly int DEATH_HASH = "Death".GetHashCode();
        protected static readonly int REVIVE_HASH = "Revive".GetHashCode();
        protected static readonly int RECEIVING_DAMAGE_HASH = "Receiving Damage".GetHashCode();

        protected static PlayerBehavior instance;
        public static PlayerBehavior Player => instance;

        [Header("Character (Single Prefab)")]
        [SerializeField] private GameObject characterPrefab; // <-- ТВОЙ Spine prefab (единственный)
        [SerializeField] private bool applyVisualsFromPlayerState = true;
        [SerializeField] protected CharactersDatabase charactersDatabase; // TO EDIT
        protected CharactersSave charactersSave;
        public CharacterData Data { get; set; }

        [Header("Base Stats (no database)")]
        [SerializeField] private float baseHP = 100f;
        [SerializeField] private float baseDamage = 10f;

        [Header("Stats")]
        [SerializeField, Min(0.01f)] protected float speed = 2;
        [SerializeField, Min(0.1f)] protected float defaultMagnetRadius = 0.75f;
        [SerializeField, Min(1f)] protected float xpMultiplier = 1;
        [SerializeField, Range(0.1f, 1f)] protected float cooldownMultiplier = 1;
        [SerializeField, Range(0, 100)] protected int initialDamageReductionPercent = 0;
        [SerializeField, Min(1f)] protected float initialProjectileSpeedMultiplier = 1;
        [SerializeField, Min(1f)] protected float initialSizeMultiplier = 1f;
        [SerializeField, Min(1f)] protected float initialDurationMultiplier = 1f;
        [SerializeField, Min(1f)] protected float initialGoldMultiplier = 1;
        [SerializeField, Range(0f, 1f)] protected float initialCritChanceMultiplier = 0.2f;
        [SerializeField, Min(1f)] protected float initialCritDamageMultiplier = 1.5f;
        [SerializeField, Range(0f, 0.6f)] protected float initialDodgeMultiplier = 0.2f;

        // ===== Rolled Card Bonus =====
        [Header("Rolled Card Bonus")]
        [SerializeField] private bool useRolledCardBonuses = true;

        private float rolledDamageBonus;
        private float rolledHpBonus;
        private float rolledMoveSpeedBonus;
        private float rolledCritChanceBonus;
        private float rolledCritDamageBonus;
        private float rolledMagnetRadiusBonus;

        [Header("References")]
        [SerializeField] protected PlayerHealthbarUI healthbar;
        [SerializeField] protected Transform centerPoint;
        [SerializeField] protected PlayerEnemyCollisionHelper collisionHelper;

        public static Transform CenterTransform => instance.centerPoint;

        public static Vector2 CenterPosition
        {
            get
            {
                if (instance._character != null && instance._character.CenterTransform != null)
                    return instance._character.CenterTransform.position;

                return instance.centerPoint.position;
            }
        }

        [Header("Death and Revive")]
        [SerializeField] protected ParticleSystem reviveParticle;

        [Space]
        [SerializeField] protected SpriteRenderer reviveBackgroundSpriteRenderer;
        [SerializeField, Range(0, 1)] protected float reviveBackgroundAlpha;
        [SerializeField, Range(0, 1)] protected float reviveBackgroundSpawnDelay;
        [SerializeField, Range(0, 1)] protected float reviveBackgroundHideDelay;

        [Space]
        [SerializeField] protected SpriteRenderer reviveBottomSpriteRenderer;
        [SerializeField, Range(0, 1)] protected float reviveBottomAlpha;
        [SerializeField, Range(0, 1)] protected float reviveBottomSpawnDelay;
        [SerializeField, Range(0, 1)] protected float reviveBottomHideDelay;

        [Header("Other")]
        [SerializeField] protected Vector2 fenceOffset;
        [SerializeField] protected Color hitColor;
        [SerializeField] protected float enemyInsideDamageInterval = 2f;

        public event UnityAction onPlayerDied;

        public float Damage { get; protected set; }
        public float MagnetRadiusSqr { get; protected set; }
        public float Speed { get; protected set; }

        public float XPMultiplier { get; protected set; }
        public float CooldownMultiplier { get; protected set; }
        public float DamageReductionMultiplier { get; protected set; }
        public float ProjectileSpeedMultiplier { get; protected set; }
        public float SizeMultiplier { get; protected set; }
        public float DurationMultiplier { get; protected set; }
        public float GoldMultiplier { get; protected set; }
        public float CritChance { get; protected set; }
        public float CritDamage { get; protected set; }
        public float Dodge { get; protected set; }

        public Vector2 LookDirection { get; protected set; }
        public bool IsMovingAlowed { get; set; }

        protected bool invincible = false;

        protected List<EnemyBehavior> enemiesInside = new List<EnemyBehavior>();

        // ===== Character runtime =====
        protected ICharacterBehavior _character;
        private GameCore.Visual.PartsManagerStateBinder _binder;

        // ===== Equipment Bonus (from GameCore) =====
        [Header("Equipment Bonus (from GameCore)")]
        [SerializeField] private bool useEquipmentBonuses = true;

        private float equipDamageBonus; // +к урону (Atk)
        private float equipHpBonus;     // +к hp (Hp)

        private float BaseDamageWithAllBonuses => baseDamage + equipDamageBonus + rolledDamageBonus;
        private float BaseHpWithAllBonuses => baseHP + equipHpBonus + rolledHpBonus;

        private bool _equipSubscribed;

        protected virtual void Awake()
        {
            instance = this;

            // TO EDIT
            charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            Data = charactersDatabase.GetCharacterData(charactersSave.SelectedCharacterId);
            //

            // 1) Spawn single character prefab
            if (characterPrefab == null)
            {
                Debug.LogError("[PlayerBehavior] characterPrefab is NULL. Assign your Spine prefab.");
                return;
            }

            var go = Instantiate(characterPrefab, transform);

            // сохраняем scale префаба (например 0.3)
            var prefabScale = go.transform.localScale;

            // ResetLocal сбрасывает scale в 1 -> возвращаем как было
            go.transform.ResetLocal();
            go.transform.localScale = prefabScale;

            _character = go.GetComponent<ICharacterBehavior>();
            if (_character == null)
                Debug.LogError("[PlayerBehavior] characterPrefab must have a component implementing ICharacterBehavior.");

            // 2) Find binder on spawned prefab (optional)
            _binder = go.GetComponentInChildren<GameCore.Visual.PartsManagerStateBinder>(true);

            // 3) Init HP UI
            healthbar.Init(baseHP);
            healthbar.SetAutoHideWhenMax(false);
            healthbar.SetAutoShowOnChanged(false);
            healthbar.ForceShow();

            // 4) Первый пулл бонусов (если сервис уже есть)
            PullEquipmentBonusesOnly();

            // 5) Recalculate all gameplay stats
            RecalculateMagnetRadius(1);
            RecalculateMoveSpeed(1);
            RecalculateDamage(1);
            RecalculateMaxHP(1);
            RecalculateXPMuliplier(1);
            RecalculateCooldownMuliplier(1);
            RecalculateDamageReduction(0);
            RecalculateProjectileSpeedMultiplier(1f);
            RecalculateSizeMultiplier(1f);
            RecalculateDurationMultiplier(1);
            RecalculateGoldMultiplier(1);
            RecalculateCritChance(0.2f);
            RecalculateCritDamage(1.5f);
            RecalculateDodge(0.2f);

            LookDirection = Vector2.right;
            IsMovingAlowed = true;

            // 5.1) Apply rolled bonuses from saved PlayerState/GameInstance
            LoadRolledBonusesFromState();

            // 6) Apply visuals from PlayerState
            if (applyVisualsFromPlayerState)
            {
                TryApplyVisualsOnce();

                var gi = GameCore.GameInstance.I;
                if (gi != null)
                    gi.StateChanged += OnStateChanged;
            }

            Debug.Log($"[Player] UI atk={GameCore.PlayerEquipmentStatsService.I?.BonusAtk} baseDamage={baseDamage} Damage={Damage}");
        }

        private void OnEnable()
        {
            SubscribeToEquipmentServiceIfReady();
        }

        private void OnDisable()
        {
            UnsubscribeFromEquipmentService();
        }

        protected virtual void OnDestroy()
        {
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.StateChanged -= OnStateChanged;

            UnsubscribeFromEquipmentService();
        }

        private void SubscribeToEquipmentServiceIfReady()
        {
            if (!useEquipmentBonuses) return;
            if (_equipSubscribed) return;

            var svc = GameCore.PlayerEquipmentStatsService.I;
            if (svc == null) return;

            svc.Changed += OnEquipmentStatsChanged;
            _equipSubscribed = true;

            PullEquipmentBonusesAndRecalc();
        }

        private void UnsubscribeFromEquipmentService()
        {
            if (!_equipSubscribed) return;

            var svc = GameCore.PlayerEquipmentStatsService.I;
            if (svc != null)
                svc.Changed -= OnEquipmentStatsChanged;

            _equipSubscribed = false;
        }

        private void OnEquipmentStatsChanged()
        {
            PullEquipmentBonusesAndRecalc();
        }

        private void PullEquipmentBonusesOnly()
        {
            if (!useEquipmentBonuses) return;

            var svc = GameCore.PlayerEquipmentStatsService.I;
            if (svc == null) return;

            equipDamageBonus = svc.BonusAtk;
            equipHpBonus = svc.BonusHp;
        }

        private void PullEquipmentBonusesAndRecalc()
        {
            if (!useEquipmentBonuses) return;

            var svc = GameCore.PlayerEquipmentStatsService.I;
            if (svc == null)
            {
                SubscribeToEquipmentServiceIfReady();
                return;
            }

            equipDamageBonus = svc.BonusAtk;
            equipHpBonus = svc.BonusHp;

            RecalculateDamage(1);
            RecalculateMaxHP(1);
        }

        private void OnStateChanged(GameCore.PlayerState _)
        {
            // refresh visuals
            TryApplyVisualsOnce();

            // если сервис появился позже — подпишемся
            SubscribeToEquipmentServiceIfReady();
        }

        private void TryApplyVisualsOnce()
        {
            if (_binder == null) return;
            if (GameCore.GameInstance.I == null || GameCore.GameInstance.I.State == null) return;

            _binder.ApplyFromState();
        }

        /// ROLLED STATS

        public void SetRolledCardBonuses(Dictionary<GameCore.Stats.StatType, float> totals)
        {
            if (!useRolledCardBonuses)
                return;

            rolledDamageBonus = 0f;
            rolledHpBonus = 0f;
            rolledMoveSpeedBonus = 0f;
            rolledCritChanceBonus = 0f;
            rolledCritDamageBonus = 0f;
            rolledMagnetRadiusBonus = 0f;

            if (totals != null)
            {
                if (totals.TryGetValue(GameCore.Stats.StatType.Attack, out float atk))
                    rolledDamageBonus = atk;

                if (totals.TryGetValue(GameCore.Stats.StatType.Health, out float hp))
                    rolledHpBonus = hp;

                if (totals.TryGetValue(GameCore.Stats.StatType.MoveSpeed, out float moveSpeed))
                    rolledMoveSpeedBonus = moveSpeed;

                if (totals.TryGetValue(GameCore.Stats.StatType.CritChance, out float critChance))
                    rolledCritChanceBonus = critChance;

                if (totals.TryGetValue(GameCore.Stats.StatType.CritDamage, out float critDamage))
                    rolledCritDamageBonus = critDamage;

                if (totals.TryGetValue(GameCore.Stats.StatType.PickupRange, out float pickupRange))
                    rolledMagnetRadiusBonus = pickupRange;
            }

            RecalculateAllStatsFromBonuses();
        }

        private void LoadRolledBonusesFromState()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || gi.State == null || charactersSave == null)
                return;

            var saved = gi.GetRolledCards();
            if (saved == null || saved.Count == 0)
            {
                SetRolledCardBonuses(null);
                return;
            }

            Dictionary<GameCore.Stats.StatType, float> totals = new Dictionary<GameCore.Stats.StatType, float>();

            for (int i = 0; i < saved.Count; i++)
            {
                var card = saved[i];
                if (card == null)
                    continue;

                var statType = (GameCore.Stats.StatType)card.statType;

                if (!totals.ContainsKey(statType))
                    totals[statType] = 0f;

                totals[statType] += card.currentValue;
            }

            SetRolledCardBonuses(totals);
        }

        private void RecalculateAllStatsFromBonuses()
        {
            RecalculateMagnetRadius(1f + rolledMagnetRadiusBonus);
            RecalculateMoveSpeed(1f + rolledMoveSpeedBonus);
            RecalculateDamage(1f);
            RecalculateMaxHP(1f);
            RecalculateXPMuliplier(1f);
            RecalculateCooldownMuliplier(1f);
            RecalculateDamageReduction(0);
            RecalculateProjectileSpeedMultiplier(1f);
            RecalculateSizeMultiplier(1f);
            RecalculateDurationMultiplier(1f);
            RecalculateGoldMultiplier(1f);
            RecalculateCritChance(0.2f + rolledCritChanceBonus);
            RecalculateCritDamage(1.5f + rolledCritDamageBonus);
            RecalculateDodge(0.2f);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void Update()
        {
            if (healthbar.IsZero) return;

            foreach (var enemy in enemiesInside)
            {
                if (Time.time - enemy.LastTimeDamagedPlayer > enemyInsideDamageInterval)
                {
                    TakeDamage(enemy.GetDamage());
                    enemy.LastTimeDamagedPlayer = Time.time;
                }
            }

            if (!IsMovingAlowed) return;

            var input = GameController.InputManager.MovementValue;

            float joysticPower = input.magnitude;
            _character?.SetSpeed(joysticPower);

            if (!Mathf.Approximately(joysticPower, 0) && Time.timeScale > 0)
            {
                var frameMovement = input * Time.deltaTime * Speed;

                if (StageController.FieldManager.ValidatePosition(transform.position + Vector3.right * frameMovement.x, fenceOffset))
                    transform.position += Vector3.right * frameMovement.x;

                if (StageController.FieldManager.ValidatePosition(transform.position + Vector3.up * frameMovement.y, fenceOffset))
                    transform.position += Vector3.up * frameMovement.y;

                collisionHelper.transform.localPosition = Vector3.zero;

                if (Mathf.Abs(input.x) > 0.001f)
                    _character?.SetLocalScale(new Vector3(Mathf.Sign(input.x), 1, 1));
                LookDirection = input.normalized;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsInsideMagnetRadius(Transform target)
        {
            return (transform.position - target.position).sqrMagnitude <= MagnetRadiusSqr;
        }

        public virtual void RecalculateMagnetRadius(float magnetRadiusMultiplier)
        {
            MagnetRadiusSqr = Mathf.Pow(defaultMagnetRadius * magnetRadiusMultiplier, 2);
        }

        public virtual void RecalculateMoveSpeed(float moveSpeedMultiplier)
        {
            Speed = speed * moveSpeedMultiplier;
        }

        public virtual void RecalculateDamage(float damageMultiplier)
        {
            Damage = BaseDamageWithAllBonuses * damageMultiplier;
        }

        public virtual void RecalculateMaxHP(float maxHPMultiplier)
        {
            healthbar.ChangeMaxHP(BaseHpWithAllBonuses * maxHPMultiplier);
        }

        public virtual void RecalculateXPMuliplier(float xpMultiplier)
        {
            XPMultiplier = this.xpMultiplier * xpMultiplier;
        }

        public virtual void RecalculateCooldownMuliplier(float cooldownMultiplier)
        {
            CooldownMultiplier = this.cooldownMultiplier * cooldownMultiplier;
        }

        public virtual void RecalculateDamageReduction(float damageReductionPercent)
        {
            DamageReductionMultiplier = (100f - initialDamageReductionPercent - damageReductionPercent) / 100f;
        }

        public virtual void RecalculateProjectileSpeedMultiplier(float projectileSpeedMultiplier)
        {
            ProjectileSpeedMultiplier = initialProjectileSpeedMultiplier * projectileSpeedMultiplier;
        }

        public virtual void RecalculateSizeMultiplier(float sizeMultiplier)
        {
            SizeMultiplier = initialSizeMultiplier * sizeMultiplier;
        }

        public virtual void RecalculateDurationMultiplier(float durationMultiplier)
        {
            DurationMultiplier = initialDurationMultiplier * durationMultiplier;
        }

        public virtual void RecalculateGoldMultiplier(float goldMultiplier)
        {
            GoldMultiplier = initialGoldMultiplier * goldMultiplier;
        }

        public virtual void RecalculateCritChance(float critChanceMultiplier)
        {
            // CritChance хранится как шанс 0..1 (рекомендую именно так)
            CritChance = initialCritChanceMultiplier * critChanceMultiplier;
            CritChance = Mathf.Clamp01(CritChance);
        }

        public virtual void RecalculateCritDamage(float critDamageMultiplier)
        {
            // CritDamage = множитель (2 = x2). Не даём упасть ниже 1.
            CritDamage = initialCritDamageMultiplier * critDamageMultiplier;
            CritDamage = Mathf.Max(1f, CritDamage);
        }

        public virtual void RecalculateDodge(float dodgeMultiplier)
        {
            // Dodge как шанс 0..1
            Dodge = initialDodgeMultiplier * dodgeMultiplier;

            // Можно капнуть, чтобы не было 100% уклонения
            Dodge = Mathf.Clamp(Dodge, 0f, 0.60f);
        }

        public virtual void RestoreHP(float hpPercent)
        {
            healthbar.AddPercentage(hpPercent);
        }

        public virtual void Heal(float hp)
        {
            healthbar.AddHP(hp);
        }

        public virtual void Revive()
        {
            _character?.PlayReviveAnimation();
            reviveParticle.Play();

            invincible = true;
            IsMovingAlowed = false;
            healthbar.ResetHP(1f);

            _character?.SetSortingOrder(102);

            reviveBackgroundSpriteRenderer.DoAlpha(0f, 0.3f, reviveBottomHideDelay).SetUnscaledTime(true)
                .SetOnFinish(() => reviveBackgroundSpriteRenderer.gameObject.SetActive(false));

            reviveBottomSpriteRenderer.DoAlpha(0f, 0.3f, reviveBottomHideDelay).SetUnscaledTime(true)
                .SetOnFinish(() => reviveBottomSpriteRenderer.gameObject.SetActive(false));

            GameController.AudioManager.PlaySound(REVIVE_HASH);

            EasingManager.DoAfter(1f, () =>
            {
                IsMovingAlowed = true;
                _character?.SetSortingOrder(0);
            });

            EasingManager.DoAfter(3, () => invincible = false);
        }

        public virtual void CheckTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.layer == 7)
            {
                if (invincible) return;

                var enemy = collision.GetComponent<EnemyBehavior>();
                if (enemy != null)
                {
                    enemiesInside.Add(enemy);
                    enemy.LastTimeDamagedPlayer = Time.time;

                    enemy.onEnemyDied += OnEnemyDied;
                    TakeDamage(enemy.GetDamage());
                }
            }
            else
            {
                if (invincible) return;

                var projectile = collision.GetComponent<SimpleEnemyProjectileBehavior>();
                if (projectile != null)
                    TakeDamage(projectile.Damage);
            }
        }

        public virtual void CheckTriggerExit2D(Collider2D collision)
        {
            if (collision.gameObject.layer == 7)
            {
                if (invincible) return;

                var enemy = collision.GetComponent<EnemyBehavior>();
                if (enemy != null)
                {
                    enemiesInside.Remove(enemy);
                    enemy.onEnemyDied -= OnEnemyDied;
                }
            }
        }

        protected virtual void OnEnemyDied(EnemyBehavior enemy)
        {
            enemy.onEnemyDied -= OnEnemyDied;
            enemiesInside.Remove(enemy);
        }

        protected float lastTimeVibrated = 0f;

        public virtual void TakeDamage(float damage)
        {
            if (invincible || healthbar.IsZero) return;

            // Dodge (chance 0..1). If dodged - no damage taken.
            if (Dodge > 0f && Random.value < Dodge)
            {
                StageController.WorldSpaceTextManager.SpawnText(
                    CenterPosition + new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.05f, 0.15f)),
                    "DODGE"
                );
                return;
            }

            healthbar.Subtract(damage * DamageReductionMultiplier);
            _character?.FlashHit();

            if (healthbar.IsZero)
            {
                _character?.PlayDefeatAnimation();
                _character?.SetSortingOrder(102);

                reviveBackgroundSpriteRenderer.gameObject.SetActive(true);
                reviveBackgroundSpriteRenderer.DoAlpha(reviveBackgroundAlpha, 0.3f, reviveBackgroundSpawnDelay).SetUnscaledTime(true);
                reviveBackgroundSpriteRenderer.transform.position = transform.position.SetZ(reviveBackgroundSpriteRenderer.transform.position.z);

                reviveBottomSpriteRenderer.gameObject.SetActive(true);
                reviveBottomSpriteRenderer.DoAlpha(reviveBottomAlpha, 0.3f, reviveBottomSpawnDelay).SetUnscaledTime(true);

                GameController.AudioManager.PlaySound(DEATH_HASH);

                EasingManager.DoAfter(0.5f, () => onPlayerDied?.Invoke()).SetUnscaledTime(true);
                GameController.VibrationManager.StrongVibration();
            }
            else
            {
                if (Time.time - lastTimeVibrated > 0.05f)
                {
                    GameController.VibrationManager.LightVibration();
                    lastTimeVibrated = Time.time;
                }

                GameController.AudioManager.PlaySound(RECEIVING_DAMAGE_HASH);
            }
        }
    }
}

