using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WuXueMaJong.Client;

namespace WuXueMaJong.Tests.PlayMode
{
    /// <summary>手动触发的截图探针：设置环境变量 MAHJONG_SCREENSHOT=1 后运行 PlayMode 测试。</summary>
    public class ScreenshotProbe
    {
        private const string OutputDir = "D:/temp/mahjong-shots";

        private static IEnumerator Capture(string fileName)
        {
            Directory.CreateDirectory(OutputDir);
            string path = OutputDir + "/" + fileName;
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);

            float deadline = Time.realtimeSinceStartup + 15f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(File.Exists(path), "截图未生成：" + path);
        }

        [UnityTest]
        public IEnumerator CaptureScreenshots()
        {
            if (Environment.GetEnvironmentVariable("MAHJONG_SCREENSHOT") != "1")
            {
                Assert.Ignore("未设置 MAHJONG_SCREENSHOT=1，跳过截图探针。");
            }

            yield return null;

            MahjongGameApp app = UnityEngine.Object.FindFirstObjectByType<MahjongGameApp>();
            if (app == null)
            {
                GameBootstrap.Install();
                yield return null;
                app = UnityEngine.Object.FindFirstObjectByType<MahjongGameApp>();
            }

            Assert.IsNotNull(app);

            // 1. 人类玩家开局视角（庄家、14 张手牌、操作栏）
            app.StartGame(false, 20261120L);
            yield return null;
            yield return null;
            yield return Capture("01-human-table.png");

            // 2. 观战视角（中局展示牌河/副露）
            app.StartGame(true, 555L, false);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Capture("02-spectator-mid.png");

            // 3. 结算面板
            float deadline = Time.realtimeSinceStartup + 180f;
            while (!app.IsHandEnded && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(app.IsHandEnded, "观战对局未结束");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("03-result.png");
        }
    }
}
