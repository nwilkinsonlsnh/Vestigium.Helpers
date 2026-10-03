namespace Vestigium.Helpers.Network;

public static class NetworkPorts
{
    private static readonly NetworkPortGuess[] Catalog =
    [
        Row(1, "tcpmux", "Tcp", "TCP port service multiplexer"),
        Row(7, "echo", "Both", "Echo"),
        Row(9, "discard", "Both", "Discard"),
        Row(11, "systat", "Tcp", "Active users"),
        Row(13, "daytime", "Both", "Daytime"),
        Row(17, "qotd", "Both", "Quote of the day"),
        Row(19, "chargen", "Both", "Character generator"),
        Row(20, "ftp-data", "Tcp", "FTP data"),
        Row(21, "ftp", "Tcp", "File Transfer Protocol"),
        Row(22, "ssh", "Tcp", "Secure Shell"),
        Row(23, "telnet", "Tcp", "Telnet"),
        Row(25, "smtp", "Tcp", "Simple Mail Transfer"),
        Row(37, "time", "Both", "Time"),
        Row(42, "nameserver", "Tcp", "Host name server"),
        Row(43, "whois", "Tcp", "Whois"),
        Row(49, "tacacs", "Tcp", "TACACS"),
        Row(53, "dns", "Both", "Domain Name System"),
        Row(67, "dhcp", "Udp", "DHCP server"),
        Row(68, "dhcp-client", "Udp", "DHCP client"),
        Row(69, "tftp", "Udp", "Trivial File Transfer"),
        Row(70, "gopher", "Tcp", "Gopher"),
        Row(79, "finger", "Tcp", "Finger"),
        Row(80, "http", "Tcp", "Hypertext Transfer"),
        Row(88, "kerberos", "Both", "Kerberos"),
        Row(102, "iso-tsap", "Tcp", "ISO TSAP"),
        Row(110, "pop3", "Tcp", "Post Office Protocol 3"),
        Row(111, "rpcbind", "Both", "RPC portmapper"),
        Row(113, "ident", "Tcp", "Ident"),
        Row(119, "nntp", "Tcp", "Network News"),
        Row(123, "ntp", "Udp", "Network Time"),
        Row(135, "rpc", "Tcp", "Microsoft RPC endpoint mapper"),
        Row(137, "netbios-ns", "Udp", "NetBIOS name service"),
        Row(138, "netbios-dgm", "Udp", "NetBIOS datagram"),
        Row(139, "netbios-ssn", "Tcp", "NetBIOS session"),
        Row(143, "imap", "Tcp", "Internet Message Access"),
        Row(161, "snmp", "Udp", "Simple Network Management"),
        Row(162, "snmp-trap", "Udp", "SNMP trap"),
        Row(179, "bgp", "Tcp", "Border Gateway Protocol"),
        Row(194, "irc", "Tcp", "Internet Relay Chat"),
        Row(201, "at-rtmp", "Both", "AppleTalk routing"),
        Row(389, "ldap", "Both", "Lightweight Directory Access"),
        Row(427, "slp", "Both", "Service Location Protocol"),
        Row(443, "https", "Tcp", "HTTP over TLS"),
        Row(445, "smb", "Tcp", "SMB over TCP"),
        Row(464, "kpasswd", "Both", "Kerberos password change"),
        Row(465, "smtps", "Tcp", "SMTP over TLS"),
        Row(500, "isakmp", "Udp", "IKE / ISAKMP"),
        Row(514, "syslog", "Udp", "Syslog"),
        Row(515, "lpd", "Tcp", "Line printer"),
        Row(520, "rip", "Udp", "Routing Information Protocol"),
        Row(540, "uucp", "Tcp", "Unix-to-Unix copy"),
        Row(546, "dhcpv6-client", "Udp", "DHCPv6 client"),
        Row(547, "dhcpv6", "Udp", "DHCPv6 server"),
        Row(554, "rtsp", "Tcp", "Real Time Streaming"),
        Row(563, "nntps", "Tcp", "NNTP over TLS"),
        Row(587, "submission", "Tcp", "SMTP submission"),
        Row(593, "rpc-http", "Tcp", "RPC over HTTP"),
        Row(631, "ipp", "Tcp", "Internet Printing Protocol"),
        Row(636, "ldaps", "Tcp", "LDAP over TLS"),
        Row(646, "ldp", "Both", "Label Distribution Protocol"),
        Row(647, "dhcp-failover", "Tcp", "DHCP failover"),
        Row(853, "dot", "Tcp", "DNS over TLS"),
        Row(873, "rsync", "Tcp", "Rsync"),
        Row(989, "ftps-data", "Tcp", "FTPS data"),
        Row(990, "ftps", "Tcp", "FTPS control"),
        Row(993, "imaps", "Tcp", "IMAP over TLS"),
        Row(995, "pop3s", "Tcp", "POP3 over TLS"),
        Row(1025, "blackjack", "Tcp", "Microsoft often uses this dynamically"),
        Row(1080, "socks", "Tcp", "SOCKS proxy"),
        Row(1099, "rmiregistry", "Tcp", "Java RMI registry"),
        Row(1194, "openvpn", "Both", "OpenVPN"),
        Row(1352, "lotusnotes", "Tcp", "Lotus Notes"),
        Row(1433, "mssql", "Tcp", "Microsoft SQL Server"),
        Row(1434, "mssql-browser", "Udp", "SQL Server browser"),
        Row(1521, "oracle", "Tcp", "Oracle listener"),
        Row(1723, "pptp", "Tcp", "Point-to-Point Tunneling"),
        Row(1812, "radius", "Udp", "RADIUS authentication"),
        Row(1813, "radius-acct", "Udp", "RADIUS accounting"),
        Row(1883, "mqtt", "Tcp", "MQTT"),
        Row(1900, "ssdp", "Udp", "Simple Service Discovery"),
        Row(2049, "nfs", "Both", "Network File System"),
        Row(2082, "cpanel", "Tcp", "cPanel"),
        Row(2083, "cpanel-ssl", "Tcp", "cPanel over TLS"),
        Row(2181, "zookeeper", "Tcp", "ZooKeeper"),
        Row(2375, "docker", "Tcp", "Docker API"),
        Row(2376, "docker-tls", "Tcp", "Docker API over TLS"),
        Row(2379, "etcd", "Tcp", "etcd client"),
        Row(2380, "etcd-peer", "Tcp", "etcd peer"),
        Row(2483, "oracle-db", "Tcp", "Oracle database"),
        Row(2484, "oracle-db-tls", "Tcp", "Oracle database over TLS"),
        Row(3260, "iscsi", "Tcp", "iSCSI"),
        Row(3268, "gc", "Tcp", "Active Directory global catalog"),
        Row(3269, "gc-ssl", "Tcp", "Global catalog over TLS"),
        Row(3306, "mysql", "Tcp", "MySQL"),
        Row(3389, "rdp", "Both", "Remote Desktop"),
        Row(3478, "stun", "Both", "STUN"),
        Row(3689, "daap", "Tcp", "iTunes music sharing"),
        Row(3690, "svn", "Tcp", "Subversion"),
        Row(4369, "epmd", "Tcp", "Erlang port mapper"),
        Row(4444, "metasploit", "Tcp", "Common reverse-shell listener"),
        Row(4500, "ipsec-nat", "Udp", "IPsec NAT traversal"),
        Row(5000, "upnp", "Tcp", "UPnP"),
        Row(5060, "sip", "Both", "Session Initiation Protocol"),
        Row(5061, "sips", "Tcp", "SIP over TLS"),
        Row(5222, "xmpp", "Tcp", "XMPP client"),
        Row(5269, "xmpp-server", "Tcp", "XMPP server"),
        Row(5353, "mdns", "Udp", "Multicast DNS"),
        Row(5355, "llmnr", "Udp", "Link-Local Multicast Name Resolution"),
        Row(5357, "wsd", "Tcp", "Web Services on Devices"),
        Row(5432, "postgres", "Tcp", "PostgreSQL"),
        Row(5555, "adb", "Tcp", "Android Debug Bridge"),
        Row(5601, "kibana", "Tcp", "Kibana"),
        Row(5671, "amqps", "Tcp", "AMQP over TLS"),
        Row(5672, "amqp", "Tcp", "AMQP"),
        Row(5900, "vnc", "Tcp", "Virtual Network Computing"),
        Row(5985, "winrm", "Tcp", "Windows Remote Management"),
        Row(5986, "winrm-ssl", "Tcp", "WinRM over TLS"),
        Row(6379, "redis", "Tcp", "Redis"),
        Row(6443, "kubernetes", "Tcp", "Kubernetes API"),
        Row(6514, "syslog-tls", "Tcp", "Syslog over TLS"),
        Row(6667, "ircd", "Tcp", "IRC server"),
        Row(7001, "weblogic", "Tcp", "WebLogic"),
        Row(7070, "realserver", "Tcp", "RealServer"),
        Row(8000, "http-alt", "Tcp", "Alternate HTTP"),
        Row(8008, "http-alt", "Tcp", "Alternate HTTP"),
        Row(8080, "http-proxy", "Tcp", "HTTP proxy or alternate HTTP"),
        Row(8081, "http-alt", "Tcp", "Alternate HTTP"),
        Row(8443, "https-alt", "Tcp", "Alternate HTTPS"),
        Row(8530, "wsus", "Tcp", "Windows Server Update Services"),
        Row(8531, "wsus-ssl", "Tcp", "WSUS over TLS"),
        Row(8888, "http-alt", "Tcp", "Alternate HTTP"),
        Row(9000, "sonarqube", "Tcp", "SonarQube or other management"),
        Row(9042, "cassandra", "Tcp", "Cassandra"),
        Row(9090, "prometheus", "Tcp", "Prometheus"),
        Row(9092, "kafka", "Tcp", "Kafka"),
        Row(9100, "jetdirect", "Tcp", "Printer raw"),
        Row(9200, "elasticsearch", "Tcp", "Elasticsearch"),
        Row(9300, "elasticsearch-node", "Tcp", "Elasticsearch node"),
        Row(9418, "git", "Tcp", "Git"),
        Row(9443, "https-alt", "Tcp", "Alternate HTTPS"),
        Row(10000, "webmin", "Tcp", "Webmin"),
        Row(11211, "memcached", "Both", "Memcached"),
        Row(15672, "rabbitmq", "Tcp", "RabbitMQ management"),
        Row(25565, "minecraft", "Tcp", "Minecraft"),
        Row(27017, "mongodb", "Tcp", "MongoDB"),
        Row(27018, "mongodb", "Tcp", "MongoDB shard"),
        Row(50000, "sap", "Tcp", "SAP"),
        Row(50070, "hadoop", "Tcp", "Hadoop NameNode")
    ];

