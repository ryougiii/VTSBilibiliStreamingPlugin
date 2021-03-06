using System.Collections.Generic;

using VTS.Networking.Impl;
using VTS.Models.Impl;
using VTS.Models;
using System.IO;
using LitJson;
using UnityEngine;
using UnityEngine.UI;

using UnityEditor;

/*
tasktype:

danmu 
yinguazi
jinguazi
jianzhang
sc
fixedTime //固定的时间
intervalTime //间隔一段时间（0时开始计算）
idleTime //无动作一段时间
guanzhu
tebieguanzhu
jinchang
giftname
*/

public class playTask
{
    public int subtaskNum;
    public string[] audios;
    public string[] hotKeys;
    public string taskType;
    public string[] taskParameters;

    public int[] probabilitys;

    public int taskId;
    public playTask()
    {
        taskId = VTS.Tasks.taskIdNum++;
        subtaskNum = 0;
        audios = new string[0];
        hotKeys = new string[0];
        taskType = "";
        taskParameters = new string[0];
        probabilitys = new int[0];
    }
}
namespace VTS
{
    public class Tasks : VTSPlugin
    {
        public static int taskIdNum = 0;

        public static List<playTask> LogicRegTasklists = new List<playTask>();
        public static List<playTask> TaskInstances = new List<playTask>();

        //time是以秒为单位的，同一秒内需要避免重复触发
        private static int lasttimeTaskTri0;
        private static int lasttimeTaskTri1;
        private static int lasttimeTaskTri2;
        public static int lastIdleTime = 0;
        public static bool VTSWebsocketConnectState = false;

        private void Awake()
        {
            var nowtime = (System.DateTime.Now.Hour * 60 + System.DateTime.Now.Minute) * 60 + System.DateTime.Now.Second;
            lasttimeTaskTri0 = nowtime;
            lasttimeTaskTri1 = nowtime;
            lasttimeTaskTri2 = nowtime;
        }
        private static string getTimeString(string tstring)
        {
            int i = int.Parse(tstring);
            print("i" + i);
            int t1, t2, t3;
            t1 = i % 60;
            i /= 60;
            t2 = i % 60;
            i /= 60;
            t3 = i;
            return t3 + "时" + t2 + "分" + t1 + "秒";
        }
        public static void pushRegTaskIntoList(playTask halfTaskGo)
        {
            string expressTaskInfo = "-------------------------------------------------------\n";
            switch (halfTaskGo.taskType)
            {
                case "danmu":
                    expressTaskInfo += "当弹幕中出现 " + halfTaskGo.taskParameters[0] + " 时，";
                    break;
                case "yinguazi":
                    expressTaskInfo += "当银瓜子数量 " + halfTaskGo.taskParameters[0] + "~" + halfTaskGo.taskParameters[1] + " 时，";
                    break;
                case "jinguazi":
                    expressTaskInfo += "当金瓜子数量 " + halfTaskGo.taskParameters[0] + "~" + halfTaskGo.taskParameters[1] + " 时，";
                    break;
                case "jianzhang":
                    expressTaskInfo += "当有人上舰时, ";
                    break;
                case "sc":
                    expressTaskInfo += "当有sc时, ";
                    break;
                case "fixedTime":
                    expressTaskInfo += "当到达" + getTimeString(halfTaskGo.taskParameters[0]) + "时, ";
                    break;
                case "intervalTime":
                    expressTaskInfo += "每隔" + getTimeString(halfTaskGo.taskParameters[0]) + "秒, ";
                    break;
                case "idleTime":
                    expressTaskInfo += getTimeString(halfTaskGo.taskParameters[0]) + "秒没有动作, ";
                    break;
                case "guanzhu":
                    expressTaskInfo += "新增关注时, ";
                    break;
                case "tebieguanzhu":
                    expressTaskInfo += "新增特别关注时, ";
                    break;
                case "jinchang":
                    expressTaskInfo += "有人进场时, ";
                    break;
                case "giftname":
                    expressTaskInfo += "有人送 " + halfTaskGo.taskParameters[0] + " 时, ";
                    break;
            }
            for (var i = 0; i < halfTaskGo.subtaskNum; i++)
            {
                print("!!! " + halfTaskGo.audios[i] + halfTaskGo.hotKeys[i] + halfTaskGo.probabilitys[i].ToString());
                expressTaskInfo += "\n-------------------------------";
                expressTaskInfo += "\n  -> " + halfTaskGo.probabilitys[i].ToString() + " % 的几率";
                if (halfTaskGo.audios[i] != "")
                {
                    expressTaskInfo += "\n   -->播放 " + halfTaskGo.audios[i];
                }
                if (halfTaskGo.hotKeys[i] != "")
                {
                    expressTaskInfo += "\n   -->触发 " + halfTaskGo.hotKeys[i];
                }
            }
            expressTaskInfo += "\n-------------------------------------------------------";

            //在面板中显示添加的规则
            var showpn = Instantiate((GameObject)Resources.Load("Prefabs/taskShowPn"), Vector3.zero, Quaternion.identity, GameObject.Find("TaskRegListContent").transform);
            showpn.transform.localPosition = Vector3.zero;
            showpn.transform.Find("Text").GetComponent<Text>().text = expressTaskInfo;
            showpn.transform.Find("Image").GetComponent<Button>().onClick.AddListener(() =>
            {
                // 取消Regtask
                removeTask(halfTaskGo.taskId);
                Destroy(showpn);
                saveTasksData();
            });
            var showPnRect = showpn.transform.GetComponent<RectTransform>();
            showPnRect.sizeDelta = new Vector2(showPnRect.rect.width, showpn.transform.GetChild(1).GetComponent<Text>().preferredHeight + 20);
            // print("WH " + showPnRect.sizeDelta.x + showPnRect.sizeDelta.y);
            //后台添加规则
            LogicRegTasklists.Add(halfTaskGo);
            saveTasksData();
        }

