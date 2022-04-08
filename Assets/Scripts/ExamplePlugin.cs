using System.Collections.Generic;

using VTS.Networking.Impl;
using VTS.Models.Impl;
using VTS.Models;

using UnityEngine;
using UnityEngine.UI;

using UnityEditor;


using NAudio;
using NAudio.Wave;



namespace VTS.Examples
{
    [RequireComponent(typeof(AudioSource))]
    public class ExamplePlugin : VTSPlugin
    {
        private bool audioPlaying = false;
        [SerializeField]
        private Text info = null;
        [SerializeField]
        private Text show_danmu = null;

        [SerializeField]
        private Button _portConnectButtonPrefab = null;

        [SerializeField]
        private Button _tryConnectButton = null;

        [SerializeField]
        private RectTransform _portConnectButtonParent = null;

        [SerializeField]
        private Image _connectionLight = null;

        [SerializeField]
        private Text _connectionText = null;

        // [SerializeField]
        // private Dropdown hotkeyDropdown = null;
        [SerializeField]
        private Dropdown liwuDropdown = null;
        [SerializeField]
        private Dropdown SCDropdown = null;
        [SerializeField]
        private Dropdown captainList = null;


        private List<HotkeyData> hotkeys = null;

        private readonly Queue<string> danmumen = new Queue<string>();
        private int guazi = int.MaxValue;
        private string superchatKeyword = "";

        private float maxTrigerTime = 0;
        IWavePlayer waveOutDevice;
        AudioFileReader audioFileReader;

