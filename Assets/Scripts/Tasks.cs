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

        // public static bool hasNextTask()
        // {
        //     if (tasklists.Count > 0)
        //         return true;
        //     else
        //         return false;
        // }
        // public static playTask getNextTask()
        // {
        //     var ta = tasklists[0];
        //     tasklists.RemoveAt(0);
        //     return ta;
        // }


        public static void pushRegTaskIntoList(playTask halfTaskGo)
        {
            string expressTaskInfo = "";
            switch (halfTaskGo.taskType)
            {
                case "danmu":
                    expressTaskInfo += "当弹幕中出现 " + halfTaskGo.taskParameters[0] + " 时，";
                    if (halfTaskGo.audio != "")
                    {
                        expressTaskInfo += "\n   ->播放 " + halfTaskGo.audio;
                    }
                    if (halfTaskGo.hotKey != "")
                    {
                        expressTaskInfo += "\n   ->触发 " + halfTaskGo.hotKey;
                        print("HHH " + halfTaskGo.hotKey.Length + halfTaskGo.hotKey);
                    }
                    break;

            }
            //在面板中显示添加的规则
            var showpn = Instantiate((GameObject)Resources.Load("Prefabs/taskShowPn"), Vector3.zero, Quaternion.identity);
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
            print("WH " + showPnRect.sizeDelta.x + showPnRect.sizeDelta.y);
            showpn.transform.SetParent(GameObject.Find("TaskRegListContent").transform);
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
                            prepareExecuteTask(ltl);
                            print("TRI DANMU " + meventParas[0]);
                        }
                        break;

                }
            }
        }
        public static void prepareExecuteTask(playTask pt)
        {
            TaskInstances.Add(pt);
            print("EXCU " + pt.hotKey + pt.audio);
        }
    }
}