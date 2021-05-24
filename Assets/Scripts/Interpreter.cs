using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using DragonBones;
using UnityEngine.UI;

namespace AnimGenerator
{
    [Serializable]
    internal class KeyFrameList
    {
        public List<KeyFrame> keyFrames = new List<KeyFrame>();
    }

    public enum EInterpreterMode
    {
        Display,
        Edit,
        Wait
    }

    public class Interpreter : MonoBehaviour
    {
        KeyFrameList keyFrameList;

        /// <summary>
        /// 加载的json文件名
        /// </summary>
        private const string JSON_FILE_NAME = "KeyFrames";

        // 2D 下的场景边界
        private const float MIN_X_2D = -16;
        private const float MIN_Y_2D = -9;
        private const float MAX_X_2D = 16;
        private const float MAX_Y_2D = 9;

        // 对话气泡的高度
        private const float DIALOG_HEIGHT = .3f;

        // 各类形象的尺寸
        private const float FLYING_HEIGHT = .4f;
        private const float FLYING_WIDTH = .2f;

        // 切换展示模式和配置模式
        private float starttime = 0;
        private EInterpreterMode interpreterMode = EInterpreterMode.Wait;

        private DirectoryInfo streamingDir;

        /// <summary>
        /// 用以索引场景中的所有 GameObject
        /// </summary>
        Dictionary<string, GameObject> gos = new Dictionary<string, GameObject>();

        /// <summary>
        /// 用以索引场景中 GameObject 的对话框
        /// </summary>
        Dictionary<string, GameObject> dialogs = new Dictionary<string, GameObject>();

        [Header("预制的一系列角色")]
        public GameObject stuff;  // 静态道具
        public GameObject human;  // 人
        public GameObject flying; // 飞行动物
        public GameObject creeping; // 四脚爬行动物
        public GameObject dialog;

        [Header("直接索引的场景中的物体或资产")]
        public GameObject audioContainer;
        public Material bkgmat;
        public Text subtitle;

        // Start is called before the first frame update
        void Start()
        {
            streamingDir = new DirectoryInfo(Application.streamingAssetsPath);
        }

        /// <summary>
        /// 用以从编辑模式切换到展示模式；或等待新的输入。
        /// </summary>
        public void StartDisplay(string jsonFileName)
        {
            // 解析 json 文件并排序
            FileStream jsonfile = File.OpenRead(jsonFileName);
            int len = (int)jsonfile.Length;
            byte[] bytes = new byte[len];
            jsonfile.Read(bytes, 0, len);

            // 输入的 json 文件使用 UTF-8 编码
            string jsonstring = System.Text.Encoding.UTF8.GetString(bytes);
            keyFrameList = JsonUtility.FromJson<KeyFrameList>(jsonstring);
            if (keyFrameList == null)
            {
                Debug.LogError("Unexpected json string.");
            }
            keyFrameList.keyFrames.Sort((k1, k2) =>
            {
                if (k1.timestamp < k2.timestamp) return -1;
                return 1;
            });

            interpreterMode = EInterpreterMode.Display;
            starttime = Time.realtimeSinceStartup;
        }

        public void WaitNewInput()
        {
            interpreterMode = EInterpreterMode.Wait;
        }

        /// <summary>
        /// 记录当前解析到第几条了
        /// </summary>
        private int i = 0;

        /// <summary>
        /// 当前解析到第几个文件了
        /// </summary>
        private int fileIndex = 0; 

        void Update()
        {
            if (interpreterMode == EInterpreterMode.Display)
            {
                while (i < keyFrameList.keyFrames.Count && Time.time - starttime > keyFrameList.keyFrames[i].timestamp)
                {
                    KeyFrame frame = keyFrameList.keyFrames[i];
                    switch (frame.action)
                    {
                        case 0: // 出现
                            Appear(frame);
                            break;
                        case 1: // 消失
                            DisAppear(frame);
                            break;
                        case 2: // 移动和动画
                            Move(frame);
                            break;
                        case 3: // 播放音乐
                            Audio(frame);
                            break;
                        case 4: // 更换背景图片
                            Background(frame);
                            break;
                        case 5:
                            ShowSubtitle(frame);
                            break;
                        case 6:
                            ShowDialog(frame);
                            break;
                        default:
                            Debug.LogError($"Unexpected action type {frame.action} at timestamp {frame.timestamp}");
                            break;
                    }
                    ++i;
                }
                if (i >= keyFrameList.keyFrames.Count)
                {
                    interpreterMode = EInterpreterMode.Wait;
                }
            }
            else if (interpreterMode == EInterpreterMode.Wait)
            {
                // 去找 streamingAssets 里有没有新文件
                FileInfo[] files = streamingDir.GetFiles();

                if(files.Length > fileIndex)
                {
                    StartDisplay($"{Application.streamingAssetsPath}/{JSON_FILE_NAME}{fileIndex++}.json");
                }
            }
        }

