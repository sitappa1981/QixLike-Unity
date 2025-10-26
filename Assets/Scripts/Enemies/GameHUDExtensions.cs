using UnityEngine;
using TMPro;

namespace QixLike
{
    public static class GameHUDExtensions
    {
        public static void SetLives(this GameHUD hud, int lives)
        {
            if (!hud) return;
            var t = FindText(hud, "Life", "Lives", "Heart");
            if (t) t.text = lives.ToString();
        }

        public static void SetTime(this GameHUD hud, float seconds)
        {
            if (!hud) return;
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int m = s / 60; s %= 60;
            var t = FindText(hud, "Time", "Timer");
            if (t) t.text = $"{m:0}:{s:00}";
        }

        public static void SetFill(this GameHUD hud, float percent)
        {
            if (!hud) return;
            var t = FindText(hud, "Fill");
            if (t) t.text = $"Fill: {percent:0.0}%";
        }

        // éqäKëwÇ©ÇÁñºëOÇ… key Ç™ä‹Ç‹ÇÍÇÈ TextMeshProUGUI ÇëÂéGîcÇ…íTçı
        static TextMeshProUGUI FindText(GameHUD hud, params string[] keys)
        {
            if (!hud) return null;
            var all = hud.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in all)
            {
                string name = txt.gameObject.name.ToLower();
                foreach (var k in keys)
                    if (name.Contains(k.ToLower()))
                        return txt;
            }
            return null;
        }
    }
}
