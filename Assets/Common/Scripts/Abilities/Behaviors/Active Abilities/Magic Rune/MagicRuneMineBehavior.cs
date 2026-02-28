using OctoberStudio.Easing;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class MagicRuneMineBehavior : MonoBehaviour
    {
        private static readonly int MAGIC_RUNE_EXPLOSION_HASH = "Magic Rune Explosion".GetHashCode();

        [SerializeField] CircleCollider2D mineTriggerCollider;
        [SerializeField] GameObject mineVisuals;
        [SerializeField] ParticleSystem explosionParticle;

        public float DamageMultiplier { get; private set; }
        public float DamageRadius { get; private set; }

        private IEasingCoroutine lifetimeCoroutine;

        public void SetData(MagicRuneAbilityLevel stage)
        {
            var size = stage.MineSize * PlayerBehavior.Player.SizeMultiplier;
            transform.localScale = Vector3.one * size;
            mineTriggerCollider.radius = stage.MineTriggerRadius / size;

            DamageMultiplier = stage.Damage;
            DamageRadius = stage.MineDamageRadius * PlayerBehavior.Player.SizeMultiplier;

            mineVisuals.SetActive(true);

            EasingManager.DoAfter(0.2f, () => mineTriggerCollider.enabled = true);
            lifetimeCoroutine = EasingManager.DoAfter(stage.MineLifetime, Explode);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var enemy = collision.GetComponent<EnemyBehavior>();

            if (enemy != null)
            {
                Explode();
            }
        }

        private void Explode()
        {
            mineTriggerCollider.enabled = false;
            mineVisuals.SetActive(false);

            var enemies = StageController.EnemiesSpawner.GetEnemiesInRadius(transform.position, DamageRadius);

            float baseDamage = PlayerBehavior.Player.Damage * DamageMultiplier;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];

                bool isCrit = Random.value < PlayerBehavior.Player.CritChance;
                float finalDamage = isCrit
                    ? baseDamage * PlayerBehavior.Player.CritDamage
                    : baseDamage;

                enemy.TakeDamage(finalDamage, isCrit);
            }

            explosionParticle.Play();

            EasingManager.DoAfter(1f, () => gameObject.SetActive(false));

            lifetimeCoroutine.StopIfExists();

            GameController.AudioManager.PlaySound(MAGIC_RUNE_EXPLOSION_HASH);
        }
    }
}