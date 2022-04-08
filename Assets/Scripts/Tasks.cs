using System.Collections.Generic;

using VTS.Networking.Impl;
using VTS.Models.Impl;
using VTS.Models;

using UnityEngine;
using UnityEngine.UI;

using UnityEditor;
public struct playTask
{
    public string audio;
    public string hotKey;
    public string taskType;
    public string[] taskParameters;
}
namespace VTS
{
    public class Tasks : VTSPlugin
    {

        public static List<playTask> LogicRegTasklists = new List<playTask>();

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

            var showpn = Instantiate((GameObject)Resources.Load("Prefabs/taskShowPn"),Vector3.zero,Quaternion.identity);
            showpn.transform.localPosition = Vector3.zero;
            showpn.transform.GetChild(1).GetComponent<Text>().text = expressTaskInfo;
            showpn.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
            {
                // 取消Regtask
                Destroy(showpn);
            });
            var showPnRect = showpn.transform.GetComponent<RectTransform>();
            showPnRect.sizeDelta = new Vector2(showPnRect.rect.width, showpn.transform.GetChild(1).GetComponent<Text>().preferredHeight + 20);
            print("WH " + showPnRect.sizeDelta.x + showPnRect.sizeDelta.y);
            LogicRegTasklists.Add(halfTaskGo);
            showpn.transform.SetParent(GameObject.Find("TaskRegListContent").transform);

        }


        public static void removeTask()
        {
            print("remove task");
        }
    }
}