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

    public class Interpreter : MonoBehaviour
    {
        KeyFrameList keyFrameList;

        /// <summary>
        /// 加载的json文件名
        /// </summary>
        private const string JSON_FILE_NAME = "/KeyFrames.json";

        /// <summary>
        /// 用以索引场景中的所有 GameObject
        /// </summary>
        Dictionary<string, GameObject> gos = new Dictionary<string, GameObject>();

        [Header("预制的一系列角色")]
        public GameObject stuff;  // 静态道具
        public GameObject human;  // 人
        public GameObject flying; // 飞行动物
        public GameObject creeping; // 四脚爬行动物

        [Header("直接索引的场景中的物体或资产")]
        public GameObject audioContainer;
        public Material bkgmat;
        public Text subtitle;

        // Start is called before the first frame update
        void Start()
        {
            // 解析 json 文件并排序
            FileStream jsonfile = File.OpenRead(Application.streamingAssetsPath + JSON_FILE_NAME);
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
        }

        /// <summary>
        /// 记录当前解析到第几条了
        /// </summary>
        private int i = 0;

        void Update()
        {
            while (i < keyFrameList.keyFrames.Count && Time.time > keyFrameList.keyFrames[i].timestamp)
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
                    default:
                        Debug.LogError($"Unexpected action type {frame.action} at timestamp {frame.timestamp}");
                        break;
                }
                ++i;
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
            go.transform.localScale = frame.StartScale;
            gos[frame.name] = go;
        }

        private void DisAppear(KeyFrame frame)
        {
            if (!gos.ContainsKey(frame.name))
            {
                Debug.LogError($"Gameobject {frame.name} dose not exsit when removing at timestamp {frame.timestamp}.");
                return;
            }
            Destroy(gos[frame.name]);
            gos.Remove(frame.name);
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
                animation.Play(frame.animation, frame.loop);
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
            subtitle.text = frame.name;
            StartCoroutine(WaitToEndSubtitle(frame.duration));
        }

        private IEnumerator WaitToEndSubtitle(float time)
        {
            yield return new WaitForSeconds(time);
            subtitle.text = string.Empty;
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