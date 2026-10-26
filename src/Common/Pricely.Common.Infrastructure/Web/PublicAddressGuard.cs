using System.Net;
using System.Net.Sockets;

namespace Pricely.Common.Infrastructure.Web;

/// <summary>
/// Connection callback of the merchant page client: the name of a merchant site must resolve to a public address. A
/// site that points to the loopback, a private network or a cloud metadata address is refused (SSRF), at connection
/// time, so a DNS answer that changes between two requests cannot bypass it.
/// </summary>
internal static class PublicAddressGuard
{
    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        IPAddress address = Array.Find(addresses, IsPublic)
            ?? throw new HttpRequestException($"The host {context.DnsEndPoint.Host} does not resolve to a public address.");

        Socket? socket = new(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);

            var stream = new NetworkStream(socket, ownsSocket: true);
            socket = null;

            return stream;
        }
        finally
        {
            socket?.Dispose();
        }
    }

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.Broadcast))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return IsPublicIPv6(address);
        }

        byte[] bytes = address.GetAddressBytes();

        return bytes[0] switch
        {
            0 or 10 or 127 => false,
            100 when bytes[1] is >= 64 and <= 127 => false,
            169 when bytes[1] == 254 => false,
            172 when bytes[1] is >= 16 and <= 31 => false,
            192 when bytes[1] == 168 || bytes[1] == 0 && bytes[2] is 0 or 2 => false,
            198 when bytes[1] is 18 or 19 || bytes[1] == 51 && bytes[2] == 100 => false,
            203 when bytes[1] == 0 && bytes[2] == 113 => false,
            >= 224 => false,
            _ => true
        };
    }

    /// <summary>
    /// Link-local, site-local, unique local (fc00::/7), multicast, documentation (2001:db8::/32), and the prefixes that
    /// carry an IPv4 address (IPv4-compatible ::/96, NAT64 64:ff9b::/96, 6to4 2002::/16): never reached.
    /// </summary>
    private static bool IsPublicIPv6(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        bool ipv4Compatible = bytes.AsSpan(0, 12).IndexOfAnyExcept((byte)0) < 0;
        bool nat64 = bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xFF && bytes[3] == 0x9B;
        bool sixToFour = bytes[0] == 0x20 && bytes[1] == 0x02;
        bool documentation = bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8;

        return !(address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6Multicast
            || (bytes[0] & 0xFE) == 0xFC
            || ipv4Compatible
            || nat64
            || sixToFour
            || documentation);
    }
}
