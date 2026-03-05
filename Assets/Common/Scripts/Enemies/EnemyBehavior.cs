using OctoberStudio.Easing;
using OctoberStudio.Enemy;
using OctoberStudio.Extensions;
using OctoberStudio.Timeline;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace OctoberStudio
{
    public class EnemyBehavior : MonoBehaviour
    {
        protected static readonly int _Overlay = Shader.PropertyToID("_Overlay");
        protected static readonly int _Disolve = Shader.PropertyToID("_Disolve");

        private static readonly int HIT_HASH = "Hit".GetHashCode();

        [Header("Settings")]
        [Tooltip("The speed of the enemy")]
        [SerializeField] protected float speed;
        public float Speed { get; protected set; }

        [Tooltip("The LevelData's 'Enemy Damage' is multiplied by this value to determine the damage of the enemy on each level")]
        [SerializeField] float damage = 1f;

        [Tooltip("The LevelData's 'Enemy HP' is multiplied by this value to determine the HP of the enemy on each level")]
        [SerializeField] float hp;

        [FormerlySerializedAs("canBekickedBack")]
        [SerializeField] bool canBeKickedBack = true;

        [SerializeField] bool shouldFadeIn;

        [Header("References")]
        [SerializeField] Rigidbody2D rb;

        // Один "главный" рендер (isVisible, fade-in, etc.)
        [SerializeField] SpriteRenderer spriteRenderer;

        [SerializeField] DissolveSettings dissolveSettings;
        [SerializeField] SpriteRenderer shadowSprite;
        [SerializeField] Collider2D enemyCollider;

        [Header("Visual Root")]
        [Tooltip("Flip/scale THIS object. Put all visuals under it. Root physics object can stay scale=1.")]
        [SerializeField] Transform visualRoot;

        [Header("Dissolve Renderers")]
        [Tooltip("All SpriteRenderers that should dissolve (body, wings, eyes, etc). If empty, auto-fills from children.")]
        [SerializeField] SpriteRenderer[] dissolveRenderers;

        public Vector2 Center => enemyCollider.bounds.center;

        [Header("Hit")]
        [SerializeField] float hitScaleAmount = 0.2f;
        [SerializeField] Color hitColor = Color.white;

        public EnemyData Data { get; private set; }
        public WaveOverride WaveOverride { get; protected set; }

        public bool IsVisible => spriteRenderer != null && spriteRenderer.isVisible;
        public bool IsAlive => HP > 0;
        public bool IsInvulnerable { get; protected set; }

        public float HP { get; private set; }
        public float MaxHP { get; private set; }

        public bool ShouldSpawnChestOnDeath { get; set; }

        IEasingCoroutine fallBackCoroutine;
        private Dictionary<EffectType, List<Effect>> appliedEffects = new Dictionary<EffectType, List<Effect>>();

        protected bool IsMoving { get; set; }
        public bool IsMovingToCustomPoint { get; protected set; }
        public Vector2 CustomPoint { get; protected set; }

        public float LastTimeDamagedPlayer { get; set; }

        private float shadowAlpha;

        public event UnityAction<EnemyBehavior> onEnemyDied;
        public event UnityAction<float, float> onHealthChanged;

        private float lastTimeSwitchedDirection = 0;

        IEasingCoroutine damageCoroutine;
        protected IEasingCoroutine scaleCoroutine;
        IEasingCoroutine fadeInCoroutine;

        private float damageTextValue;
        private float lastTimeDamageText;

        private static int lastFrameHitSound;
        private float lastTimeHitSound;

        // base scale for hit-scale & flip
        private Vector3 _visualBaseScale;

        // flip state
        private bool _facingRight = true;

        // Per-renderer materials (so we can animate all parts)
        private Material[] sharedMaterials;
        private Material[] effectsMaterials;

        protected virtual void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            _visualBaseScale = visualRoot.localScale;

            if (dissolveRenderers == null || dissolveRenderers.Length == 0)
                dissolveRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            sharedMaterials = new Material[dissolveRenderers.Length];
            effectsMaterials = new Material[dissolveRenderers.Length];

            for (int i = 0; i < dissolveRenderers.Length; i++)
            {
                var sr = dissolveRenderers[i];
                if (sr == null) continue;

                sharedMaterials[i] = sr.sharedMaterial;
                effectsMaterials[i] = Instantiate(sharedMaterials[i]);
            }

            if (shadowSprite != null)
                shadowAlpha = shadowSprite.color.a;
        }

        public void SetData(EnemyData data) => Data = data;
        public void SetWaveOverride(WaveOverride waveOverride) => WaveOverride = waveOverride;

        public virtual void Play()
        {
            MaxHP = StageController.Stage.EnemyHP * hp;
            Speed = speed;

            if (WaveOverride != null)
            {
                MaxHP = WaveOverride.ApplyHPOverride(MaxHP);
                Speed = WaveOverride.ApplySpeedOverride(Speed);
            }

            HP = MaxHP;
            IsMoving = true;

            if (shadowSprite != null)
                shadowSprite.SetAlpha(shadowAlpha);

            enemyCollider.enabled = true;

            // Reset visuals (pool-safe)
            visualRoot.localScale = _visualBaseScale;

            // Reset flip to default (facing right)
            _facingRight = true;
            SetFacing(true);

            // Restore materials (pool-safe)
            RestoreSharedMaterials();

            if (shouldFadeIn)
            {
                SetAlphaAll(0f);

                if (spriteRenderer != null)
                    fadeInCoroutine = spriteRenderer.DoAlpha(1, 0.2f);

                // остальные сразу в 1 (чтобы не моргали)
                SetAlphaAll(1f, exceptMain: true);
            }
        }

        protected virtual void Update()
        {
            if (!IsAlive || !IsMoving || PlayerBehavior.Player == null) return;

            Vector3 target = IsMovingToCustomPoint ? (Vector3)CustomPoint : PlayerBehavior.Player.transform.position;
            Vector3 direction = (target - transform.position).normalized;

            float finalSpeed = Speed;

            if (appliedEffects.TryGetValue(EffectType.Speed, out var speedEffects))
            {
                for (int i = 0; i < speedEffects.Count; i++)
                    finalSpeed *= speedEffects[i].Modifier;
            }

            transform.position += direction * Time.deltaTime * finalSpeed;

            // Flip parent (visualRoot) by sign of scale.x
            if (!scaleCoroutine.ExistsAndActive())
            {
                if (Mathf.Abs(direction.x) > 0.001f)
                {
                    bool shouldFaceRight = direction.x > 0f;

                    if (shouldFaceRight != _facingRight && Time.unscaledTime - lastTimeSwitchedDirection > 0.1f)
                    {
                        _facingRight = shouldFaceRight;
                        SetFacing(_facingRight);
                        lastTimeSwitchedDirection = Time.unscaledTime;
                    }
                }
            }
        }

        private void SetFacing(bool faceRight)
        {
            if (visualRoot == null) return;

            var s = visualRoot.localScale;

            float absX = Mathf.Abs(_visualBaseScale.x);
            float sign = faceRight ? 1f : -1f;

            s.x = absX * sign;
            s.y = _visualBaseScale.y;
            s.z = _visualBaseScale.z;

            visualRoot.localScale = s;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProjectileBehavior projectile = other.GetComponent<ProjectileBehavior>();

            if (projectile != null)
            {
                float baseDamage = PlayerBehavior.Player.Damage * projectile.DamageMultiplier;

                bool isCrit = Random.value < PlayerBehavior.Player.CritChance;
                float finalDamage = isCrit
                    ? baseDamage * PlayerBehavior.Player.CritDamage
                    : baseDamage;

                TakeDamage(finalDamage, isCrit);

                if (HP > 0)
                {
                    if (projectile.KickBack && canBeKickedBack)
                        KickBack(PlayerBehavior.CenterPosition);

                    if (projectile.Effects != null && projectile.Effects.Count > 0)
                        AddEffects(projectile.Effects);
                }
            }
        }

        public float GetDamage()
        {
            var dmg = this.damage;
            if (WaveOverride != null) dmg = WaveOverride.ApplyDamageOverride(dmg);

            var baseDamage = StageController.Stage.EnemyDamage * dmg;

            if (appliedEffects.ContainsKey(EffectType.Damage))
            {
                var damageEffects = appliedEffects[EffectType.Damage];
                for (int i = 0; i < damageEffects.Count; i++)
                    baseDamage *= damageEffects[i].Modifier;
            }

            return baseDamage;
        }

        public List<EnemyDropData> GetDropData()
        {
            if (WaveOverride != null) return WaveOverride.ApplyDropOverride(Data.EnemyDrop);
            return Data.EnemyDrop;
        }

        public void TakeDamage(float damage) => TakeDamage(damage, false);

        public void TakeDamage(float damage, bool isCrit)
        {
            if (!IsAlive) return;
            if (IsInvulnerable) return;

            HP -= damage;
            onHealthChanged?.Invoke(HP, MaxHP);

            // Damage text
            if (isCrit)
            {
                var critText = Mathf.RoundToInt(damage).ToString();
                StageController.WorldSpaceTextManager.SpawnText(
                    transform.position + new Vector3(Random.Range(-0.1f, 0.1f), Random.value * 0.1f),
                    critText,
                    new Color(1f, 0.82f, 0f)
                );

                damageTextValue = 0;
                lastTimeDamageText = Time.unscaledTime;
            }
            else
            {
                damageTextValue += damage;

                if (Time.unscaledTime - lastTimeDamageText > 0.2f && damageTextValue >= 1f)
                {
                    var damageText = Mathf.RoundToInt(damageTextValue).ToString();
                    StageController.WorldSpaceTextManager.SpawnText(
                        transform.position + new Vector3(Random.Range(-0.1f, 0.1f), Random.value * 0.1f),
                        damageText
                    );

                    damageTextValue = 0;
                    lastTimeDamageText = Time.unscaledTime;
                }
            }

            // Hit sound
            if (Time.frameCount != lastFrameHitSound && Time.unscaledTime - lastTimeHitSound > 0.2f)
            {
                GameController.AudioManager.PlaySound(HIT_HASH);
                lastFrameHitSound = Time.frameCount;
                lastTimeHitSound = Time.unscaledTime;
            }

            if (HP <= 0)
            {
                Die(true);
            }
            else
            {
                // Flash on hit (all parts)
                if (!damageCoroutine.ExistsAndActive())
                    FlashHitAll(true);

                // Hit scale (preserve facing sign)
                if (!scaleCoroutine.ExistsAndActive() && visualRoot != null)
                {
                    var baseScale = _visualBaseScale;
                    baseScale.x = Mathf.Sign(visualRoot.localScale.x) * Mathf.Abs(baseScale.x);

                    var hitScale = new Vector3(
                        baseScale.x * (1f - hitScaleAmount),
                        baseScale.y * (1f + hitScaleAmount),
                        baseScale.z
                    );

                    scaleCoroutine = visualRoot.DoLocalScale(hitScale, 0.07f)
                        .SetEasing(EasingType.SineOut)
                        .SetOnFinish(() =>
                        {
                            scaleCoroutine = visualRoot.DoLocalScale(baseScale, 0.07f)
                                .SetEasing(EasingType.SineInOut);
                        });
                }
            }
        }

        private void FlashHitAll(bool resetMaterialsAfter, UnityAction onFinish = null)
        {
            AssignEffectsMaterials();

            var transparentColor = hitColor;
            transparentColor.a = 0;

            for (int i = 0; i < effectsMaterials.Length; i++)
            {
                var mat = effectsMaterials[i];
                if (mat == null) continue;
                mat.SetColor(_Overlay, transparentColor);
            }

            // driver = first valid material
            Material driver = null;
            for (int i = 0; i < effectsMaterials.Length; i++)
            {
                if (effectsMaterials[i] != null) { driver = effectsMaterials[i]; break; }
            }

            if (driver == null)
            {
                onFinish?.Invoke();
                return;
            }

            damageCoroutine = driver.DoColor(_Overlay, hitColor, 0.05f).SetOnFinish(() =>
            {
                for (int i = 0; i < effectsMaterials.Length; i++)
                {
                    var mat = effectsMaterials[i];
                    if (mat == null) continue;
                    mat.SetColor(_Overlay, hitColor);
                }

                damageCoroutine = driver.DoColor(_Overlay, transparentColor, 0.05f).SetOnFinish(() =>
                {
                    for (int i = 0; i < effectsMaterials.Length; i++)
                    {
                        var mat = effectsMaterials[i];
                        if (mat == null) continue;
                        mat.SetColor(_Overlay, transparentColor);
                    }

                    if (resetMaterialsAfter) RestoreSharedMaterials();
                    onFinish?.Invoke();
                });
            });
        }

        public void Kill()
        {
            HP = 0;
            Die(false);
        }

        protected virtual void Die(bool flash)
        {
            enemyCollider.enabled = false;

            damageCoroutine.StopIfExists();

            onEnemyDied?.Invoke(this);
            fallBackCoroutine.StopIfExists();
            rb.simulated = true;

            fadeInCoroutine.StopIfExists();

            AssignEffectsMaterials();

            // Overlay color animation
            if (flash)
            {
                FlashHitAll(false, () =>
                {
                    for (int i = 0; i < effectsMaterials.Length; i++)
                    {
                        var mat = effectsMaterials[i];
                        if (mat == null) continue;
                        mat.SetColor(_Overlay, Color.clear);
                        mat.DoColor(_Overlay, dissolveSettings.DissolveColor, dissolveSettings.Duration - 0.1f);
                    }
                });
            }
            else
            {
                for (int i = 0; i < effectsMaterials.Length; i++)
                {
                    var mat = effectsMaterials[i];
                    if (mat == null) continue;
                    mat.SetColor(_Overlay, Color.clear);
                    mat.DoColor(_Overlay, dissolveSettings.DissolveColor, dissolveSettings.Duration);
                }
            }

            // Dissolve all parts
            int remaining = 0;
            for (int i = 0; i < effectsMaterials.Length; i++)
                if (effectsMaterials[i] != null) remaining++;

            if (remaining == 0)
            {
                gameObject.SetActive(false);
                appliedEffects.Clear();
                WaveOverride = null;
                return;
            }

            for (int i = 0; i < effectsMaterials.Length; i++)
            {
                var mat = effectsMaterials[i];
                if (mat == null) continue;

                mat.SetFloat(_Disolve, 0);

                mat.DoFloat(_Disolve, 1, dissolveSettings.Duration + 0.02f)
                    .SetEasingCurve(dissolveSettings.DissolveCurve)
                    .SetOnFinish(() =>
                    {
                        remaining--;

                        mat.SetColor(_Overlay, Color.clear);
                        mat.SetFloat(_Disolve, 0);

                        if (remaining <= 0)
                        {
                            RestoreSharedMaterials();

                            gameObject.SetActive(false);

                            appliedEffects.Clear();
                            WaveOverride = null;
                        }
                    });
            }

            if (shadowSprite != null)
                shadowSprite.DoAlpha(0, dissolveSettings.Duration);
        }

        public void KickBack(Vector3 position)
        {
            var direction = (transform.position - position).normalized;
            rb.simulated = false;
            fallBackCoroutine.StopIfExists();
            fallBackCoroutine = transform.DoPosition(transform.position + direction * 0.6f, 0.15f)
                .SetEasing(EasingType.ExpoOut)
                .SetOnFinish(() => rb.simulated = true);
        }

        public void AddEffects(List<Effect> effects)
        {
            for (int i = 0; i < effects.Count; i++)
                AddEffect(effects[i]);
        }

        public void AddEffect(Effect effect)
        {
            if (!appliedEffects.ContainsKey(effect.EffectType))
                appliedEffects.Add(effect.EffectType, new List<Effect>());

            List<Effect> effects = appliedEffects[effect.EffectType];
            if (!effects.Contains(effect))
                effects.Add(effect);
        }

        public void RemoveEffect(Effect effect)
        {
            if (!appliedEffects.ContainsKey(effect.EffectType)) return;

            List<Effect> effects = appliedEffects[effect.EffectType];
            if (effects.Contains(effect))
                effects.Remove(effect);
        }

        // ---------- Helpers ----------

        private void AssignEffectsMaterials()
        {
            for (int i = 0; i < dissolveRenderers.Length; i++)
            {
                var sr = dissolveRenderers[i];
                if (sr == null) continue;

                if (effectsMaterials[i] != null)
                    sr.material = effectsMaterials[i];
            }
        }

        private void RestoreSharedMaterials()
        {
            for (int i = 0; i < dissolveRenderers.Length; i++)
            {
                var sr = dissolveRenderers[i];
                if (sr == null) continue;

                if (sharedMaterials[i] != null)
                    sr.material = sharedMaterials[i];
            }
        }

        private void SetAlphaAll(float a, bool exceptMain = false)
        {
            if (dissolveRenderers == null) return;

            for (int i = 0; i < dissolveRenderers.Length; i++)
            {
                var sr = dissolveRenderers[i];
                if (sr == null) continue;
                if (exceptMain && sr == spriteRenderer) continue;

                sr.SetAlpha(a);
            }
        }
    }
}