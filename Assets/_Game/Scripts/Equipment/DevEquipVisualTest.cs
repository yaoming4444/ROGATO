using UnityEngine;
using LayerLab.ArtMaker;

namespace GameCore.Visual
{
    public class DevEquipVisualTest : MonoBehaviour
    {
        [Header("Тестовые значения (skin names из Spine)")]
        [SerializeField] private string visual_top = "top/top_c_10";
        [SerializeField] private string visual_boots = "boots/boots_c_3";

        [SerializeField] private string visual_top1 = "top/top_c_11";
        [SerializeField] private string visual_boots1 = "boots/boots_c_4";

        public void ApplyTest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || gi.State == null) return;

            gi.SetVisual(PartsType.Top, visual_top, notify: false);
            gi.SetVisual(PartsType.Boots, visual_boots, notify: false);

            gi.RaiseStateChanged(); // <-- обновит ВСЕХ подписчиков (оба биндера)
        }

        public void ApplySEX()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || gi.State == null) return;

            gi.SetVisual(PartsType.Top, visual_top1, notify: false);
            gi.SetVisual(PartsType.Boots, visual_boots1, notify: false);

            gi.RaiseStateChanged();
        }

        public void ClearTest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || gi.State == null) return;

            gi.SetVisual(PartsType.Top, "", notify: false);
            gi.SetVisual(PartsType.Boots, "", notify: false);

            gi.RaiseStateChanged();
        }
    }
}


