using System;

namespace ProcessHelper
{
    public class IpcEventArgs : EventArgs
    {
        public string SerializedObject { get; set; }
    }
}
