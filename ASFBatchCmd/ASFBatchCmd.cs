using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ASFBatchCmd.Core;
using ASFBatchCmd.Data;
using System.ComponentModel;
using System.Composition;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ASFBatchCmd;

[Export(typeof(IPlugin))]
internal sealed class ASFBatchCmd : IASF, IBotCommand2
{
    private bool ASFEBridge;

    private Timer? StatisticTimer;

    /// <summary>
    ///     获取插件信息
    /// </summary>
    private string PluginInfo => $"{Name} {Version}";

    public string Name => "ASF Batch Cmd";

    public Version Version => MyVersion;

    /// <summary>
    ///     ASF启动事件
    /// </summary>
    /// <param name="additionalConfigProperties"></param>
    /// <returns></returns>
    public Task OnASFInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null)
    {
        PluginConfig? config = null;

        if (additionalConfigProperties != null)
        {
            foreach (var (configProperty, configValue) in additionalConfigProperties)
            {
                if (configProperty != "ASFEnhance" || configValue.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                try
                {
                    config = configValue.ToJsonObject<PluginConfig>();
                    if (config != null)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    ASFLogger.LogGenericException(ex);
                }
            }
        }

        Config = config ?? new PluginConfig(false, false);

        var sb = new StringBuilder();

        //使用协议
        if (!Config.EULA)
        {
            sb.AppendLine();
            sb.AppendLine(Langs.Line);
            sb.AppendLineFormat(Langs.EulaWarning, Name);
            sb.AppendLine(Langs.Line);
        }

        if (sb.Length > 0)
        {
            ASFLogger.LogGenericWarning(sb.ToString());
        }

        //统计
        if (Config.Statistic && !ASFEBridge)
        {
            var request = new Uri("https://asfe.chrxw.com/asfbatchcmd");
            StatisticTimer = new Timer(
                async _ => await ASF.WebBrowser!.UrlGetToHtmlDocument(request).ConfigureAwait(false),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromHours(24)
            );
        }

        return FileManager.InitConfig();
    }

    /// <summary>
    ///     插件加载事件
    /// </summary>
    /// <returns></returns>
    public Task OnLoaded()
    {
        ASFLogger.LogGenericInfo(Langs.PluginContact);
        ASFLogger.LogGenericInfo(Langs.PluginInfo);

        const BindingFlags flag = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var handler = typeof(ASFBatchCmd).GetMethod(nameof(ResponseCommand), flag);

        const string pluginId = nameof(ASFBatchCmd);
        const string cmdPrefix = "ABC";
        const string repoName = "ASFBatchCmd";

        ASFEBridge = AdapterBridge.InitAdapter(Name, pluginId, cmdPrefix, repoName, handler);

        if (ASFEBridge)
        {
            ASFLogger.LogGenericDebug(Langs.ASFEnhanceRegisterSuccess);
        }
        else
        {
            ASFLogger.LogGenericInfo(Langs.ASFEnhanceRegisterFailed);
            ASFLogger.LogGenericWarning(Langs.PluginStandalongMode);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     处理命令事件
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <param name="steamId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamId = 0)
    {
        if (ASFEBridge)
        {
            return null;
        }

        if (!Enum.IsDefined(access))
        {
            throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
        }

        try
        {
            var cmd = args[0].ToUpperInvariant();

            if (cmd.StartsWith("ABC."))
            {
                cmd = cmd[4..];
            }

            var task = ResponseCommand(access, cmd, message, args);
            if (task != null)
            {
                return await task.ConfigureAwait(false);
            }

            return null;
        }
        catch (Exception ex)
        {
            _ = Task.Run(async () => {
                await Task.Delay(500).ConfigureAwait(false);
                ASFLogger.LogGenericException(ex);
            }).ConfigureAwait(false);

            return ex.StackTrace;
        }
    }

    /// <summary>
    ///     处理命令
    /// </summary>
    /// <param name="access"></param>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private Task<string>? ResponseCommand(EAccess access, string cmd, string message, string[] args)
    {
        var argLength = args.Length;

        return argLength switch {
            0 => throw new InvalidOperationException(nameof(args)),
            1 => cmd switch //不带参数
            {
                //插件信息
                "ASFBATCHCMD" or
                "ABC" when access >= EAccess.FamilySharing => Task.FromResult(PluginInfo),

                "BATCHRANGE" or
                "BATRANGE" when access >= EAccess.Master => Command.ResponseSetBatchRange("ASF"),

                "BATCHARGUMWENTS" or
                "BATCHARGUMENT" or
                "BATCHARGS" or
                "BATCHARG" or
                "BATARGUMWENTS" or
                "BATARGUMENT" or
                "BATARGS" or
                "BATARG" when access >= EAccess.Master => Command.ResponseSetBatchArgument(""),

                "BATCHCMD" or
                "BATCMD" or
                "BATCHCMDR" or
                "BATCMDR" or
                "BATCHCMDU" or
                "BATCMDU" or
                "BATCHCMDRU" or
                "BATCMDRU" or
                "BATCHCMDUR" or
                "BATCMDUR" when access >= EAccess.Master => Task.FromResult(Command.ResponseBatchCmd()),

                _ => null
            },
            _ => cmd switch //带参数
            {
                "BATCHRANGE" or
                "BATRANGE" when access >= EAccess.Master => Command.ResponseSetBatchRange(Utilities.GetArgsAsText(message, 1)),

                "BATCHARGUMWENTS" or
                "BATCHARGUMENT" or
                "BATCHARGS" or
                "BATCHARG" or
                "BATARGUMWENTS" or
                "BATARGUMENT" or
                "BATARGS" or
                "BATARG" when access >= EAccess.Master => Command.ResponseSetBatchArgument(Utilities.GetArgsAsText(args, 1, ",")),

                "BATCHCMD" or
                "BATCMD" when access >= EAccess.Master => Command.ResponseBatchCmd(Utilities.GetArgsAsText(message, 1), false, false),

                "BATCHCMDR" or
                "BATCMDR" when access >= EAccess.Master => Command.ResponseBatchCmd(Utilities.GetArgsAsText(message, 1), true, false),


                "BATCHCMDU" or
                "BATCMDU" when access >= EAccess.Master => Command.ResponseBatchCmd(Utilities.GetArgsAsText(message, 1), false, true),

                "BATCHCMDRU" or
                "BATCMDRU" or
                "BATCHCMDUR" or
                "BATCMDUR" when access >= EAccess.Master => Command.ResponseBatchCmd(Utilities.GetArgsAsText(message, 1), true, true),

                _ => null
            }
        };
    }
}