using SharpPcap;
using PacketDotNet;
using Timer = System.Timers.Timer;

class Program
{

    // create a static counter for packets
    static int packetCount = 0;
    static int maxPacketCount = -1;
    static ICaptureDevice device;
    static void Main(string[] args)
    {
        // Retrieve the device list
        CaptureDeviceList devices = CaptureDeviceList.Instance;

        // If no devices were found, print an error
        if (devices.Count < 1)
        {
            Console.WriteLine("No devices were found on this machine");
            return;
        }

        // If -l option is provided, list the devices and return
        if (args.Length > 0 && args[0] == "-l")
        {
            Console.WriteLine("\nAvailable devices: ");
            for (int i = 0; i < devices.Count; ++i)
            {
                Console.WriteLine($"{i + 1}) {devices[i].Description}");
            }
            return;
        }

        // If not enough arguments were provided, error
        if (args.Length < 1)
        {
            Console.WriteLine("Please provide the device index as an argument");
            return;
        }

        // read the arguments
        int deviceIndex = int.Parse(args[0]) - 1;
        string filter = "";

        if (args.Length >= 3 && args[1] == "-t")
        {
            filter = args[2];
        }

        // Open the chosen device
        ICaptureDevice device = devices[deviceIndex];
        device.Open();
        device.Filter = filter;

        int packetCountIndex = Array.IndexOf(args, "-n");
        if (packetCountIndex == -1)
            packetCountIndex = Array.IndexOf(args, "--num-packets");
        if (packetCountIndex != -1 && args.Length > packetCountIndex + 1)
            maxPacketCount = int.Parse(args[packetCountIndex + 1]);

        Timer? timer = null;

        if (Array.Exists(args, arg => arg == "-s"))
        {
            try
            {
                // Generate a random packet
                byte[] bytes = GetRandomPacket();

                // Cast the device to LibPcapLiveDevice and send the packet if the cast is successful
                if (device is SharpPcap.LibPcap.LibPcapLiveDevice pcapDevice)
                {
                    // Send the packet out the network device
                    pcapDevice.SendPacket(bytes);
                    Console.WriteLine("-- Packet sent successfully.");
                }
                else
                {
                    Console.WriteLine("-- Could not send packet: device is not a LibPcapLiveDevice.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("-- " + e.Message);
            }
        }
        else
        {
            device.OnPacketArrival += device_OnPacketArrival;

            if (args.Length >= 3 && args[2] == "-d")
            {
                double duration = double.Parse(args[3]) * 1000;
                timer = new Timer(duration);
                timer.Elapsed += (s, e) =>
                {
                    device.StopCapture();
                    device.Close();
                    Environment.Exit(0);
                };
                timer.Start();
            }

            device.StartCapture();
        }

        Console.ReadLine();
        device.StopCapture();
        device.Close();
        timer?.Dispose();
    }

    private static void device_OnPacketArrival(object sender, PacketCapture e)
    {
        // Check if the maximum packet count is reached
        if (maxPacketCount != -1 && packetCount >= maxPacketCount)
        {
            device.StopCapture();
            device.Close();
            return; // Do not process this packet or any subsequent ones
        }

        // Increment the packet counter
        packetCount++;

        //Parse the packet
        var packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);

        // If the packet is an ARP packet
        if (packet is ArpPacket arpPacket)
        {
            Console.WriteLine($"-- ARP --");
            Console.WriteLine($"Protocol type: {arpPacket.ProtocolAddressType}");
            Console.WriteLine($"Operation: {arpPacket.Operation}");
            Console.WriteLine($"Sender hardware address: {arpPacket.SenderHardwareAddress}");
            Console.WriteLine($"Sender protocol address: {arpPacket.SenderProtocolAddress}");
            Console.WriteLine($"Target hardware address: {arpPacket.TargetHardwareAddress}");
            Console.WriteLine($"Target protocol address: {arpPacket.TargetProtocolAddress}");
            Console.WriteLine();
        }

        // If the packet is an Ethernet packet
        if (packet is EthernetPacket ethernetPacket)
        {
            Console.WriteLine($"-- ETHERNET --");
            Console.WriteLine($"Source: {ethernetPacket.SourceHardwareAddress}");
            Console.WriteLine($"Destination: {ethernetPacket.DestinationHardwareAddress}");
            Console.WriteLine($"Type: {ethernetPacket.Type}");
            Console.WriteLine($"Total Bytes: {ethernetPacket.TotalPacketLength}");
            Console.WriteLine();
        }

        // If the Ethernet's payload is an IP packet
        var ipPacket = packet.Extract<IPPacket>();
        if (ipPacket != null)
        {
            Console.WriteLine($"-- IP --");
            Console.WriteLine($"Source: {ipPacket.SourceAddress}");
            Console.WriteLine($"Destination: {ipPacket.DestinationAddress}");
            Console.WriteLine($"TTL: {ipPacket.TimeToLive}");
            Console.WriteLine($"Version: {ipPacket.Version}");

            if (ipPacket.Version == IPVersion.IPv4)
            {
                var ipv4Packet = (IPv4Packet)ipPacket;
                Console.WriteLine($"Fragment Offset: {ipv4Packet.FragmentOffset}");
                Console.WriteLine($"ID: {ipv4Packet.Id}");
                Console.WriteLine($"Checksum: {ipv4Packet.Checksum}");
            }
            else if (ipPacket.Version == IPVersion.IPv6)
            {
                var ipv6Packet = (IPv6Packet)ipPacket;
            }
            Console.WriteLine();
        }

        // If the IP's payload is a TCP packet
        var tcpPacket = ipPacket?.PayloadPacket as TcpPacket;
        if (tcpPacket != null)
        {
            Console.WriteLine($"-- TCP --");
            Console.WriteLine($"Source port: {tcpPacket.SourcePort}");
            Console.WriteLine($"Destination port: {tcpPacket.DestinationPort}");
            Console.WriteLine($"Sequence number: {tcpPacket.SequenceNumber}");
            Console.WriteLine($"Acknowledgment number: {tcpPacket.AcknowledgmentNumber}");
            Console.WriteLine($"Window size: {tcpPacket.WindowSize}");
            Console.WriteLine($"URG: {tcpPacket.Urgent}");
            Console.WriteLine($"ACK: {tcpPacket.Acknowledgment}");
            Console.WriteLine($"PSH: {tcpPacket.PayloadData}");
            Console.WriteLine($"RST: {tcpPacket.Reset}");
            Console.WriteLine($"SYN: {tcpPacket.SequenceNumber}");
            Console.WriteLine($"FIN: {tcpPacket.Finished}");
            Console.WriteLine();
        }

        // If the IP's payload is a UDP packet
        var udpPacket = ipPacket?.PayloadPacket as UdpPacket;
        if (udpPacket != null)
        {
            Console.WriteLine($"-- UDP --");
            Console.WriteLine($"Source port: {udpPacket.SourcePort}");
            Console.WriteLine($"Destination port: {udpPacket.DestinationPort}");
            Console.WriteLine($"Length: {udpPacket.Length}");
            Console.WriteLine($"Checksum: {udpPacket.Checksum}");
            Console.WriteLine();
        }
    }
    private static byte[] GetRandomPacket()
    {
        byte[] packet = new byte[200];
        Random rand = new Random();
        rand.NextBytes(packet);

        // Add identifiable bytes
        byte[] identifier = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }; // 200 bytes! DE AD BE EF
        Array.Copy(identifier, packet, identifier.Length);

        return packet;
    }
}