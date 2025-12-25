using UnityEngine;
using Newtonsoft.Json;
using IDosGames;
using GameCore.Items;

public class TestSave : MonoBehaviour
{
    [Header("Load from server")]
    [SerializeField] private float requestDelaySeconds = 0.6f;

    [Header("Test Equip")]
    [Tooltip("ItemDef.Id (из ScriptableObject). Например: Weapon_G_01")]
    [SerializeField] private string testItemId = "";

    // ? как у взрослых: меняем через GameInstance API
    public void AddGold123()
    {
        GameCore.GameInstance.I.AddGold(123);
        Debug.Log("[TestSave] AddGold(123)");
    }

    public void LevelUp()
    {
        GameCore.GameInstance.I.LevelUp();
        Debug.Log("[TestSave] LevelUp()");
    }

    public void SaveLocal()
    {
        GameCore.GameInstance.I.SaveLocalNow();
        Debug.Log("[TestSave] SaveLocalNow()");
    }

    public void SaveServer()
    {
        GameCore.GameInstance.I.SaveServerNow();
        Debug.Log("[TestSave] SaveServerNow()");
    }

    public void SaveAll()
    {
        GameCore.GameInstance.I.SaveAllNow();
        Debug.Log("[TestSave] SaveAllNow()");
    }

    public void PrintState()
    {
        var s = GameCore.GameInstance.I.State;

        Debug.Log(
            $"[PlayerState] Level={s.Level} Gold={s.Gold} Gems={s.Gems} Skin={s.SelectedSkinId} LastSavedUnix={s.LastSavedUnix}"
        );

        Debug.Log("[PlayerState JSON]\n" + JsonConvert.SerializeObject(s, Formatting.Indented));
    }

    // Удобная тестовая кнопка: сделать изменение -> сохранить на сервер -> вывести стейт
    public void AddGoldSaveServerPrint()
    {
        AddGold123();
        SaveServer();
        PrintState();
    }

    // ----------------- EQUIP TEST -----------------

    /// <summary>
    /// Надеть предмет по ID из поля testItemId (берётся слот из ItemDef).
    /// </summary>
    public void EquipTestItemId()
    {
        var id = (testItemId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("[TestSave] EquipTestItemId: testItemId is empty");
            return;
        }

        var db = ItemDatabase.I;
        if (db == null)
        {
            Debug.LogError("[TestSave] EquipTestItemId: ItemDatabase.I is null (Resources/ItemDatabase.asset?)");
            return;
        }

        var def = db.GetById(id);
        if (def == null)
        {
            Debug.LogError($"[TestSave] EquipTestItemId: item not found by id='{id}'");
            return;
        }

        // Надеваем в правильный слот (def.Slot)
        GameCore.GameInstance.I.EquipItem(def.Slot, def.Id);

        Debug.Log($"[TestSave] Equipped item id='{def.Id}' slot={def.Slot} rarity={def.Rarity}");
    }

    /// <summary>
    /// Надеть предмет по ID (если хочешь вызывать из кода).
    /// </summary>
    public void EquipById(string itemId)
    {
        testItemId = itemId;
        EquipTestItemId();
    }
}



