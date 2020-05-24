using System;

namespace WADView
{
    [Serializable]
    public class NotDWFBException : Exception
    {
        public NotDWFBException(string message) : base(message) { }
    }

    [Serializable]
    public class NotRefpackException : Exception
    {
        public NotRefpackException(string message) : base(message) { }
    }
}
