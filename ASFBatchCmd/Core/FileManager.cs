using ArchiSteamFarm.Steam;
using System.Text;

namespace ASFBatchCmd.Core;
internal static class FileManager
{
    private const string BotRangeName = "BotRange.txt";
    private const string ArgumentName = "Argument.txt";
    private const string LogName = "Log.txt";

    private static string BatchDirectory = null!;
    internal static string BotRangePath = null!;
    internal static string ArgumentPath = null!;
    internal static string LogPath = null!;

    private static SemaphoreSlim SemaphoreSlim = new(1, 1);

    public static async Task InitConfig()
    {
        BatchDirectory = Path.Combine(MyDirectory, "BatchCmd");
        BotRangePath = Path.Combine(BatchDirectory, BotRangeName);
        ArgumentPath = Path.Combine(BatchDirectory, ArgumentName);
        LogPath = Path.Combine(BatchDirectory, LogName);

        EnsureBaseDirectory();

        if (!File.Exists(BotRangePath))
        {
            await SetBatchRange(null).ConfigureAwait(false);
        }

        if (!File.Exists(ArgumentPath))
        {
            await SetBatchArguments(null).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    public static void EnsureBaseDirectory()
    {
        if (!Directory.Exists(BatchDirectory))
        {
            Directory.CreateDirectory(BatchDirectory);
        }
    }

    /// <summary>
    /// 获取机器人范围
    /// </summary>
    /// <returns></returns>
    public static async Task<List<string>> GetBatchRange()
    {
        List<string> result = [];

        if (!File.Exists(BotRangePath))
        {
            await SetBatchRange(null).ConfigureAwait(false);
        }

        if (!File.Exists(BotRangePath))
        {
            return result;
        }

        try
        {
            using var reader = new StreamReader(BotRangePath, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(line))
                {
                    result.Add(line.Trim());
                }
            }
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            ASFLogger.LogGenericError($"读取文件 {BotRangePath} 失败");
        }

        return result;
    }

    /// <summary>
    /// 设置机器人范围
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    public static async Task SetBatchRange(List<string>? range)
    {
        EnsureBaseDirectory();

        try
        {
            using var stream = new FileStream(BotRangePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream, Encoding.UTF8);

            if (range != null)
            {
                foreach (var bot in range)
                {
                    await writer.WriteLineAsync(bot).ConfigureAwait(false);
                }
            }

            await writer.FlushAsync().ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            ASFLogger.LogGenericError($"写入文件 {BotRangePath} 失败");
        }
    }

    public static async Task<List<string>> GetBatchArguments()
    {
        List<string> result = [];

        if (!File.Exists(ArgumentPath))
        {
            await SetBatchRange(null).ConfigureAwait(false);
        }

        if (!File.Exists(ArgumentPath))
        {
            return result;
        }

        try
        {
            using var reader = new StreamReader(ArgumentPath, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(line))
                {
                    result.Add(line.Trim());
                }
            }
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            ASFLogger.LogGenericError($"读取文件 {ArgumentPath} 失败");
        }

        return result;
    }

    /// <summary>
    /// 设置机器人范围
    /// </summary>
    /// <param name="words"></param>
    /// <returns></returns>
    public static async Task SetBatchArguments(List<string>? words)
    {
        EnsureBaseDirectory();

        try
        {
            using var stream = new FileStream(ArgumentPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream, Encoding.UTF8);

            if (words != null)
            {
                foreach (var bot in words)
                {
                    await writer.WriteLineAsync(bot).ConfigureAwait(false);
                }
            }

            await writer.FlushAsync().ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            ASFLogger.LogGenericError($"写入文件 {ArgumentPath} 失败");
        }
    }

    public static bool IsRunning => SemaphoreSlim.CurrentCount == 0;

    private static async Task WriteLog(StreamWriter? writer, string message)
    {
        if (writer != null)
        {
            await writer.WriteLineAsync(message).ConfigureAwait(false);
        }

        ASFLogger.LogGenericInfo(message);
    }

    public static async void ExecuteCommands(string raw, List<string> commands)
    {
        await SemaphoreSlim.WaitAsync().ConfigureAwait(false);

        EnsureBaseDirectory();

        try
        {
            FileStream? stream = null;
            StreamWriter? writer = null;

            if (Config.EnableLog)
            {
                stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                writer = new StreamWriter(stream, Encoding.UTF8);

                await writer.WriteLineAsync().ConfigureAwait(false);
            }

            await WriteLog(writer, "==========================================").ConfigureAwait(false);
            await WriteLog(writer, string.Format("-- 模板命令 {0} --", raw)).ConfigureAwait(false);
            await WriteLog(writer, string.Format("-- 开始时间 {0} --", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))).ConfigureAwait(false);

            int i = 0;
            foreach (var command in commands)
            {
                var kv = Bot.BotsReadOnly?.FirstOrDefault();
                var bot = kv?.Value;
                if (bot == null)
                {
                    await WriteLog(writer, "没有可用机器人, 无法执行").ConfigureAwait(false);
                    break;
                }

                var isOffline = !bot.IsConnectedAndLoggedOn;

                if (isOffline)
                {
                    bot.Actions.Start();

                    int tries = 5;
                    while (tries-- > 0)
                    {
                        await Task.Delay(2000).ConfigureAwait(false);
                        if (bot.IsConnectedAndLoggedOn)
                        {
                            break;
                        }
                    }
                }

                await WriteLog(writer, string.Format("{0} > {1}", i, command)).ConfigureAwait(false);
                var result = await bot.Commands.Response(EAccess.Owner, command, 0).ConfigureAwait(false);
                await WriteLog(writer, string.Format("{0} < {1}", i++, result)).ConfigureAwait(false);

                if (isOffline)
                {
                    await bot.Actions.Stop().ConfigureAwait(false);
                }

                if (Config.ExecuteDelay > 0)
                {
                    await Task.Delay(Config.ExecuteDelay).ConfigureAwait(false);
                }
            }

            await WriteLog(writer, string.Format("-- 完成时间 {0} --", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))).ConfigureAwait(false);

            if (writer != null && stream != null)
            {
                await writer.FlushAsync().ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }

            writer?.Dispose();
            stream?.Dispose();
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            ASFLogger.LogGenericError($"写入文件 {LogPath} 失败");
        }
        finally
        {
            SemaphoreSlim.Release();
        }
    }
}
