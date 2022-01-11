using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityThreads;

namespace ProcessHelper
{
    public class IpcServerInterface : IDisposable
    {
        private readonly HttpListener _server;
        Regex reg = new Regex(@"(?<=\*\[)[^\[\]]+(?=\]\*)");

        /// <summary>
        /// 消息事件
        /// </summary>
        public event EventHandler<IpcEventArgs> OnMessageReceived;

        /// <summary>
        /// 消息事件
        /// </summary>
        private Dictionary<string, EventHandler<IpcEventArgs>> _dictReceivedRequest = new Dictionary<string, EventHandler<IpcEventArgs>>();

        /// <summary>
        /// 监听IPC接口地址
        /// </summary>
        public Uri Address => new Uri($"http://localhost:{Port}");

        /// <summary>
        /// 监听IPC接口端口
        /// </summary>
        public int Port { get; }


        /// <summary>
        /// 创建IPC接口
        /// </summary>
        public IpcServerInterface(int port)
        {
            Port = port != 0 ? port : FreePortHelper.GetFreePort();
            Loom.Initialize();
            _server = new HttpListener();
            _server.Prefixes.Add(Address.AbsoluteUri);
            _server.Start();
            _server.BeginGetContext(Result, null);
        }

        /// <summary>
        /// 随机端口创建IPC接口
        /// </summary>
        public IpcServerInterface() : this(0) { }

        /// <summary>
        /// 序列化
        /// </summary>
        public void On<T>(Action<T> action)
        {
            OnMessageReceived += (sender, e) =>
            {
                T obj;
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                };
                try
                {
                    obj = JsonConvert.DeserializeObject<T>(e.SerializedObject, settings);
                }
                catch (JsonSerializationException)
                {
                    return;
                }
                action(obj);
            };
        }

        /// <summary>
        /// 添加事件监听
        /// </summary>
        public void AddListeningMessage(ReceivedEventType eventType, EventHandler<IpcEventArgs> eventHandler)
        {
            var type = eventType.ToString();
            _dictReceivedRequest[type] = eventHandler;
        }

        /// <summary>
        /// 删除事件
        /// </summary>
        public void RemoveListeningMessage(ReceivedEventType eventType)
        {
            var type = eventType.ToString();
            _dictReceivedRequest[type] = null;
        }

        /// <summary>
        ///  处理数据
        /// </summary>
        private void InvokeOnMessageReceived(string obj)
        {
            var handler = OnMessageReceived;
            string eventType = reg.Matches(obj)[0].Value;
            var data = obj.Substring(eventType.Length + 4);
            var ipcEventArgs = new IpcEventArgs { SerializedObject = data };
            handler?.Invoke(this, ipcEventArgs);

            if (_dictReceivedRequest.Count > 0)
            {
                EventHandler<IpcEventArgs> receivedRequest;
                _dictReceivedRequest.TryGetValue(eventType, out receivedRequest);
                receivedRequest?.Invoke(this, ipcEventArgs);
            }
        }

        private void Result(IAsyncResult ar)
        {
            _server.BeginGetContext(Result, null);
            var context = _server.EndGetContext(ar);
            var request = context.Request;
            var response = context.Response;
            context.Response.ContentType = "text/plain;charset=UTF-8";
            context.Response.AddHeader("Content-type", "text/plain");
            context.Response.ContentEncoding = Encoding.UTF8;
            string returnObj = null;
            if (request.HttpMethod == "POST" && request.InputStream != null)
            {
                returnObj = HandleRequest(request, response);
            }
            else
            {
                returnObj = "不是post请求或者传过来的数据为空";
            }
            var returnByteArr = Encoding.UTF8.GetBytes(returnObj);
            try
            {
                using (var stream = response.OutputStream)
                {
                    stream.Write(returnByteArr, 0, returnByteArr.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"网络错误:{ex}");
            }
        }

        private string HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var stream = request.InputStream;
                if (stream == null)
                    throw new ArgumentNullException(nameof(request.InputStream));
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    var data = Encoding.UTF8.GetString(memoryStream.GetBuffer());
                    Loom.QueueOnMainThread(() => InvokeOnMessageReceived(data));
                }
            }
            catch (Exception ex)
            {
                response.StatusDescription = "404";
                response.StatusCode = 404;
                UnityEngine.Debug.Log($"在接收数据时发生错误:{ex}");
                return $"在接收数据时发生错误:{ex}";
            }
            response.StatusDescription = "200";
            response.StatusCode = 200;
            return $"接收数据完成";
        }

        public void Stop()
        {
            _server.Stop();
        }

        public void Dispose()
        {
            _server.Close();
            GC.SuppressFinalize(this);
        }
    }
}
