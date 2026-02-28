using OctoberStudio.Easing;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class LightningAmuletAbilityBehavior : AbilityBehavior<LightningAmuletAbilityData, LightningAmuletAbilityLevel>
    {
        private static readonly int LIGHTNING_AMULET_HASH = "Lightning Amulet".GetHashCode();

        [SerializeField] GameObject lightningPrefab;
        public GameObject LightningPrefab => lightningPrefab;

        public PoolComponent<ParticleSystem> lightningPool;

        private List<IEasingCoroutine> easingCoroutines = new List<IEasingCoroutine>();

        private Coroutine abilityCoroutine;

        private void Awake()
        {
            lightningPool = new PoolComponent<ParticleSystem>("Lightning ability particle", LightningPrefab, 6);
        }

        public override void Init(AbilityData data, int stageId)
        {
            base.Init(data, stageId);

            abilityCoroutine = StartCoroutine(AbilityCoroutine());
        }

        private IEnumerator AbilityCoroutine()
        {
            while (true)
            {
                for (int i = 0; i < AbilityLevel.LightningsCount; i++)
                {
                    yield return new WaitForSeconds(AbilityLevel.DurationBetweenHits);

                    var particle = lightningPool.GetEntity();

                    var spawner = StageController.EnemiesSpawner;
                    var enemy = spawner.GetRandomVisibleEnemy();

                    if (enemy != null)
                    {
                        particle.transform.position = enemy.transform.position;

                        // -------- MAIN TARGET DAMAGE --------
                        float baseDamage = PlayerBehavior.Player.Damage * AbilityLevel.Damage;

                        bool isCrit = Random.value < PlayerBehavior.Player.CritChance;
                        float finalDamage = isCrit
                            ? baseDamage * PlayerBehavior.Player.CritDamage
                            : baseDamage;

                        enemy.TakeDamage(finalDamage, isCrit);

                        // -------- CHAIN / ADDITIONAL DAMAGE --------
                        var enemiesInRadius = StageController.EnemiesSpawner.GetEnemiesInRadius(
                            enemy.transform.position,
                            AbilityLevel.AdditionalDamageRadius
                        );

                        foreach (var closeEnemy in enemiesInRadius)
                        {
                            if (closeEnemy == null || closeEnemy == enemy) continue;

                            float chainBaseDamage = PlayerBehavior.Player.Damage * AbilityLevel.AdditionalDamage;

                            bool chainCrit = Random.value < PlayerBehavior.Player.CritChance;
                            float chainFinalDamage = chainCrit
                                ? chainBaseDamage * PlayerBehavior.Player.CritDamage
                                : chainBaseDamage;

                            closeEnemy.TakeDamage(chainFinalDamage, chainCrit);
                        }
                    }
                    else
                    {
                        particle.transform.position =
                            PlayerBehavior.Player.transform.position + Vector3.up + Vector3.left;
                    }

                    IEasingCoroutine easingCoroutine = null;
                    easingCoroutine = EasingManager.DoAfter(1, () =>
                    {
                        particle.gameObject.SetActive(false);
                        easingCoroutines.Remove(easingCoroutine);
                    });

                    easingCoroutines.Add(easingCoroutine);

                    GameController.AudioManager.PlaySound(LIGHTNING_AMULET_HASH);
                }

                yield return new WaitForSeconds(
                    AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier
                    - AbilityLevel.DurationBetweenHits
                );
            }
        }

        public override void Clear()
        {
            StopCoroutine(abilityCoroutine);

            for (int i = 0; i < easingCoroutines.Count; i++)
            {
                var easingCoroutine = easingCoroutines[i];
                if (easingCoroutine.ExistsAndActive()) easingCoroutine.Stop();
            }

            lightningPool.Destroy();

            base.Clear();
        }
    }
}