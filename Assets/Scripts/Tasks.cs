using System.Collections.Generic;

using VTS.Networking.Impl;
using VTS.Models.Impl;
using VTS.Models;

using UnityEngine;
using UnityEngine.UI;

using UnityEditor;
public class playTask
{
    public string audio;
    public string hotKey;
    public string taskType;
    public string[] taskParameters;

    public int taskId;
    public playTask()
    {
        taskId = VTS.Tasks.taskIdNum++;
        audio = "";
        hotKey = "";
        taskType = "";
        taskParameters = new string[0];
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
            string expressTaskInfo = "";
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
            if (halfTaskGo.audio != "")
            {
                expressTaskInfo += "\n   ->播放 " + halfTaskGo.audio;
            }
            if (halfTaskGo.hotKey != "")
            {
                expressTaskInfo += "\n   ->触发 " + halfTaskGo.hotKey;
                print("HHH " + halfTaskGo.hotKey.Length + halfTaskGo.hotKey);
            }
            //在面板中显示添加的规则
            var showpn = Instantiate((GameObject)Resources.Load("Prefabs/taskShowPn"), Vector3.zero, Quaternion.identity, GameObject.Find("TaskRegListContent").transform);
            showpn.transform.localPosition = Vector3.zero;
            showpn.transform.GetChild(1).GetComponent<Text>().text = expressTaskInfo;
            showpn.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
            {
                // 取消Regtask
                removeTask(halfTaskGo.taskId);
                Destroy(showpn);
            });
            var showPnRect = showpn.transform.GetComponent<RectTransform>();
            showPnRect.sizeDelta = new Vector2(showPnRect.rect.width, showpn.transform.GetChild(1).GetComponent<Text>().preferredHeight + 20);
            // print("WH " + showPnRect.sizeDelta.x + showPnRect.sizeDelta.y);
            //后台添加规则
            LogicRegTasklists.Add(halfTaskGo);
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
            foreach (var ltl in LogicRegTasklists)
            {
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
                            prepareExecuteTask(ltl);
                            addTaskToExcuteGuiPn(ltl);
                            print("TRI DANMU " + meventParas[0]);
                        }
                        break;
                    case "yinguazi":
                        if (float.Parse(meventParas[0]) >= float.Parse(ltl.taskParameters[0]))
                        {
                            prepareExecuteTask(ltl);
                            addTaskToExcuteGuiPn(ltl);
                            print("TRI yinguazi" + meventParas[0]);
                        }
                        break;
                    case "jinguazi":
                        if (float.Parse(meventParas[0]) >= float.Parse(ltl.taskParameters[0]))
                        {
                            prepareExecuteTask(ltl);
                            addTaskToExcuteGuiPn(ltl);
                            print("TRI jinguazi " + meventParas[0]);
                        }
                        break;
                    case "jianzhang":
                        prepareExecuteTask(ltl);
                        addTaskToExcuteGuiPn(ltl);
                        print("TRI jianzhang ");
                        break;
                    case "sc":
                        prepareExecuteTask(ltl);
                        addTaskToExcuteGuiPn(ltl);
                        print("TRI sc ");
                        break;
                }
            }

        }
        private static void addTaskToExcuteGuiPn(playTask excTask)
        {
            //要执行的任务加到gui执行情况列表里
            var showNowTaskPn = Instantiate((GameObject)Resources.Load("Prefabs/taskShowPn"), Vector3.zero, Quaternion.identity, GameObject.Find("TaskListContent").transform);
            showNowTaskPn.transform.position = Vector3.zero;
            showNowTaskPn.transform.GetChild(1).GetComponent<Text>().text = "----------\n" +
                (excTask.audio == "" ? "" : (" +" + excTask.audio + "\n")) +
                (excTask.hotKey == "" ? "" : (" +" + excTask.hotKey + "\n"));
            //取消这个任务
            showNowTaskPn.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
            {
                Destroy(showNowTaskPn);
                TaskInstances.Remove(excTask);
            });
            var showPnRect = showNowTaskPn.transform.GetComponent<RectTransform>();
            showPnRect.sizeDelta = new Vector2(showPnRect.rect.width, showNowTaskPn.transform.GetChild(1).GetComponent<Text>().preferredHeight + 20);
        }
        public static void prepareExecuteTask(playTask pt)
        {
            TaskInstances.Add(pt);
            print("EXCU " + TaskInstances.Count + " " + pt.hotKey + pt.audio);
        }
    }
}