[English](README.md) · [简体中文](README.zh-CN.md)

# SceneReplay

**面向多种游戏引擎的可编辑 3D 资产语义中间层。**

<p align="center">
  <img src="Docs/branding/scenereplay-readme-hero.jpg" alt="一个语义化 3D 资产定义适配为多个引擎原生层级" />
</p>

[![仓库校验](https://github.com/TristinOrg/SceneReplay/actions/workflows/repository-validation.yml/badge.svg)](https://github.com/TristinOrg/SceneReplay/actions/workflows/repository-validation.yml)
[![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-000000.svg)](https://unity.com/releases/editor/whats-new/2022.3.62)
[![Godot](https://img.shields.io/badge/Godot-4.x-478cbf.svg)](https://godotengine.org/download/)
[![版本](https://img.shields.io/badge/release-v0.1.0-4c8bf5.svg)](https://github.com/TristinOrg/SceneReplay/releases/tag/v0.1.0)
[![许可证](https://img.shields.io/badge/license-AGPL--3.0--or--later%20%2F%20Commercial-blue.svg)](LICENSE.md)

SceneReplay 为 AI 和建模工具提供一种引擎无关的 3D 资产描述与编辑方式。资产由具名部件、稳定 ID、变换、几何参数、Rig 角色和游戏语义元数据组成。你可以先验证定义，只修改其中一个部件，再将相同的语义结构适配为目标引擎的原生层级。

```text
AI / 建模工具 → SceneReplay 定义 → 验证 / 补丁 → 引擎适配器 → 可编辑场景
```

SceneReplay 与 Unity、Unreal、Godot、Three.js 等引擎协作：引擎负责渲染和运行时行为，SceneReplay 负责在工具之间传递资产结构和身份。它不是图片转 3D 工具，也不替代 Blender、Maya 或引擎编辑器。

## SceneReplay 的价值

- **让资产保持可理解。** 稳定 ID 将语义部件关联到编辑、重新生成、验证和引擎导入流程。
- **只修改变化的部分。** 补丁通过 ID 定位部件，工具无需每次都重新发送整个模型。
- **跨引擎保留意图。** Rig 角色、锚点、碰撞规则、LOD 声明、调色板角色和游戏标签可与几何定义一起保存。
- **AI 可选。** SceneReplay 可以验证和适配文档，不依赖特定模型服务商。

## 引擎支持情况

| 目标引擎 | 状态 | 当前能力 |
|---|---|---|
| Unity | 可用 | 完整 Package 栈、11 种 Low Poly 几何、语义 Rig、可逆补丁、原生碰撞体、LOD Group 和 GLB 导出 |
| Three.js | 可用 | JavaScript 适配器、11 种几何、镜像实例、语义 Rig 目标、游戏元数据、原子语义补丁和可运行的浏览器示例 |
| Godot 4 | 可用 | GDScript 适配器、11 种几何、语义 Rig 目标、原子补丁、可选原生碰撞体和可运行项目 |
| Unreal Engine | 计划中 | 尚无适配器；需要实际 Unreal 项目进行原生编译和验证 |

适配器会明确声明已支持的能力。详细信息见[适配器能力说明](Docs/Technical-Reference.md#semantic-interoperability-boundary)。

## 运行示例

### Three.js

需要 Node.js 20.19+ 或 22.12+。在仓库根目录运行：

```sh
cd Samples/three-consumer
npm ci
npm test
npm run dev
```

浏览器示例包含 Unity 预设生成的日式小镇和发明家工作台，以及角色、武器、建筑、Profile 几何和语义 Rig 场景。小镇与工作台直接复用 Unity 预设工厂生成的定义和调色板，可用于跨引擎对比。点击 **Fit view** 可适配视角，点击 **Save PNG** 可导出当前画面。

| 日式小镇 | 发明家工作台 |
|---|---|
| ![SceneReplay 日式小镇的 Three.js 画面](Samples/three-consumer/screenshots/japanese-town.png) | ![SceneReplay 发明家工作台的 Three.js 画面](Samples/three-consumer/screenshots/inventor-workbench.png) |

### Godot

需要 Godot 4.x。用 Godot 打开 `Samples/godot-consumer/project.godot`，或运行无界面适配器检查：

```sh
godot --headless --path Samples/godot-consumer --script res://tests/run_tests.gd
```

示例可以切换角色、Profile 展示、补丁演示和共享 Rig 样例。

### Unity

Unity Package 支持 Unity 2022.3 LTS。将本仓库中的 Package 添加到 Unity 项目，参见[安装和 Package 说明](Docs/Technical-Reference.md#installation)。仓库还包含预设、编辑器工作流，以及通过 Unity Editor 执行文档操作的 Python CLI。

## Unity 快速开始

```csharp
using SceneReplay.LowPoly;
using SceneReplay;
using UnityEngine;

ShapeDefinition definition = LowPolyRobotPreset.CreateDefinition();
ShapeStyleDefinition style = LowPolyRobotPreset.CreateStyle();
var generator = new LowPolyModelGenerator(new[] { style });
GameObject robot = generator.Generate(definition);
```

生成的节点保留稳定语义 ID。可以通过 ID 获取目标，而不依赖自动生成的对象名称：

```csharp
using SceneReplay.Unity;

UnityShapeModel model = robot.GetComponent<UnityShapeModel>();
if (model.TryGetTarget("robot.head.pivot", out IShapeTransformTarget head))
    head.LocalEulerAngles = new Vector3(0f, 25f, 0f);
```

## 面向 AI 的编辑流程

SceneReplay 文档是普通的版本化 JSON。Schema 和 Core 会在适配器创建原生对象之前，验证结构、能力、ID、引用和安全限制。工具可以读取诊断信息，生成小型补丁，然后再次验证：

```text
定义 → 验证 / 质量报告 → 基于 ID 的补丁 → 引擎适配器
  ↑                                          │
  └──────────── Diff、审查或撤销 ────────────┘
```

验证、Diff、补丁、质量门、构建计划和 GLB 导出的权威实现位于 C#。`tools/scenereplay.py` 通过 Unity Editor bridge 提供可重复的工作流：

```sh
python tools/scenereplay.py validate model.json
python tools/scenereplay.py diff before.json after.json
python tools/scenereplay.py patch model.json change.json -o updated.json
python tools/scenereplay.py quality model.json policy.json
python tools/scenereplay.py export-glb model.json --asset Library/SceneReplay/model.glb -o report.json
```

完整命令见 [CLI 文档](Docs/Technical-Reference.md#automation-cli)，JSON Schema 见 [已发布的 Schema 文件](Packages/com.scenereplay.schema/Documentation~/Schemas)。

## 几何与扩展

官方 Low Poly 后端目前支持立方体、球体、圆柱体、胶囊体、楔形体、截锥体、轮廓挤出、轮廓 Loft、轮廓 Cage、旋转体和路径扫掠。几何只是语义格式之后的一种实现；团队可以添加写实、Voxel、SDF、自定义网格或引擎专用后端，而无需把这些风格写入 Core。

适配器和几何后端是独立的扩展点。Schema 和 Core 不依赖 Unity、渲染器、特定美术风格或 AI 服务商。实现细节、性能、安全限制、预设和扩展指南请阅读[技术参考](Docs/Technical-Reference.md)。

## 文档

| 内容 | 资源 |
|---|---|
| 详细 API、契约、性能和工作流 | [技术参考（英文）](Docs/Technical-Reference.md) |
| 开发方向与规划 | [路线图](ROADMAP.md) |
| Shape 与 Style 契约 | [Schema 规范](Packages/com.scenereplay.schema/Documentation~/SceneReplay-Specification.md) |
| 可运行的引擎示例 | [Three.js](Samples/three-consumer) · [Godot](Samples/godot-consumer) |
| 共享适配器样例 | [Rig 一致性 JSON](Samples/conformance/rigged-character.json) |
| 变更记录 | [CHANGELOG](CHANGELOG.md) |
| 参与贡献 | [贡献指南](CONTRIBUTING.md) |
| 安全问题 | [安全策略](SECURITY.md) |

## 自动验证

GitHub Actions 会检查 Package 契约、Three.js 适配器测试与生产构建，以及 Godot 项目导入和无界面测试。Unity EditMode 测试需要 Unity 2022.3；Python CLI 的文档操作需要已连接的 Unity Editor。

## 许可证

SceneReplay 采用双许可证：开源使用遵循 **AGPL-3.0-or-later**；专有产品可使用**商业许可证**。详见 [LICENSE.md](LICENSE.md) 和[商业授权说明](COMMERCIAL-LICENSE.md)。仅仅因为模型由未修改的 SceneReplay 生成，并不会让生成模型自动受该软件许可证约束。

---

SceneReplay 由 Tristin Wen 创建，目标是让语义可编辑的 3D 资产成为 AI 辅助建模和多引擎工作流中的实用交换层。