        private void Appear(KeyFrame frame)
        {
            // 重复名称报错
            if (gos.ContainsKey(frame.name))
            {
                Debug.LogError($"Duplicate gameobject name {frame.name}.");
                return;
            }

            // 实例化物体
            GameObject tobeinstantiate = creeping;
            switch (frame.type)
            {
                case 0:
                    tobeinstantiate = stuff;
                    break;
                case 1:
                    tobeinstantiate = human;
                    break;
                case 2:
                    tobeinstantiate = creeping;
                    break;
                case 3:
                    tobeinstantiate = flying;
                    break;
                default:
                    Debug.LogError($"Unexpected gameobject type {frame.type} when instantiating at timestamp {frame.timestamp}");
                    break;
            }
            GameObject go = Instantiate(tobeinstantiate, frame.StartPosition, frame.StartRotation);
            GameObject dia = Instantiate(dialog, GameObject.Find("Canvas").transform);
            UpdateDialogPos(frame.StartPosition, dia.GetComponent<RectTransform>());
            dia.SetActive(false);
            go.transform.localScale = frame.StartScale;
            gos[frame.name] = go;
            dialogs[frame.name] = dia;
        }

        private void UpdateDialogPos(Vector3 pos, RectTransform trans)
        {
            Vector2 anchor = new Vector2((pos.x - MIN_X_2D) / (MAX_X_2D - MIN_X_2D), (pos.y - MIN_Y_2D) / (MAX_Y_2D - MIN_Y_2D));
            trans.anchorMin = anchor + new Vector2(-FLYING_WIDTH / 2, FLYING_HEIGHT / 2);
            trans.anchorMax = anchor + new Vector2(FLYING_WIDTH / 2, FLYING_HEIGHT / 2 + DIALOG_HEIGHT);
        }

        private void DisAppear(KeyFrame frame)
        {
            if (!gos.ContainsKey(frame.name))
            {
                Debug.LogError($"Gameobject {frame.name} dose not exsit when removing at timestamp {frame.timestamp}.");
                return;
            }
            Destroy(gos[frame.name]);
            Destroy(dialogs[frame.name]);
            gos.Remove(frame.name);
            dialogs.Remove(frame.name);
        }

        private void Move(KeyFrame frame)
        {
            if (!gos.ContainsKey(frame.name))
            {
                Debug.LogError($"Gameobject {frame.name} does not exist when animating at timestamp {frame.timestamp}.");
                return;
            }
            StartCoroutine(Animate(frame));
        }

