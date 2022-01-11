using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class IpcClientInterface : IDisposable
{
    private readonly WebClient _client;

    /// <summary>
    /// IPC接口地址
    /// </summary>
    public Uri PartnerAddress => new Uri($"http://localhost:{PartnerPort}");

    /// <summary>
    /// IPC接口端口
    /// </summary>
    public int PartnerPort { get; }

    public IpcClientInterface(int partnerPort)
    {
        PartnerPort = partnerPort;
        _client = new WebClient();
    }

    /// <summary>
    /// 根据端口获取地址
    /// </summary>
    public Uri GetAddress(int port)
    {
        return new Uri($"http://localhost:{port}");
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public string SendMessage<T>(T obj, ReceivedEventType eventType = ReceivedEventType.None)
    {
        var json = JsonConvert.SerializeObject(obj);
        var returnInfo = _client.UploadData(PartnerAddress, Encoding.UTF8.GetBytes($"*[{eventType}]*" + json));
        var str = Encoding.UTF8.GetString(returnInfo);
        return str;
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public string SendMessage(string obj, ReceivedEventType eventType = ReceivedEventType.None)
    {
        var returnInfo = _client.UploadData(PartnerAddress, Encoding.UTF8.GetBytes($"*[{eventType}]*" + obj));
        var str = Encoding.UTF8.GetString(returnInfo);
        return str;
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public string SendPortMesage<T>(Uri uri, T obj, ReceivedEventType eventType = ReceivedEventType.None)
    {
        var json = JsonConvert.SerializeObject(obj);
        var returnInfo = _client.UploadData(uri, Encoding.UTF8.GetBytes($"*[{eventType}]*" + json));
        var str = Encoding.UTF8.GetString(returnInfo);
        return str;
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public string SendPortMesage(Uri uri, string obj, ReceivedEventType eventType = ReceivedEventType.None)
    {
        var returnInfo = _client.UploadData(uri, Encoding.UTF8.GetBytes($"*[{eventType}]*" + obj));
        var str = Encoding.UTF8.GetString(returnInfo);
        return str;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
