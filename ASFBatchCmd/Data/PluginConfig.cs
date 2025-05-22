namespace ASFBatchCmd.Data;

internal sealed record PluginConfig(
    bool EULA,
    bool Statistic = true,
    int ExecuteDelay = 500,
    bool EnableLog = true);