    private static readonly Dictionary<int, NetworkPortGuess> ByPort = Catalog.ToDictionary(row => row.Port);
    private static readonly Dictionary<string, NetworkPortGuess[]> ByService = BuildNames();

    public static IReadOnlyList<NetworkPortGuess> All => Catalog;

    public static bool TryByPort(int port, out NetworkPortGuess guess)
        => ByPort.TryGetValue(port, out guess!);

    public static IReadOnlyList<NetworkPortGuess> ByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return [];
        return ByService.TryGetValue(name.Trim(), out var rows) ? rows : [];
    }

    private static Dictionary<string, NetworkPortGuess[]> BuildNames()
    {
        var map = new Dictionary<string, List<NetworkPortGuess>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Catalog)
            Add(map, row.Name, row);
        Add(map, "domain", Catalog.First(row => row.Port == 53));
        Add(map, "bootps", Catalog.First(row => row.Port == 67));
        Add(map, "bootpc", Catalog.First(row => row.Port == 68));
        Add(map, "www", Catalog.First(row => row.Port == 80));
        Add(map, "microsoft-ds", Catalog.First(row => row.Port == 445));
        Add(map, "ms-sql", Catalog.First(row => row.Port == 1433));
        Add(map, "sql", Catalog.First(row => row.Port == 1433));
        Add(map, "remote-desktop", Catalog.First(row => row.Port == 3389));
        Add(map, "ms-wbt-server", Catalog.First(row => row.Port == 3389));
        Add(map, "postgresql", Catalog.First(row => row.Port == 5432));
        return map.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static void Add(Dictionary<string, List<NetworkPortGuess>> map, string name, NetworkPortGuess row)
    {
        if (!map.TryGetValue(name, out var list))
        {
            list = [];
            map[name] = list;
        }

        if (!list.Contains(row))
            list.Add(row);
    }

    private static NetworkPortGuess Row(int port, string name, string transport, string description)
        => new(port, name, transport, description);
}