        private void Awake()
        {
            Connect();
            //UnityEngine.Debug.Log(AppData);
            //UnityEngine.Debug.Log(Application.dataPath);
            Application.targetFrameRate = 30;
            GameObject.Find("AddReg").GetComponent<Button>().onClick.AddListener(() =>
            {
                addReg();
            });
        }
        void OnApplicationQuit()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
                waveOutDevice.Dispose();
                waveOutDevice = null;
            }
        }
        private void Connect()
        {
            print("PRE conn");
            this._connectionLight.color = Color.yellow;
            this._connectionText.text = "Connecting...";
            Initialize(new WebSocketSharpImpl(), new JsonUtilityImpl(), new TokenStorageImpl(),
            () =>
            {
                UnityEngine.Debug.Log("Connected!");
                this._connectionLight.color = Color.green;
                this._connectionText.text = "Connected!";
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
        }

        public void TestB()
        {

            // var au = EditorUtility.OpenFilePanel("opppp", "", "*");
            // print(au);
            // waveOutDevice = new WaveOut();
            // audioFileReader = new AudioFileReader(au);
            // waveOutDevice.Init(audioFileReader);
            // waveOutDevice.Play();



            var tt = (GameObject)Resources.Load("Prefabs/taskShowPn");
            tt.transform.SetParent(GameObject.Find("TaskListContent").transform);
            // tt.transform.parent = GameObject.Find("TaskListContent").transform;

        }

        public void changeTrigerMaxtime()
        {
            maxTrigerTime = float.Parse(GameObject.Find("maxTrigerTime").GetComponent<InputField>().text);
            print(maxTrigerTime);
            // print(gameObject.name);


        }
        public void TestB2()
        {

            // var au = EditorUtility.OpenFilePanel("opppp", "", "*");
            // print(au);
            // waveOutDevice = new WaveOut();
            // audioFileReader = new AudioFileReader(au);
            // waveOutDevice.Init(audioFileReader);
            // waveOutDevice.Play();
            var ti = Resources.Load("Prefabs/taskShowPn");
            var g = Instantiate((GameObject)ti, Vector3.zero, Quaternion.identity);
            g.transform.SetParent(GameObject.Find("TaskRegListContent").transform);

        }


        private HotkeyData TriggerSelectedHotkey(Dropdown dp)
        {
            string currentHotkeySelected = dp.options[dp.value].text;
            foreach (var hotkey in hotkeys)
            {
                if (hotkey.file == currentHotkeySelected)
                {
                    TriggerHotkey(hotkey.hotkeyID,
                        (r) => { },
                        e => { info.text = $"热键不存在，请刷新"; }
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
                Button button = Instantiate<Button>(this._portConnectButtonPrefab, Vector3.zero, Quaternion.identity, this._portConnectButtonParent);
                button.name = port.ToString();
                button.GetComponentInChildren<Text>().text = button.name;
                button.onClick.AddListener(() =>
                {
                    if (SetPort(int.Parse(button.name)))
                    {
                        Connect();
                    }
                });
            }
        }

        public void ChangeGuazi(string msg)
        {
            if (msg == "")
            {
                guazi = int.MaxValue;
                info.text = "取消金瓜子设定";
            }
            else
            {
                int.TryParse(msg, out guazi);
                info.text = $"礼物阈值：{guazi}金瓜子";
            }
        }

        public void ChangeKeyword(string msg)
        {
            superchatKeyword = msg;
            info.text = $"SuperChat关键词：{superchatKeyword}";
        }

        // 人气  f'R[{client.room_id}] 当前人气: {message.popularity}'
        // 弹幕  f'D[{client.room_id}] {message.uname}: {message.msg}'
        // 礼物  f'G[{client.room_id}] {message.uname} 赠送了 {message.gift_name}x{message.num}'
        //                         f' ({message.coin_type} 瓜子 x {message.total_coin})'
        // 舰长  f'J[{client.room_id}] {message.username} 购买了 {message.gift_name}'
        // 艾西  f'S[{client.room_id}] 醒目留言 ￥{message.price} {message.uname}：{message.message}'
        public void receiveDanmu(string message) => danmumen.Enqueue(message);

        private void playaudio(string file)
        {


        }

        public void fillTask(GameObject rootgo, GameObject parentgo, playTask halfpt)
        {
            //添加设定任务面板
            var taskPn = Instantiate((GameObject)Resources.Load("Prefabs/CreateTaskPanel"), Vector3.zero, Quaternion.identity);
            taskPn.transform.parent = parentgo.transform;
            taskPn.transform.localPosition = new Vector3(taskPn.GetComponent<RectTransform>().rect.width / 2 + parentgo.GetComponent<RectTransform>().rect.width / 2, 0, 0);
            //取消按钮
            taskPn.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() =>
            {
                Destroy(rootgo);
                return;
            });
            var hotkeyDropdownComp = taskPn.transform.GetChild(3).GetComponent<Dropdown>();
            var dropdownData = new List<Dropdown.OptionData>();
            var audioSourcePn = taskPn.transform.GetChild(1);
            var choiseAudiosourceContent = "";
            var choiseHotkeyContent = "";
            //异步处理
            //获取hotkey和选择结果
            foreach (var i in hotkeys)
            {
                dropdownData.Add(new Dropdown.OptionData() { text = i.name });
            }

            hotkeyDropdownComp.AddOptions(dropdownData);

            hotkeyDropdownComp.onValueChanged.AddListener((num) =>
            {
                choiseHotkeyContent = hotkeyDropdownComp.options[num].text;
                if (num != 0)
                {
                    halfpt.hotKey = choiseHotkeyContent;
                }
                print("HKD " + choiseHotkeyContent);
            });

            //打开audio选择面板
            audioSourcePn.GetComponent<Button>().onClick.AddListener(() =>
            {
                choiseAudiosourceContent = EditorUtility.OpenFilePanel("选个播放的声音吧", "", "*");
                halfpt.audio = choiseAudiosourceContent;
                audioSourcePn.transform.GetChild(0).GetComponent<Text>().text = choiseAudiosourceContent;
                print("aud souuuuuiii " + choiseAudiosourceContent);
            });

            //完成选择，推入任务
            taskPn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (halfpt.audio == "" && halfpt.hotKey == "")
                {
                    //选个任务吧
                }
                else
                {
                    Tasks.pushRegTaskIntoList(halfpt);
                    Destroy(rootgo);
                }
            });


        }


        public void addReg()
        {
            var addRegButton = GameObject.Find("AddReg");
            var choiseStartPn = Instantiate((GameObject)Resources.Load("Prefabs/choise/choiseStart"), Vector3.zero, Quaternion.identity);
            choiseStartPn.transform.parent = addRegButton.transform;
            choiseStartPn.transform.localPosition = new Vector3(addRegButton.GetComponent<RectTransform>().rect.width / 2 + choiseStartPn.GetComponent<RectTransform>().rect.width / 2, 0, 0);
            choiseStartPn.GetComponent<Dropdown>().onValueChanged.AddListener((val) =>
            {
                playTask pt = new playTask();
                pt.hotKey = "";
                pt.audio = "";
                pt.taskParameters = new string[0];
                switch (val)
                {
                    case 0:
                        break;
                    case 1://弹幕

                        // print("?" + this.danmuRegInputtext == null);
                        // TestB2();
                        var danmuInputBar = Instantiate((GameObject)Resources.Load("Prefabs/danmuRegInputBar"), Vector3.zero, Quaternion.identity);
                        danmuInputBar.transform.parent = choiseStartPn.transform;
                        danmuInputBar.transform.localPosition = new Vector3(choiseStartPn.GetComponent<RectTransform>().rect.width / 2 + danmuInputBar.GetComponent<RectTransform>().rect.width / 2, 0, 0);
                        //提前获取hotkeys
                        GetHotkeysInCurrentModel(null, (r) => { hotkeys = new List<HotkeyData>(r.data.availableHotkeys); }, (e) => { });

                        danmuInputBar.GetComponent<InputField>().onEndEdit.AddListener((inputval) =>
                        {
                            print(inputval);
                            if (inputval == null || inputval == "")
                            {
                                Destroy(choiseStartPn);
                                return;
                            }
                            //添加任务
                            pt.taskType = "danmu";
                            pt.taskParameters = new string[1];
                            pt.taskParameters[0] = inputval;
                            fillTask(choiseStartPn, danmuInputBar, pt);//组件补全任务
                        });

                        break;
                    case 2://礼物

                        break;
                    case 3://舰长
                        break;
                    case 4://SC
                        break;
                }
            });
        }




        public void FixedUpdate()
        {
            // GetExpressionStateList((a) =>
            // {
            //     print(a.data.expressionFile);
            //     // print(a.data.);
            //     for (var i = 0; i < a.data.expressions.Length; i++)
            //     {
            //         print("expx " + i + " " + a.data.expressions[i].name);
            //         print("expxx " + i + " " + a.data.expressions[i].active);
            //         print("expxxx " + i + " " + a.data.expressions[i].file);
            //     }
            // }, (a) => { });


            while (danmumen.Count > 0)
            {
                string danmu = danmumen.Dequeue();
                string[] danmu_msg = danmu.Split("$#**#$");
                //                Debug.Log(string.Join(",", danmu_msg));
                // Debug.Log(danmu[0]);
                print("md type " + danmu[0] + " - " + danmu);
                switch (danmu[0])
                {
                    // 收到弹幕
                    case 'D':
                        // show_danmu.text += $"\nD[{danmu_msg[0]}] {danmu_msg[1]}: {danmu_msg[2]}";
                        break;

                    // 收到礼物
                    case 'G':
                        // if (!(danmu_msg[4] == "silver"))
                        // show_danmu.text += $"\nG[{danmu_msg[0]}] {danmu_msg[1]} 赠送了 {danmu_msg[2]}x{danmu_msg[3]}"
                        //                 + $" ({danmu_msg[4]} 瓜子 x {danmu_msg[5]})";
                        if (danmu_msg[4] == "gold" && int.Parse(danmu_msg[5]) >= guazi)
                        {
                            HotkeyData giftTrigger = TriggerSelectedHotkey(liwuDropdown);
                            info.text += $"\n{danmu_msg[1]} 的礼物触发了 {giftTrigger.name}({giftTrigger.file})";
                        }
                        break;

                    // 有人上舰
                    case 'J':
                        show_danmu.text += $"\nJ[{danmu_msg[0]}] {danmu_msg[1]} 购买了 {danmu_msg[2]}";
                        HotkeyData captainTrigger = TriggerSelectedHotkey(captainList);
                        info.text += $"\n{danmu_msg[1]} 的礼物触发了 {captainTrigger.name}({captainTrigger.file})";
                        break;

                    // SC
                    case 'S':
                        show_danmu.text += $"\nS[{danmu_msg[0]}] 发送了醒目留言 ￥{danmu_msg[1]} {danmu_msg[2]}：{danmu_msg[3]}";
                        if (danmu_msg[3].Contains(superchatKeyword))
                        {
                            HotkeyData SCTrigger = TriggerSelectedHotkey(SCDropdown);
                            info.text += $"\n{danmu_msg[1]} 的礼物触发了 {SCTrigger.name}({SCTrigger.file})";
                        }
                        break;
                }
            }
        }
    }

}
