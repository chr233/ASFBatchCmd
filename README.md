# ASFDailyExecute

## 配置文件

```json
{
  "IPCPassword": "",
  ...
  "ASFEnhance": {
    "EULA": true,
    "Statistic": true,
    "ExecuteDelay": "00:00",
    "EnableLog": false
  }
}
```

| 配置项目       | 类型   | 默认值  | 说明                         |
| -------------- | ------ | ------- | ---------------------------- |
| `EULA`         | `bool` | `false` | 是否同意使用协议             |
| `Statistic`    | `bool` | `true`  | 发送统计信息                 |
| `ExecuteDelay` | `int`  | `500`   | 每条命令执行延迟             |
| `EnableLog`    | `bool` | `true`  | 命令执行过程是否保存日志文件 |

## 命令

| Command             | Shortcut   | Right           | Description                                                                           |
| ------------------- | ---------- | --------------- | ------------------------------------------------------------------------------------- |
| `ASFBATCMD`         | `ABC`      | `FamilySharing` | 测试插件                                                                              |
| -                   | -          | -               | -                                                                                     |
| `BATCHRANGE [Bots]` | `BATRANGE` | `Master`        | 设置机器人范围, 例如 `BATRANGE bot1 bot2 bot3 ...`, 也可以手动编辑 `BotRang.txt`      |
| `BATCHARGS [Args]`  | `BATARGS`  | `Master`        | 设置命令参数列表, 例如 `BATARGS 参数1 参数2 参数3 ...`, 也可以手动编辑 `Argument.txt` |
| `BATCHCMD 命令模板` | `BATCMD`   | `Master`        | 根据设置的执行范围和命令参数列表, 批量执行命令                                        |
| `BATCHCMD`          | `BATCMD`   | `Master`        | 查看使用帮助                                                                          |

1. 插件的配置文件保存在 `plugins\BatCmd` 下, 其中

   - `BotRang.txt` 机器人范围, 每行代表一个机器人, 请避免在这个文件中使用 ASF 指代全体机器人, 请执行命令 `BATRANGE ASF`, 会生成所有机器人的列表
   - `Argument.txt` 参数列表, 每行代表一个参数
   - `Log.txt` 日志文件, 启用 `EnableLog` 后会生成, 记录命令执行的详细信息

2. `BATCMD` 说明

   - 在命令模板中, 可以用 `$B` 代指机器人, `$A` 代指参数
   - "机器人范围" 可以使用命令 `BATRANGE` 设置, 例如 `BATRANGE Bot1 Bot2` (也支持使用 ASF 指代所有机器人)
   - "参数列表" 可以使用命令 `BATARGS` 设置, 例如 `BATARGS 参数1 参数2` (也支持使用 ASF 指代所有参数)
   - 插件会给每个机器人使用一个参数, 组装成最终命令, 例如命令模板 `NICKNAME $B $A` 会被替换为 `NICKNAME Bot1 参数1`
   - 默认状态下会按顺序给每个机器人分配参数, 如果参数数量少于机器人数量, 没有分到参数的机器人将不会参与执行
   - 如果需要将参数随机分配给机器人, 可以使用 `BATCMDR` , R 代表 Random
   - 如果需要将参数设置为可以重复使用, 可以使用 `BATCMDU` , U 代表 Reuse
   - 如果需要将参数设置为可以重复使用, 并且随机分配, 可以使用 `BATCMDRU` 或者 `BATCMDUR`

3. `BATCMD` 示例用法
   - 比如需要批量执行 `NICKNAME bot name`, 这条命令, 其中每个机器人分配一个不一样的 name
   - 首先需要设置机器人范围, 使用 `BATRANGE ASF`, 将执行范围设定为全部机器人, 也可以手动编辑 `BotRange.txt` 文件
   - 然后需要设置参数列表, 使用 `BATARGS 昵称1 昵称2 昵称3`, 配置预定义的昵称列表, 也可以手动编辑 `Argument.txt` 文件
   - 最后将命令改写为命令模板, 使用 `$B` `$A` 替换对应的机器人和参数, 比如 `NICKNAME $B $A`
   - 然后根据具体情况选择批量执行的模式
     - `BATCMD NICKNAME $B $A`, 参数按顺序分配, 每个参数只会使用一次, 如果参数数量小于机器人数量, 未分配到的机器人不会执行命令
     - `BATCMDR NICKNAME $B $A`, 参数随机分配, 每个参数只会使用一次
     - `BATCMDU NICKNAME $B $A`, 参数按顺序分配, 重复使用参数直到每个机器人都分配到参数
     - `BATCMDRU NICKNAME $B $A`, 参数随机分配, 重复使用参数
     - `BATCMDUR NICKNAME $B $A`, 参数随机分配, 重复使用参数

   - 如果不需要使用参数, 也可以只使用 `$B`, 例如 `BATCMD LEVEL $B`, 这种情况下可以不设置 `BATARGS`