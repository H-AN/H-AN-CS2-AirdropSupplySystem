using CounterStrikeSharp.API.Core;
using System.Text.Json;
using System.Text.Encodings.Web;

public class HanAirDropMessageCFG
{
    public string AdminDropMessage { get; set; } 
    public string AirDropMessage { get; set; } 

    public string BlockTeamMessage { get; set; } 
    public string BlockRoundGlobalMessage { get; set; } 

    public string BlockRoundBoxMessage { get; set; } 

    public string BlockRoundSpawnlMessage { get; set; } 

    public string PlayerPickUpMessage { get; set; } 

    public string CtSpawnPointName { get; set; } 
    public string TSpawnPointName { get; set; } 
    
    public static string ConfigPath = Path.Combine(Application.RootDirectory, "configs", "HanAirDrop", "HanAirDropMessageCFG.json");

    public static HanAirDropMessageCFG Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                // 反序列化配置
                var config = JsonSerializer.Deserialize<HanAirDropMessageCFG>(json);
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
        var defaultConfig = new HanAirDropMessageCFG
        {
            AdminDropMessage = "Administrator {0} has summoned air drop support!!",
            AirDropMessage = "The air drop supply has been deployed to {0}",
            BlockTeamMessage = "Your team is not allowed to pick up!!",
            BlockRoundGlobalMessage = "You can only pick up {0} supply boxes per round, you have exceeded the limit and cannot pick up!!",
            BlockRoundBoxMessage = "This air drop box can only be picked up {0} times per round, you have exceeded the limit and cannot pick up!!",
            BlockRoundSpawnlMessage = "This air drop box can only be picked up {0} times per life, you have exceeded the limit and cannot pick up!!",
            PlayerPickUpMessage = "Player: {0} has picked up {1} and received {2}!!",
            CtSpawnPointName = "CT Spawn Point",
            TSpawnPointName  = "CT Spawn Point"

        };
        Save(defaultConfig); // 保存默认配置
        return defaultConfig;
    }

    public static void Save(HanAirDropMessageCFG config)
    {
        try
        {
            string directoryPath = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"[HanAirDropMessageCFG] 文件夹 {directoryPath} 不存在，已创建.");
            }

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            Console.WriteLine("[HanAirDropMessageCFG] 配置文件已保存。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HanAirDropMessageCFG] 无法写入配置文件: {ex.Message}");
            Console.WriteLine($"详细错误：{ex.StackTrace}");
        }
    }


}
