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
    public int hotKey;
    public string taskType;
    public string[] taskParameters;
}
namespace VTS
{
    public class Tasks : VTSPlugin
    {

        public static List<playTask> tasklists = new List<playTask>();

        public static bool hasNextTask()
        {
            if (tasklists.Count > 0)
                return true;
            else
                return false;
        }
        public static playTask getNextTask()
        {
            var ta = tasklists[0];
            tasklists.RemoveAt(0);
            return ta;
        }
        public static int aaa = 0;
        public static void pushTaskIntoList(int hotkey, string audio, GameObject TextPrefab)
        {
            playTask pt = new playTask();
            pt.audio = audio;
            pt.hotKey = hotkey;
            tasklists.Add(pt);
            var gob = GameObject.Find("TaskListContent");
            var taskObj = Instantiate(TextPrefab, Vector3.zero, Quaternion.identity);
            taskObj.transform.parent = gob.transform;
            // taskObj.transform.SetPositionAndRotation(gob.transform.position, Quaternion.identity);
            var taskText = taskObj.transform.GetChild(0).GetComponent<Text>();
            var taskExtBut = taskObj.transform.GetChild(1).GetComponent<Button>();
            // taskText.text = "1233222222" + aaa.ToString();
            // aaa++;
            taskExtBut.onClick.AddListener(() =>
            {
                tasklists.Remove(pt);
                Destroy(taskObj);
            });

            // var r = taskObj.transform.GetChild(0).name;

            // tex.transform.SetParent(gob.transform);
        }

        
        public static void removeTask()
        {
            print("remove task");
        }
    }
}