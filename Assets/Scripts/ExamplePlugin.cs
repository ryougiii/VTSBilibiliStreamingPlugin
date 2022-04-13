using System.Collections.Generic;
using System;
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

        private List<HotkeyData> hotkeys = null;

        private readonly Queue<string> danmumen = new Queue<string>();

        private float maxTrigerTime = 1;
        IWavePlayer waveOutDevice;
        AudioFileReader audioFileReader;

        private void Awake()
        {
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
                GetHotkeysInCurrentModel(null, (r) => { hotkeys = new List<HotkeyData>(r.data.availableHotkeys); }, (e) => { });

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
            GetHotkeysInCurrentModel(null, (r) => { hotkeys = new List<HotkeyData>(r.data.availableHotkeys); }, (e) => { });

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

            string filepath = Application.dataPath + @"/savedata/savetasks.json";
            print("path " + filepath);
            FileInfo file = new FileInfo(filepath);
            StreamWriter sw = file.CreateText();
            string json = JsonMapper.ToJson(Tasks.LogicRegTasklists);
            print("json " + json);
            sw.WriteLine(json);
            sw.Close();
            sw.Dispose();
        }
        public void TestB2()
        {
            string filepath = Application.dataPath + @"/savedata/savetasks.json";
            FileInfo file = new FileInfo(filepath);

            StreamReader sr = file.OpenText();
            string str = sr.ReadLine();
            print("read " + str);
            var readtasks = JsonMapper.ToObject<List<playTask>>(str);
            foreach (var item in readtasks)
            {
                Tasks.pushRegTaskIntoList(item);
            }
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
            foreach (var hotkey in hotkeys)
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

            var subPnNum = 0;
            var allSubTasksParent = Instantiate(new GameObject(), Vector3.zero, Quaternion.identity, taskPn.transform);
            allSubTasksParent.name = "allSubTasksParent";
            allSubTasksParent.transform.localPosition = Vector3.zero;
            taskPn.transform.Find("addProbTaskPn").GetComponent<Button>().onClick.AddListener(() =>
            {
                var subtaskPn = Instantiate((GameObject)Resources.Load("Prefabs/filltask_subtasks"), Vector3.zero, Quaternion.identity, allSubTasksParent.transform);
                subtaskPn.transform.localPosition = new Vector3(taskPn.GetComponent<RectTransform>().rect.width + subPnNum++ * subtaskPn.transform.Find("hotkey").GetComponent<RectTransform>().rect.width, 0, 0);
                print(subPnNum * subtaskPn.transform.Find("hotkey").GetComponent<RectTransform>().rect.width);

            });


            // var hotkeyDropdownComp = taskPn.transform.GetChild(3).GetComponent<Dropdown>();
            // var dropdownData = new List<Dropdown.OptionData>();
            // var audioSourcePn = taskPn.transform.GetChild(1);
            // var choiseAudiosourceContent = "";
            // var choiseHotkeyContent = "";
            // //异步处理
            // //获取hotkey和选择结果
            // foreach (var i in hotkeys)
            // {
            //     dropdownData.Add(new Dropdown.OptionData() { text = i.name });
            // }

            // hotkeyDropdownComp.AddOptions(dropdownData);

            // hotkeyDropdownComp.onValueChanged.AddListener((num) =>
            // {
            //     choiseHotkeyContent = hotkeyDropdownComp.options[num].text;
            //     if (num != 0)
            //     {
            //         halfpt.hotKey = choiseHotkeyContent;
            //     }
            //     // print("HKD " + choiseHotkeyContent);
            // });

            // //打开audio选择面板
            // audioSourcePn.GetComponent<Button>().onClick.AddListener(() =>
            // {
            //     choiseAudiosourceContent = OpenFileByWin32.OpenFile();
            //     halfpt.audio = choiseAudiosourceContent;
            //     audioSourcePn.transform.GetChild(0).GetComponent<Text>().text = choiseAudiosourceContent;
            //     // print("aud souuuuuiii " + choiseAudiosourceContent);
            // });

            // //完成选择，推入任务
            // taskPn.GetComponent<Button>().onClick.AddListener(() =>
            // {
            //     if (halfpt.audio == "" && halfpt.hotKey == "")
            //     {
            //         show_danmu.text = "选个任务吧\n" + show_danmu;
            //         //选个任务吧
            //     }
            //     else if (halfpt.audio == "" && maxTrigerTime == 0)
            //     {
            //         show_danmu.text = "快捷键最大时间和声音不能同时为空哦\n" + show_danmu;
            //     }
            //     else
            //     {
            //         Tasks.pushRegTaskIntoList(halfpt);
            //         Destroy(rootgo);
            //     }
            // });
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
                GetHotkeysInCurrentModel(null, (r) => { hotkeys = new List<HotkeyData>(r.data.availableHotkeys); }, (e) => { });
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
            // if (taskExcuteAvaliable && Tasks.TaskInstances.Count > 0)
            // {
            //     // print("EEEEEXXX");
            //     taskExcuteAvaliable = false;
            //     var nt = Tasks.TaskInstances[0];
            //     Tasks.TaskInstances.Remove(nt);
            //     // print("TESK NUM " + Tasks.TaskInstances.Count);
            //     // Tasks.executeTask(nt);
            //     var audiotime = 0.0f;
            //     var hotkeytime = 0.0f;
            //     if (nt.audio != "")
            //     {
            //         audiotime = playaudio(nt.audio);
            //     }
            //     if (nt.hotKey != "")
            //     {
            //         hotkeytime = maxTrigerTime * 1000;
            //         var res = TriggerSelectedHotkey(nt.hotKey);
            //         // print("TRI HOT " + nt.hotKey);
            //     }
            //     print("TIMES " + audiotime + " " + hotkeytime + " " + Math.Max(audiotime, hotkeytime));
            //     //设置定时器
            //     Invoke("taskExcuteFinish", Math.Max(audiotime, hotkeytime) / 1000 + 0.1f);

            // }


            while (danmumen.Count > 0)
            {
                string danmu = danmumen.Dequeue();
                print(" -" + danmu + "- ");
                string[] danmu_msg = danmu.Split("$#**#$");
                //                Debug.Log(string.Join(",", danmu_msg));
                // Debug.Log(danmu[0]);
                // print("md type " + danmu[0] + " - " + danmu);
                switch (danmu[0])
                {
                    // 收到弹幕
                    case 'D':
                        Tasks.testTrigerTask("danmu", new string[1] { danmu_msg[2] });
                        show_danmu.text = $"{danmu_msg[1]}: {danmu_msg[2]}\n" + show_danmu.text;
                        break;

                    // 收到礼物
                    case 'G':
                        show_danmu.text = $"{danmu_msg[1]} 赠送了 {danmu_msg[2]}x{danmu_msg[3]}"
                                        + $" ({danmu_msg[4]} 瓜子 x {danmu_msg[5]})\n" + show_danmu.text;
                        if ((danmu_msg[4] == "silver"))
                        {
                            Tasks.testTrigerTask("yinguazi", new string[1] { danmu_msg[5] });
                        }
                        else if ((danmu_msg[4] == "gold"))
                        {
                            Tasks.testTrigerTask("jinguazi", new string[1] { danmu_msg[5] });
                        }
                        // if (danmu_msg[4] == "gold" && int.Parse(danmu_msg[5]) >= guazi)
                        // {
                        //     HotkeyData giftTrigger = TriggerSelectedHotkey(liwuDropdown);
                        //     info.text += $"\n{danmu_msg[1]} 的礼物触发了 {giftTrigger.name}({giftTrigger.file})";
                        // }
                        break;

                    // 有人上舰
                    case 'J':
                        show_danmu.text = $"{danmu_msg[1]} 购买了 {danmu_msg[2]}\n" + show_danmu.text;
                        Tasks.testTrigerTask("jianzhang", new string[0]);

                        // HotkeyData captainTrigger = TriggerSelectedHotkey(captainList);
                        // info.text += $"\n{danmu_msg[1]} 的礼物触发了 {captainTrigger.name}({captainTrigger.file})";
                        break;

                    // SC
                    case 'S':
                        show_danmu.text = $"发送了醒目留言 ￥{danmu_msg[1]} {danmu_msg[2]}：{danmu_msg[3]}\n" + show_danmu.text;
                        Tasks.testTrigerTask("sc", new string[0]);

                        // if (danmu_msg[3].Contains(superchatKeyword))
                        // {
                        //     HotkeyData SCTrigger = TriggerSelectedHotkey(SCDropdown);
                        //     info.text += $"\n{danmu_msg[1]} 的礼物触发了 {SCTrigger.name}({SCTrigger.file})";
                        // }
                        break;
                }
            }
        }
    }

}
