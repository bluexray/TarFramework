using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarFramework.Core.DataStruct
{
    public enum ProtocolType : byte
    {
        HOPOPTS = (byte)0,
        IP = (byte)0,
        ICMP = (byte)1,
        IGMP = (byte)2,
        IPIP = (byte)4,
        TCP = (byte)6,
        EGP = (byte)8,
        PUP = (byte)12,
        UDP = (byte)17,
        IDP = (byte)22,
        TP = (byte)29,
        IPV6 = (byte)41,
        ROUTING = (byte)43,
        FRAGMENT = (byte)44,
        RSVP = (byte)46,
        GRE = (byte)47,
        ESP = (byte)50,
        AH = (byte)51,
        ICMPV6 = (byte)58,
        NONE = (byte)59,
        DSTOPTS = (byte)60,
        MTP = (byte)92,
        ENCAP = (byte)98,
        PIM = (byte)103,
        COMP = (byte)108,
        MASK = (byte)255,
        RAW = (byte)255,
    }
}
