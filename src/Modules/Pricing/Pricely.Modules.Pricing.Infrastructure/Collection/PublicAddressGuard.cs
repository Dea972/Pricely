using System.Net;
using System.Net.Sockets;

namespace Pricely.Modules.Pricing.Infrastructure.Collection;

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
            byte first = address.GetAddressBytes()[0];

            // Link-local, site-local, unique local (fc00::/7) and multicast.
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || (first & 0xFE) == 0xFC);
        }

        byte[] bytes = address.GetAddressBytes();

        return bytes[0] switch
        {
            0 or 10 or 127 => false,
            100 when bytes[1] is >= 64 and <= 127 => false,
            169 when bytes[1] == 254 => false,
            172 when bytes[1] is >= 16 and <= 31 => false,
            192 when bytes[1] == 168 => false,
            >= 224 => false,
            _ => true
        };
    }
}
