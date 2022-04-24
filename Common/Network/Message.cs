using MessagePack;
using System.Collections.Generic;

namespace Common.Network
{
    public enum MsgType
    {
        // Common
        HELLO = 0x00,  // certificate
        KEY = 0x01,    // session key exchange message
        NICK = 0x02,   // nickname acquire / result
        MSG = 0x03,    // chat message
        // Server only
        SYS = 0xA0, // human-read server msg
    }
    [MessagePackObject]
    public class Message
    {
        [Key(0)]
        public MsgType Type;
        [Key(1)]
        public object[] Params;

        public Message(MsgType type, params object[] param)
        {
            Type = type;
            Params = param;
        }

        public T GetParam<T>(int index)
        {
            return (T)Params[index];
        }

        //tostring method for debugging
        public override string ToString()
        {
            return $"{Type} {Params}";
        }
    }
}
