using CounterStrikeSharp.API.Core;
using System.Text.Json;

public class HanAirDropCFG
{
    public bool AirDropEnble { get; set; } //是否开启 Whether air drop is enabled  
    public int AirDropMode { get; set; } //掉落模式 0 随时间生成 1 玩家死亡生成 2 两种均开启 Drop mode: 0 - Spawn over time, 1 - Spawn on player death, 2 - Both enabled
    public int AirDropPosMode { get; set; } //掉落模式为 随时间时 掉落位置 配置 0 ct t 复活点随机 1 仅ct 复活点 2仅 t复活点  When drop mode is "over time," position config: 0 - Random CT/T spawn, 1 - Only CT spawn, 2 - Only T spawn  
    public float DeathDropPercent { get; set; } //死亡掉落的几率  Probability of drop on death  
    public float AirDropTimer { get; set; } //随时间生成的 间隔秒 Time interval (seconds) for timed spawns  
    public float AirDropKillTimer { get; set; } //存在的时间 多少秒后自动删除消失 Lifetime (seconds) before auto-removal  
    public required string AirDropName { get; set; } //可以掉落的空投名称  Name of the droppable air supply  
    public int PlayerPickEachRound { get; set; } //玩家每回合可以拾取空投的次数限制 0 无限  Player pickup limit per round (0 = unlimited)  
    public int AirDropSpawnMode { get; set; } //空投生成模式 0 固定模式 1 根据玩家数量动态生成模式 Spawn mode: 0 - Fixed amount, 1 - Dynamic based on player count  
    public int AirDropCount { get; set; } //固定模式 一次生成多少个 Fixed mode: number of drops per spawn  
    public int AirDropDynamicCount { get; set; } //动态模式 根据玩家数量 生成 几个 假设填写 1 Dynamic mode: base multiplier (e.g., if 1)  
    public int AirDropPlayerCount { get; set; } //动态模式 每几个玩家 假设 AirDropPlayerCount  填写 2 AirDropDynamicCount 填写 1 则 服务器 玩家 / 2 * 1 = 生成数量 少于2 生成 1 Dynamic mode: divisor (e.g., if AirDropPlayerCount=2 and AirDropDynamicCount=1, spawn count = (player count / 2) * 1; min 1 if <2)  
    public required string PrecacheSoundEvent { get; set; } //预缓存soundevent Precached sound event  
    public required string AdminCommand { get; set; } //自定义管理员召唤空投的命令 Custom admin command to summon an airdrop
    public int Openrandomspawn { get; set; } //打开deathmatch 生成点 用于随机生成
    public required string AdminCommandFlags { get; set; } //管理员召唤随机空投所需要的Flags
    public required string AdminSelectBoxCommand { get; set; } //召唤自选空投箱命令
    public required string AdminSelectBoxCommandFlags { get; set; } //召唤自选空投箱的权限
    public int AdminSelectBoxCount { get; set; } //限制恶意生成数量过多炸服

    public float AdminSelectBoxColdCown { get; set; }

    public static string ConfigPath = Path.Combine(Application.RootDirectory, "configs", "HanAirDrop", "HanAirDropCFG.json");

    public static HanAirDropCFG Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                // 反序列化配置
                var config = JsonSerializer.Deserialize<HanAirDropCFG>(json);
                if (config != null)
                {
                    return config; // 成功反序列化，返回配置
                }
                else
                {
                    Console.WriteLine("读取配置文件失败，使用默认配置。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取配置文件时发生错误: {ex.Message}，使用默认配置。");
            }
        }

        // 如果配置文件不存在或读取失败，则创建默认配置并保存
        var defaultConfig = new HanAirDropCFG
        {
            AirDropEnble = true,
            AirDropMode = 0,
            AirDropPosMode = 0,
            DeathDropPercent = 0.1f,
            AirDropTimer = 60.0f,
            AirDropKillTimer = 20.0f,
            AirDropName = "空投1,空投2",
            PlayerPickEachRound = 0,
            AirDropSpawnMode = 0,
            AirDropCount = 3,
            AirDropDynamicCount = 1,
            AirDropPlayerCount = 1,
            PrecacheSoundEvent = "",
            AdminCommand = "css_createbox",
            Openrandomspawn = 0,
            AdminCommandFlags = "",
            AdminSelectBoxCommand = "css_selectbox",
            AdminSelectBoxCommandFlags = "",
            AdminSelectBoxCount = 10,
            AdminSelectBoxColdCown = 1.0f

        };
        Save(defaultConfig); // 保存默认配置
        return defaultConfig;
    }

    public static void Save(HanAirDropCFG config)
    {
        try
        {
            string directoryPath = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"[HanAirDropCFG] 文件夹 {directoryPath} 不存在，已创建.");
            }

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            Console.WriteLine("[HanAirDropCFG] 配置文件已保存。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HanAirDropCFG] 无法写入配置文件: {ex.Message}");
            Console.WriteLine($"详细错误：{ex.StackTrace}");
        }
    }


}
