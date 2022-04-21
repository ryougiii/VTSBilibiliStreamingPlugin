using System.Collections.Generic;
using System;
using System.Globalization;
using System.Timers;
using VTS.Networking.Impl;
using VTS.Models.Impl;
using VTS.Models;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
// using UnityEditor;


using NAudio;
using NAudio.Wave;

namespace VTS.Examples
{
    [RequireComponent(typeof(AudioSource))]
    public class ExamplePlugin : VTSPlugin
    {
        private bool audioPlaying = false;

        [SerializeField]
        private Text show_danmu = null;

        // [SerializeField]
        // private Button _portConnectButtonPrefab = null;

        // [SerializeField]
        // private Button _tryConnectButton = null;

        [SerializeField]
        private RectTransform _portConnectButtonParent = null;

        [SerializeField]
        private Image _connectionLight = null;

        [SerializeField]
        private Text _connectionText = null;

        public static bool taskExcuteAvaliable = true;

        private List<HotkeyData> modelhotkeys = null;

        private readonly Queue<string> danmumen = new Queue<string>();

        private float maxTrigerTime = 1;
        IWavePlayer waveOutDevice;
        AudioFileReader audioFileReader;

        public InputField roomInput;
        public BliveDanmuManager danmuManager;

        private void Awake()
        {
            Screen.SetResolution(1000, 500, false);
            Connect();
            //UnityEngine.Debug.Log(AppData);
            //UnityEngine.Debug.Log(Application.dataPath);
            Application.targetFrameRate = 30;
            Tasks.loadTasksData();
            GameObject.Find("AddReg").GetComponent<Button>().onClick.AddListener(() =>
            {
                //添加规则按钮
                addReg();
            });
            show_danmu = GameObject.Find("danmuContentPnContent").GetComponent<Text>();

            // 注册弹幕事件回调
            danmuManager.DanmuEvent += d => {
                Tasks.testTrigerTask("danmu", new string[] { d.Content });
                show_danmu.text = $"{d.Username}: {d.Content}\n" + show_danmu.text;
            };

            danmuManager.GiftEvent += g => {
                show_danmu.text = $"{g.Username} 赠送了 {g.Name}x{g.Combo}"
                                  + $" ({g.Unit} 瓜子 x {g.Currency})\n" + show_danmu.text;
                if (g.Unit == "silver")
                {
                    Tasks.testTrigerTask("yinguazi", new string[1] { g.Currency.ToString(CultureInfo.InvariantCulture) });
                }
                else if (g.Unit == "gold")
                {
                    Tasks.testTrigerTask("jinguazi", new string[1] { g.Currency.ToString(CultureInfo.InvariantCulture) });
                }
            };

            danmuManager.GuardBuyEvent += g => {
                show_danmu.text = $"{g.Username} 购买了 {g.Name}\n" + show_danmu.text;
                Tasks.testTrigerTask("jianzhang", Array.Empty<string>()); 
            };
            
            danmuManager.SuperchatEvent += s => {
                show_danmu.text = $"发送了醒目留言 ￥{s.Price} {s.Username}：{s.Content}\n" + show_danmu.text;
                Tasks.testTrigerTask("sc", Array.Empty<string>()); 
            };

            danmuManager.JoinRoomEvent += j => {
                Debug.Log($"{j.Username} 进入直播间");
            };
            
            danmuManager.HeatEvent += h => {
                Debug.Log($"当前人气 {h}");
            };

            danmuManager.WatchedChangeEvent += h => {
                Debug.Log($"当前 {h} 人看过");
            }; 
        }

        public void ConnectDanmu() {
            if (int.TryParse(roomInput.text, out var id)) {
                danmuManager.Connect(id);
            }
        }
        
        public void DisconnectDanmu() {
            danmuManager.Disconnect();
        }
        
