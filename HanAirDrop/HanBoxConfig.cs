using CounterStrikeSharp.API.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

public class HanBoxCFG
{
    public class Box
    {
        [JsonPropertyName("name")] //名称 Display name
        public required string Name { get; set; } 

        [JsonPropertyName("Path")] //空投箱模型路径 Model path for airdrop crate
        public required string ModelPath { get; set; }

        [JsonPropertyName("Sound")] //空投时的声音 Sound effect when airdropping
        public required string DropSound { get; set; }
        
        [JsonPropertyName("addItems")] //空投可以投出的道具 Items contained in the airdrop
        public required string Items { get; set; }

        [JsonPropertyName("teamonly")] //队伍限定 Team restriction (0=any, 1=CT, 2=T)
        public int TeamOnly { get; set; } 

        [JsonPropertyName("roundpicklimit")]  //回合拾取限制 Pickup limit per round
        public int RoundPickLimit { get; set; } 

        [JsonPropertyName("spawnpicklimit")] //复活拾取限制 Pickup limit per spawn/respawn
        public int SpawnPickLimit { get; set; } 

        [JsonPropertyName("probability")] //概率  Drop probability (0.0-1.0)
        public float Probability { get; set; }

        [JsonPropertyName("enable")] //是否开启 Whether this airdrop is enabled
        public bool Enabled { get; set; }

        [JsonPropertyName("code")] //唯一代码 Unique identifier code
        public int Code { get; set; }

        [JsonPropertyName("flags")] //管理员/vip flags
        public required string Flags { get; set; }

        [JsonPropertyName("openglow")] //是否开启透视外发光
        public bool OpenGlow { get; set;}

        [JsonPropertyName("glowcolor")] //发光颜色设置 ARGB
        public required string GlowColor { get; set; }

        [JsonPropertyName("modelphysics")] //发光颜色设置 ARGB
        public bool ModelPhysics { get; set; }

    }

    [JsonPropertyName("BoxList")]
    public List<Box> BoxList { get; set; } = new List<Box>();
    
    public static string ConfigPath = Path.Combine(Application.RootDirectory, "configs", "HanAirDrop", "HanBoxCFG.json");

    public static HanBoxCFG Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                // 尝试反序列化配置
                var config = JsonSerializer.Deserialize<HanBoxCFG>(json);
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
        var defaultConfig = new HanBoxCFG
        {
            BoxList = new List<Box>
            {
                new Box
                {
                    Name = "空投测试1",
                    ModelPath = "model/path1",
                    DropSound = "soundevent/sound",
                    Items = "item1,item2",
                    TeamOnly = 1,
                    RoundPickLimit = 0,
                    SpawnPickLimit = 0,
                    Probability = 0.5f,
                    Enabled = true,
                    Code = 1,
                    Flags = "",
                    OpenGlow = true,
                    GlowColor = "255,255,0,0",
                    ModelPhysics = true

                },
                new Box
                {
                    Name = "空投测试2",
                    ModelPath = "model/path1",
                    DropSound = "soundevent/sound",
                    Items = "item3,item4",
                    TeamOnly = 1,
                    RoundPickLimit = 0,
                    SpawnPickLimit = 0,
                    Probability = 0.5f,
                    Enabled = true,
                    Code = 2,
                    Flags = "",
                    OpenGlow = true,
                    GlowColor = "255,255,0,0",
                    ModelPhysics = true
                }
            }
        };
        Save(defaultConfig); // 保存默认配置
        return defaultConfig;
    }

    public static void Save(HanBoxCFG config)
    {
        try
        {
            string directoryPath = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"[HanBoxCFG] 文件夹 {directoryPath} 不存在，已创建.");
            }

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            Console.WriteLine("[HanBoxCFG] 配置文件已保存。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HanBoxCFG] 无法写入配置文件: {ex.Message}");
            Console.WriteLine($"详细错误：{ex.StackTrace}");
        }
    }

    public List<HanBoxCFG.Box> BoxConfig()
    {
        // 从配置文件加载并返回列表
        var config = HanBoxCFG.Load();
        return config.BoxList;
    }

    public Box? GetRandomBox()
    {
        var enabledBoxes = BoxList.Where(b => b.Enabled).ToList();
        if (!enabledBoxes.Any()) return null;

        // 计算总概率
        float totalProb = enabledBoxes.Sum(b => b.Probability);
        float randomPoint = (float)new Random().NextDouble() * totalProb;

        // 按概率选择
        float cumulative = 0f;
        foreach (var box in enabledBoxes)
        {
            cumulative += box.Probability;
            if (randomPoint <= cumulative)
                return box;
        }

        return enabledBoxes.Last(); // 兜底
    }

    // 随机选择空投配置
    // 根据AirDropName筛选并随机选择
    public Box? SelectRandomBoxConfig(string allowedNames)
    {
        //解析允许的空投名称
        var allowedNameList = allowedNames.Split(',')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        // 调试输出允许的名称列表
        //Console.WriteLine($"[调试] 允许的空投名称列表: {string.Join(", ", allowedNameList)}");

        //获取可用的空投配置
        var availableBoxes = BoxList
            .Where(b => b.Enabled && allowedNameList.Contains(b.Name))
            .ToList();

        // 调试输出所有可用的空投配置
        //Console.WriteLine($"[调试] 所有可用的空投配置: {string.Join(", ", availableBoxes.Select(b => b.Name))}");

        if (availableBoxes.Count == 0) 
        {
            Console.WriteLine($"[警告] 没有可用的空投配置。检查以下内容：");
            Console.WriteLine($"- 允许的名称: {allowedNames}");
            Console.WriteLine($"- 所有启用的配置: {string.Join(", ", BoxList.Where(b => b.Enabled).Select(b => b.Name))}");
            return null;
        }

        //按概率随机选择
        float totalProb = availableBoxes.Sum(b => b.Probability);
        float randomPoint = (float)new Random().NextDouble() * totalProb;

        float cumulative = 0f;
        foreach (var box in availableBoxes)
        {
            cumulative += box.Probability;
            if (randomPoint <= cumulative)
                return box;
        }

        return availableBoxes.Last();
    }

    // 按概率选择道具
    public HanItemCFG.Item? SelectByProbability(List<HanItemCFG.Item> items)
    {
        float total = items.Sum(i => i.ItemProbability);
        float randomPoint = (float)new Random().NextDouble() * total;
        
        float cumulative = 0f;
        foreach (var item in items)
        {
            cumulative += item.ItemProbability;
            if (randomPoint <= cumulative)
                return item;
        }

        return items.LastOrDefault();
    }

}
