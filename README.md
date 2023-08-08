# Coyote Packet Sniffer

Coyote is a C# packet capturing and sniffing program. The program currently supports a couple of arguments, such as listing network interfaces, sniffing packet data, duration of sniffs, number of packets to snuff, and much more such as file redirection. 

This program was made possible by [sharpcap](https://github.com/dotpcap/sharppcap)

### Prerequisites

- Make sure you have .NET SDK downloaded, preferably 6.0 or 7.0
- [dotnet](https://dotnet.microsoft.com/en-us/download)

### Installation

After you have made sure you have a .NET SDK...

Open an admin PowerShell terminal and navigate to the root, run the following command:

`dotnet restore`

This will install the needed NuGet packages

### Running and Arguments

Coyote is run via command line, `dotnet run --` is required, followed by optional arguments

Providing an index number after `dotnet run -- ` such as `dotnet run -- 1` will select your network interface which is indexed at position 1 to start capturing. 

Use your command lines default SIGINT to stop the program from running, in PowerShell this is `ctrl + c`

Optional arguments:

- `-l` lists out all devices, numbering them to be used to capture
- `-d {duration in seconds}` the number of seconds you would like to capture packets for
- `-n {1-int max size}` determines if you would like to stop the program after a number of packets have been captured
- `-s` sends a packet to the specified network interface
- `-t {"filter"}` a filter argument, used to filter based on specification. For example `-t "tcp port 80"`

All of these arguments, except `-l` require a index number for which network interface you are sniffing, or sending a packet to. Below are some more indepth examples of complete commands:

- `dotnet run -- 2 -d 30` sniffs packets on the net interface 2, defined by `-l` for a duration of 30 seconds
- `dotnet run -- 4 -s` sends a packet to net interface 4
- `dotnet run -- 23 -n 100 -t "tcp port 80" -d 2000` Combos can be used too, sniff net interface 23, for 100 packets, or a duration of 2000 seconds, with a filter of tcp port 80

Coyote also supports command line file redirection:

- `dotnet run -- -l > output.txt` Send terminal device list to a txt file
- `dotnet run -- 8 -n 10 > output.txt` Sends terminal packet capture for 10 packets to txt file
