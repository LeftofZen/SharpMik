using SharpMik.IO;
using System.IO;

namespace SharpMik.Depackers
{
    public interface IDepacker
    {
        bool Unpack(ModuleReader reader, out Stream read);
    }
}
