using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using System.Drawing;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static HanBoxCFG;






namespace HanAirDropPlugin;

public class HanAirDropPlugin : BasePlugin
{
    public override string ModuleName => "[华仔]CS2空投补给系统 Airdrop Supply System";
    public override string ModuleVersion => "1.5.1";
    public override string ModuleAuthor => "By : 华仔H-AN";
    public override string ModuleDescription => "空投补给, QQ群107866133, github https://github.com/H-AN";

    public string GetTranslatedText(string name, params object[] args) => Localizer[name, args];

    private readonly Dictionary<ulong, DateTime> AdminCreateBoxCooldown = new();

    public string physicsBox = "models/de_overpass/junk/cardboard_box/cardboard_box_4.vmdl_c";



    HanAirDropCFG AirDropCFG = HanAirDropCFG.Load();
    HanBoxCFG AirBoxCFG = HanBoxCFG.Load();
    HanItemCFG AirItemCFG = HanItemCFG.Load();

    //HanAirDropMessageCFG AirDropMessageCFG = HanAirDropMessageCFG.Load();


    private CounterStrikeSharp.API.Modules.Timers.Timer? MapStartDropTimer { get; set; } = null;

    private int [] PlayerPickUpLimit = new int[65];

    // 使用字典存储每个箱子的限制
    private Dictionary<int, Dictionary<int, int>> PlayerRoundPickUpLimit = new(); // [玩家Slot][箱子Code] = 剩余次数
    private Dictionary<int, Dictionary<int, int>> PlayerSpawnPickUpLimit = new(); // [玩家Slot][箱子Code] = 剩余次数
    
    public Dictionary<CEntityInstance, CBaseProp> BoxTriggers = new();
    // 用于存储实体对应的数据
    public Dictionary<CBaseProp, AirBoxData> BoxData = new();

    public class AirBoxData
    {
        public int Code { get; set; }
        public string[] Items { get; set; }
        public int TeamOnly { get; set; }
        public string Name { get; set; }
        public string DropSound { get; set; }
        public int RoundPickLimit { get; set; }
        public int SpawnPickLimit { get; set; }
        public string Flags { get; set; }
        public bool OpenGlow { get; set; }

        //public bool Modelphysics { get; set; }
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        //RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        RegisterEventHandler<EventRoundStart>(OnRoundStartTimer);

        HookEntityOutput("trigger_multiple", "OnStartTouch", trigger_multiple, HookMode.Pre);

        AddCommand($"{AirDropCFG.AdminCommand}", "createbox", createbox);
        AddCommand($"{AirDropCFG.AdminSelectBoxCommand}", "createbox2", createbox2);

