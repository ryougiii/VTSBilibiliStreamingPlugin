using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq; 
using System.Net.WebSockets; 
using System.Timers;
using Extension; 
using Newtonsoft.Json.Linq; 
using UnityEngine;
using UnityEngine.Networking;  

public delegate void BliveCallback<TData>(TData data);

interface IBliveDispatch {
    void Dispatch();
}

public class BliveEvent<T> : IBliveDispatch {
    private event BliveCallback<T> _syncEvent;
    private readonly ConcurrentQueue<T> _queue = new();

    public void Dispatch() {
        while (_queue.TryDequeue(out var t)) {
            _syncEvent?.Invoke(t);
        }
    }

    public int Count => _queue.Count;

    public void Enqueue(T obj) {
        _queue.Enqueue(obj);
    }

    public bool TryDequeue(out T obj) {
        return _queue.TryDequeue(out obj);
    }

    public void Invoke(T obj) {
        _syncEvent?.Invoke(obj);
    }

    public static BliveEvent<T> operator +(BliveEvent<T> @event, BliveCallback<T> callback) {
        @event._syncEvent += callback;
        return @event;
    }

    public static BliveEvent<T> operator -(BliveEvent<T> @event, BliveCallback<T> callback) {
        @event._syncEvent -= callback;
        return @event;
    }
}

public class BliveDanmuManager : MonoBehaviour {
    public static BliveDanmuManager Instance { get; private set; }

    public BliveDanmuManager() {
        Instance = this;
    }

    public BliveEvent<Danmu> DanmuEvent = new();
    public BliveEvent<Superchat> SuperchatEvent = new();
    public BliveEvent<Gift> SendGiftEvent = new();
    public BliveEvent<Gift> ComboSendEvent = new();
    public BliveEvent<SuperchatDelete> SuperchatDeleteEvent = new();
    public BliveEvent<InteractWord> InteractWordEvent = new();
    public BliveEvent<GuardBuy> GuardBuyEvent = new();
    public BliveEvent<GainMedal> GainMedalEvent = new();
    
    public BliveEvent<long> HeatEvent = new();
    public BliveEvent<long> WatchedChangeEvent = new();

    private List<IBliveDispatch> _events = new();
    private ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

    private void Awake() {
        _events.AddRange(new IBliveDispatch[] {
            DanmuEvent,
            SuperchatEvent,
            SendGiftEvent,
            ComboSendEvent,
            SuperchatDeleteEvent,
            InteractWordEvent,
            GuardBuyEvent,
            GainMedalEvent,
            HeatEvent,
            WatchedChangeEvent
        });
    }

    private void Start() {
        _heartbeat.Elapsed += (sender, e) => HeartbeatHandler();
        _heartbeat.AutoReset = true;
        _heartbeat.Start();
    }

    public void RunInMainThread(Action action) {
        _actions.Enqueue(action);
    }

    private void Update() {
        foreach (var e in _events) {
            e.Dispatch();
        }

        while (_actions.TryDequeue(out Action action)) {
            action();
        }
    }

    private WebSocketClient _ws;
    private readonly Timer _heartbeat = new(30000);
    private int _room = 510;
    private int _uid;

    private async void HeartbeatHandler() {
        if (_ws is {IsAlive: true}) {
            await _ws.SendAsync(BliveUtility.EncodeHeartbeat());
            RunInMainThread(() => { StartCoroutine(QueryFollowerCoroutine()); });
        }
    }

    private IEnumerator QueryFollowerCoroutine() {
        Debug.Log($"查关注: {_uid}");
        if (_uid == 0)
            yield break;
        using var web = UnityWebRequest.Get($"https://api.bilibili.com/x/relation/stat?vmid={_uid}");
        yield return web.SendWebRequest();
        if (web.result != UnityWebRequest.Result.Success)
            yield break;

        var j = JObject.Parse(web.downloadHandler.text);
        var follower = j["data"]["follower"].Value<int>().ToString();
    }

