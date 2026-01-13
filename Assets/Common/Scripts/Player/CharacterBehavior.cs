using OctoberStudio.Easing;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    /// <summary>
    /// ICharacterBehavior implementation for Spine персонажа (SkeletonAnimation).
    /// ”правл€ет анимаци€ми по именам (Idle/Run/Die/Hit).
    /// </summary>
    public class CharacterBehavior : MonoBehaviour, ICharacterBehavior
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation; // основной компонент (внутри есть и SkeletonRenderer)

        [Header("Animation Names")]
        [SerializeField] private string idleAnim = "Idle";
        [SerializeField] private string runAnim = "Run";
        [SerializeField] private string walkAnim = "";     // опционально
        [SerializeField] private string defeatAnim = "Die";
        [SerializeField] private string reviveAnim = "";   // если нет Ч оставь пустым
        [SerializeField] private string hitAnim = "Hit";   // если нет Ч оставь пустым

        [Header("Movement")]
        [SerializeField, Range(0.01f, 0.5f)] private float moveThreshold = 0.05f;

        [Header("Hit Flash")]
        [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private float hitFlashDuration = 0.10f;

        [Header("Center")]
        [SerializeField] private Transform centerTransform;
        public Transform CenterTransform => centerTransform != null ? centerTransform : transform;

        public Transform Transform => transform;

        private IEasingCoroutine _hitCoroutine;
        private bool _isDead;
        private bool _isMoving;

        private Spine.AnimationState _state; // Spine.AnimationState

        [Header("Flip/Scale")]
        [SerializeField] private Transform visualRoot; // сюда: объект где SkeletonAnimation (обычно child)
        [SerializeField] private bool invertFlip = false; // если вправо смотрит влево Ч включи

        [Header("Center Auto-find")]
        [SerializeField] private bool autoFindCenterInParents = true;
        [SerializeField] private string[] centerNames = { "Center", "center", "CenterPoint", "Center Point" };

        private Vector3 _baseVisualScale;
        private int _facingSign = 1;


        private void Awake()
        {
            if (!skeletonAnimation)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);

            if (!visualRoot)
                visualRoot = skeletonAnimation != null ? skeletonAnimation.transform : transform;

            if (centerTransform == null && autoFindCenterInParents)
            {
                centerTransform = FindCenterInParents();
            }

            _baseVisualScale = visualRoot.localScale; // тут твои 0.3,0.3,0.3


            EnsureReady();
        }

        private void Start()
        {
            EnsureReady();
            PlayBase(idleAnim, loop: true);
        }

        private Transform FindCenterInParents()
        {
            // 1) »щем у родител€ (Player) по стандартным именам
            var p = transform.parent;
            if (p != null)
            {
                foreach (var n in centerNames)
                {
                    var t = p.Find(n);
                    if (t != null) return t;
                }
            }

            // 2) ≈сли вдруг Center лежит глубже/выше Ч пробегаем вверх по цепочке
            var cur = transform.parent;
            while (cur != null)
            {
                foreach (var n in centerNames)
                {
                    var t = cur.Find(n);
                    if (t != null) return t;
                }
                cur = cur.parent;
            }

            // 3) ‘олбэк: если вообще не нашли Ч используем свой transform
            return transform;
        }

        private bool EnsureReady()
        {
            if (skeletonAnimation == null) return false;

            // ¬ твоей версии нет IsValid Ч есть skeletonAnimation.valid (унаследовано от SkeletonRenderer).
            // Initialize(false) безопасно: если уже valid, оно просто вернетс€.
            skeletonAnimation.Initialize(false);

            if (!skeletonAnimation.valid) return false;

            // AnimationState property сам вызывает Initialize(false) и возвращает state
            _state ??= skeletonAnimation.AnimationState;
            return _state != null;
        }

        public virtual void SetSpeed(float speed)
        {
            if (_isDead) return;
            if (!EnsureReady()) return;

            bool movingNow = speed > moveThreshold;
            if (movingNow == _isMoving) return;
            _isMoving = movingNow;

            if (_isMoving)
            {
                var anim = !string.IsNullOrWhiteSpace(runAnim) ? runAnim : walkAnim;
                PlayBase(anim, loop: true);
            }
            else
            {
                PlayBase(idleAnim, loop: true);
            }
        }

        public void SetLocalScale(Vector3 scale)
        {
            if (visualRoot == null) return;

            int sign = scale.x >= 0 ? 1 : -1;
            if (invertFlip) sign = -sign;

            if (_facingSign == sign) return;
            _facingSign = sign;

            var s = _baseVisualScale;
            s.x = Mathf.Abs(_baseVisualScale.x) * _facingSign; // сохран€ем 0.3 и мен€ем только знак
            visualRoot.localScale = s;
        }


        public void PlayReviveAnimation()
        {
            _isDead = false;
            if (!EnsureReady()) return;

            if (!string.IsNullOrWhiteSpace(reviveAnim) && HasAnimation(reviveAnim))
            {
                _state.SetAnimation(0, reviveAnim, false);

                if (!string.IsNullOrWhiteSpace(idleAnim) && HasAnimation(idleAnim))
                    _state.AddAnimation(0, idleAnim, true, 0f);
            }
            else
            {
                PlayBase(idleAnim, loop: true);
            }
        }

        public void PlayDefeatAnimation()
        {
            _isDead = true;
            if (!EnsureReady()) return;

            if (!string.IsNullOrWhiteSpace(defeatAnim) && HasAnimation(defeatAnim))
                _state.SetAnimation(0, defeatAnim, false);
            else
                PlayBase(idleAnim, loop: true);
        }

        public void SetSortingOrder(int order)
        {
            // SkeletonRenderer Ќ≈ имеет sortingOrder Ч он на MeshRenderer.
            MeshRenderer mr = null;

            if (skeletonAnimation != null)
                mr = skeletonAnimation.GetComponent<MeshRenderer>();

            if (!mr)
                mr = GetComponentInChildren<MeshRenderer>(true);

            if (mr)
                mr.sortingOrder = order;
        }

        public void FlashHit(UnityAction onFinish = null)
        {
            if (!EnsureReady())
            {
                onFinish?.Invoke();
                return;
            }

            if (_hitCoroutine.ExistsAndActive())
                return;

            // 1) Hit анимаци€ на overlay track (1)
            if (!string.IsNullOrWhiteSpace(hitAnim) && HasAnimation(hitAnim))
            {
                _state.SetAnimation(1, hitAnim, false);
                _state.AddEmptyAnimation(1, 0.05f, hitFlashDuration);
            }

            // 2) флешим tint скелета
            var skel = skeletonAnimation.Skeleton;
            var old = new Color(skel.R, skel.G, skel.B, skel.A);

            SetSkeletonColor(hitColor);

            _hitCoroutine = EasingManager.DoAfter(hitFlashDuration, () =>
            {
                SetSkeletonColor(old);
                onFinish?.Invoke();
            });
        }

        // ===== helpers =====

        private void PlayBase(string animName, bool loop)
        {
            if (!EnsureReady()) return;
            if (string.IsNullOrWhiteSpace(animName) || !HasAnimation(animName))
                return;

            TrackEntry cur = _state.GetCurrent(0);
            if (cur != null && cur.Animation != null && cur.Animation.Name == animName && cur.Loop == loop)
                return;

            _state.SetAnimation(0, animName, loop);
        }

        private bool HasAnimation(string animName)
        {
            if (!EnsureReady()) return false;

            var asset = skeletonAnimation.SkeletonDataAsset;
            if (asset == null) return false;

            var data = asset.GetSkeletonData(false);
            if (data == null) return false;

            return data.FindAnimation(animName) != null;
        }

        private void SetSkeletonColor(Color c)
        {
            if (!EnsureReady()) return;

            var skel = skeletonAnimation.Skeleton;
            skel.R = c.r;
            skel.G = c.g;
            skel.B = c.b;
            skel.A = c.a;
        }
    }
}

