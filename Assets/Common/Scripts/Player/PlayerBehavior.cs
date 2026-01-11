using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Upgrades;
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
        [SerializeField] private GameObject characterPrefab; // <-- “¬ќ… Spine prefab (единственный)
        [SerializeField] private bool applyVisualsFromPlayerState = true;

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

        [Header("References")]
        [SerializeField] protected HealthbarBehavior healthbar;
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

        public Vector2 LookDirection { get; protected set; }
        public bool IsMovingAlowed { get; set; }

        protected bool invincible = false;

        protected List<EnemyBehavior> enemiesInside = new List<EnemyBehavior>();

        // ===== Character runtime =====
        protected ICharacterBehavior _character;
        private GameCore.Visual.PartsManagerStateBinder _binder;

        protected virtual void Awake()
        {
            instance = this;

            // 1) Spawn single character prefab
            if (characterPrefab == null)
            {
                Debug.LogError("[PlayerBehavior] characterPrefab is NULL. Assign your Spine prefab.");
                return;
            }

            var go = Instantiate(characterPrefab, transform);

            // сохран€ем scale префаба (например 0.3)
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
            healthbar.SetAutoHideWhenMax(true);
            healthbar.SetAutoShowOnChanged(true);

            // 4) Recalculate all gameplay stats
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

            LookDirection = Vector2.right;
            IsMovingAlowed = true;

            // 5) Apply visuals from PlayerState
            if (applyVisualsFromPlayerState)
            {
                TryApplyVisualsOnce();

                var gi = GameCore.GameInstance.I;
                if (gi != null)
                    gi.StateChanged += OnStateChanged;
            }
        }

        protected virtual void OnDestroy()
        {
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameCore.PlayerState _)
        {
            // When equip/unequip happens -> refresh parts
            TryApplyVisualsOnce();
        }

        private void TryApplyVisualsOnce()
        {
            if (_binder == null) return;

            // если GameInstance/State ещЄ не готов (ранний Awake) Ч просто пропустим
            if (GameCore.GameInstance.I == null || GameCore.GameInstance.I.State == null) return;

            _binder.ApplyFromState();
        }

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
            Damage = baseDamage * damageMultiplier;

            if (GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Damage))
                Damage *= GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Damage);
        }

        public virtual void RecalculateMaxHP(float maxHPMultiplier)
        {
            var upgradeValue = GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Health);
            healthbar.ChangeMaxHP((baseHP + upgradeValue) * maxHPMultiplier);
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

            if (GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Armor))
                DamageReductionMultiplier *= GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Armor);
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

        public virtual void RestoreHP(float hpPercent)
        {
            healthbar.AddPercentage(hpPercent);
        }

        public virtual void Heal(float hp)
        {
            healthbar.AddHP(hp + GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Healing));
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
