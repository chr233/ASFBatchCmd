using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using System.Text;

namespace ASFBatchCmd.Core;

internal static class Command
{
    /// <summary>
    /// 设置执行范围
    /// </summary>
    /// <param name="botNames"></param>
    /// <returns></returns>
    public static async Task<string> ResponseSetBatchRange(string botNames)
    {
        var bots = Bot.GetBots(botNames);
        if (bots == null || bots.Count == 0)
        {
            await FileManager.SetBatchRange(null).ConfigureAwait(false);
        }
        else
        {
            List<string> botNamesList = [];
            foreach (var bot in bots)
            {
                botNamesList.Add(bot.BotName);
            }
            await FileManager.SetBatchRange(botNamesList).ConfigureAwait(false);
        }

        var range = await FileManager.GetBatchRange().ConfigureAwait(false);

        return FormatStaticResponse("修改执行范围为 -> {0} 个机器人", range.Count);
    }

    /// <summary>
    /// 设置执行参数
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static async Task<string> ResponseSetBatchArgument(string query)
    {
        List<string> args = [];

        var lines = query.Split(',', SplitOptions);
        foreach (var line in lines)
        {
            args.Add(line);
        }

        await FileManager.SetBatchArguments(args).ConfigureAwait(false);

        var range = await FileManager.GetBatchArguments().ConfigureAwait(false);

        return FormatStaticResponse("修改执行参数为 -> {0} 条", range.Count);
    }

    /// <summary>
    /// 使用帮助
    /// </summary>
    /// <returns></returns>
    public static string ResponseBatchCmd()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);
        sb.AppendLine("命令用法: BATCMD 命令模板");
        sb.AppendLine("1. 在命令模板中, 可以用 $B 代指机器人, $A 代指参数");
        sb.AppendLine("2. 机器人范围可用命令 BATRANGE 设置, 例如 BATRANGE Bot1 Bot2 (也支持使用 ASF 指代所有机器人)");
        sb.AppendLine("3. 插件会给每个机器人使用一个参数, 组装成最终命令, 例如命令模板 NICKNAME $B $A 会被替换为 NICKNAME Bot1 参数1");
        sb.AppendLine("4. 默认状态下会按顺序给每个机器人分配参数, 如果参数数量少于机器人数量, 没有分到参数的机器人将不会参与执行");
        sb.AppendLine("5. 如果需要将参数随机分配给机器人, 可以使用 BATCMDR , R 代表 Random");
        sb.AppendLine("6. 如果需要将参数设置为可以重复使用, 可以使用 BATCMDU , U 代表 Reuse");
        sb.AppendLine("7. 如果需要将参数设置为可以重复使用, 并且随机分配, 可以使用 BATCMDRU 或者 BATCMDUR");
        return FormatStaticResponse(sb.ToString());
    }

    /// <summary>
    /// 批量执行命令
    /// </summary>
    /// <param name="message"></param>
    /// <param name="randomArgs"></param>
    /// <param name="reuseArgs"></param>
    /// <returns></returns>
    public static async Task<string> ResponseBatchCmd(string message, bool randomArgs = false, bool reuseArgs = false)
    {
        if (FileManager.IsRunning)
        {
            return FormatStaticResponse("后台任务正在执行中, 请等待执行结束");
        }

        var bots = await FileManager.GetBatchRange().ConfigureAwait(false);
        var args = await FileManager.GetBatchArguments().ConfigureAwait(false);

        if (bots.Count == 0)
        {
            return FormatStaticResponse("没有设置执行范围, 使用命令 BATRANGE 设置, 或者编辑文件 {0}", FileManager.BotRangePath);
        }

        if (args.Count == 0)
        {
            return FormatStaticResponse("没有设置执行参数, 使用命令 BATARGS 设置, 或者编辑文件 {0}", FileManager.ArgumentPath);
        }

        if (randomArgs)
        {
            args = [.. args.OrderBy(static _ => Random.Shared.Next())];
        }

        List<string> commands = [];
        int i = 0;
        foreach (var bot in bots)
        {
            var arg = args[i++];

            var cmd = message
                .Replace("$B", bot)
                .Replace("$b", bot)
                .Replace("$A", arg)
                .Replace("$a", arg);

            commands.Add(cmd);

            if (i >= args.Count)
            {
                if (reuseArgs)
                {
                    i = 0;

                    if (randomArgs)
                    {
                        args = [.. args.OrderBy(static _ => Random.Shared.Next())];
                    }
                }
                else
                {
                    break;
                }
            }
        }

        Utilities.InBackground(() => FileManager.ExecuteCommands(message, commands));

        if (Config.EnableLog)
        {
            return FormatStaticResponse("将在后台执行 {0} 条命令, 日志可以在 {1} 查看", commands.Count, FileManager.LogPath);
        }
        else
        {
            return FormatStaticResponse("将在后台执行 {0} 条命令", commands.Count);
        }
    }
}