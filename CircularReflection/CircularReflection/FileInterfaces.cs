using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CircularReflection;


internal class FileTarget : IFileTarget
{
    private readonly string _path;

    public FileTarget(string path)
    {
        _path = path;
        File.Delete(path);
    }

    public void Append(string content) => File.AppendAllText(_path, content);
}

internal interface IFileTarget
{
    void Append(string content);
}

internal interface IFileSource
{
    IEnumerable<IFileProxy> GetFiles();
}

internal interface IFileProxy
{
    string BodyText { get; }
    string Path { get; }
}

internal class FileSource: IFileSource
{
    private readonly string[] _paths;

    public FileSource(string basePath, string pattern)
    {
        _paths = Directory.GetFiles(basePath, pattern);
    }

    public IEnumerable<IFileProxy> GetFiles() => _paths.Select(p=> new FileProxy(p));

    private class FileProxy:IFileProxy
    {
        public FileProxy(string path)
        {
            Path = path;
            BodyText = File.ReadAllText(path);
        }

        public string BodyText { get; private set; }
        public string Path { get; private set; }
    }
}