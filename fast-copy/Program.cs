using System.IO.MemoryMappedFiles;
using System.Threading.Channels;

using Microsoft.Extensions.ObjectPool;

if (args.Length < 2)
{
    Console.WriteLine("Usage: ");
}

string source = args?[0] ?? throw new ArgumentNullException(nameof(source));
string dest = args?[1] ?? throw new ArgumentNullException(nameof(dest));

int threads = 12;

ObjectPool<Chunk> objectPool = ObjectPool.Create<Chunk>();
Channel<Chunk> channel = Channel.CreateBounded<Chunk>(threads * 4);
FileInfo fileInfo = new FileInfo(source);

Task readTask = ReadAsync(source, channel.Writer, objectPool);

Task[] writeTasks = new Task[threads];
using (MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(dest, FileMode.OpenOrCreate, null, fileInfo.Length))
{
    for (int i = 0; i < threads; i++)
    {
        writeTasks[i] = WriteAsync(memoryMappedFile, channel, objectPool);
    }

    await Task.WhenAll(writeTasks);
}

await readTask;

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
            chunk.Offset = fileStream.Position;
            chunk.Count = await fileStream.ReadAsync(chunk.Buffer);
            if (chunk.Count == 0)
            {
                channelWriter.Complete();
                return;
            }
            await channelWriter.WriteAsync(chunk);
        }
    }
}

static async Task WriteAsync(
    MemoryMappedFile dest,
    ChannelReader<Chunk> channelReader,
    ObjectPool<Chunk> objectPool)
{
    while (await channelReader.WaitToReadAsync())
    {
        if (channelReader.TryRead(out Chunk? chunk))
        {
            using (MemoryMappedViewAccessor viewAccessor = dest.CreateViewAccessor(chunk.Offset, chunk.Count))
            {
                viewAccessor.WriteArray(0, chunk.Buffer, 0, chunk.Count);
            }

            objectPool.Return(chunk);
        }
    }
}

internal class Chunk
{
    public byte[] Buffer { get; } = new byte[System.Environment.SystemPageSize];

    public int Count { get; set; }

    public long Offset { get; set; }
}