using System;
using UnityEngine;
using GameCore.Items;

namespace GameCore.UI
{
    public class ChestController : MonoBehaviour
    {
        [SerializeField] private ChestDropTable dropTable;
        [SerializeField] private ChestRewardPopup popup;

        [Header("Chest Button Animation")]
        [SerializeField] private ChestAnimDriver chestAnim;

        [Header("Auto Open Runner (optional)")]
        [SerializeField] private AutoChestRunner autoRunner; // ? ????????

        private bool _busy;           // protects from spam clicks while opening / popup open
        private ItemDef _pendingItem; // item awaiting decision

        public bool IsBusy => _busy;
        public ItemDef PendingItem => _pendingItem;

        private void Awake()
        {
            if (popup != null && popup.gameObject.activeSelf)
                popup.gameObject.SetActive(false);

            if (autoRunner == null)
                autoRunner = FindObjectOfType<AutoChestRunner>(true);

            if (popup != null)
            {
                popup.OnDecisionMade -= OnPopupDecision;
                popup.OnDecisionMade += OnPopupDecision;
            }
        }

        private void Start()
        {
            RestorePendingIfAny();
        }

        private void OnDestroy()
        {
            if (popup != null)
                popup.OnDecisionMade -= OnPopupDecision;
        }

        /// <summary>
        /// ? ?????? "X / Close" ?? ?????? ?????? ???????? ???? ?????.
        /// - ???????? ?????
        /// - ????????? ????-??? (?? pending ?????????!)
        /// - ?????? ??????? ???????? ? ? ??????? ????????
        /// </summary>
        public void HideRewardPopup()
        {
            // ? ?????????? ????-???, ?? ?? ??????? pending
            if (autoRunner != null && autoRunner.IsRunning)
                autoRunner.DisableAutoKeepPending();

            if (popup != null && popup.gameObject.activeSelf)
                popup.Hide();

            // ?????? ????? ???????? ?? ??????? ? ??????? ????? ???????
            _busy = false;
        }

        private void OnPopupDecision()
        {
            // ????? ?????? ??????? => ??????? pending, ?????????? ?????? ? idle
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.ClearPendingChestReward(immediateSave: false);

            if (chestAnim != null)
                chestAnim.ResetToIdle();

            _pendingItem = null;
            _busy = false;

            if (gi != null)
                gi.SaveAllNow();
        }

        private void RestorePendingIfAny()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || popup == null)
                return;

            if (!gi.HasPendingChestReward)
                return;

            var item = ResolveItem(gi.PendingChestItemId);
            if (item == null)
            {
                gi.ClearPendingChestReward(immediateSave: true);
                if (chestAnim != null) chestAnim.ResetToIdle();
                return;
            }

            _pendingItem = item;
            _busy = false;

            if (popup.gameObject.activeSelf)
                popup.Hide();