        public static void removeTask(int taskId)
        {
            foreach (var ltl in LogicRegTasklists)
            {
                if (ltl.taskId == taskId)
                {
                    LogicRegTasklists.Remove(ltl);
                    break;
                }
            }
        }

        public static void testTrigerTask(string meventType, string[] meventParas)
        {
            if (!VTSWebsocketConnectState)
            {
                return;
            }
            var nowtime = (System.DateTime.Now.Hour * 60 + System.DateTime.Now.Minute) * 60 + System.DateTime.Now.Second;
            if (meventType != "fixedTime" && meventType != "intervalTime" && meventType != "idleTime")
            {
                print("testTrigerTask " + meventType);
            }
            for (var i = 0; i < LogicRegTasklists.Count; i++)
            {
                var ltl = LogicRegTasklists[i];
                //类型不一样的跳过
                if (ltl.taskType != meventType)
                {
                    continue;
                }
                switch (meventType)
                {
                    case "danmu":
                        if (meventParas[0].Contains(ltl.taskParameters[0]))
                        {
                            //添加到执行等待队列中和等待执行gui中
                            TaskInstances.Add(ltl);
                            addTaskToExcuteGuiPn(ltl, i);
                            print("TRI DANMU " + meventParas[0]);
                        }
                        break;
                    case "yinguazi":
                        if (float.Parse(meventParas[0]) >= float.Parse(ltl.taskParameters[0]) && float.Parse(meventParas[0]) <= float.Parse(ltl.taskParameters[1]))
                        {
                            TaskInstances.Add(ltl);
                            addTaskToExcuteGuiPn(ltl, i);
                            print("TRI yinguazi" + meventParas[0]);
                        }
                        break;
                    case "jinguazi":
                        if (float.Parse(meventParas[0]) >= float.Parse(ltl.taskParameters[0]) && float.Parse(meventParas[0]) <= float.Parse(ltl.taskParameters[1]))
                        {
                            TaskInstances.Add(ltl);
                            addTaskToExcuteGuiPn(ltl, i);
                            print("TRI jinguazi " + meventParas[0]);
                        }
                        break;
                    case "jianzhang":
                        TaskInstances.Add(ltl);
                        addTaskToExcuteGuiPn(ltl, i);
                        print("TRI jianzhang ");
                        break;
                    case "sc":
                        TaskInstances.Add(ltl);
                        addTaskToExcuteGuiPn(ltl, i);
                        print("TRI sc ");
                        break;
                    case "fixedTime":
                        // print("fff " + meventParas[0] + " " + ltl.taskParameters[0] + " " + timeTaskAvaliable[0]);
                        if (meventParas[0] == ltl.taskParameters[0] && nowtime - lasttimeTaskTri0 > 1)
                        {
                            print("TRI fixedTime ");
                            lasttimeTaskTri0 = nowtime;
                            TaskInstances.Add(ltl);
                            addTaskToExcuteGuiPn(ltl, i);
                        }
                        break;
                    case "intervalTime":
                        if (int.Parse(meventParas[0]) % int.Parse(ltl.taskParameters[0]) == 0 && nowtime - lasttimeTaskTri1 > 1)
                        {
                            lasttimeTaskTri1 = nowtime;
                            TaskInstances.Add(ltl);
                            addTaskToExcuteGuiPn(ltl, i);

                            print("TRI intervalTime ");
                        }
                        break;
                    case "idleTime":
                        if (int.Parse(meventParas[0]) >= lastIdleTime && nowtime - lasttimeTaskTri2 > 1)
                        {
                            lasttimeTaskTri2 = nowtime;
                            TaskInstances.Add(ltl);
                            addTaskToExcuteGuiPn(ltl, i);
                            lastIdleTime = int.Parse(meventParas[0]);
                            print("TRI idleTime ");
                        }
                        break;
                    case "guanzhu":
                        TaskInstances.Add(ltl);
                        addTaskToExcuteGuiPn(ltl, i);
                        print("TRI guanzhu ");
                        break;
                    case "tebieguanzhu":
                        TaskInstances.Add(ltl);
                        addTaskToExcuteGuiPn(ltl, i);
                        print("TRI tebieguanzhu ");
                        break;
                    case "jinchang":
                        TaskInstances.Add(ltl);
                        addTaskToExcuteGuiPn(ltl, i);
                        print("TRI jinchang ");
                        break;
                    case "giftname":
                        if (meventParas[0] == ltl.taskParameters[0])
                        {
                            print("TRI giftname");
                            TaskInstances.Add(ltl);
                            addTaskToExcuteGuiPn(ltl, i);
                        }
                        break;
                }
            }

        }
        private static void addTaskToExcuteGuiPn(playTask excTask, int taskNo)
        {
            //要执行的任务加到gui执行情况列表里
            var showNowTaskPn = Instantiate((GameObject)Resources.Load("Prefabs/taskShowPn"), Vector3.zero, Quaternion.identity, GameObject.Find("TaskListContent").transform);
            showNowTaskPn.transform.position = Vector3.zero;
            showNowTaskPn.transform.Find("Text").GetComponent<Text>().text = "----------\n 触发了第 " + taskNo.ToString() + "条规则\n";
            //取消这个任务
            showNowTaskPn.transform.Find("Image").GetComponent<Button>().onClick.AddListener(() =>
            {
                Destroy(showNowTaskPn);
                TaskInstances.Remove(excTask);
            });
            var showPnRect = showNowTaskPn.transform.GetComponent<RectTransform>();
            showPnRect.sizeDelta = new Vector2(showPnRect.rect.width, showNowTaskPn.transform.Find("Text").GetComponent<Text>().preferredHeight + 20);
        }


