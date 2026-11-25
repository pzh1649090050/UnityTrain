using UnityEngine;
namespace UnityTrain
{
    // 场景显式引用开源中文字体，构建后无需依赖学员机器的字体安装。
    public class ChineseFont : MonoBehaviour
    {
        public Font bundledFont;
        void Awake()
        {
            Font font = bundledFont ? bundledFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var label in GetComponentsInChildren<UnityEngine.UI.Text>(true)) label.font = font;
        }
    }
}