        private const bool IS_3D_MODE = false;
        private IEnumerator Animate(KeyFrame frame)
        {
            float elapsed = 0;
            GameObject go = gos[frame.name];
            RectTransform rectTransform = dialogs[frame.name].GetComponent<RectTransform>();

            if (IS_3D_MODE)
            {
                // 改变动画状态机的状态
                Animator animator = go.GetComponent<Animator>();
                animator.SetTrigger(frame.name);
            }
            else
            {
                // 播放 DrangonBones 动画
                UnityArmatureComponent armatureComp = go.GetComponent<UnityArmatureComponent>();
                DragonBones.Animation animation = armatureComp.animation;
                Dictionary<string, AnimationData> animations = animation.animations;
                float length = animations[frame.content].duration;
                if (frame.loop > 0)
                    animation.timeScale = length / (frame.duration / frame.loop);
                else
                    animation.timeScale = 1;
                animation.Play(frame.content, frame.loop);
            }

            // 插值改变物体 transform
            UnityEngine.Transform transform = go.transform;
            transform.SetPositionAndRotation(frame.StartPosition, frame.StartRotation);
            transform.localScale = frame.StartScale;

            while (elapsed < frame.duration)
            {
                transform.SetPositionAndRotation(
                    LerpUtility.Linear(frame.StartPosition, frame.EndPosition, elapsed / frame.duration),
                    LerpUtility.Linear(frame.StartRotation, frame.EndRotation, elapsed / frame.duration)
                    );
                transform.localScale = LerpUtility.Linear(frame.StartScale, frame.EndScale, elapsed / frame.duration);
                if (!IS_3D_MODE)
                {
                    UpdateDialogPos(transform.position, rectTransform);
                }
                elapsed += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }

        private void Audio(KeyFrame frame)
        {
            StartCoroutine(DestroyAfterPlay(frame));
        }

        IEnumerator DestroyAfterPlay(KeyFrame frame)
        {
            AudioClip clip = Resources.Load<AudioClip>($"Audio/{frame.name}");
            if (clip == null)
            {
                Debug.LogError($"Failed to load audio clip {frame.name} at timestamp {frame.timestamp}");
                yield break;
            }

            // 先实例化一个声源，循环播放音频
            GameObject go = Instantiate(audioContainer, Vector3.zero, Quaternion.identity);
            AudioSource source = go.GetComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.Play();

            // 如果是循环模式是 -1，直接结束
            if (frame.loop == -1)
                yield break;

            // 否则等待对应时间后销毁声源
            yield return new WaitForSeconds(frame.loop == 0 ? frame.duration : clip.length * frame.loop);
            source.Stop();
            Destroy(go);
        }

        private void Background(KeyFrame frame)
        {
            Texture2D newtex = Resources.Load<Texture2D>($"Texture/{frame.name}");
            if(newtex == null)
            {
                Debug.LogError($"Failed to load texture {frame.name} at timestamp {frame.timestamp}.");
                return;
            }
            bkgmat.SetTexture("_MainTex", newtex);
        }

        private void ShowSubtitle(KeyFrame frame)
        {
            subtitle.text = frame.content;
            StartCoroutine(WaitToEndSubtitle(frame.duration));
        }

        private IEnumerator WaitToEndSubtitle(float time)
        {
            yield return new WaitForSeconds(time);
            subtitle.text = string.Empty;
        }

        private void ShowDialog(KeyFrame frame)
        {
            if (!dialogs.ContainsKey(frame.name))
            {
                Debug.LogError($"Failed to find dialog attached to gameobject {frame.name} at timestamp {frame.timestamp}");
                return;
            }
            StartCoroutine(DisableAfterDialog(frame));
        }

        private IEnumerator DisableAfterDialog(KeyFrame frame)
        {
            GameObject dia = dialogs[frame.name];
            dia.SetActive(true);
            dia.GetComponentInChildren<Text>().text = frame.content;
            yield return new WaitForSeconds(frame.duration);
            dia.SetActive(false);
        }

//        public Texture2D testTexture;
//        private GameObject GenerateDragonbones(GameObject go)
//        {
//            UnityDragonBonesData origData = Instantiate(birddata);
//            origData.textureAtlas[0].texture = testTexture;
//            UnityArmatureComponent armature = go.GetComponent<UnityArmatureComponent>();
//            armature.unityData = origData;
//            Slot slot = armature.armature.GetSlot("头");
//            // RefreshAllAtlasTextures
//            UnityFactory.factory.ReplaceSlotDisplay(origData.dataName, armature.armature.name, slot.name, "头", slot, testTexture);
////            ChangeArmatureData(armature, armature.armatureName, origData.dataName);

//            return go;
//        }

//        public static void ChangeArmatureData(UnityArmatureComponent _armatureComponent, string armatureName, string dragonBonesName)
//        {
//            bool isUGUI = _armatureComponent.isUGUI;
//            UnityDragonBonesData unityData = null;
//            Slot slot = null;
//            if (_armatureComponent.armature != null)
//            {
//                unityData = _armatureComponent.unityData;
//                slot = _armatureComponent.armature.parent;
//                _armatureComponent.Dispose(false);

//                UnityFactory.factory._dragonBones.AdvanceTime(0.0f);

//                _armatureComponent.unityData = unityData;
//            }

//            _armatureComponent.armatureName = armatureName;
//            _armatureComponent.isUGUI = isUGUI;

//            _armatureComponent = UnityFactory.factory.BuildArmatureComponent(_armatureComponent.armatureName, dragonBonesName, null, _armatureComponent.unityData.dataName, _armatureComponent.gameObject, _armatureComponent.isUGUI);
//            if (slot != null)
//            {
//                slot.childArmature = _armatureComponent.armature;
//            }

//            _armatureComponent.sortingLayerName = _armatureComponent.sortingLayerName;
//            _armatureComponent.sortingOrder = _armatureComponent.sortingOrder;
//        }
    }
}