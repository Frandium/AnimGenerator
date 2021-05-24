# json 结构
见 Scripts/keyframe.cs 中的注释。

# 2D模型与动画
* **模型导出** 模型导出 2D 模型基于 DragonBones 动画插件，在 Dragonbones 软件中导出 xxx_ske.json、xxx_tex.json、xxx_tex.png 三份文件。创建新文件夹 Resources/xxx 并将导出的文件存放在其中。

* **创建 prefab** 在场景中创建 DragonBones Gameobject，基于 xxx_ske.json 创建物体的 Dragonbones Data，将 data 赋给创建的 DragonBones Gameobject，插件会生成静态的游戏物体。直接将其保存为 prefab 即可。2D 模型目前仅能静态加载，动态加载还在研究中。

# 3D模型与动画
* **模型导出** 3D 模型建议的导出格式为 .fbx，其中应当包含模型的 mesh、bones、animation、avatar（人形模型，若有）。需要注意的是，Unity 默认左手坐标系，Blender 默认右手坐标系，其他建模软件如果同样存在该问题，导出时请注意调整坐标。

* **创建 prefab** 将 .fbx 导入工程 Model 文件夹内，直接拖入场景中，如果 mesh 和绑骨信息正确，应该能生成一个预期的 GameObject。可以轴向拖动物体以确认坐标轴是否匹配，若不匹配，可套一层 parent GameObject 对坐标系和缩放进行修正。此 GameObject 默认是 .fbx 的 prefab variant，请将其重新保存为 original prefab。

* **创建动画状态机** 解释器目前在 3D 模式中将读入的 animation 字段解释为 Animator.SetTrigger 的参数，所以建议现阶段先只使用 Trigger 类型的变量配置物体的动画状态机。

* **创建模型材质** 不建议使用建模软件导出的材质，因其编码方式与 Unity 可能有不同。Unity 的 Standard Shader 已经实现了基本的 PBR 流程，调整参数和贴图即可。如果后续需要动态替换功能，建议将可能动态替换的部分作为一个单独的 mesh 设计 uv 和材质，避免使用 atlas 造成贴图替换困难。

# 流式解析json文件
约定将要流式生成的若干 json 文件名称为 Keyframes0.json，Keyframes1.json...，程序不停地检测 Applacation.StreamingAssets 文件夹下是否有新的 json 文件；若有，则按顺序读入一个尚未被解释的 json 文件，解释执行。重复这一过程。
