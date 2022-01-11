using System.Net;
using System.Net.Sockets;

public static class FreePortHelper
{
    /// <summary>
    /// 获取可用的端口
    /// </summary>
    public static int GetFreePort()
    {
        using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            sock.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            return ((IPEndPoint)sock.LocalEndPoint).Port;
        }
    }
}
