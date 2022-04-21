
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