        RegisterListener<Listeners.OnServerPrecacheResources>((manifest) => 
        {
            List<HanBoxCFG.Box> BoxList = AirBoxCFG.BoxConfig();
            foreach (var Box in BoxList)
            {
                manifest.AddResource(Box.ModelPath);    //预缓存
            }
            if (!string.IsNullOrEmpty(AirDropCFG.PrecacheSoundEvent))
            {
                foreach (var sound in AirDropCFG.PrecacheSoundEvent.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)))
                {
                    manifest.AddResource(sound);
                }
            }
            manifest.AddResource(physicsBox);    //预缓存通用物理模型
        });
        
    }

    [RequiresPermissions(@"css/slay")]
    public void createbox(CCSPlayerController? client, CommandInfo info)
    {
        if(client == null || !client.IsValid) 
            return;

        if (!HasPermission(client, AirDropCFG.AdminCommandFlags))
        {
            string FlagsName = $"{AirDropCFG.AdminCommandFlags}";
            client.PrintToChat(Localizer["AdminCreateRandomBox", FlagsName]); //需要的权限提示
            return;
        }

        CreateDrop();
        client.PrintToChat(Localizer["AdminDropMessage", client.PlayerName]);
    }

    [RequiresPermissions(@"css/slay")]
    public void createbox2(CCSPlayerController? client, CommandInfo info)
    {
        if (client == null || !client.IsValid || !client.PawnIsAlive)
            return;

        var steamId = client.SteamID;

        if (!HasPermission(client, AirDropCFG.AdminSelectBoxCommandFlags))
        {
            string FlagsName = $"{AirDropCFG.AdminSelectBoxCommandFlags}";
            client.PrintToChat(Localizer["AdminSelectBoxFlags", FlagsName]); //需要的权限提示
            return;
        }

        // 冷却限制
        if (AdminCreateBoxCooldown.TryGetValue(steamId, out var lastTime))
        {
            double secondsSince = (DateTime.Now - lastTime).TotalSeconds;
            if (secondsSince < AirDropCFG.AdminSelectBoxColdCown)
            {
                client.PrintToChat(Localizer["AdminSelectBoxColdCown", AirDropCFG.AdminSelectBoxColdCown]);
                return;
            }
        }

        AdminCreateBoxCooldown[steamId] = DateTime.Now;

        if (info.ArgCount < 3)
        {
            client.PrintToChat(Localizer["AdminSelectBoxError"]); //用法: !createbox 空投名 次数
            return;
        }

        string boxName = info.ArgByIndex(1);
        if (!int.TryParse(info.ArgByIndex(2), out int count) || count <= 0)
        {
            client.PrintToChat(Localizer["AdminSelectBoxError2"]); //请输入有效的次数（正整数）
            return;
        }

        if (count > AirDropCFG.AdminSelectBoxCount)
        {
            client.PrintToChat(Localizer["AdminSelectBoxCount", AirDropCFG.AdminSelectBoxCount]); //请输入有效的次数（正整数）
            return;
        }

        // 查找配置中是否存在该空投名
        var boxConfig = AirBoxCFG.BoxList.FirstOrDefault(b => b.Enabled && b.Name == boxName);
        if (boxConfig == null)
        {
            client.PrintToChat(Localizer["AdminSelectBoxError3", boxName]); //$"找不到名为 [{boxName}] 的空投配置，或该配置未启用。
            return;
        }

        // 获取管理员位置
        //Vector startPosition = client.Pawn.Value.AbsOrigin;
        Vector spawnPos = GetForwardPosition(client, 120f);

        for (int i = 0; i < count; i++)
        {
            //每个空投间隔 80单位
            Vector dropPos = new Vector(spawnPos.X + (i * 50), spawnPos.Y, spawnPos.Z);
            CreateAirDropAtPosition(boxConfig, dropPos);
        }
        string BoxNameMessage = $"{boxName}";
        string BoxCountMessage = $"{count}";
        Server.PrintToChatAll(Localizer["AdminSelectBoxCreated", client.PlayerName, BoxNameMessage, BoxCountMessage]); ////已创建 {count} 个空投 [{boxName}]。
    }

    public static Vector GetForwardPosition(CCSPlayerController player, float distance = 100f)
    {
        if (player == null || player.Pawn == null || player.PlayerPawn == null)
            return new Vector(0, 0, 0); // fallback

        // 克隆原始位置和朝向，避免引用原始结构造成副作用
        Vector origin = new Vector(
            player.Pawn.Value.AbsOrigin.X,
            player.Pawn.Value.AbsOrigin.Y,
            player.Pawn.Value.AbsOrigin.Z
        );

        QAngle angle = new QAngle(
            player.PlayerPawn.Value.EyeAngles.X,
            player.PlayerPawn.Value.EyeAngles.Y,
            player.PlayerPawn.Value.EyeAngles.Z
        );

        // 根据 Yaw（水平旋转角）计算前方向量
        float yaw = angle.Y * MathF.PI / 180f;
        Vector forward = new Vector(MathF.Cos(yaw), MathF.Sin(yaw), 0);

        // 计算前方目标点（适当提高 Z 高度避免地面卡住）
        Vector target = origin + forward * distance;
        target.Z += 10f;

        return target;
    }

    public void CreateAirDropAtPosition(Box config, Vector position)
    {
        if(config.ModelPhysics)
        {
            var Box = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");
            if (Box == null) return;

            Box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            Box.SetModel(config.ModelPath);
            Box.DispatchSpawn();

            string propName = "华仔空投_" + new Random().Next(1000000, 9999999).ToString();
            Box.Entity!.Name = propName;

            BoxData[Box] = new AirBoxData
            {
                Code = config.Code,
                Items = config.Items.Split(','),
                TeamOnly = config.TeamOnly,
                Name = config.Name,
                DropSound = config.DropSound,
                RoundPickLimit = config.RoundPickLimit,
                SpawnPickLimit = config.SpawnPickLimit,
                Flags = config.Flags,
                OpenGlow = config.OpenGlow,
                //Modelphysics = config.ModelPhysics
            };

            Box.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Utilities.SetStateChanged(Box, "CCollisionProperty", "m_CollisionGroup");
            Box.Teleport(position);

            var trigger = CreateTrigger(Box);
            BoxTriggers.Add(trigger, Box);

            if (!string.IsNullOrEmpty(config.DropSound))
            {
                var world = Utilities.GetEntityFromIndex<CBaseEntity>(0);
                world?.EmitSound(config.DropSound);
            }

            AddTimer(AirDropCFG.AirDropKillTimer, () => BoxSelfKill(trigger, Box), TimerFlags.STOP_ON_MAPCHANGE);

            if (BoxData[Box].OpenGlow)
            {
                Color defaultColor = Color.FromArgb(255, 255, 0, 0);
                HanAirDropGlow.TryParseColor(config.GlowColor, out var glowColor, defaultColor);
                HanAirDropGlow.SetGlow(Box, glowColor.A, glowColor.R, glowColor.G, glowColor.B);
            }
        }
        else
        {
            var Box = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");
            if (Box == null) return;

            Box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            Box.SetModel(physicsBox); //config.ModelPath
            Box.DispatchSpawn();

            string propName = "华仔空投_" + new Random().Next(1000000, 9999999).ToString();
            Box.Entity!.Name = propName;

            BoxData[Box] = new AirBoxData
            {
                Code = config.Code,
                Items = config.Items.Split(','),
                TeamOnly = config.TeamOnly,
                Name = config.Name,
                DropSound = config.DropSound,
                RoundPickLimit = config.RoundPickLimit,
                SpawnPickLimit = config.SpawnPickLimit,
                Flags = config.Flags,
                OpenGlow = config.OpenGlow,
                //Modelphysics = config.ModelPhysics
            };

            Box.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Utilities.SetStateChanged(Box, "CCollisionProperty", "m_CollisionGroup");
            Box.Teleport(position);

            var trigger = CreateTrigger(Box);
            BoxTriggers.Add(trigger, Box);

            if (!string.IsNullOrEmpty(config.DropSound))
            {
                var world = Utilities.GetEntityFromIndex<CBaseEntity>(0);
                world?.EmitSound(config.DropSound);
            }

            AddTimer(AirDropCFG.AirDropKillTimer, () => BoxSelfKill(trigger, Box), TimerFlags.STOP_ON_MAPCHANGE);


            string newColor = BoxData[Box].OpenGlow ? config.GlowColor : "0,0,0,0";
            var cloneprop = CreateClone(Box, config.ModelPath, propName, newColor);

        }
    }


    private void OnMapStart(string mapname)
    { 
        if (!AirDropCFG.AirDropEnble || AirDropCFG.AirDropMode == 1)
            return;

        if(MapStartDropTimer != null)
            return;
        
        MapStartDropTimer = AddTimer(AirDropCFG.AirDropTimer, () =>  {CreateDrop();}, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);  
   
    }

    private HookResult OnRoundStartTimer(EventRoundStart @event, GameEventInfo info)
    {
        if (!AirDropCFG.AirDropEnble || AirDropCFG.AirDropMode == 1)
            return HookResult.Continue;

        int code = 0;
        if (AirDropCFG.Openrandomspawn == 0)
        {
            code = 0;
        }
        else
        {
            code = 1;
        }
        Server.ExecuteCommand($"mp_randomspawn {code}");

        MapStartDropTimer?.Kill();
        MapStartDropTimer = null;
        MapStartDropTimer = AddTimer(AirDropCFG.AirDropTimer, () => { CreateDrop(); }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    public void CreateDrop()
    {
        //检查玩家数量
        int playerCount = GetPlayerCount();
        if(playerCount <= 0)
            return;

        //计算要生成的空投数量
        int count = CalculateDropCount(playerCount);
        if(count <= 0)
            return;


        //生成空投
        for (int i = 0; i < count; i++)
        {
            var spawn = GetRandomSpawnPosition(AirDropCFG.AirDropPosMode);
            if (spawn != null)
            {
                CreateAirDrop(spawn.Value.Position);
                Server.PrintToChatAll(Localizer["AirDropMessage", spawn.Value.SpawnType]);
                

            }
        }

    }


    private int CalculateDropCount(int playerCount)
    {
        switch(AirDropCFG.AirDropSpawnMode)
        {
            case 0: // 固定数量模式
                return Math.Max(0, AirDropCFG.AirDropCount);
            
            case 1: // 动态数量模式
                if(AirDropCFG.AirDropPlayerCount <= 0 || AirDropCFG.AirDropDynamicCount <= 0)
                    return 0;
                    
                return Math.Max(1, playerCount / AirDropCFG.AirDropPlayerCount * AirDropCFG.AirDropDynamicCount);
            
            default:
                return 0;
        }
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var client = @event.Userid;
        if(client == null || !client.IsValid)
            return HookResult.Continue;

        var Playerpawn = client.PlayerPawn.Value;
        if(Playerpawn == null || !Playerpawn.IsValid)
            return HookResult.Continue;

        if(client.TeamNum != 2)
            return HookResult.Continue;

        if(!AirDropCFG.AirDropEnble || AirDropCFG.AirDropMode == 0)
            return HookResult.Continue;

        Vector DropPos = Playerpawn.AbsOrigin;
        if(new Random().NextDouble() <= AirDropCFG.DeathDropPercent)
        {
            if(DropPos != null)
            {
                CreateAirDrop(DropPos);
            }
        }
        return HookResult.Continue;

    }


    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var client = @event.Userid;
        if(client == null || !client.IsValid)
        return HookResult.Continue;

        int slot = client.Slot;

        foreach(var box in AirBoxCFG.BoxList.Where(b => b.SpawnPickLimit > 0))
        {
            if(!PlayerSpawnPickUpLimit.ContainsKey(slot))
                PlayerSpawnPickUpLimit[slot] = new();
            
            PlayerSpawnPickUpLimit[slot][box.Code] = box.SpawnPickLimit;
        }

        return HookResult.Continue;

    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    { 
        BoxTriggers.Clear();

        var Human = Utilities.GetPlayers().Where(client => client.TeamNum == 3 && client.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE);
        foreach (var client in Human)
        {
            int slot = client.Slot;
            if(AirDropCFG.PlayerPickEachRound > 0)
            {
                PlayerPickUpLimit[client.Slot] = AirDropCFG.PlayerPickEachRound;
            }
            foreach(var box in AirBoxCFG.BoxList.Where(b => b.RoundPickLimit > 0))
            {
                if(!PlayerRoundPickUpLimit.ContainsKey(slot))
                    PlayerRoundPickUpLimit[slot] = new();
                
                PlayerRoundPickUpLimit[slot][box.Code] = box.RoundPickLimit;
            }

        }
         
        
        return HookResult.Continue;
    }


    public void CreateAirDrop(Vector Position) 
    {
        // 从HanAirDropCFG获取允许的空投名称，并随机选择配置
        var boxConfig = AirBoxCFG.SelectRandomBoxConfig(AirDropCFG.AirDropName);
        if (boxConfig == null) 
        {
            Console.WriteLine("[华仔空投]无可用空投配置！检查AirDropName和Enabled状态");
            return;
        }

        if (boxConfig.ModelPhysics)
        {
            var Box = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override")!;
            if (Box == null)
                return;
        
            Box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            Box.SetModel($"{boxConfig.ModelPath}");
            Box.DispatchSpawn();

            string propName = "华仔空投_" + new Random().Next(1000000, 9999999).ToString();
            Box.Entity!.Name = propName;
            // 存储到字典
            BoxData[Box] = new AirBoxData 
            {
                Code = boxConfig.Code,
                Items = boxConfig.Items.Split(','),
                TeamOnly = boxConfig.TeamOnly,
                Name = boxConfig.Name,
                DropSound = boxConfig.DropSound,
                RoundPickLimit = boxConfig.RoundPickLimit,
                SpawnPickLimit = boxConfig.SpawnPickLimit,
                Flags = boxConfig.Flags,
                OpenGlow = boxConfig.OpenGlow
            };

            Box.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Box.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Utilities.SetStateChanged(Box, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(Box, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
            Vector SafePosition = new Vector(Position.X, Position.Y, Position.Z);
            Box.Teleport(SafePosition);//, Rotation, new Vector(0, 0, 0)
            var trigger = CreateTrigger(Box);
            BoxTriggers.Add(trigger, Box); // 关键：建立触发器与实体的关联
            if (!string.IsNullOrEmpty(boxConfig.DropSound))
            {
                var worldEntity = Utilities.GetEntityFromIndex<CBaseEntity>(0);
                if (worldEntity is null || worldEntity.IsValid is not true|| worldEntity.DesignerName.Contains("world") is not true) 
                    return;
                worldEntity.EmitSound($"{boxConfig.DropSound}");
            }
            AddTimer(AirDropCFG.AirDropKillTimer, () =>  {BoxSelfKill(trigger, Box);},TimerFlags.STOP_ON_MAPCHANGE);

            if (BoxData[Box].OpenGlow)
            {
                // 定义默认颜色
                Color defaultGlowColor = Color.FromArgb(255, 255, 0, 0);
                HanAirDropGlow.TryParseColor(boxConfig.GlowColor, out var glowColor, defaultGlowColor);
                HanAirDropGlow.SetGlow(Box, glowColor.A, glowColor.R, glowColor.G, glowColor.B);
            }
                //Server.PrintToChatAll($"空投 {AirDropCFG.AirDropName} , 实体名 {Box.Entity!.Name} 已创建");

        }
        else
        {

            var Box = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override")!;
            if (Box == null)
                return;

            Box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            Box.SetModel(physicsBox); //config.ModelPath
            //Box.SetModel($"{boxConfig.ModelPath}");
            Box.DispatchSpawn();

            string propName = "华仔空投_" + new Random().Next(1000000, 9999999).ToString();
            Box.Entity!.Name = propName;
            // 存储到字典
            BoxData[Box] = new AirBoxData
            {
                Code = boxConfig.Code,
                Items = boxConfig.Items.Split(','),
                TeamOnly = boxConfig.TeamOnly,
                Name = boxConfig.Name,
                DropSound = boxConfig.DropSound,
                RoundPickLimit = boxConfig.RoundPickLimit,
                SpawnPickLimit = boxConfig.SpawnPickLimit,
                Flags = boxConfig.Flags,
                OpenGlow = boxConfig.OpenGlow
            };

            Box.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Box.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            Utilities.SetStateChanged(Box, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(Box, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
            Vector SafePosition = new Vector(Position.X, Position.Y, Position.Z);
            Box.Teleport(SafePosition);//, Rotation, new Vector(0, 0, 0)
            var trigger = CreateTrigger(Box);
            BoxTriggers.Add(trigger, Box); // 关键：建立触发器与实体的关联
            if (!string.IsNullOrEmpty(boxConfig.DropSound))
            {
                var worldEntity = Utilities.GetEntityFromIndex<CBaseEntity>(0);
                if (worldEntity is null || worldEntity.IsValid is not true || worldEntity.DesignerName.Contains("world") is not true)
                    return;
                worldEntity.EmitSound($"{boxConfig.DropSound}");
            }
            AddTimer(AirDropCFG.AirDropKillTimer, () => { BoxSelfKill(trigger, Box); }, TimerFlags.STOP_ON_MAPCHANGE);

            string newColor = BoxData[Box].OpenGlow ? boxConfig.GlowColor : "0,0,0,0";
            var cloneprop = CreateClone(Box, boxConfig.ModelPath, propName, newColor);

        }
    }


    public void BoxTouch(CCSPlayerController client, CEntityInstance trigger, CBaseProp entity)
    {
        if (client?.PlayerPawn?.Value == null || !client.IsValid)
            return;

        // 通过触发器获取实体
        if (!BoxTriggers.TryGetValue(trigger, out var box) || !BoxData.TryGetValue(box, out var data))
        {
            Console.WriteLine("[华仔空投] 找不到对应的空投数据");
            return;
        }
        bool canPick = data.TeamOnly == 0 ? true :data.TeamOnly == 1 ? client.TeamNum == 3 : data.TeamOnly == 2 ? client.TeamNum == 2 : false;
        if (!canPick) 
        {
            client.PrintToChat(Localizer["BlockTeamMessage"]);
            return;
        }
        if(AirDropCFG.PlayerPickEachRound > 0 && PlayerPickUpLimit[client.Slot] == 0)
        {

            client.PrintToChat(Localizer["BlockRoundGlobalMessage"]);
            return;
        }
        if(data.RoundPickLimit > 0)
        {
            if(!PlayerRoundPickUpLimit.TryGetValue(client.Slot, out var roundLimits) || !roundLimits.TryGetValue(data.Code, out var remaining) || remaining <= 0)
            {
                client.PrintToChat(Localizer["BlockRoundBoxMessage"]);
                return;
            }
        }
        if(data.SpawnPickLimit > 0)
        {
            if(!PlayerSpawnPickUpLimit.TryGetValue(client.Slot, out var spawnLimits) || !spawnLimits.TryGetValue(data.Code, out var remaining) || remaining <= 0)
            {
                client.PrintToChat(Localizer["BlockSpawnMessage"]);
                return;
            }
        }
        if (!HasPermission(client, data.Flags))
        {
            string FlagsName = $"{data.Flags}";
            client.PrintToChat(Localizer["BlockFlagsMessage", FlagsName]);
            return;
        }

        BoxData.Remove(box); //清理数据
        BoxTriggers.Remove(trigger);
        if (trigger.IsValid)
        {
            trigger.Remove();
        } 
        if (box.IsValid) 
        {
            box.Remove();
        }
        //检查实体
        if (entity != null && entity.IsValid)
        {
            entity.Remove(); // 安全删除
        }
        //字典清理（二次确认）
        if (trigger != null && BoxTriggers.ContainsKey(trigger))
        {
            BoxTriggers.Remove(trigger);
        }
        if(AirDropCFG.PlayerPickEachRound > 0 && PlayerPickUpLimit[client.Slot] > 0)
        {
            PlayerPickUpLimit[client.Slot]--;
        }
        // 减少箱子回合拾取次数
        if(data.RoundPickLimit > 0)
        {
            PlayerRoundPickUpLimit[client.Slot][data.Code]--;
        }
        // 减少箱子每条命拾取次数
        if(data.SpawnPickLimit > 0)
        {
            PlayerSpawnPickUpLimit[client.Slot][data.Code]--;
        }

        // 道具发放
        if (data.Items.Length == 0)
        {
            Console.WriteLine("[华仔空投] 警告 空投道具池为空");
            return;
        }

        //var validItems = AirItemCFG.ItemList.Where(item => item.Enabled && data.Items.Contains(item.Name)).ToList();
        var validItems = AirItemCFG.ItemList
        .Where(item => item.Enabled && data.Items.Contains(item.Name))
        .Where(item => HasPermission(client, item.Permissions))
        .ToList();

        // 玩家没有任何可用权限的道具，给出提示
        if (validItems.Count == 0)
        {
            client.PrintToChat(Localizer["NoPermissionToItems"]); // 提示语言
            //Console.WriteLine($"[华仔空投] 玩家 {client.PlayerName} 无法拾取箱子 {data.Name} 中的任何道具（权限不足）");
            return;
        }

        var chosenItem = AirBoxCFG.SelectByProbability(validItems);
        if (chosenItem == null) 
        {
            return;
        }

        client.ExecuteClientCommandFromServer(chosenItem.Command); //给予空投命令

        Server.PrintToChatAll(Localizer["PlayerPickUpMessage", client.PlayerName, data.Name, chosenItem.Name]);

        if (!string.IsNullOrEmpty(chosenItem.PickSound))
        {
            client.EmitSound($"{chosenItem.PickSound}");
        }

    }
    
    public void BoxSelfKill(CEntityInstance trigger, CBaseProp entity)
    {
        if (trigger != null && trigger.IsValid)
        {
            //清理触发器与实体的关联
            if (BoxTriggers.ContainsKey(trigger))
            {
                // 通过触发器获取关联的箱子（二次获取）
                var linkedBox = BoxTriggers[trigger];
                // 清理箱子数据（如果存在）
                if (BoxData.ContainsKey(linkedBox))
                {
                    BoxData.Remove(linkedBox);
                }
                // 移除触发器关联
                BoxTriggers.Remove(trigger);
            }
            
            //销毁触发器实体
            trigger.Remove();
        }
        if (entity != null && entity.IsValid)
        {
            //清理实体数据存储
            if (BoxData.ContainsKey(entity))
            {
                BoxData.Remove(entity);
            }
            
            //销毁物理实体
            entity.Remove();
        }
        // 处理可能存在的残留映射
        if (trigger != null)
        {
            BoxTriggers.Remove(trigger); // 无论是否存在都移除
        }
        
        if (entity != null)
        {
            BoxData.Remove(entity); // 无论是否存在都移除
        }
    }

    HookResult trigger_multiple(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        if (activator.DesignerName != "player")
            return HookResult.Continue;

        var pawn = activator.As<CCSPlayerPawn>();
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var client = pawn.OriginalController?.Value?.As<CCSPlayerController>();
        if (client == null || client.IsBot)
            return HookResult.Continue;

        
        // 通过字典获取对应的箱子实体
        if (BoxTriggers.TryGetValue(caller, out var box))
        {
            BoxTouch(client, caller, box);
            BoxTriggers.Remove(caller); // 从字典中移除
        }

        return HookResult.Continue;
    }

    CTriggerMultiple CreateTrigger(CBaseProp pack)
    {
        var trigger = Utilities.CreateEntityByName<CTriggerMultiple>("trigger_multiple")!;

        trigger.Entity!.Name = pack.Entity!.Name + "_trigger";
        trigger.Spawnflags = 1;
        trigger.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
        trigger.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        trigger.Collision.SolidFlags = 0;
        trigger.Collision.CollisionGroup = 14;

        trigger.SetModel(pack.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
        trigger.DispatchSpawn();
        trigger.Teleport(pack.AbsOrigin, pack.AbsRotation);
        trigger.AcceptInput("FollowEntity", pack, trigger, "!activator");
        trigger.AcceptInput("Enable");

        return trigger;
    }

    public (Vector Position, string SpawnType)? GetRandomSpawnPosition(int spawnType)
    {
        List<(SpawnPoint spawn, string type)> spawnPoints = new List<(SpawnPoint, string)>();

        string ctspawnposname = Localizer["CtSpawnPointName"];
        string tspawnposname = Localizer["TSpawnPointName"];
        string dspawnposname = Localizer["DSpawnPointName"];

        switch (spawnType)
        {
            case 0: // T + CT + 死亡竞赛复活点 混合
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                    .Select(s => (s, $"{ctspawnposname}")));
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                    .Select(s => (s, $"{tspawnposname}")));
                if (AirDropCFG.Openrandomspawn != 0)
                {
                    spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_deathmatch_spawn")
                    .Select(s => (s, $"{dspawnposname}")));
                }
                break;
            
            case 1: // 仅 CT
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                    .Select(s => (s, $"{ctspawnposname}")));
                break;
            
            case 2: // 仅 T
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                    .Select(s => (s, $"{tspawnposname}")));
                break;
            case 3: // 仅 死亡竞赛复活点
                if (AirDropCFG.Openrandomspawn != 0)
                {
                    spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_deathmatch_spawn")
                    .Select(s => (s, $"{dspawnposname}")));
                }
                else
                {
                    spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                                        .Select(s => (s, $"{ctspawnposname}")));
                    spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                        .Select(s => (s, $"{tspawnposname}")));
                }
                break;
            case 4: // 仅 CT + T
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                    .Select(s => (s, $"{ctspawnposname}")));
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                    .Select(s => (s, $"{tspawnposname}")));
                break;
            case 5: // 仅 CT + 死亡竞赛复活点
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                    .Select(s => (s, $"{ctspawnposname}")));
                if (AirDropCFG.Openrandomspawn != 0)
                {
                    spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_deathmatch_spawn")
                    .Select(s => (s, $"{dspawnposname}")));
                }
                break;
            case 6: // 仅 T + 死亡竞赛复活点
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                    .Select(s => (s, $"{ctspawnposname}")));
                if (AirDropCFG.Openrandomspawn != 0)
                {
                    spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_deathmatch_spawn")
                    .Select(s => (s, $"{dspawnposname}")));
                }
                break;

            default: // 全部默认混合
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                    .Select(s => (s, $"{ctspawnposname}")));
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                    .Select(s => (s, $"{tspawnposname}")));
                if (AirDropCFG.Openrandomspawn != 0)
                {
                    spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_deathmatch_spawn")
                    .Select(s => (s, $"{dspawnposname}")));
                }
                break;
        }

        if (!spawnPoints.Any())
            return null;

        var (randomSpawn, spawnTypeName) = spawnPoints[new Random().Next(spawnPoints.Count)];
        var position = randomSpawn.AbsOrigin;
        
        return position == null 
            ? null 
            : (new Vector(position.X, position.Y, position.Z), spawnTypeName);
    }

    public static int GetPlayerCount() 
    {
        return Utilities.GetPlayers().Count(player => player != null && player.IsValid && !player.IsBot);
    }

    private bool HasPermission(CCSPlayerController client, string permissionString)
    {
        if (string.IsNullOrWhiteSpace(permissionString))
            return true; // 无任何权限要求，默认拾取

        var flags = permissionString
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return flags.Any(flag => AdminManager.PlayerHasPermissions(client, flag));
    }

    public CDynamicProp? CreateClone(CPhysicsPropOverride prop, string model, string propName, string glowcolor) // , string glowcolor
    {
        if (string.IsNullOrEmpty(model))
        {
            return null;
        }

        CDynamicProp? clone = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (clone == null || clone.Entity == null || clone.Entity.Handle == IntPtr.Zero || !clone.IsValid)
        {
            return null;
        }

        clone.Entity.Name = propName + "_clone";
        clone.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= unchecked((uint)~(1 << 2));

        clone.SetModel(model);
        
        clone.DispatchSpawn();
        clone.Teleport(prop.AbsOrigin, prop.AbsRotation, null);
        clone.UseAnimGraph = false;

        clone.AcceptInput("FollowEntity", prop, prop, propName); //


        clone.Render = Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(clone, "CBaseModelEntity", "m_clrRender");

        Color defaultGlowColor = Color.FromArgb(255, 255, 0, 0);
        HanAirDropGlow.TryParseColor(glowcolor, out var glowColor, defaultGlowColor);
        HanAirDropGlow.SetGlow(clone, glowColor.A, glowColor.R, glowColor.G, glowColor.B);

        SetPropInvisible(prop);

        return clone;
    }

    public void SetPropInvisible(CPhysicsPropOverride entity)
    {
        if (entity == null || !entity.IsValid)
        {
            return;
        }

        entity.Render = Color.FromArgb(0, 255, 255, 255);
        Utilities.SetStateChanged(entity, "CBaseModelEntity", "m_clrRender");
    }

}
