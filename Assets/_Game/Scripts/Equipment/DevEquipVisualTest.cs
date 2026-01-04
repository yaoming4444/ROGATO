using UnityEngine;

namespace GameCore.Visual
{
    public class DevEquipVisualTest : MonoBehaviour
    {
        [Header("Куда пушим")]
        [SerializeField] private PartsManagerStateBinder uiBinder;
        // [SerializeField] private PartsManagerStateBinder worldBinder;

        [Header("Тестовые значения (skin names из Spine)")]
        [SerializeField] private string visual_top = "top/top_c_10";
        [SerializeField] private string visual_boots = "boots/boots_c_3";

        [SerializeField] private string visual_top1 = "top/top_c_11";
        [SerializeField] private string visual_boots1 = "boots/boots_c_4";

        // Кнопка: надеть тестовые шмотки
        public void ApplyTest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || gi.State == null)
            {
                Debug.LogError("[DevEquipVisualTest] GameInstance/State is null");
                return;
            }

            // !!! ВАЖНО: тут имена полей должны совпасть с твоим PlayerState !!!
            gi.State.visual_top = visual_top;
            gi.State.visual_boots = visual_boots;

            Debug.Log($"[DevEquipVisualTest] Set visual_top={visual_top}, visual_boots={visual_boots}");

            // форсим обновление отображения
            if (uiBinder) uiBinder.ApplyFromState();
            //if (worldBinder) worldBinder.ApplyFromState();
        }

        public void ApplySEX()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || gi.State == null)
            {
                Debug.LogError("[DevEquipVisualTest] GameInstance/State is null");
                return;
            }

            // !!! ВАЖНО: тут имена полей должны совпасть с твоим PlayerState !!!
            gi.State.visual_top = visual_top1;
            gi.State.visual_boots = visual_boots1;

            Debug.Log($"[DevEquipVisualTest] Set visual_top={visual_top}, visual_boots={visual_boots}");

            // форсим обновление отображения
            if (uiBinder) uiBinder.ApplyFromState();
            //if (worldBinder) worldBinder.ApplyFromState();
        }

        // Кнопка: снять (поставить -1 через пустую строку)
        public void ClearTest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || gi.State == null) return;

            gi.State.visual_top = "";
            gi.State.visual_boots = "";

            if (uiBinder) uiBinder.ApplyFromState();
            //if (worldBinder) worldBinder.ApplyFromState();
        }
    }
}

