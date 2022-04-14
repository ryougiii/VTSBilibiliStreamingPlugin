using System.Collections.Generic;

using VTS.Networking.Impl;
using VTS.Models.Impl;
using VTS.Models;
using System.IO;
using LitJson;
using UnityEngine;
using UnityEngine.UI;

using UnityEditor;
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

        public static void pushRegTaskIntoList(playTask halfTaskGo)
        {
            string expressTaskInfo = "-------------------------------------------------------\n";
            switch (halfTaskGo.taskType)
            {
                case "danmu":
                    expressTaskInfo += "当弹幕中出现 " + halfTaskGo.taskParameters[0] + " 时，";
                    break;
                case "yinguazi":
                    expressTaskInfo += "当银瓜子>= " + halfTaskGo.taskParameters[0] + " 时，";
                    break;
                case "jinguazi":
                    expressTaskInfo += "当金瓜子>= " + halfTaskGo.taskParameters[0] + " 时，";
                    break;
                case "jianzhang":
                    expressTaskInfo += "当有人上舰时, ";
                    break;
                case "sc":
                    expressTaskInfo += "当有sc时, ";
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
            print("testTrigerTask " + meventType);
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
                        if (float.Parse(meventParas[0]) >= float.Parse(ltl.taskParameters[0]))
                        {
                            TaskInstances.Add(ltl);
                            addTaskToExcuteGuiPn(ltl, i);
                            print("TRI yinguazi" + meventParas[0]);
                        }
                        break;
                    case "jinguazi":
                        if (float.Parse(meventParas[0]) >= float.Parse(ltl.taskParameters[0]))
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
        public static void saveRoomData()
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
            sw.WriteLine(LoadPython.room_id.ToString());
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
                LoadPython.room_id = readroomid;
            }
        }

    }
}