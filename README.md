# 武学麻将（WuXueMaJong）

单机版武汉麻将「红中赖子杠」，Unity 6.3（6000.3.29f1）+ URP 2D。

- 玩家 + 3 个 AI 对局，或全 AI 观战
- 完整红中赖子杠规则：翻牌定癞子、红中杠、癞子杠、二五八将、必须开口、口口翻计分
- UI 全部在运行时用代码创建，不依赖 prefab / 场景配置 / 美术资源

## 运行

1. 用 Unity 6000.3.29f1 打开项目
2. 打开 `Assets/Scenes/SampleScene.unity`
3. 点击 Play（游戏界面会自动创建）
4. 主菜单选择「玩家 vs 3 AI」或「全 AI 观战」

> 因为使用 `[RuntimeInitializeOnLoadMethod]` 自动启动，任何场景进入 Play 都会出现牌桌。

## 操作说明

- 点击手牌选中，再点一次或点击「打出」出牌
- 吃、碰、杠、胡按钮只在可用时出现
- 红中、癞子不能普通打出：请使用「红中杠」「癞子杠」按钮
- 吃牌有多种组合时会显示多个「吃 …」按钮供选择
- 响应窗口 8 秒未操作会自动「过」
- 结算面板显示胡法、大小胡、每家倍数与得分，可「下一局」（庄家轮转）或「重开」

## 命令行验证

EditMode 单元测试：

```bash
"D:/software/unity-editor/6000.3.29f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "D:/software/unity-editor/WuXueMaJong" \
  -runTests -testPlatform EditMode \
  -testResults "D:/software/unity-editor/WuXueMaJong/Logs/EditModeResults.xml" \
  -logFile "D:/software/unity-editor/WuXueMaJong/Logs/EditMode.log"
```

PlayMode 冒烟测试（UI 启动 + 全 AI 完整对局）：

```bash
"D:/software/unity-editor/6000.3.29f1/Editor/Unity.exe" -batchmode \
  -projectPath "D:/software/unity-editor/WuXueMaJong" \
  -runTests -testPlatform PlayMode \
  -testResults "D:/software/unity-editor/WuXueMaJong/Logs/PlayModeResults.xml" \
  -logFile "D:/software/unity-editor/WuXueMaJong/Logs/PlayMode.log"
```

构建 Windows 播放器：

```bash
"D:/software/unity-editor/6000.3.29f1/Editor/Unity.exe" -batchmode -nographics -quit \
  -projectPath "D:/software/unity-editor/WuXueMaJong" \
  -buildWindows64Player "D:/temp/WuXueMaJongBuild/WuXueMaJong.exe" \
  -logFile "D:/software/unity-editor/WuXueMaJong/Logs/build.log"
```

构建后可用的启动参数：

```text
-mahjong-autostart human|spectator|spectator-fast  直接开局（跳过主菜单）
-mahjong-seed <数字>                                固定牌局种子
```

## 工程结构

```text
Assets/Mahjong/Core/    纯 C# 规则引擎（不依赖 UnityEngine，可复用于联网服务器）
  Tiles/ Wall/ Model/ Rules/ Win/ Score/ Engine/ Ai/ Session/ Util/
Assets/Mahjong/Client/  Unity 表现层（uGUI 运行时构建）
  Bootstrap/  UI/
Assets/Tests/EditMode/  规则引擎单元测试（43 项）
Assets/Tests/PlayMode/  启动与全 AI 对局冒烟测试
Docs/                   规则说明与开发路线图
```

## 规则与文档

- 实现规则：[Docs/WuhanRules.md](Docs/WuhanRules.md)
- 开发路线图：[Docs/DevelopmentRoadmap.md](Docs/DevelopmentRoadmap.md)

## 可调参数

所有规则倍率与开关集中在 `Assets/Mahjong/Core/Rules/RulesConfig.cs`，例如：

- 小胡/大胡底分、开口/各杠倍率
- 是否要求开口、是否要求二五八将
- 大胡自摸倍率（当前实现统一 ×2，属简化口径）

修改后跑一遍 EditMode 测试即可确认没有破坏规则。