    private async void WsResponseHandler(WebSocketMessageType _, byte[] msg) {
        try {
            foreach ((string str, BliveOp op) in BliveUtility.Decode(msg)) {
                try { 
                    Debug.Log(op);
                    Debug.Log(str);
                    switch (op) {
                        case BliveOp.ConnectSucceed:
                            Debug.Log("连接成功");
                            await _ws.SendAsync(BliveUtility.EncodeHeartbeat());
                            break;
                        case BliveOp.HeartbeatReply:
                            Debug.Log($"Heat: {str}");
                            if (long.TryParse(str, out var v)) {
                                HeatEvent.Enqueue(v);
                            }
                            break;
                        case BliveOp.Message: {
                            var m = JObject.Parse(str);
                            var cmd = m["cmd"].Value<string>();
                            try {
                                switch (cmd) {
                                    case "DANMU_MSG": {
                                        var info = m["info"];
                                        // Extract from app.js
                                        var danmu = new Danmu {
                                            Time = DateTime.Now,
                                            MedalName = !info[3].Any() ? null : info[3][1].Value<string>(),
                                            MedalLevel = !info[3].Any() ? 0 : info[3][0].Value<int>(),
                                            UserId = info[2][0].Value<int>(),
                                            Username = info[2][1].Value<string>(),
                                            Content = info[1].Value<string>(),
                                            GuardLevel = info[7].Value<int>()
                                        };
                                        DanmuEvent.Enqueue(danmu);
                                        break;
                                    }
                                    //case "SUPER_CHAT_MESSAGE_JPN": 
                                    case "SUPER_CHAT_MESSAGE": {
                                        var data = m["data"];

                                        var sc = new Superchat {
                                            Time = DateTime.Now,
                                            SuperchatId = data["id"].Value<int>(),
                                            BackgroundColor = data["background_bottom_color"].Value<string>(),
                                            HeaderColor = data["background_color"].Value<string>(),
                                            Content = data["message"].Value<string>(),
                                            Price = data["price"].Value<int>(),
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["user_info"]["uname"].Value<string>(),
                                            MedalName = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_name"].Value<string>()
                                                : null,
                                            MedalLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_level"].Value<int>()
                                                : 0,
                                            GuardLevel = data["user_info"].HasValues
                                                ? data["user_info"]["guard_level"].Value<int>()
                                                : 0,
                                            Face = data["user_info"]["face"].Value<string>(),
                                            FaceFrame = data["user_info"]["face_frame"].Value<string>(),
                                            Thanked = false
                                        };
                                        SuperchatEvent.Enqueue(sc);
                                        break;
                                    }
                                    case "SEND_GIFT": {
                                        var data = m["data"];

                                        var combo = data["num"].Value<int>(); 

                                        var gift = new Gift {
                                            Time = DateTime.Now,
                                            Action = data["action"].Value<string>(),
                                            Name = data["giftName"].Value<string>(),
                                            DiscountPrice = data["discount_price"].Value<float>(),
                                            SinglePrice = data["price"].Value<float>(),
                                            TotalCoin = data["total_coin"].Value<float>(),
                                            Unit = data["coin_type"].Value<string>(),
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["uname"].Value<string>(),
                                            MedalName = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_name"].Value<string>()
                                                : null,
                                            MedalLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_level"].Value<int>()
                                                : 0,
                                            GuardLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["guard_level"].Value<int>()
                                                : 0,
                                            ComboId = data["batch_combo_id"].Value<string>(),
                                            Combo = data["super_gift_num"].Value<int>(),
                                            Count = data["num"].Value<int>(),
                                        };
                                        SendGiftEvent.Enqueue(gift);
                                        break;
                                    }
                                    case "COMBO_SEND": {
                                        var data = m["data"] as JObject;

                                        var gift = new Gift {
                                            IsComboSend = true,
                                            Time = DateTime.Now,
                                            Action = data["action"].Value<string>(),
                                            Name = data["gift_name"].Value<string>(),
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["uname"].Value<string>(),
                                            MedalName = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_name"].Value<string>()
                                                : null,
                                            MedalLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_level"].Value<int>()
                                                : 0,
                                            GuardLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["guard_level"].Value<int>()
                                                : 0,
                                            ComboId = data["batch_combo_id"].Value<string>(),
                                            Combo = data["batch_combo_num"].Value<int>(),
                                            IsFirst = data.ContainsKey("is_first") && data["is_first"].Value<bool>()
                                        };
                                        ComboSendEvent.Enqueue(gift);
                                        break;
                                    }
                                    case "USER_TOAST_MSG": {
                                        var data = m["data"];
                                        var price = data["price"].Value<float>();
                                        var buy = new GuardBuy {
                                            Time = DateTime.Now,
                                            Name = data["role_name"].Value<string>(),
                                            Currency = price,
                                            Unit = $"gold",
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["username"].Value<string>(),
                                            MedalName = null,
                                            GuardLevel = data["guard_level"].Value<int>(),
                                        };
                                        GuardBuyEvent.Enqueue(buy);
                                        break;
                                    }
                                    case "INTERACT_WORD": {
                                        var data = m["data"];
                                        var join = new InteractWord() {
                                            Time = DateTime.Now,
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["uname"].Value<string>(),
                                            MedalName = data["fans_medal"].HasValues
                                                ? data["fans_medal"]["medal_name"].Value<string>()
                                                : null,
                                            MedalLevel = data["fans_medal"].HasValues
                                                ? data["fans_medal"]["medal_level"].Value<int>()
                                                : 0,
                                            GuardLevel = data["fans_medal"].HasValues
                                                ? data["fans_medal"]["guard_level"].Value<int>()
                                                : 0,
                                            Type = (InteractWordType) data["msg_type"].Value<int>()
                                        };
                                        InteractWordEvent.Enqueue(join);
                                        break;
                                    }
                                    case "SUPER_CHAT_MESSAGE_DELETE": {
                                        var data = m["data"];
                                        var delete = data.ToObject<SuperchatDelete>();
                                        SuperchatDeleteEvent.Enqueue(delete);
                                        break;
                                    }
                                    case "WATCHED_CHANGE": {
                                        var data = m["data"];
                                        WatchedChangeEvent.Enqueue(data["num"].Value<long>());
                                        break;
                                    }
                                    case "ROOM_REAL_TIME_MESSAGE_UPDATE": {
                                        var data = m["data"];
                                        var fans = data["fans"].Value<int>();
                                        var fansClub = data["fans_club"].Value<int>();
                                        break;
                                    }
                                    case "MESSAGEBOX_USER_GAIN_MEDAL": {
                                        var data = m["data"];
                                        var gain = data.ToObject<GainMedal>();
                                        GainMedalEvent.Enqueue(gain);
                                        break;
                                    }
                                }
                            } catch (Exception ex) {
                                Debug.LogWarning($"解析事件出错：{cmd}");
                                Debug.LogException(ex);
                                Debug.LogError(m);
                            }
                            break;
                        }
                    }
                } catch (Exception ex) {
                    Debug.LogException(ex);
                    Debug.Log(op);
                    Debug.Log(str);
                }
            }
        } catch (Exception ex) {
            Debug.LogWarning(ex);
        }
    }

