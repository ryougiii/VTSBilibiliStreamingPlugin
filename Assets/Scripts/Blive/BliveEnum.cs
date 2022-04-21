public enum BliveOp {
    Invalid = 0,
    Heartbeat = 2,
    HeartbeatReply = 3,
    Message = 5,
    UserAuthentication = 7,
    ConnectSucceed = 8
}

public enum BliveBodyVersion {
    Normal = 0,
    ZLib = 1,
    Brotli = 3
}

public enum InteractWordType {
    Unknown = 0,
    Entry = 1, // 进入
    Attention = 2, // 关注
    Share = 3, // 分享
    SpecialAttention = 4, // 特别关注
    MutualAttention = 5 // 互粉
}