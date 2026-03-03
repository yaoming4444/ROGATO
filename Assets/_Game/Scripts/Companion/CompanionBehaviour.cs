using System.Collections;
using UnityEngine;
using Spine.Unity;
using OctoberStudio;
using OctoberStudio.Pool;
using OctoberStudio.Abilities;

public class CompanionBehaviour : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float smoothTime = 0.12f;

    [Header("Visual / Flip")]
    [Tooltip("Transform that contains the visuals (Skeleton, weapon, etc.). Flip happens here, not on the root.")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool flipByMovement = true;
    [SerializeField] private float flipDeadZone = 0.01f; // avoid jitter

    [Header("Spine")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private string idleAnim = "Idle2";
    [SerializeField] private string runAnim = "Run";

    [Header("Weapon / Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform weaponRoot; // rotate this around Z
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileLifeTime = 1.25f;
    [SerializeField] private float projectileSize = 1f;
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Aim")]
    [SerializeField] private float maxAimAngle = 90f;

    [Header("Fire Rate")]
    [SerializeField] private float shootCooldown = 0.6f;
    [SerializeField] private float maxTargetDistance = 999f;

    [Header("Audio (optional)")]
    [SerializeField] private bool playWandSfx = true;

    private PoolComponent<SimplePlayerProjectileBehavior> projectilePool;
    private Coroutine shootRoutine;

    private Vector2 _velRef;
    private Vector2 _lastMoveDir;
    private bool _isMoving;
    private string _currentAnim;

    private float _visualBaseScaleX;

    private void Awake()
    {
        if (!skeletonAnimation) skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);

        if (!visualRoot)
        {
            // Prefer SkeletonAnimation transform as visual root
            visualRoot = skeletonAnimation ? skeletonAnimation.transform : transform;
        }

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

        Follow();
        if (flipByMovement) UpdateFlip();
        UpdateAnimation();
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
            Vector2 delta = newPos - pos;

            transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);

            _isMoving = delta.sqrMagnitude > (flipDeadZone * flipDeadZone);
            if (_isMoving) _lastMoveDir = delta.normalized;
        }
        else
        {
            _velRef = Vector2.zero;
            _isMoving = false;
        }
    }

    private void UpdateFlip()
    {
        if (visualRoot == null) return;

        // flip based on last movement direction (stable)
        if (!_isMoving) return;

        if (Mathf.Abs(_lastMoveDir.x) < flipDeadZone)
            return;

        float sign = (_lastMoveDir.x >= 0f) ? -1f : 1f;

        var s = visualRoot.localScale;
        s.x = _visualBaseScaleX * sign;
        visualRoot.localScale = s;
    }

    private void UpdateAnimation()
    {
        if (!skeletonAnimation) return;

        var desired = _isMoving ? runAnim : idleAnim;
        if (_currentAnim == desired) return;

        skeletonAnimation.AnimationState.SetAnimation(0, desired, true);
        _currentAnim = desired;
    }

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

        Vector2 d = (target - origin);
        if (d.sqrMagnitude < 0.0001f) d = Vector2.right;

        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

        // If weapon art is "up" at Z=0, add +90 (or -90) here:
        // angle += 90f;

        angle = Mathf.Clamp(angle, -maxAimAngle, maxAimAngle);
        weaponRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
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
            projectile.DamageMultiplier *= PlayerBehavior.Player.Damage;
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