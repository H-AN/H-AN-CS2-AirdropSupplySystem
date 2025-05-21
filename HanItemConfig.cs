using CounterStrikeSharp.API.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

public class HanItemCFG
{
    public class Item
    {
        [JsonPropertyName("name")] //道具名称 Item display name
        public required string Name { get; set; } 

        [JsonPropertyName("command")] //命令 Console command to execute
        public required string Command { get; set; }

        [JsonPropertyName("picksound")] //拾取声音  Sound when item is picked up
        public required string PickSound { get; set; }

        [JsonPropertyName("itemprobability")] //概率 Drop probability (0.0-1.0)
        public float ItemProbability { get; set; }

        [JsonPropertyName("enable")] //是否开启  Whether this item is enabled
        public bool Enabled { get; set; }

        [JsonPropertyName("permissions")] //道具权限 
        public required string Permissions { get; set; } 

    }

    [JsonPropertyName("ItemList")]
    public List<Item> ItemList { get; set; } = new List<Item>();
    
    public static string ConfigPath = Path.Combine(Application.RootDirectory, "configs", "HanAirDrop", "HanItemCFG.json");

    public static HanItemCFG Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                // 尝试反序列化配置
                var config = JsonSerializer.Deserialize<HanItemCFG>(json);
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
        var defaultConfig = new HanItemCFG
        {
            ItemList = new List<Item>
            {
                new Item
                {
                    Name = "AK-47",
                    Command = "css_ak",
                    PickSound = "",
                    ItemProbability = 0.5f,
                    Enabled = true,
                    Permissions = ""
                },
                new Item
                {
                    Name = "m4-a1",
                    Command = "css_m4",
                    PickSound = "",
                    ItemProbability = 0.3f,
                    Enabled = true,
                    Permissions = ""
                }
            }
        };
        Save(defaultConfig); // 保存默认配置
        return defaultConfig;
    }

    public static void Save(HanItemCFG config)
    {
        try
        {
            string directoryPath = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"[HanItemCFG] 文件夹 {directoryPath} 不存在，已创建.");
            }

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            Console.WriteLine("[HanItemCFG] 配置文件已保存。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HanItemCFG] 无法写入配置文件: {ex.Message}");
            Console.WriteLine($"详细错误：{ex.StackTrace}");
        }
    }


}
