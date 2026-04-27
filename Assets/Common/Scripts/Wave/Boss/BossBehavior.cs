using OctoberStudio.Bossfight;
using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using UnityEngine;
using UnityEngine.Playables;

namespace OctoberStudio.Timeline.Bossfight
{
    public class BossBehavior : PlayableBehaviour
    {
        public BossType BossType { get; set; }
        public GameObject FencePrefab { get; set; }
        public bool ShouldSpawnChest { get; set; }

        public float WarningDuration { get; set; }
        public float BossRedCircleStayDuration { get; set; }
        public float BossRedCircleSpawnDuration { get; set; }
        public Vector2 BossSpawnOffset { get; set; }

        private const float PlayerTeleportDistanceFromCenter = 2f;
        private const float PlayerTeleportPadding = 0.75f;

        bool hasStarted = false;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (hasStarted) return;

            hasStarted = true;

            StageController.GameScreen.ShowBossfightWarning();

            EasingManager.DoAfter(WarningDuration, () =>
            {
                StageController.Director.Pause();

                StageController.GameScreen.HideBossFightWarning();

                // Doing it next frame because there still could be spawns during this one
                EasingManager.DoNextFrame().SetOnFinish(StageController.EnemiesSpawner.KillEveryEnemy);

                var bossSpawnPosition = StageController.FieldManager.SpawnFence(BossType, BossSpawnOffset);

                // Переносим игрока в арену босса после спавна ворот
                TeleportPlayerToBossArena(bossSpawnPosition);

                if (StageController.Stage.RemovePropFromBossfight && StageController.Stage.SpawnProp)
                {
                    StageController.FieldManager.RemovePropFromFence();
                }

                EasingManager.DoAfter(0.3f, () =>
                {
                    var data = StageController.EnemiesSpawner.GetBossData(BossType);
                    StageController.GameScreen.ShowBossHealthBar(data);
                });

                var warningPool = StageController.PoolsManager.GetPool("Warning Circle");
                var warning = warningPool.GetEntity<WarningCircleBehavior>();

                warning.transform.position = bossSpawnPosition;
                warning.Play(2, BossRedCircleSpawnDuration, BossRedCircleStayDuration, () =>
                {
                    var boss = StageController.EnemiesSpawner.SpawnBoss(BossType, bossSpawnPosition, OnBossDied);
                    boss.ShouldSpawnChestOnDeath = ShouldSpawnChest;
                    StageController.GameScreen.LinkBossToHealthbar(boss);
                });

                StageController.EnemiesSpawner.IsBossfightActive = true;
            });
        }

        private void TeleportPlayerToBossArena(Vector2 bossSpawnPosition)
        {
            var player = FindPlayer();

            if (player == null)
            {
                Debug.LogError("[BossBehavior] PlayerBehavior was not found. Boss arena teleport skipped.");
                return;
            }

            var targetPosition = bossSpawnPosition + Vector2.down * PlayerTeleportDistanceFromCenter;
            targetPosition = StageController.FieldManager.ClampPositionInsideField(targetPosition, PlayerTeleportPadding);

            Debug.Log($"[BossBehavior] Teleporting player to boss arena. Boss center: {bossSpawnPosition}, Player target: {targetPosition}, Player object: {player.gameObject.name}");

            ForceMovePlayer(player, targetPosition);

            // На следующий кадр повторяем, если какой-то movement/controller перезаписал позицию в этот же кадр.
            EasingManager.DoNextFrame().SetOnFinish(() =>
            {
                if (player != null)
                {
                    ForceMovePlayer(player, targetPosition);
                    Debug.Log($"[BossBehavior] Player teleport repeated next frame. Current position: {player.transform.position}");
                }
            });
        }

        private PlayerBehavior FindPlayer()
        {
#if UNITY_2023_1_OR_NEWER
            var player = Object.FindFirstObjectByType<PlayerBehavior>();
#else
            var player = Object.FindObjectOfType<PlayerBehavior>();
#endif

            if (player != null)
                return player;

            var allBehaviours = Object.FindObjectsOfType<MonoBehaviour>(true);

            for (int i = 0; i < allBehaviours.Length; i++)
            {
                if (allBehaviours[i] is PlayerBehavior playerBehavior)
                    return playerBehavior;
            }

            return null;
        }

        private void ForceMovePlayer(PlayerBehavior player, Vector2 targetPosition)
        {
            var targetPosition3D = new Vector3(
                targetPosition.x,
                targetPosition.y,
                player.transform.position.z
            );

            var rb2D = player.GetComponent<Rigidbody2D>();

            if (rb2D != null)
            {
                rb2D.velocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
                rb2D.position = targetPosition;
            }

            player.transform.position = targetPosition3D;

            Physics2D.SyncTransforms();
        }

        private void OnBossDied(EnemyBehavior boss)
        {
            boss.onEnemyDied -= OnBossDied;

            StageController.GameScreen.HideBossHealthbar();

            StageController.FieldManager.RemoveFence();
            StageController.Director.Play();

            StageController.EnemiesSpawner.IsBossfightActive = false;
        }
    }
}