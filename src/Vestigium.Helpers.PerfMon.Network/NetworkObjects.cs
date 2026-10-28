namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>PDH objects this probe is allowed to name.</summary>
public static class NetworkObjects
{
    public const string PerProcessorNetworkActivityCycles = "Per Processor Network Activity Cycles";
    public const string PerProcessorNicActivity = "Per Processor Network Interface Card Activity";
    public const string TcpipPerformanceDiagnosticsPerCpu = "TCPIP Performance Diagnostics (Per-CPU)";
    public const string BluetoothDevice = "Bluetooth Device";
    public const string BluetoothRadio = "Bluetooth Radio";
    public const string Dns64Global = "DNS64 Global";
    public const string FirewallRulesByProfile = "Firewall Rules by Profile";
    public const string FirewallRulesByStore = "Firewall Rules by Store";
    public const string HttpService = "HTTP Service";
    public const string HttpServiceRequestQueues = "HTTP Service Request Queues";
    public const string HttpServiceUrlGroups = "HTTP Service Url Groups";
    public const string HyperVVmBusPipes = "Hyper-V Virtual Machine Bus Pipes";
    public const string Icmp = "ICMP";
    public const string IcmpV6 = "ICMPv6";
    public const string IpHttpsGlobal = "IPHTTPS Global";
    public const string IpHttpsSession = "IPHTTPS Session";
    public const string IpsecAuthIpV4 = "IPsec AuthIP IPv4";
    public const string IpsecAuthIpV6 = "IPsec AuthIP IPv6";
    public const string IpsecConnections = "IPsec Connections";
    public const string IpsecDriver = "IPsec Driver";
    public const string IpsecIkeV1V4 = "IPsec IKEv1 IPv4";
    public const string IpsecIkeV1V6 = "IPsec IKEv1 IPv6";
    public const string IpsecIkeV2V4 = "IPsec IKEv2 IPv4";
    public const string IpsecIkeV2V6 = "IPsec IKEv2 IPv6";
    public const string IpV4 = "IPv4";
    public const string IpV6 = "IPv6";
    public const string NetworkAdapter = "Network Adapter";
    public const string NetworkInterface = "Network Interface";
    public const string NetworkQosPolicy = "Network QoS Policy";
    public const string PacketDirectEcUtilization = "PacketDirect EC Utilization";
    public const string PacketDirectQueueDepth = "PacketDirect Queue Depth";
    public const string PacketDirectReceiveCounters = "PacketDirect Receive Counters";
    public const string PacketDirectReceiveFilters = "PacketDirect Receive Filters";
    public const string PacketDirectTransmitCounters = "PacketDirect Transmit Counters";
    public const string PhysicalNicActivity = "Physical Network Interface Card Activity";
    public const string RemoteFxNetwork = "RemoteFX Network";
    public const string SmbClientShares = "SMB Client Shares";
    public const string SmbDirect = "SMB Direct";
    public const string SmbServer = "SMB Server";
    public const string SmbServerSessions = "SMB Server Sessions";
    public const string SmbServerShares = "SMB Server Shares";
    public const string TcpipExtendedPerformanceDiagnostics = "TCPIP Extended Performance Diagnostics";
    public const string TcpipPerformanceDiagnostics = "TCPIP Performance Diagnostics";
    public const string TcpipTransportPacketDrops = "TCPIP Transport Layer Packet Drop Counters";
    public const string TcpV4 = "TCPv4";
    public const string TcpV6 = "TCPv6";
    public const string TeredoClient = "Teredo Client";
    public const string TeredoRelay = "Teredo Relay";
    public const string TeredoServer = "Teredo Server";
    public const string UdpV4 = "UDPv4";
    public const string UdpV6 = "UDPv6";
    public const string Wfp = "WFP";
    public const string WfpClassify = "WFP Classify";
    public const string WfpFilterCount = "WFP Filter Count";
    public const string WfpFilterSize = "WFP Filter Size";
    public const string WfpReauthorization = "WFP Reauthorization";
    public const string WfpV4 = "WFPv4";
    public const string WfpV6 = "WFPv6";
    public const string WinNat = "WinNAT";
    public const string WinNatIcmp = "WinNAT ICMP";
    public const string WinNatInstance = "WinNAT Instance";
    public const string WinNatTcp = "WinNAT TCP";
    public const string WinNatUdp = "WinNAT UDP";

    public static IReadOnlyList<string> All { get; } =
    [
        PerProcessorNetworkActivityCycles,
        PerProcessorNicActivity,
        TcpipPerformanceDiagnosticsPerCpu,
        BluetoothDevice,
        BluetoothRadio,
        Dns64Global,
        FirewallRulesByProfile,
        FirewallRulesByStore,
        HttpService,
        HttpServiceRequestQueues,
        HttpServiceUrlGroups,
        HyperVVmBusPipes,
        Icmp,
        IcmpV6,
        IpHttpsGlobal,
        IpHttpsSession,
        IpsecAuthIpV4,
        IpsecAuthIpV6,
        IpsecConnections,
        IpsecDriver,
        IpsecIkeV1V4,
        IpsecIkeV1V6,
        IpsecIkeV2V4,
        IpsecIkeV2V6,
        IpV4,
        IpV6,
        NetworkAdapter,
        NetworkInterface,
        NetworkQosPolicy,
        PacketDirectEcUtilization,
        PacketDirectQueueDepth,
        PacketDirectReceiveCounters,
        PacketDirectReceiveFilters,
        PacketDirectTransmitCounters,
        PhysicalNicActivity,
        RemoteFxNetwork,
        SmbClientShares,
        SmbDirect,
        SmbServer,
        SmbServerSessions,
        SmbServerShares,
        TcpipExtendedPerformanceDiagnostics,
        TcpipPerformanceDiagnostics,
        TcpipTransportPacketDrops,
        TcpV4,
        TcpV6,
        TeredoClient,
        TeredoRelay,
        TeredoServer,
        UdpV4,
        UdpV6,
        Wfp,
        WfpClassify,
        WfpFilterCount,
        WfpFilterSize,
        WfpReauthorization,
        WfpV4,
        WfpV6,
        WinNat,
        WinNatIcmp,
        WinNatInstance,
        WinNatTcp,
        WinNatUdp,
    ];
}
