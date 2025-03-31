using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;





namespace HanAirDropPlugin;

public class HanZriotWeapon : BasePlugin
{
    public override string ModuleName => "[华仔]CS2空投补给系统 Airdrop Supply System";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "By : 华仔H-AN";
    public override string ModuleDescription => "空投补给, QQ群107866133, github https://github.com/H-AN";

    public string GetTranslatedText(string name, params object[] args) => Localizer[name, args];



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
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        HookEntityOutput("trigger_multiple", "OnStartTouch", trigger_multiple, HookMode.Pre);

        AddCommand($"{AirDropCFG.AdminCommand}", "createbox", createbox);

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
            
        });
        
    }

    [RequiresPermissions(@"css/slay")]
    public void createbox(CCSPlayerController? client, CommandInfo info)
    {
        if(client == null || !client.IsValid) 
            return;

        CreateDrop();
        client.PrintToChat(Localizer["AdminDropMessage", client.PlayerName]);
    }


    private void OnMapStart(string mapname)
    { 
        if (!AirDropCFG.AirDropEnble || AirDropCFG.AirDropMode == 1)
            return;

        if(MapStartDropTimer != null)
            return;
        
        MapStartDropTimer = AddTimer(AirDropCFG.AirDropTimer, () =>  {CreateDrop();}, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);  
   
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

        var Box = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override")!;
        if (Box == null)
            return;
        
        Box.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
        Box.SetModel($"{boxConfig.ModelPath}");
        Box.DispatchSpawn();

        Box.Entity!.Name = $"华仔空投";
        // 存储到字典
        BoxData[Box] = new AirBoxData 
        {
            Code = boxConfig.Code,
            Items = boxConfig.Items.Split(','),
            TeamOnly = boxConfig.TeamOnly,
            Name = boxConfig.Name,
            DropSound = boxConfig.DropSound,
            RoundPickLimit = boxConfig.RoundPickLimit,
            SpawnPickLimit = boxConfig.SpawnPickLimit
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
        //Server.PrintToChatAll($"空投 {AirDropCFG.AirDropName} , 实体名 {Box.Entity!.Name} 已创建");
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

        var validItems = AirItemCFG.ItemList.Where(item => item.Enabled && data.Items.Contains(item.Name)).ToList();

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

        switch (spawnType)
        {
            case 0: // T + CT 混合
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                    .Select(s => (s, $"{ctspawnposname}")));
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                    .Select(s => (s, $"{tspawnposname}")));
                break;
            
            case 1: // 仅 CT
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                    .Select(s => (s, $"{ctspawnposname}")));
                break;
            
            case 2: // 仅 T
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                    .Select(s => (s, $"{tspawnposname}")));
                break;
            
            default: // 默认混合
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                    .Select(s => (s, $"{ctspawnposname}")));
                spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                    .Select(s => (s, $"{tspawnposname}")));
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

}