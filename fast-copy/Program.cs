using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.ObjectPool;

string source = args?[0] ?? throw new ArgumentNullException(nameof(source));
string dest = args?[1] ?? throw new ArgumentNullException(nameof(dest));

int threads = 12;

Console.WriteLine($"Copying {source} to {dest}");

ObjectPool<Chunk> objectPool = ObjectPool.Create<Chunk>();
FileInfo fileInfo = new FileInfo(source);

static async Task ReadAsync(
    string source,
    ChannelWriter<Chunk> channelWriter,
    ObjectPool<Chunk> objectPool)
{
    using (FileStream fileStream = File.OpenRead(source))
    {
        while (true)
        {
            Chunk chunk = objectPool.Get();
            chunk.
        }
    }
}

public class Chunk
{
    public byte[] Buffer { get; } = new byte[516 * 1024];
    

}