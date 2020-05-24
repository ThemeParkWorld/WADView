using System;

namespace WADView
{
    public interface IArchive : IDisposable
    {
        void LoadArchive(string path);
    }
}