        public static void saveTasksData()
        {
            string floderpath = Application.dataPath + @"/savedata";
            string filepath = Application.dataPath + @"/savedata/savetasks.json";
            if (!System.IO.File.Exists(floderpath))
            {
                System.IO.Directory.CreateDirectory(floderpath);
            }
            if (!System.IO.File.Exists(filepath))
            {
                System.IO.File.Create(filepath).Dispose();
            }
            FileInfo file = new FileInfo(filepath);
            StreamWriter sw = file.CreateText();
            string json = JsonMapper.ToJson(Tasks.LogicRegTasklists);
            sw.WriteLine(json);
            sw.Close();
            sw.Dispose();
        }
        public static void loadTasksData()
        {
            string floderpath = Application.dataPath + @"/savedata";
            string filepath = Application.dataPath + @"/savedata/savetasks.json";
            if (!System.IO.File.Exists(floderpath))
            {
                System.IO.Directory.CreateDirectory(floderpath);
            }
            if (!System.IO.File.Exists(filepath))
            {
                System.IO.File.Create(filepath).Dispose();
            }
            FileInfo file = new FileInfo(filepath);
            StreamReader sr = file.OpenText();
            string str = sr.ReadLine();
            sr.Close();
            sr.Dispose();
            if (str != null && str != "")
            {
                var readtasks = JsonMapper.ToObject<List<playTask>>(str);
                foreach (var item in readtasks)
                {
                    Tasks.pushRegTaskIntoList(item);
                }
            }
        }
        public static void saveRoomData(string roomid)
        {
            string floderpath = Application.dataPath + @"/savedata";
            string filepath = Application.dataPath + @"/savedata/saveroom.json";
            if (!System.IO.File.Exists(floderpath))
            {
                System.IO.Directory.CreateDirectory(floderpath);
            }
            if (!System.IO.File.Exists(filepath))
            {
                System.IO.File.Create(filepath).Dispose();
            }
            FileInfo file = new FileInfo(filepath);
            StreamWriter sw = file.CreateText();
            sw.WriteLine(roomid);
            sw.Close();
            sw.Dispose();
        }

        public static void loadRoomData()
        {
            string floderpath = Application.dataPath + @"/savedata";
            string filepath = Application.dataPath + @"/savedata/saveroom.json";
            if (!System.IO.File.Exists(floderpath))
            {
                System.IO.Directory.CreateDirectory(floderpath);
            }
            if (!System.IO.File.Exists(filepath))
            {
                System.IO.File.Create(filepath).Dispose();
            }
            FileInfo file = new FileInfo(filepath);
            StreamReader sr = file.OpenText();
            string str = sr.ReadLine();
            sr.Close();
            sr.Dispose();
            if (str != null && str != "")
            {
                var readroomid = int.Parse(str);
                GameObject.Find("Room_ID").GetComponent<InputField>().text = readroomid.ToString(); ;
            }
        }

    }
}