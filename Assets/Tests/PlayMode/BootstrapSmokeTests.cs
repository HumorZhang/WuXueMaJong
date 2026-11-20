using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WuXueMaJong.Client;
using WuXueMaJong.Core;

namespace WuXueMaJong.Tests.PlayMode
{
    public class BootstrapSmokeTests
    {
        private static MahjongGameApp FindApp()
        {
            MahjongGameApp app = Object.FindFirstObjectByType<MahjongGameApp>();
            if (app == null)
            {
                GameBootstrap.Install();
                app = Object.FindFirstObjectByType<MahjongGameApp>();
            }

            return app;
        }

        [UnityTest]
        public IEnumerator Bootstrap_CreatesTableAndEventSystem()
        {
            yield return null;

            MahjongGameApp app = FindApp();
            Assert.IsNotNull(app, "未自动创建 MahjongGameApp");

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.IsNotNull(canvas, "未创建 Canvas");
            Assert.IsNotNull(canvas.GetComponent<CanvasScaler>(), "Canvas 缺少 CanvasScaler");

            var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            Assert.IsNotNull(eventSystem, "未创建 EventSystem");
            Assert.IsNotNull(
                eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(),
                "EventSystem 未使用 InputSystemUIInputModule");
        }

        [UnityTest]
        public IEnumerator AllAiGame_CompletesWithoutErrors()
        {
            yield return null;

            MahjongGameApp app = FindApp();
            Assert.IsNotNull(app);

            app.StartSpectatorFast(20261120L);

            float deadline = Time.realtimeSinceStartup + 120f;
            while (!app.IsHandEnded && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(app.IsHandEnded, "全 AI 对局未在限定时间内结束");
            Assert.IsNotNull(app.Engine);
            Assert.IsNotNull(app.Engine.Result);
            Assert.AreEqual(GamePhase.HandEnded, app.Engine.Phase);
            Assert.AreEqual(0, app.Engine.Result.Score.Deltas.Sum(), "分数不守恒");
        }
    }
}
