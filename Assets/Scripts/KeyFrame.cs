using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimGenerator
{
    [Serializable]
    internal class KeyFrame
    {
        /// <summary>
        /// 0 出现，1 消失，2 移动，3 音频播放和停止，4 背景图切换，5字幕切换
        /// </summary>
        public int action;

        /// <summary>
        /// 0 道具，1 人，2 四脚爬行，3 飞行动物
        /// </summary>
        public int type;

        /// <summary>
        /// action == 0 or 1 or 2 时，用以索引物体的名字
        /// action == 3 时，指示要播放的音频在 Resources 文件夹下的路径，不包含后缀名
        /// action == 4 时，指示要切换的图片在 Resources 文件夹下的路径，不包含后缀名
        /// action == 5 时，指示字幕的内容
        /// </summary>
        public string name;

        /// <summary>
        /// 该动作开始执行的时间戳，单位是秒(s)
        /// </summary>
        public float timestamp;

        // 下列为物体在变化过程中的位置、旋转和缩放。其中 action == 1 时，仅 startxxx 有意义；action == 2 时都有意义。
        // 2D 模式：
        //       摄像机正交投影，z 轴指示物体前后关系。背景图片 z 坐标为 10，摄像机 z 坐标为 -10，观察方向为 z 轴正方向，物体的 z 坐标越大，越靠后。
        //       画面左下角在 xy 平面的投影为 (-16, -9)，画面右上角在 xy 平面的投影为 (16, 9)
        // 3D 模式：
        //       请将摄像机切换到透视投影，摄像机位置为 (0, 0, -10)，观察方向为 (0, 0, 1)
        public List<float> startpos; 
        public List<float> endpos;
        public List<float> startrotation;
        public List<float> endrotation;
        public List<float> startscale;
        public List<float> endscale;

        /// <summary>
        /// 该动作的持续时间，在 action == 2/3/5 时有意义。
        /// </summary>
        public float duration;

        /// <summary>
        /// 2D 模式下指示播放动画的名字，3D 模式下指示要设置的状态机 trigger 名称
        /// </summary>
        public string animation;

        /// <summary>
        /// 动画（action == 2）或音频（action == 3）的循环方式
        /// >0 时为循环次数，
        /// =0 时，若 action == 2 无意义，action == 3 表示音频播放时长跟随 duration 字段
        /// =-1 时表示无限循环
        /// </summary>
        public int loop;

        public KeyFrame(int action, int type, string name, float timestamp,
            List<float> startpos, List<float> endpos,
            List<float> startrotation, List<float> endrotation,
            List<float> startscale, List<float> endscale,
            float duration, string animation, int loop)
        {
            this.action = action;
            this.type = type;
            this.name = name;
            this.timestamp = timestamp;
            this.startpos = startpos;
            this.endpos = endpos;
            this.startrotation = startrotation;
            this.endrotation = endrotation;
            this.startscale = startscale;
            this.endscale = endscale;
            this.duration = duration;
            this.animation = animation;
            this.loop = loop;
        }

        private bool b_startpos = false;
        private bool b_endpos = false;
        private bool b_startrot = false;
        private bool b_endrot = false;
        private bool b_startscale = false;
        private bool b_endscale = false;

        private Vector3 _startpos;
        private Vector3 _endpos;
        private Quaternion _startrotation;
        private Quaternion _endrotation;
        private Vector3 _startscale;
        private Vector3 _endscale;

        // 访问 keyframe 的下列字段时，请使用下列访问器，避免数据被多次构造
        public Vector3 StartPosition
        {
            get
            {
                if (!b_startpos)
                {
                    b_startpos = true;
                    _startpos = new Vector3(startpos[0], startpos[1], startpos[2]);
                }
                return _startpos;
            }
        }

        public Vector3 EndPosition
        {
            get
            {
                if (!b_endpos)
                {
                    b_endpos = true;
                    _endpos = new Vector3(endpos[0], endpos[1], endpos[2]);
                }
                return _endpos;
            }
        }

        public Quaternion StartRotation
        {
            get
            {
                if (!b_startrot)
                {
                    _startrotation = Quaternion.Euler(startrotation[0], startrotation[1], startrotation[2]);
                    b_startrot = true;
                }
                return _startrotation;
            }
        }

        public Quaternion EndRotation
        {
            get
            {
                if (!b_endrot)
                {
                    _endrotation = Quaternion.Euler(endrotation[0], endrotation[1], endrotation[2]);
                    b_endrot = true;
                }
                return _endrotation;
            }
        }

        public Vector3 StartScale
        {
            get
            {
                if (!b_startscale)
                {
                    b_startscale = true;
                    _startscale = new Vector3(startscale[0], startscale[1], startscale[2]);
                }
                return _startscale;
            }
        }

        public Vector3 EndScale
        {
            get
            {
                if (!b_endscale)
                {
                    b_endscale = true;
                    _endscale = new Vector3(endscale[0], endscale[1], endscale[2]);
                }
                return _endscale;
            }
        }
    }
}