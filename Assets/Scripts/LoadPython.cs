
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Diagnostics;  //需要添加这个名词空间，调用DataReceivedEventArg 
using System.Text;
using UnityEngine.UI;

using VTS.Examples;
using VTS;
public class LoadPython : MonoBehaviour
{

    private Text info = null;

    public static int room_id = 0;

    private string sArguments;
    private string AppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private string AssetsPath = "";
    public static string temp;

    public int processid = 0;
    Process p = null;

    // public static Thread pythonProcess;

    public ExamplePlugin mainScript;

    // Use this for initialization
    void Start()
    {
        temp = "";
        AssetsPath = Application.streamingAssetsPath;
        info = GameObject.Find("RoomStates").GetComponent<Text>();
        Tasks.loadRoomData();
        StartPy();
        // mainScript = GameObject.Find("ExamplePlugin");
    }

    ThreadStart childRef;
    Thread childThread;

    public void StartPy()
    {
        if (childThread != null && childThread.IsAlive)
        {
            childThread.Abort();
        }
        if (room_id == 0)
        {
            return;
        }
        // info.text = $"连接直播间{room_id}";
        childRef = new ThreadStart(ThreadTest1);
        childThread = new Thread(childRef);
        childThread.Start();
        GameObject.Find("RoomStates").GetComponent<Text>().text = room_id.ToString();
        Tasks.saveRoomData();

    }

    public void EndPy()
    {
        if (p != null)
        {
            p.Close();
            p.Dispose();
            p = null;
        }
        info.text = $"停止连接房间{room_id}";
        GameObject.Find("RoomStates").GetComponent<Text>().text = "--";

    }

    // Update is called once per frame
    void Update()
    {
        // UnityEngine.Debug.Log(temp);
    }
    public void ThreadTest1()
    {
        // sArguments = @"/blivedm.py " + room_id.ToString();
        sArguments = room_id.ToString();
        print("ThreadTest1 is working");
        RunPythonScript(sArguments, "-u");
    }
    public void RunPythonScript(string sArgName, string args = "")
    {
        if (p != null)
        {
            p.Close();
            p.Dispose();
            p = null;
        }
        p = new Process();

        // p.StartInfo.FileName = AppData + @"\Programs\Python\Python39\python.exe";
        p.StartInfo.FileName = AssetsPath + @"\blivedm\blivedm.exe";
        // UnityEngine.Debug.Log(path);
        // UnityEngine.Debug.Log(p.StartInfo.FileName);
        p.StartInfo.UseShellExecute = false;
        // p.StartInfo.Arguments = sArguments;
        p.StartInfo.Arguments = sArgName;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        processid = p.Id;
        p.BeginOutputReadLine();
        p.OutputDataReceived += new DataReceivedEventHandler(Out_RecvData);
        Console.ReadLine();
        p.WaitForExit();


    }
    void Out_RecvData(object sender, DataReceivedEventArgs e)
    {
        if (temp != e.Data)
        {
            temp = e.Data;
            if (temp != "" && temp != null)
            {
                // UnityEngine.Debug.Log(temp);
                // print("DM: " + temp);
                mainScript.receiveDanmu(temp);
            }

        }
    }
    public void ChangeRoomID(string msg)
    {
        int.TryParse(msg, out room_id);
        info.text = $"当前房间号：{room_id}";
    }
    void OnApplicationQuit()
    {

        if (childThread.IsAlive)
        {
            childThread.Abort();
        }
        if(p!=null){
            p.Close();
            p.Dispose();
            p = null;
        }

    }

}