        void OnApplicationQuit()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
                waveOutDevice.Dispose();
                waveOutDevice = null;
            }
            if (audioFileReader != null)
            {
                audioFileReader.Dispose();
                waveOutDevice = null;
            }
        }
        private void Connect()
        {
            this._connectionLight.color = Color.yellow;
            this._connectionText.text = "Connecting...";
            Initialize(new WebSocketSharpImpl(), new JsonUtilityImpl(), new TokenStorageImpl(),
            () =>
            {
                UnityEngine.Debug.Log("Connected!");
                this._connectionLight.color = Color.green;
                this._connectionText.text = "Connected!";
                GetHotkeysInCurrentModel(null, (r) => { modelhotkeys = new List<HotkeyData>(r.data.availableHotkeys); }, (e) => { });
            },
            () =>
            {
                UnityEngine.Debug.LogWarning("Disconnected!");
                this._connectionLight.color = Color.gray;
                this._connectionText.text = "Disconnected.";
            },
            () =>
            {
                UnityEngine.Debug.LogError("Error!");
                this._connectionLight.color = Color.red;
                this._connectionText.text = "Error!";
            });
        }
        public void RefeshConnect()
        {
            Connect();
            GetHotkeysInCurrentModel(null, (r) => { modelhotkeys = new List<HotkeyData>(r.data.availableHotkeys); }, (e) => { });

        }

        public void taskExcuteFinish()
        {
            var todestory = GameObject.Find("TaskListContent").transform.GetChild(0).gameObject;
            if (todestory != null)
            {
                Destroy(todestory);
            }
            else
            {
                print("ERROR not find todestory task excute pn");
            }
            taskExcuteAvaliable = true;
        }


        public void changeTrigerMaxtime(string tistr)
        {
            maxTrigerTime = float.Parse(tistr);
            print("change tritime " + maxTrigerTime);
        }

        public void TestB()
        {
            // receiveDanmu("D7387093$#**#$雨天lul$#**#$qwe");
            // receiveDanmu("G7387093$#**#$雨天lul$#**#$qwe$#**#$11$#**#$silver$#**#$20");
            // receiveDanmu("G7387093$#**#$雨天lul$#**#$qwe$#**#$11$#**#$gold$#**#$20");
            // receiveDanmu("J7387093$#**#$雨天lul$#**#$qwe");
            // receiveDanmu("S7387093$#**#$雨天lul$#**#$qwe");
            Screen.SetResolution(1920, 1080, false);
            print(Screen.currentResolution.height);
        }
        public void TestB2()
        {

        } 

        // 人气  f'R[{client.room_id}] 当前人气: {message.popularity}'
        // 弹幕  f'D[{client.room_id}] {message.uname}: {message.msg}'
        // 礼物  f'G[{client.room_id}] {message.uname} 赠送了 {message.gift_name}x{message.num}'
        //                         f' ({message.coin_type} 瓜子 x {message.total_coin})'
        // 舰长  f'J[{client.room_id}] {message.username} 购买了 {message.gift_name}'
        // SC   f'S[{client.room_id}] 醒目留言 ￥{message.price} {message.uname}：{message.message}'
        public void receiveDanmu(string message) => danmumen.Enqueue(message);

        private HotkeyData TriggerSelectedHotkey(string currentHotkeySelected)
        {
            foreach (var hotkey in modelhotkeys)
            {
                if (hotkey.name == currentHotkeySelected)
                {
                    TriggerHotkey(hotkey.hotkeyID,
                        (r) => { print("TRI HOTKEY"); },
                        e => { }
                        );
                    return hotkey;
                }
            }
            return null;
        }

        private void SyncValues(VTSParameterInjectionValue[] values)
        {
            InjectParameterValues(
                values,
                (r) => { },
                (e) => { print(e.data.message); }
            );
        }

        public void RefreshPortList()
        {
            List<int> ports = new List<int>(GetPorts().Keys);
            foreach (Transform child in this._portConnectButtonParent)
            {
                Destroy(child.gameObject);
            }
            foreach (int port in ports)
            {
                //TOFIX
                // Button button = Instantiate<Button>(this._portConnectButtonPrefab, Vector3.zero, Quaternion.identity, this._portConnectButtonParent);
                // button.name = port.ToString();
                // button.GetComponentInChildren<Text>().text = button.name;
                // button.onClick.AddListener(() =>
                // {
                //     if (SetPort(int.Parse(button.name)))
                //     {
                //         Connect();
                //     }
                // });
            }
        }


        private float playaudio(string audiofile)
        {
            // audioFileReader.Close();
            if (waveOutDevice == null)
            {
                waveOutDevice = new WaveOut();
            }
            audioFileReader = new AudioFileReader(audiofile);
            waveOutDevice.Init(audioFileReader);
            waveOutDevice.Play();
            return (float)audioFileReader.TotalTime.TotalMilliseconds;
        }

        public GameObject fillTask(GameObject rootgo, GameObject parentgo, playTask halfpt)
        {
            //通过面板选择音频和快捷键
            //添加设定任务面板
            var taskPn = Instantiate((GameObject)Resources.Load("Prefabs/CreateTaskPanel"), Vector3.zero, Quaternion.identity, parentgo.transform);
            taskPn.transform.localPosition = new Vector3(parentgo.GetComponent<RectTransform>().rect.width, parentgo.GetComponent<RectTransform>().rect.height * 3, 0);
            //取消按钮
            taskPn.transform.Find("exitcreat").GetComponent<Button>().onClick.AddListener(() =>
            {
                Destroy(rootgo);
                return;
            }); ;

            //添加子任务面板
            var subPnNum = 0;
            var allSubTasksParent = taskPn.transform.Find("addProbTaskPn/subTasksPn/Viewport/subTasksParent");
            taskPn.transform.Find("addProbTaskPn").GetComponent<Button>().onClick.AddListener(() =>
            {
                var subtaskPn = Instantiate((GameObject)Resources.Load("Prefabs/filltask_subtasks"), Vector3.zero, Quaternion.identity, allSubTasksParent.transform);
                subtaskPn.transform.localPosition = Vector3.zero;
                subPnNum++;
                var hotkeyDropdownComp = subtaskPn.transform.Find("hotkey").GetComponent<Dropdown>();
                var dropdownData = new List<Dropdown.OptionData>();
                var audioSourcePn = subtaskPn.transform.Find("audio");
                var exitPn = subtaskPn.transform.Find("exit");

                if (modelhotkeys == null)
                {
                    show_danmu.text = "请先连接VTS\n" + show_danmu.text;
                    Destroy(subtaskPn);
                    subPnNum--;
                    return;
                }
                //在hotkey dropdown里加入请求到的hotkey
                foreach (var i in modelhotkeys)
                {
                    dropdownData.Add(new Dropdown.OptionData() { text = i.name });
                }

                hotkeyDropdownComp.AddOptions(dropdownData);

                //通过按钮选择audio以后把text改成选择的结果
                audioSourcePn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    var choiseAudiosourceContent = OpenFileByWin32.OpenFile();
                    audioSourcePn.transform.Find("Text").GetComponent<Text>().text = choiseAudiosourceContent;
                });

                //删除按钮
                exitPn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    subPnNum--;
                    Destroy(subtaskPn);
                    return;
                });
            });


            //完成选择，获取每个subtask，推入任务
            taskPn.GetComponent<Button>().onClick.AddListener(() =>
            {
                halfpt.subtaskNum = subPnNum;
                //检查非法的输入
                halfpt.hotKeys = new string[subPnNum];
                halfpt.audios = new string[subPnNum];
                halfpt.probabilitys = new int[subPnNum];
                var childcount = allSubTasksParent.transform.childCount;
                var prosum = 0;
                for (var i = 0; i < childcount; i++)
                {
                    var childi = allSubTasksParent.transform.GetChild(i);
                    if (childi.transform.Find("audio/Text").GetComponent<Text>().text == "添加音频")
                    {
                        childi.transform.Find("audio/Text").GetComponent<Text>().text = "";
                    }
                    var childiAudiotext = childi.transform.Find("audio/Text").GetComponent<Text>().text;
                    var childiHotkeyval = childi.transform.Find("hotkey").GetComponent<Dropdown>().value;
                    var childiHotkeytext = childiHotkeyval == 0 ? "" : childi.transform.Find("hotkey").GetComponent<Dropdown>().options[childi.transform.Find("hotkey").GetComponent<Dropdown>().value].text;
                    var childiProbtext = childi.transform.Find("probInputBar/probtext").GetComponent<Text>().text;
                    // print("--- " + childiAudiotext + "  " + childiHotkeytext + "  " + childiProbtext);

                    if (childiAudiotext == "" && childiHotkeytext == "")
                    {
                        show_danmu.text = "不能有任务为空\n" + show_danmu;
                        return;
                    }
                    else if (childiAudiotext == "" && maxTrigerTime == 0)
                    {
                        show_danmu.text = "快捷键最大时间和声音不能同时为空哦\n" + show_danmu;
                        return;
                    }
                    halfpt.audios[i] = childiAudiotext;
                    halfpt.hotKeys[i] = childiHotkeytext;
                    halfpt.probabilitys[i] = childiProbtext == "" ? 0 : int.Parse(childiProbtext);
                    prosum += halfpt.probabilitys[i];
                }
                //检查概率和为100
                if (prosum != 100)
                {
                    show_danmu.text = "概率和需要为100~\n" + show_danmu;
                    return;
                }
                else
                {
                    Tasks.pushRegTaskIntoList(halfpt);
                    Destroy(rootgo);
                }
            });
            return taskPn;

        }


        public void addReg()
        {
            var addRegButton = GameObject.Find("AddReg");
            var choiseStartPn = Instantiate((GameObject)Resources.Load("Prefabs/choise/choiseStart"), Vector3.zero, Quaternion.identity, addRegButton.transform);
            choiseStartPn.transform.localPosition = new Vector3(0, -addRegButton.GetComponent<RectTransform>().rect.height, 0);
            //重新选择时取消上次添加的面板
            GameObject nextStepPn = null;
            GameObject nextnextStepPn = null;

            //任务类型选择
            choiseStartPn.GetComponent<Dropdown>().onValueChanged.AddListener((val) =>
            {
                playTask pt = new playTask();
                pt.taskParameters = new string[0];
                if (nextStepPn != null)
                {
                    Destroy(nextStepPn);
                }
                if (nextnextStepPn != null)
                {
                    Destroy(nextnextStepPn);
                }
                //提前获取hotkeys
                GetHotkeysInCurrentModel(null, (r) => { modelhotkeys = new List<HotkeyData>(r.data.availableHotkeys); }, (e) => { });
                switch (val)
                {
                    case 0:
                        break;
                    case 1://弹幕
                           //用来输入匹配的弹幕的框
                        var danmuInputBar = Instantiate((GameObject)Resources.Load("Prefabs/danmuRegInputBar"), Vector3.zero, Quaternion.identity, choiseStartPn.transform);
                        danmuInputBar.transform.localPosition = new Vector3(0, -choiseStartPn.GetComponent<RectTransform>().rect.height, 0);
                        nextStepPn = danmuInputBar;
                        danmuInputBar.GetComponent<InputField>().onEndEdit.AddListener((inputval) =>
                        {
                            if (inputval == null || inputval == "")
                            {
                                Destroy(choiseStartPn);
                                return;
                            }
                            //添加任务
                            pt.taskType = "danmu";
                            pt.taskParameters = new string[1];
                            pt.taskParameters[0] = inputval;
                            nextnextStepPn = fillTask(choiseStartPn, danmuInputBar, pt);//组件补全任务
                        });

                        break;
                    case 2://礼物(银瓜子)
                        var yinguaziInputBar = Instantiate((GameObject)Resources.Load("Prefabs/danmuRegInputBar"), Vector3.zero, Quaternion.identity, choiseStartPn.transform);
                        yinguaziInputBar.transform.localPosition = new Vector3(0, -choiseStartPn.GetComponent<RectTransform>().rect.height, 0);
                        nextStepPn = yinguaziInputBar;
                        yinguaziInputBar.GetComponent<InputField>().onEndEdit.AddListener((inputval) =>
                        {
                            if (inputval == null || inputval == "")
                            {
                                Destroy(choiseStartPn);
                                return;
                            }
                            //添加任务
                            pt.taskType = "yinguazi";
                            pt.taskParameters = new string[1];
                            pt.taskParameters[0] = inputval;
                            nextnextStepPn = fillTask(choiseStartPn, yinguaziInputBar, pt);//组件补全任务
                        });
                        break;
                    case 3://礼物(金瓜子)
                        var jinguaziInputBar = Instantiate((GameObject)Resources.Load("Prefabs/danmuRegInputBar"), Vector3.zero, Quaternion.identity, choiseStartPn.transform);
                        jinguaziInputBar.transform.localPosition = new Vector3(0, -choiseStartPn.GetComponent<RectTransform>().rect.height, 0);
                        nextStepPn = jinguaziInputBar;
                        jinguaziInputBar.GetComponent<InputField>().onEndEdit.AddListener((inputval) =>
                        {
                            if (inputval == null || inputval == "")
                            {
                                Destroy(choiseStartPn);
                                return;
                            }
                            //添加任务
                            pt.taskType = "jinguazi";
                            pt.taskParameters = new string[1];
                            pt.taskParameters[0] = inputval;
                            fillTask(choiseStartPn, jinguaziInputBar, pt);//组件补全任务
                        });
                        break;
                    case 4://舰长
                           //添加任务
                        pt.taskType = "jianzhang";
                        fillTask(choiseStartPn, choiseStartPn, pt);//组件补全任务

                        break;
                    case 5://SC
                           //添加任务
                        pt.taskType = "sc";
                        fillTask(choiseStartPn, choiseStartPn, pt);//组件补全任务
                        break;
                }
            });
        }


        public void FixedUpdate()
        {
            //更新执行的任务情况,执行新的任务
            if (taskExcuteAvaliable && Tasks.TaskInstances.Count > 0)
            {
                // print("EEEEEXXX");
                taskExcuteAvaliable = false;
                var nt = Tasks.TaskInstances[0];
                Tasks.TaskInstances.Remove(nt);
                // print("TESK NUM " + Tasks.TaskInstances.Count);
                // Tasks.executeTask(nt);
                //随机触发
                var audiotime = 0.0f;
                var hotkeytime = 0.0f;
                var ran = new System.Random().Next(0, 101);
                var ransum = 0;
                for (var i = 0; i < nt.subtaskNum; i++)
                {
                    if (ran <= ransum + nt.probabilitys[i])
                    {
                        if (nt.audios[i] != "")
                        {
                            audiotime = playaudio(nt.audios[i]);
                        }
                        if (nt.hotKeys[i] != "")
                        {
                            hotkeytime = maxTrigerTime * 1000;
                            var res = TriggerSelectedHotkey(nt.hotKeys[i]);
                            // print("TRI HOT " + nt.hotKey);
                        }
                        show_danmu.text = "触发 %" + nt.probabilitys[i].ToString() + " 的 " + nt.audios[i] + " " + nt.hotKeys[i] + "\n" + show_danmu.text;
                        break;
                    }
                    else
                    {
                        ransum += nt.probabilitys[i];
                    }

                }
                print("TIMES " + audiotime + " " + hotkeytime + " " + Math.Max(audiotime, hotkeytime));
                //设置定时器
                Invoke("taskExcuteFinish", Math.Max(audiotime, hotkeytime) / 1000 + 0.1f);

            }

            // Obsolete python
            // while (danmumen.Count > 0)
            // {
            //     string danmu = danmumen.Dequeue();
            //     print(" -" + danmu + "- ");
            //     string[] danmu_msg = danmu.Split("$#**#$");
            //     //                Debug.Log(string.Join(",", danmu_msg));
            //     // Debug.Log(danmu[0]);
            //     // print("md type " + danmu[0] + " - " + danmu);
            //     switch (danmu[0])
            //     {
            //         // 收到弹幕
            //         case 'D':
            //             Tasks.testTrigerTask("danmu", new string[1] { danmu_msg[2] });
            //             show_danmu.text = $"{danmu_msg[1]}: {danmu_msg[2]}\n" + show_danmu.text;
            //             break;
            //
            //         // 收到礼物
            //         case 'G':
            //             show_danmu.text = $"{danmu_msg[1]} 赠送了 {danmu_msg[2]}x{danmu_msg[3]}"
            //                             + $" ({danmu_msg[4]} 瓜子 x {danmu_msg[5]})\n" + show_danmu.text;
            //             if ((danmu_msg[4] == "silver"))
            //             {
            //                 Tasks.testTrigerTask("yinguazi", new string[1] { danmu_msg[5] });
            //             }
            //             else if ((danmu_msg[4] == "gold"))
            //             {
            //                 Tasks.testTrigerTask("jinguazi", new string[1] { danmu_msg[5] });
            //             }
            //             // if (danmu_msg[4] == "gold" && int.Parse(danmu_msg[5]) >= guazi)
            //             // {
            //             //     HotkeyData giftTrigger = TriggerSelectedHotkey(liwuDropdown);
            //             //     info.text += $"\n{danmu_msg[1]} 的礼物触发了 {giftTrigger.name}({giftTrigger.file})";
            //             // }
            //             break;
            //
            //         // 有人上舰
            //         case 'J':
            //             show_danmu.text = $"{danmu_msg[1]} 购买了 {danmu_msg[2]}\n" + show_danmu.text;
            //             Tasks.testTrigerTask("jianzhang", new string[0]);
            //
            //             // HotkeyData captainTrigger = TriggerSelectedHotkey(captainList);
            //             // info.text += $"\n{danmu_msg[1]} 的礼物触发了 {captainTrigger.name}({captainTrigger.file})";
            //             break;
            //
            //         // SC
            //         case 'S':
            //             show_danmu.text = $"发送了醒目留言 ￥{danmu_msg[1]} {danmu_msg[2]}：{danmu_msg[3]}\n" + show_danmu.text;
            //             Tasks.testTrigerTask("sc", new string[0]);
            //
            //             // if (danmu_msg[3].Contains(superchatKeyword))
            //             // {
            //             //     HotkeyData SCTrigger = TriggerSelectedHotkey(SCDropdown);
            //             //     info.text += $"\n{danmu_msg[1]} 的礼物触发了 {SCTrigger.name}({SCTrigger.file})";
            //             // }
            //             break;
            //     }
            // }
        }
    }

}
