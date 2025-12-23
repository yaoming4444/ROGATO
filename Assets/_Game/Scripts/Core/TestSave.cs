using UnityEngine;
using Newtonsoft.Json;

public class TestSave : MonoBehaviour
{
    public void TestSaveServer()
    {
        GameCore.GameInstance.I.State.Gold += 123;
        GameCore.GameInstance.I.MarkDirty();
        GameCore.GameInstance.I.SaveServerNow();
    }


    public void PrintState()
    {
        var s = GameCore.GameInstance.I.State;

        Debug.Log(
            $"[PlayerState] Level={s.Level} Gold={s.Gold} Gems={s.Gems} Skin={s.SelectedSkinId} LastSavedUnix={s.LastSavedUnix}"
        );

        // если хочешь прям весь JSON:
        Debug.Log("[PlayerState JSON]\n" + JsonConvert.SerializeObject(s, Formatting.Indented));
    }
}