    private void WsErrorHandler(Exception ex) {
        Debug.LogError("与弹幕服务器连接出错");
        Debug.LogException(ex);
    }

    private void WsCloseHandler(WebSocketCloseStatus? status, string reason) {
        Debug.LogError($"与弹幕服务器连接关闭");
        Debug.LogWarning($"{status} {reason}");
    }

    private bool _reconnecting;

    private void WsReconnect() {
        Debug.LogWarning($"弹幕服务器重连");
        RunInMainThread(() => {
            if (_reconnecting)
                return;
            _reconnecting = true;
            StartCoroutine(WsReconnectCoroutine());
        });
    }

    private IEnumerator WsReconnectCoroutine() {
        yield return new WaitForSeconds(2);
        Connect(_room);
        _reconnecting = false;
    }

    private bool _connecting = false;

    public void Connect(int roomId) {
        if (_connecting)
            return;
        Debug.Log($"连接弹幕服务器 {roomId}");
        _room = roomId;
        _connecting = true;
        Disconnect();
        StartCoroutine(ConnectAsync(roomId));
    }

    private IEnumerator ConnectAsync(int roomId) {
        var req =
            UnityWebRequest.Get($"https://api.live.bilibili.com/room/v1/Room/get_info_by_id?ids[]={roomId}");
        req.SetRequestHeader("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36");
        req.SetRequestHeader("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
        req.timeout = 3;
        yield return req.SendWebRequest();

        if (req.responseCode != 200) {
            Debug.LogWarning($"获取房间信息错误 ({req.responseCode})");
            WsReconnect();
            req.Dispose();
            _connecting = false;
            yield break;
        }

        var roomInfoStr = req.downloadHandler.text;
        var realRoomId = 0;
        try {
            var roomInfo = JObject.Parse(roomInfoStr);
            var first = ((JObject) roomInfo["data"]).Properties().First();
            _uid = int.Parse(first.Value["uid"].Value<string>());
            StartCoroutine(QueryFollowerCoroutine());
            realRoomId = int.Parse(first.Name);
            Debug.Log($"房间号：{realRoomId}");
        } catch (Exception ex) {
            Debug.LogException(ex);
            Debug.LogWarning($"解析房间信息异常：{ex}");
            WsReconnect();
            _connecting = false;
            yield break;
        } finally {
            req.Dispose();
        }

        req = UnityWebRequest.Get(
            $"https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id={realRoomId}");
        req.SetRequestHeader("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36");
        req.SetRequestHeader("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
        req.timeout = 3;
        yield return req.SendWebRequest();

        if (req.responseCode != 200) {
            Debug.LogWarning($"获取弹幕信息错误 ({req.responseCode})");
            req.Dispose();
            WsReconnect();
            _connecting = false;
            yield break;
        }

        Debug.Log(req.downloadHandler.text);

        string wssUrl;
        string token;
        try {
            var danmuInfo = JObject.Parse(req.downloadHandler.text);
            token = danmuInfo["data"]["token"].Value<string>();
            var danmuServerList = danmuInfo["data"]["host_list"].ToList();
            var danmuServer = danmuServerList[new System.Random().Next(0, danmuServerList.Count)];
            wssUrl = $"wss://{danmuServer["host"].Value<string>()}:{danmuServer["wss_port"].Value<int>()}/sub";
            Debug.Log($"地址：{wssUrl}");
        } catch (Exception ex) {
            Debug.LogException(ex);
            Debug.LogWarning($"解析弹幕信息异常：{ex}");
            WsReconnect();
            _connecting = false;
            yield break;
        } finally {
            req.Dispose();
        }

        _ws = new WebSocketClient();
        _ws.OnReceive += WsResponseHandler;
        _ws.OnException += WsErrorHandler;
        _ws.OnClose += WsCloseHandler;
        _ws.OnConnectionLost += WsReconnect;
        Debug.Log($"连接弹幕服务器中");
        var task = _ws.ConnectAsync(wssUrl).AsCoroutine();
        yield return task;
        if (task.Error) {
            Debug.LogWarning($"连接WS异常：{task.Task.Exception}");
            WsReconnect();
            _connecting = false;
            yield break;
        }
        task = _ws.SendAsync(BliveUtility.EncodeUserAuthentication(realRoomId, token, _uid)).AsCoroutine();
        yield return task;
        if (task.Error) {
            Debug.LogWarning($"连接WS异常：{task.Task.Exception}");
            WsReconnect();
            _connecting = false;
            yield break;
        }
        _connecting = false;
    }

    public void Disconnect() {
        if (_ws != null) {
            try {
                _ws.CloseAsync().AsTask().Wait(TimeSpan.FromSeconds(1));
            } catch (Exception ex) {
                Debug.LogException(ex);
            }
            _ws = null;
        }
    }

    private void OnApplicationQuit() {
        Disconnect();
        _heartbeat.Stop();
        _heartbeat.Close();
        _heartbeat.Dispose();
    }
}