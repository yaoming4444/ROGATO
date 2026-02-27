using OctoberStudio.Bossfight;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using OctoberStudio.Timeline;
using OctoberStudio.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace OctoberStudio
{
    [DefaultExecutionOrder(-10)]
    public class EnemiesSpawner : MonoBehaviour
    {
        protected List<EnemyBehavior> enemies = new List<EnemyBehavior>();

        [SerializeField] protected EnemiesDatabase database;
        [SerializeField] protected BossfightDatabase bossfightDatabase;

        [Space]
        [SerializeField] protected ScalingLabelBehavior enemiesDiedLabel;

        [Space]
        [Tooltip("Maximum amount of alive enemies at a time. No more enemies will be spawned until some of existing aren't defeated")]
        [SerializeField] protected int enemiesCap = 2000;

        [Header("Spawn Bounds")]
        [Tooltip("Safety padding from map borders for enemy spawn/teleport. Prevents enemies from spawning partially outside the map.")]
        [SerializeField] protected float enemySpawnPadding = 0.6f;

        [Header("Spawn Preview (Telegraph)")]
        [Tooltip("Prefab for red spawn point marker (telegraph). Spawned before enemy appears.")]
        [SerializeField] private GameObject spawnPreviewPrefab;

        [Tooltip("How long the preview marker stays before enemy appears.")]
        [SerializeField] private float spawnPreviewDelay = 0.6f;

        [Tooltip("After enemy appears, disable its colliders for this duration so it can't hit the player instantly.")]
        [SerializeField] private float spawnHitGraceDuration = 0.2f;

        [Tooltip("Initial pool size for SpawnPreviewPrefab. Will expand automatically if needed.")]
        [SerializeField] private int spawnPreviewPoolSize = 32;

        [Header("Spawn Near Player")]
        [Tooltip("Enemies will spawn around the player within this radius range (instead of only outside the camera).")]
        [SerializeField] protected float spawnNearPlayerMinRadius = 3f;
        [SerializeField] protected float spawnNearPlayerMaxRadius = 9f;

        [Header("Offscreen Teleport")]
        [Tooltip("When enabled, enemies that are behaind of the player will teleport to the front")]
        [SerializeField] protected bool isOffscreenTeleportEnabled = true;
        [Tooltip("Distance from the player to enemy, where 1 is a camera diagonal length")]
        [SerializeField] protected float diagonalDistanceMultiplier = 1.3f;
        [SerializeField, Range(0, 1f)] protected float teleportConeSize = 0.8f;

        [Tooltip("Enemy will stop teleporting if there are more than this amount of enemies")]
        [SerializeField] protected int enemiesTeleportCap = 100;

        protected int enemiesDiedCounter;

        protected Dictionary<EnemyType, PoolComponent<EnemyBehavior>> enemyPools;
        protected Dictionary<EnemyType, EnemyData> enemyDataDictionary;

        protected StageSave stageSave;

        public bool IsBossfightActive { get; set; }

        protected Camera mainCamera;

        // Spawn preview pooling (local simple pool)
        private Queue<GameObject> spawnPreviewPool;
        private Transform spawnPreviewPoolRoot;

        protected virtual void Awake()
        {
            mainCamera = Camera.main;
        }

        // Were creating pools only for the enemies that are present in the Stage Timeline
        public virtual void Init(PlayableDirector director)
        {
            enemyDataDictionary = database.GetEnemyDataDictionary();

            stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");

            Dictionary<EnemyType, int> enemiesOnLevel = new Dictionary<EnemyType, int>();

            var waves = director.GetAssets<WaveTrack, WaveAsset>();

            for (int i = 0; i < waves.Count; i++)
            {
                var wave = waves[i];
                var enemyType = wave.EnemyType;
                var enemiesCount = wave.EnemiesCount;

                if (enemiesOnLevel.ContainsKey(enemyType))
                {
                    if (enemiesOnLevel[enemyType] < enemiesCount)
                    {
                        enemiesOnLevel[enemyType] = enemiesCount;
                    }
                }
                else
                {
                    enemiesOnLevel.Add(enemyType, enemiesCount);
                }
            }

            var trackEnemies = new List<EnemyType>();
            foreach (var output in director.playableAsset.outputs)
            {
                if (output.sourceObject is WaveTrack waveTrack)
                {
                    if (!trackEnemies.Contains(waveTrack.EnemyType))
                    {
                        trackEnemies.Add(waveTrack.EnemyType);
                    }
                }
            }

            enemyPools = new Dictionary<EnemyType, PoolComponent<EnemyBehavior>>();

            foreach (var enemyType in enemiesOnLevel.Keys)
            {
                var data = database.GetEnemyData(enemyType);

                var amount = enemiesOnLevel[enemyType];
                if (amount > 100) amount = 100;
                if (amount < 0) amount = 1;

                var pool = new PoolComponent<EnemyBehavior>($"Enemy {enemyType}", data.Prefab, amount);

                enemyPools.Add(data.Type, pool);
            }

            foreach (var enemyType in trackEnemies)
            {
                if (!enemyPools.ContainsKey(enemyType))
                {
                    var data = database.GetEnemyData(enemyType);
                    var pool = new PoolComponent<EnemyBehavior>($"Enemy {enemyType}", data.Prefab, 1);

                    enemyPools.Add(data.Type, pool);
                }
            }

            enemiesDiedCounter = 0;
            if (!stageSave.ResetStageData)
            {
                enemiesDiedCounter = stageSave.EnemiesKilled;
            }

            enemiesDiedLabel.SetAmount(enemiesDiedCounter);

            InitSpawnPreviewPool();
        }

        private void InitSpawnPreviewPool()
        {
            // Reset pool (if Init called multiple times)
            if (spawnPreviewPool == null) spawnPreviewPool = new Queue<GameObject>();
            else spawnPreviewPool.Clear();

            if (spawnPreviewPoolRoot == null)
            {
                var rootGo = new GameObject("SpawnPreviewPool");
                rootGo.transform.SetParent(transform, false);
                spawnPreviewPoolRoot = rootGo.transform;
            }

            if (spawnPreviewPrefab == null) return;

            int prewarm = Mathf.Max(0, spawnPreviewPoolSize);
            for (int i = 0; i < prewarm; i++)
            {
                var go = Instantiate(spawnPreviewPrefab, spawnPreviewPoolRoot);
                go.SetActive(false);
                spawnPreviewPool.Enqueue(go);
            }
        }

        private GameObject GetSpawnPreview(Vector3 position)
        {
            if (spawnPreviewPrefab == null) return null;

            GameObject go = null;

            if (spawnPreviewPool != null && spawnPreviewPool.Count > 0)
            {
                go = spawnPreviewPool.Dequeue();
                if (go == null)
                {
                    // In case something got destroyed unexpectedly, fallback
                    go = Instantiate(spawnPreviewPrefab);
                }
            }
            else
            {
                // Pool can expand if needed
                go = Instantiate(spawnPreviewPrefab);
            }

            // Keep it organized
            go.transform.SetParent(null);

            position.z = 0f;
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            go.SetActive(true);

            return go;
        }

        private void ReleaseSpawnPreview(GameObject go)
        {
            if (go == null) return;

            go.SetActive(false);
            if (spawnPreviewPoolRoot != null)
                go.transform.SetParent(spawnPreviewPoolRoot, false);

            if (spawnPreviewPool == null) spawnPreviewPool = new Queue<GameObject>();
            spawnPreviewPool.Enqueue(go);
        }

        protected virtual void Update()
        {
            if (!isOffscreenTeleportEnabled || IsBossfightActive || enemies.Count > enemiesTeleportCap) return;

            var diagonalSqr = (CameraManager.HalfWidth * CameraManager.HalfWidth + CameraManager.HalfHeight * CameraManager.HalfHeight) * diagonalDistanceMultiplier;
            var diagonal = Mathf.Sqrt(diagonalSqr);

            var dotValue = teleportConeSize - 1;
            var modValue = Mathf.Clamp(enemies.Count / 20, 1, 100);
            int frame = Time.frameCount % modValue;

            for (int i = frame; i < enemies.Count; i += modValue)
            {
                var enemy = enemies[i];

                if (enemy == null) continue;
                if (enemy.WaveOverride != null && enemy.WaveOverride.DisableOffscreenTeleport) continue;

                var enemyToPlayer = enemy.transform.position - PlayerBehavior.Player.transform.position;
                var direction = enemyToPlayer.normalized;
                var dot = Vector2.Dot(direction, PlayerBehavior.Player.LookDirection);

                if (diagonalSqr < enemyToPlayer.sqrMagnitude && dot < dotValue)
                {
                    var teleportPosition =
                        PlayerBehavior.Player.transform.position +
                        Quaternion.Euler(0, 0, Random.Range(-45, 45)) * PlayerBehavior.Player.LookDirection * diagonal;

                    // Clamp teleport strictly inside map
                    teleportPosition = StageController.FieldManager.ClampPositionInsideField(teleportPosition, enemySpawnPadding);

                    enemy.transform.position = teleportPosition;
                }
            }
        }

        public virtual EnemyBehavior GetClosestEnemy(Vector2 point)
        {
            EnemyBehavior closestEnemy = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                    i--;
                    continue;
                }

                float distance = (point - enemies[i].transform.position.XY()).sqrMagnitude;

                if (distance < closestDistance)
                {
                    closestEnemy = enemies[i];
                    closestDistance = distance;
                }
            }

            return closestEnemy;
        }

        // Instant spawn (kept for backwards compatibility / special cases).
        public virtual EnemyBehavior Spawn(EnemyType enemyType, Vector2 position, UnityAction<EnemyBehavior> onEnemyDiedCallback = null)
        {
            return SpawnInternal(enemyType, position, null, onEnemyDiedCallback);
        }

        // Spawn with preview marker (from pool) then enemy appears after delay.
        public virtual Coroutine SpawnWithPreview(EnemyType enemyType, Vector2 position, WaveOverride waveOverride = null, UnityAction<EnemyBehavior> onEnemyDiedCallback = null)
        {
            if (enemies.Count >= enemiesCap) return null;

            // Ensure preview & enemy share the same final safe position
            position = StageController.FieldManager.ClampPositionInsideField(position, enemySpawnPadding);

            return StartCoroutine(SpawnWithPreviewRoutine(enemyType, position, waveOverride, onEnemyDiedCallback));
        }

        private IEnumerator SpawnWithPreviewRoutine(EnemyType enemyType, Vector2 position, WaveOverride waveOverride, UnityAction<EnemyBehavior> onEnemyDiedCallback)
        {
            GameObject preview = null;

            if (spawnPreviewPrefab != null)
            {
                preview = GetSpawnPreview(position);
            }

            if (spawnPreviewDelay > 0f)
                yield return new WaitForSeconds(spawnPreviewDelay);

            if (preview != null)
                ReleaseSpawnPreview(preview);

            var enemy = SpawnInternal(enemyType, position, waveOverride, onEnemyDiedCallback);

            // Grace period: enemy can't hit player instantly (disable colliders briefly)
            if (enemy != null && spawnHitGraceDuration > 0f)
            {
                StartCoroutine(DisableEnemyCollidersTemporarily(enemy, spawnHitGraceDuration));
            }
        }

        private IEnumerator DisableEnemyCollidersTemporarily(EnemyBehavior enemy, float duration)
        {
            if (enemy == null) yield break;

            var colliders = enemy.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = false;
            }

            yield return new WaitForSeconds(duration);

            // enemy may be dead / returned to pool
            if (enemy == null || !enemy.gameObject.activeInHierarchy) yield break;
            if (!enemy.IsAlive) yield break;

            colliders = enemy.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = true;
            }
        }

        private EnemyBehavior SpawnInternal(EnemyType enemyType, Vector2 position, WaveOverride waveOverride, UnityAction<EnemyBehavior> onEnemyDiedCallback)
        {
            if (enemies.Count >= enemiesCap) return null;

            var enemyData = enemyDataDictionary[enemyType];

            if (!enemyPools.ContainsKey(enemyType))
            {
                var pool = new PoolComponent<EnemyBehavior>($"Enemy {enemyType}", enemyData.Prefab, 10);
                enemyPools.Add(enemyData.Type, pool);
            }

            var enemy = enemyPools[enemyType].GetEntity();

            enemy.SetData(enemyData);

            enemy.SetWaveOverride(waveOverride);

            // Safety clamp in case someone calls Spawn(...) with an out-of-map position
            position = StageController.FieldManager.ClampPositionInsideField(position, enemySpawnPadding);
            enemy.transform.position = position;

            enemy.onEnemyDied += OnEnemyDied;
            if (onEnemyDiedCallback != null) enemy.onEnemyDied += onEnemyDiedCallback;

            enemy.Play();

            enemies.Add(enemy);

            return enemy;
        }

        public virtual void Spawn(EnemyType type, WaveOverride waveOverride, bool circularSpawn = false, int amount = 1, UnityAction<EnemyBehavior> onEnemyDiedCallback = null)
        {
            for (int i = 0; i < amount; i++)
            {
                if (enemies.Count >= enemiesCap) return;

                var triesCount = 0;
                var maxTriesCount = 10;

                var position = Vector3.zero;
                var foundPosition = false;

                while (triesCount < maxTriesCount)
                {
                    triesCount++;

                    // when spawning a lot of enemies at once we want to offset them to not overload physics computations
                    var additionalDistance = amount > 100 ? Mathf.Sqrt(enemies.Count) * 0.1f : 0f;

                    var minRadius = Mathf.Max(0f, spawnNearPlayerMinRadius) + additionalDistance;
                    var maxRadius = Mathf.Max(minRadius + 0.01f, spawnNearPlayerMaxRadius + additionalDistance);

                    // Spawn around the player (nearby)
                    Vector3 dir = circularSpawn ? Random.onUnitSphere.SetZ(0).normalized : (Vector3)Random.insideUnitCircle.normalized;
                    position = PlayerBehavior.Player.transform.position + dir * Random.Range(minRadius, maxRadius);

                    if (StageController.FieldManager.ValidatePositionWithPadding(position, enemySpawnPadding))
                    {
                        foundPosition = true;
                        break;
                    }
                }

                if (!foundPosition)
                {
                    for (int j = 1; j < 10; j++)
                    {
                        var middlePosition = Vector3.Lerp(position, PlayerBehavior.Player.transform.position, 1f - j / 10f);

                        if (StageController.FieldManager.ValidatePositionWithPadding(middlePosition, enemySpawnPadding))
                        {
                            foundPosition = true;
                            position = middlePosition;
                            break;
                        }
                    }
                }

                if (!foundPosition)
                {
                    // Fallback to safe border point already moved inside by padding
                    position = StageController.FieldManager.GetRandomPositionOnBorder(enemySpawnPadding);
                }
                else
                {
                    // Final safety clamp (handles edge cases near corners)
                    position = StageController.FieldManager.ClampPositionInsideField(position, enemySpawnPadding);
                }

                // Use telegraph + grace period
                SpawnWithPreview(type, position, waveOverride, onEnemyDiedCallback);
            }
        }

        public virtual EnemyBehavior GetRandomVisibleEnemy()
        {
            if (enemies.Count == 0) return null;

            // Trying to find random visible enemy 10 times
            for (int i = 0; i < 10; i++)
            {
                var randomIndex = Random.Range(0, enemies.Count);

                var enemy = enemies[randomIndex];

                if (enemy.IsVisible) return enemy;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];

                if (enemy.IsVisible) return enemy;
            }

            return null;
        }

        public virtual List<EnemyBehavior> GetEnemiesInRadius(Vector2 position, float radius)
        {
            var result = new List<EnemyBehavior>();

            float radiusSqr = radius * radius;

            for (int i = 0; i < enemies.Count; i++)
            {
                if ((enemies[i].transform.position.XY() - position).sqrMagnitude <= radiusSqr)
                {
                    result.Add(enemies[i]);
                }
            }

            return result;
        }

        public virtual void KillEveryEnemy()
        {
            foreach (var enemy in enemies)
            {
                enemy.onEnemyDied -= OnEnemyDied;
                enemy.Kill();
            }

            enemiesDiedCounter += enemies.Count;
            stageSave.EnemiesKilled = enemiesDiedCounter;

            enemiesDiedLabel.SetAmount(enemiesDiedCounter);

            enemies.Clear();
        }

        public virtual void DealDamageToAllEnemies(float damage)
        {
            var aliveEnemies = new List<EnemyBehavior>();

            foreach (var enemy in enemies)
            {
                if (enemy.HP <= damage)
                {
                    // if enemy is not a boss
                    if (enemy.Data != null)
                    {
                        enemy.onEnemyDied -= OnEnemyDied;
                        enemy.Kill();

                        foreach (var dropData in enemy.GetDropData())
                        {
                            if (dropData.Chance == 0) continue;

                            if (Random.value * 100 <= dropData.Chance && StageController.DropManager.CheckDropCooldown(dropData.DropType))
                            {
                                StageController.DropManager.Drop(dropData.DropType, enemy.transform.position.XY() + Random.insideUnitCircle * 0.2f);
                            }
                        }
                    }
                    else
                    {
                        aliveEnemies.Add(enemy);
                    }
                }
                else
                {
                    // if enemy is not a boss
                    if (enemy.Data != null)
                    {
                        enemy.TakeDamage(damage);
                    }
                    aliveEnemies.Add(enemy);
                }
            }

            enemiesDiedCounter += enemies.Count - aliveEnemies.Count;

            stageSave.EnemiesKilled = enemiesDiedCounter;
            enemiesDiedLabel.SetAmount(enemiesDiedCounter);

            enemies.Clear();
            enemies.AddRange(aliveEnemies);
        }

        protected virtual void OnEnemyDied(EnemyBehavior enemy)
        {
            enemies.RemoveSwapBack(enemy);
            enemy.onEnemyDied -= OnEnemyDied;

            foreach (var dropData in enemy.GetDropData())
            {
                if (dropData.Chance == 0) continue;
                if (Random.value * 100 <= dropData.Chance && StageController.DropManager.CheckDropCooldown(dropData.DropType))
                {
                    StageController.DropManager.Drop(dropData.DropType, enemy.transform.position.XY() + Random.insideUnitCircle * 0.2f);
                }
            }

            enemiesDiedCounter++;
            stageSave.EnemiesKilled = enemiesDiedCounter;
            enemiesDiedLabel.SetAmount(enemiesDiedCounter);
        }

        protected virtual void OnBossDied(EnemyBehavior boss)
        {
            enemies.RemoveSwapBack(boss);
            boss.onEnemyDied -= OnBossDied;

            if (boss.ShouldSpawnChestOnDeath && StageController.AbilityManager.HasAvailableAbilities()) StageController.DropManager.Drop(DropType.Chest, boss.transform.position.XY() + Random.insideUnitCircle);
            StageController.DropManager.Drop(DropType.Magnet, boss.transform.position.XY() + Random.insideUnitCircle);
            StageController.DropManager.Drop(DropType.Food, boss.transform.position.XY() + Random.insideUnitCircle);

            enemiesDiedCounter++;
            stageSave.EnemiesKilled = enemiesDiedCounter;
            enemiesDiedLabel.SetAmount(enemiesDiedCounter);
        }

        public virtual EnemyBehavior SpawnBoss(BossType bossType, Vector2 spawnPosition, UnityAction<EnemyBehavior> onBossDied = null)
        {
            var bossData = bossfightDatabase.GetBossfight(bossType);

            var boss = Instantiate(bossData.BossPrefab).GetComponent<EnemyBehavior>();

            // На всякий случай тоже не даём боссу появиться вне карты
            spawnPosition = StageController.FieldManager.ClampPositionInsideField(spawnPosition, enemySpawnPadding);
            boss.transform.position = spawnPosition;

            boss.Play();

            boss.onEnemyDied += OnBossDied;
            boss.onEnemyDied += onBossDied;

            enemies.Add(boss);

            return boss;
        }

        public virtual BossfightData GetBossData(BossType bossType)
        {
            return bossfightDatabase.GetBossfight(bossType);
        }
    }
}