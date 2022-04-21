using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Danmu {
    public DateTime Time { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Content { get; set; }
    public string MedalName { get; set; }
    public int MedalLevel { get; set; }
    public int GuardLevel { get; set; }
}

public class Superchat {
    public int SuperchatId { get; set; }
    public DateTime Time { get; set; }
    public string BackgroundColor { get; set; }
    public string HeaderColor { get; set; }
    public string Content { get; set; }
    public string ContentJpn { get; set; }
    public int Price { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string MedalName { get; set; }
    public int MedalLevel { get; set; }
    public int GuardLevel { get; set; }
    public string Face { get; set; }
    public string FaceFrame { get; set; }
    public bool Thanked { get; set; } = false;
}

public class InteractWord {
    public DateTime Time { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string MedalName { get; set; }
    public int MedalLevel { get; set; }
    public int GuardLevel { get; set; }
    public InteractWordType Type { get; set; }
}

public class GainMedal {
    [JsonProperty("uid")]
    public int UserId { get; set; }
    [JsonProperty("guard_level")]
    public int GuardLevel { get; set; }
    [JsonProperty("fan_name")]
    public string FanName { get; set; }
    [JsonProperty("medal_name")]
    public string MedalName { get; set; }
}

public class GuardBuy { 
    public DateTime Time { get; set; }
    public string Name { get; set; }
    public float Currency { get; set; }
    public string Unit { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string MedalName { get; set; }
    public int MedalLevel { get; set; }
    public int GuardLevel { get; set; }
    public bool Thanked { get; set; } = false;

    public string ComboId { get; set; }
    public int Combo { get; set; } = 1; 
    public bool IsComboSend { get; set; }
    public float Price {
        get {
            if (Unit == "gold")
                return Currency / 1000 * Combo;
            return 0;
        }
    }
}

public class Gift {
    public string Action { get; set; }
    public DateTime Time { get; set; }
    public string Name { get; set; }
    public float Currency { get; set; }
    public string Unit { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string MedalName { get; set; }
    public int MedalLevel { get; set; }
    public int GuardLevel { get; set; } 
    public bool IsFirst { get; set; }

    public string ComboId { get; set; }
    public int Combo { get; set; } = 1; 
    public bool IsComboSend { get; set; }
    public float Price {
        get {
            if (Unit == "gold")
                return Currency / 1000 * Combo;
            return 0;
        }
    }
}

public class SuperchatDelete {
    [JsonProperty("ids")] public List<long> SuperchatIds { get; set; }
}