            if (chestAnim != null)
                chestAnim.SetOpenedStatic(item.Icon, item.RarityVFX);
        }

        private static ItemDef ResolveItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            var db = ItemDatabase.I;
            if (db == null)
                return null;

            return db.GetById(itemId);
        }

        /// <summary>
        /// ???????? ????? ?? ???????? pending.
        /// </summary>
        public void ShowPendingPopup()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || popup == null) return;
            if (!gi.HasPendingChestReward) return;

            if (_pendingItem == null || _pendingItem.Id != gi.PendingChestItemId)
                _pendingItem = ResolveItem(gi.PendingChestItemId);

            if (_pendingItem == null)
            {
                gi.ClearPendingChestReward(immediateSave: true);
                if (chestAnim != null) chestAnim.ResetToIdle();
                _busy = false;
                return;
            }

            if (!popup.gameObject.activeSelf)
            {
                int lvl = gi.PendingChestItemLevel;
                popup.Show(_pendingItem, lvl);
                _busy = true;
            }
        }

        /// <summary>
        /// ??????? pending ??? ?????? (??? ????-???????/????-?????).
        /// </summary>
        public void FinalizePending()
        {
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.ClearPendingChestReward(immediateSave: false);

            if (chestAnim != null)
                chestAnim.ResetToIdle();

            _pendingItem = null;
            _busy = false;

            if (gi != null)
                gi.SaveAllNow();
        }

        public void FinalizePendingKeepVisual()
        {
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.ClearPendingChestReward(immediateSave: false);

            _pendingItem = null;
            _busy = false;

            if (gi != null)
                gi.SaveAllNow();
        }

        /// <summary>
        /// ????-?????: ????????? ?????? ? ???????? onOpened ????? ????????? ????????.
        /// ????? ?? ?????????? ?????????????.
        /// </summary>
        public bool TryOpenChestAuto(Action<ItemDef> onOpened)
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || dropTable == null || popup == null)
                return false;

            // ???? pending ? ?? ?????? ??????, ?????? ????? ???????
            if (gi.HasPendingChestReward)
            {
                if (_pendingItem == null || _pendingItem.Id != gi.PendingChestItemId)
                    _pendingItem = ResolveItem(gi.PendingChestItemId);

                if (_pendingItem == null)
                {
                    gi.ClearPendingChestReward(immediateSave: true);
                    if (chestAnim != null) chestAnim.ResetToIdle();
                    _busy = false;
                    return false;
                }

                onOpened?.Invoke(_pendingItem);
                return true;
            }

            if (_busy) return false;

            // 1) ?????? ??????
            if (!gi.SpendChest(1, immediateSave: false))
                return false;

            _busy = true;

            // 2) ????
            gi.AddExp(10, immediateSave: false);

            // 3) ???? ????????
            var rolled = ChestService.Roll(dropTable);
            var item = rolled.Item;
            if (item == null)
            {
                Debug.LogWarning("[Chest] Rolled null item (db pool empty?)");
                gi.SaveAllNow();
                _busy = false;
                return false;
            }

            _pendingItem = item;

            // itemLevel from player level (+ small chestLevel impact)
            int playerLevel = (gi.State != null) ? Mathf.Max(1, gi.State.Level) : 1;
            int chestLevel = (gi.State != null) ? Mathf.Max(1, gi.State.ChestLevel) : 1;
            int itemLevel = CalcDropItemLevel(playerLevel, chestLevel);

            // 4) pending save (id + level)
            gi.SetPendingChestReward(item.Id, itemLevel, immediateSave: false);

            // 5) ??????? ?????
            if (popup.gameObject.activeSelf)
                popup.Hide();

            // 6) ????????? ????????; callback ????? ????? ????????
            if (chestAnim != null)
            {
                chestAnim.PlayOpen(item.Icon, item.RarityVFX, onOpened: () =>
                {
                    if (_pendingItem == null) { _busy = false; return; }
                    onOpened?.Invoke(_pendingItem);
                });
            }
            else
            {
                onOpened?.Invoke(item);
            }

            gi.SaveAllNow();
            return true;
        }

        // ?????? ?????
        public void OpenChest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || dropTable == null || popup == null)
                return;

            if (gi.HasPendingChestReward)
            {
                if (_pendingItem == null || _pendingItem.Id != gi.PendingChestItemId)
                    _pendingItem = ResolveItem(gi.PendingChestItemId);

                if (_pendingItem == null)
                {
                    gi.ClearPendingChestReward(immediateSave: true);
                    if (chestAnim != null) chestAnim.ResetToIdle();
                    _busy = false;
                    return;
                }

                if (!popup.gameObject.activeSelf)
                {
                    int lvl = gi.PendingChestItemLevel;
                    popup.Show(_pendingItem, lvl);
                    _busy = true;
                }

                return;
            }

            if (_busy) return;

            if (!gi.SpendChest(1, immediateSave: false))
                return;

            _busy = true;

            gi.AddExp(10, immediateSave: false);

            var rolled = ChestService.Roll(dropTable);
            var item = rolled.Item;
            if (item == null)
            {
                Debug.LogWarning("[Chest] Rolled null item (db pool empty?)");
                gi.SaveAllNow();
                _busy = false;
                return;
            }

            _pendingItem = item;

            // itemLevel from player level (+ small chestLevel impact)
            int playerLevel = (gi.State != null) ? Mathf.Max(1, gi.State.Level) : 1;
            int chestLevel = (gi.State != null) ? Mathf.Max(1, gi.State.ChestLevel) : 1;
            int itemLevel = CalcDropItemLevel(playerLevel, chestLevel);

            gi.SetPendingChestReward(item.Id, itemLevel, immediateSave: false);

            if (popup.gameObject.activeSelf)
                popup.Hide();

            if (chestAnim != null)
            {
                chestAnim.PlayOpen(item.Icon, item.RarityVFX, onOpened: () =>
                {
                    if (_pendingItem == null) { _busy = false; return; }
                    popup.Show(_pendingItem, itemLevel);
                });
            }
            else
            {
                popup.Show(item, itemLevel);
            }

            gi.SaveAllNow();
        }

        // =========================
        // Item level logic (simple)
        // =========================
        private static int CalcDropItemLevel(int playerLevel, int chestLevel)
        {
            int p = Mathf.Max(1, playerLevel);
            int c = Mathf.Max(1, chestLevel);

            int minOffset = -3;
            if (c >= 5) minOffset = -2;
            if (c >= 10) minOffset = -1;

            // Range(min, max) for int: max not included, so (1) gives 0 as max
            int offset = UnityEngine.Random.Range(minOffset, 1); // [minOffset..0]
            int lvl = p + offset;

            lvl = Mathf.Clamp(lvl, 1, p);
            return lvl;
        }
    }
}
