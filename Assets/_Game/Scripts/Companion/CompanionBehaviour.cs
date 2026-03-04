using System.Collections;
using UnityEngine;
using Spine.Unity;
using OctoberStudio;
using OctoberStudio.Pool;
using OctoberStudio.Abilities;

public class CompanionBehaviour : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform followTarget;               // auto-find PlayerBehavior in Start if null
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float smoothTime = 0.12f;

    [Header("Visual / Flip")]
    [Tooltip("Transform that contains the visuals (Skeleton, weapon, etc.). Flip happens here, not on the root.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("Face enemy when possible. If no enemies -> fallback to movement flip.")]
    [SerializeField] private bool faceEnemyWhenPossible = true;

    [SerializeField] private bool flipByMovementFallback = true;
    [SerializeField] private float flipDeadZone = 0.01f;

    [Header("Target Stabilization")]
    [Tooltip("How often companion is allowed to reacquire/switch targets (seconds).")]
    [SerializeField] private float targetReacquireInterval = 0.25f;

    [Tooltip("New target must be closer by this amount to force a switch (prevents jitter between similar distances).")]
    [SerializeField] private float switchDistanceBias = 0.75f;

    [Header("Spine")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private string idleAnim = "Idle2";
    [SerializeField] private string runAnim = "Run";

    [Header("Animation Stability")]
    [SerializeField] private float runEnterSpeed = 0.05f;
    [SerializeField] private float runExitSpeed = 0.02f;

    [Header("Weapon / Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform weaponRoot;                 // rotate this around Z
    [SerializeField] private GameObject projectilePrefab;          // same prefab as wand
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileLifeTime = 1.25f;
    [SerializeField] private float projectileSize = 1f;
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Aim")]
    [Tooltip("If your weapon art points UP when Z=0, set +90. If it points RIGHT when Z=0, set 0.")]
    [SerializeField] private float weaponAngleOffset = 0f;

    [Tooltip("Your proven fix: pistol art faces LEFT by default; when NOT flipped we add 180 to aim correctly.")]
    [SerializeField] private bool enableFlipAimFix = true;

    [Header("Fire Rate")]
    [SerializeField] private float shootCooldown = 0.6f;
    [SerializeField] private float maxTargetDistance = 999f;

    [Header("Audio (optional)")]
    [SerializeField] private bool playWandSfx = true;

    private PoolComponent<SimplePlayerProjectileBehavior> projectilePool;
    private Coroutine shootRoutine;

    private Vector2 _velRef;
    private Vector2 _lastMoveDir;
    private float _currentSpeed;

    private bool _isRunning;
    private string _currentAnim;

    private float _visualBaseScaleX;

    // Enemy facing data (no dynamic/object)
    private bool _hasEnemyTarget;
    private Vector2 _enemyCenter;      // chosen target center
    private float _nextReacquireTime;

    private void Awake()
    {
        if (!skeletonAnimation) skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);

        if (!visualRoot)
            visualRoot = skeletonAnimation ? skeletonAnimation.transform : transform;

        _visualBaseScaleX = Mathf.Abs(visualRoot.localScale.x);

        if (projectilePrefab == null)
            Debug.LogError("[CompanionBehaviour] projectilePrefab is NULL. Assign the same prefab as WandWeaponAbilityBehavior.", this);

        projectilePool = new PoolComponent<SimplePlayerProjectileBehavior>("Companion Projectiles", projectilePrefab, 50);
    }

    private void Start()
    {
        if (followTarget == null)
        {
            var pb = FindFirstObjectByType<PlayerBehavior>();
            if (pb != null) followTarget = pb.transform;
        }

        if (followTarget == null)
            Debug.LogWarning("[CompanionBehaviour] followTarget not found. Companion will not move until assigned.", this);

        PlayAnim(idleAnim);
        _isRunning = false;
    }

    private void OnEnable()
    {
        shootRoutine = StartCoroutine(ShootLoop());
    }

    private void OnDisable()
    {
        if (shootRoutine != null) StopCoroutine(shootRoutine);
        shootRoutine = null;
    }

    private void Update()
    {
        if (followTarget == null) return;

        Vector3 before = transform.position;

        Follow();

        Vector3 after = transform.position;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        _currentSpeed = (after - before).magnitude / dt;

        // 1) Update stable enemy target (center only)
        if (faceEnemyWhenPossible)
            UpdateEnemyTargetStable();

        // 2) Flip logic: face enemy if exists, otherwise fallback to movement
        if (_hasEnemyTarget)
        {
            Vector2 dirToEnemy = _enemyCenter - (Vector2)transform.position;
            FlipTowardsTarget(dirToEnemy);
        }
        else if (flipByMovementFallback)
        {
            UpdateFlipByMovement(before, after);
        }

        UpdateAnimationStable();
    }

    private void Follow()
    {
        Vector2 targetPos = followTarget.position;
        Vector2 pos = transform.position;

        float dist = Vector2.Distance(pos, targetPos);

        if (dist > followDistance)
        {
            Vector2 dirToTarget = (targetPos - pos).normalized;
            Vector2 desired = targetPos - dirToTarget * stopDistance;

            Vector2 newPos = Vector2.SmoothDamp(pos, desired, ref _velRef, smoothTime, moveSpeed);
            transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
        }
        else
        {
            _velRef = Vector2.zero;
        }
    }

    /// <summary>
    /// Stabilized target picking:
    /// - Reacquire no more often than targetReacquireInterval
    /// - Switch only if new target is closer by switchDistanceBias
    /// Stores only enemy.Center (Vector2), no references needed.
    /// </summary>
    private void UpdateEnemyTargetStable()
    {
        _hasEnemyTarget = false;

        if (StageController.EnemiesSpawner == null)
            return;

        Vector2 origin = firePoint ? (Vector2)firePoint.position : (Vector2)transform.position;

        // If we already have a target and it's too soon to reacquire, keep it
        if (Time.time < _nextReacquireTime && _enemyCenter != Vector2.zero)
        {
            if (maxTargetDistance > 0f && Vector2.Distance(origin, _enemyCenter) > maxTargetDistance)
            {
                _enemyCenter = Vector2.zero;
                return;
            }

            _hasEnemyTarget = true;
            return;
        }

        _nextReacquireTime = Time.time + targetReacquireInterval;

        var candidate = StageController.EnemiesSpawner.GetClosestEnemy(origin);
        if (candidate == null)
        {
            _enemyCenter = Vector2.zero;
            return;
        }

        Vector2 candidateCenter = candidate.Center;

        if (maxTargetDistance > 0f && Vector2.Distance(origin, candidateCenter) > maxTargetDistance)
        {
            _enemyCenter = Vector2.zero;
            return;
        }

        // If we had a previous center, only switch if candidate is meaningfully closer
        if (_enemyCenter != Vector2.zero)
        {
            float currentDist = Vector2.Distance(origin, _enemyCenter);
            float candDist = Vector2.Distance(origin, candidateCenter);

            if (candDist + switchDistanceBias < currentDist)
                _enemyCenter = candidateCenter; // switch
            // else keep old
        }
        else
        {
            _enemyCenter = candidateCenter;
        }

        _hasEnemyTarget = (_enemyCenter != Vector2.zero);
    }

    private void FlipTowardsTarget(Vector2 dirToEnemy)
    {
        if (visualRoot == null) return;
        if (Mathf.Abs(dirToEnemy.x) < flipDeadZone) return;

        // Base companion art faces LEFT at positive scale.
        // Enemy on the RIGHT -> negative scale to face RIGHT.
        float sign = dirToEnemy.x >= 0f ? -1f : 1f;

        var s = visualRoot.localScale;
        s.x = _visualBaseScaleX * sign;
        visualRoot.localScale = s;
    }

    private void UpdateFlipByMovement(Vector3 before, Vector3 after)
    {
        if (visualRoot == null) return;

        Vector2 delta = (Vector2)(after - before);
        if (delta.sqrMagnitude <= (flipDeadZone * flipDeadZone)) return;

        _lastMoveDir = delta.normalized;

        if (Mathf.Abs(_lastMoveDir.x) < flipDeadZone)
            return;

        // Same base-facing-left convention:
        float sign = _lastMoveDir.x >= 0f ? -1f : 1f;

        var s = visualRoot.localScale;
        s.x = _visualBaseScaleX * sign;
        visualRoot.localScale = s;
    }

    private void UpdateAnimationStable()
    {
        if (!skeletonAnimation) return;

        if (!_isRunning)
        {
            if (_currentSpeed >= runEnterSpeed)
                _isRunning = true;
        }
        else
        {
            if (_currentSpeed <= runExitSpeed)
                _isRunning = false;
        }

        string desired = _isRunning ? runAnim : idleAnim;
        PlayAnim(desired);
    }

    private void PlayAnim(string animName)
    {
        if (string.IsNullOrWhiteSpace(animName) || !skeletonAnimation) return;
        if (_currentAnim == animName) return;

        skeletonAnimation.AnimationState.SetAnimation(0, animName, true);
        _currentAnim = animName;
    }

    // ---------------- Shooting ----------------
    private IEnumerator ShootLoop()
    {
        yield return null;

        while (true)
        {
            TryShootAtClosestEnemy();
            yield return new WaitForSeconds(shootCooldown);
        }
    }

    private void TryShootAtClosestEnemy()
    {
        if (StageController.EnemiesSpawner == null) return;

        Vector2 origin = firePoint ? (Vector2)firePoint.position : (Vector2)transform.position;

        var enemy = StageController.EnemiesSpawner.GetClosestEnemy(origin);
        if (enemy == null) return;

        if (maxTargetDistance > 0f && Vector2.Distance(origin, enemy.Center) > maxTargetDistance)
            return;

        Vector2 dir = (enemy.Center - origin);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        AimWeapon(origin, enemy.Center);
        SpawnProjectile(origin, dir);
    }

    private void AimWeapon(Vector2 origin, Vector2 target)
    {
        if (weaponRoot == null) return;

        Vector2 d = target - origin;
        if (d.sqrMagnitude < 0.0001f) d = Vector2.right;

        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        angle += weaponAngleOffset;

        // Your proven fix:
        bool flipped = visualRoot != null && visualRoot.lossyScale.x < 0f;
        if (enableFlipAimFix && !flipped)
            angle += 180f;

        weaponRoot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SpawnProjectile(Vector2 position, Vector2 direction)
    {
        var projectile = projectilePool.GetEntity();

        projectile.Init(position, direction);
        projectile.Speed = projectileSpeed;
        projectile.transform.localScale = Vector3.one * projectileSize;
        projectile.LifeTime = projectileLifeTime;
        projectile.DamageMultiplier = damageMultiplier;

        if (PlayerBehavior.Player != null)
        {
            projectile.Speed *= PlayerBehavior.Player.ProjectileSpeedMultiplier;
            projectile.transform.localScale *= PlayerBehavior.Player.SizeMultiplier;
        }

        if (playWandSfx)
            GameController.AudioManager.PlaySound(WandWeaponAbilityBehavior.WAND_PROJECTILE_LAUNCH_HASH);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followDistance);
    }
#endif
}