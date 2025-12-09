# [华仔]CS2空投补给系统 Airdrop Supply System
创建空投补给，多配置适用于任何模式，通过命令结合空投系统 给予玩家道具状态等任何东西. 

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z31PY52N)

https://www.youtube.com/watch?v=yVuhYGvRtDA

https://www.bilibili.com/video/BV1wJZ6YvENd

Create airdrop supplies with multiple configurations suitable for any game mode. Through commands integrated with the airdrop system, players can receive weapons, items, status effects, and more.

/* 基础设置 / Basic Settings */

AirDropEnble = true,            // 是否启用空投系统 / Enable the airdrop system

AirDropMode = 0,                // 空投模式：0=定时生成 1=玩家死亡生成 2=两种模式 / Drop mode: 0=Time-based 1=Death-drop 2=Both

AirDropPosMode = 0,             // 生成位置：0=随机CT/T出生点 1=仅CT 2=仅T / Spawn position: 0=Random CT/T 1=CT only 2=T only

/* 概率与计时 / Probability & Timing */

DeathDropPercent = 0.1f,        // 玩家死亡掉落概率(0.1=10%) / Death drop chance (0.0-1.0)

AirDropTimer = 60.0f,           // 定时空投间隔(秒) / Time-based spawn interval (seconds)

AirDropKillTimer = 20.0f,       // 空投存在时间(秒) / Airdrop lifetime (seconds)

/* 物品设置 / Item Settings */ 

AirDropName = "空投1,空投2",     // 可生成的空投类型(用逗号分隔) / Airdrop types (comma-separated)

PlayerPickEachRound = 0,        // 每回合拾取限制(0=无限制) / Pickup limit per round (0=unlimited)


/* 生成数量控制 / Spawn Quantity Control */

AirDropSpawnMode = 0,           // 生成模式：0=固定数量 1=动态数量 / Spawn mode: 0=Fixed 1=Dynamic

AirDropCount = 3,               // 固定模式生成数量 / Fixed spawn quantity

AirDropDynamicCount = 1,        // 动态模式基数 / Dynamic mode multiplier

AirDropPlayerCount = 1,         // 每N个玩家生成1个 / Players per drop (dynamic mode)

/* 其他设置 / Miscellaneous */

PrecacheSoundEvent = "",        // 自定义音效(留空使用默认) / Custom sound (empty for default)

AdminCommand = "css_createbox"  // 管理员召唤指令 / Admin summon command

详细说明 / Detailed Explanation

空投模式 (AirDropMode)

0 定时生成：每 [AirDropTimer] 秒自动生成空投

0 Time-based: Auto-spawn every [AirDropTimer] seconds

1 死亡掉落：玩家死亡时有 [DeathDropPercent] 概率生成

1 Death-drop: [DeathDropPercent] chance to spawn on player death

2 混合模式：同时启用以上两种方式

2 Both modes: Enable both methods simultaneously

动态生成公式 (Dynamic Spawn Formula)

当 AirDropSpawnMode = 1 时：

When AirDropSpawnMode = 1:

生成数量 = (当前玩家数 / AirDropPlayerCount) * AirDropDynamicCount  

Spawn count = (Online players / AirDropPlayerCount) * AirDropDynamicCount

示例/Example: 10玩家, AirDropPlayerCount=2, AirDropDynamicCount=1 → 生成5个空投 → 5 airdrops

管理员指令 (Admin Command)

使用 !createbox 或控制台输入 css_createbox 手动召唤空投

Use !createbox or console command css_createbox to summon manually

/* 空投箱基础设置 / Box Basic Settings */

Name = "空投测试1",               // 箱子显示名称 / Box display name

ModelPath = "model/path1",       // 模型路径 /  model path

DropSound = "soundevent/sound",  // 掉落音效 / Drop sound effect

/* 物品设置 / Item Settings */

Items = "item1,item2",          // 包含物品(逗号分隔) / Contained items (comma-separated)

Probability = 0.5f,              // 出现概率(50%) / Spawn probability (0.0-1.0)

/* 限制条件 / Restrictions */

TeamOnly = 1,                    // 队伍限制: 1=CT 2=T 0=无 / Team restriction (1=CT 2=T 0=Any)

RoundPickLimit = 0,              // 每回合拾取限制(0=无限制) / Pickups per round (0=unlimited)

SpawnPickLimit = 0,              // 每次复活拾取限制 / Pickups per spawn

/* 系统设置 / System */
Enabled = true,                  // 是否启用 / Enable this box

Code = 1                         // 唯一识别码 / Unique identifier

配置说明 / Configuration Guide

物品字段 (Items Field)

格式："物品1,物品2,物品3"

Format: "item1,item2,item3"

系统会随机选择其中一个物品生成

System will randomly select one item to spawn

队伍限制 (TeamOnly)

值/Value	说明/Description

0	所有队伍可以拾取 / All teams

1	仅反恐精英(CT) / CT only

2	仅恐怖分子(T) / T only

概率计算 (Probability)

0.5 = 50% 生成几率

0.5 = 50% spawn chance

多个箱子时概率相互独立

Probabilities are independent between boxes

最佳实践 (Best Practice)

每个Code必须唯一

Each Code must be unique

禁用未使用的箱子(Enabled=false)

Disable unused boxes (Enabled=false)

/* 道具基础设置 / Item Basic Settings */

ItemList = new List<Item>

{

  new Item

  {
  
    Name = "AK-47",               // 显示名称 / Display name
    
    Command = "css_ak",           // 隐藏命令代码（见下方说明） / Hidden command code (see details)
    
    PickSound = "",               // 拾取音效（留空使用默认） / Pickup sound (empty for default)
    
    /* 概率设置 / Probability */
    
    ItemProbability = 0.5f,       // 出现概率(50%) / Spawn probability (0.0-1.0)
    
    Enabled = true                // 是否启用 / Enable this item
    
  },
  
  new Item
  
  {
  
    Name = "m4-a1",
    
    Command = "css_m4",          // 必须与服务器命令一致 / Must match server command
    
    PickSound = "",
    
    ItemProbability = 0.3f,       // 30%概率 / 30% chance
    
    Enabled = true
    
  }

Command 字段详解 / Command Field Explanation

功能说明 / Function

当玩家拾取空投时，服务器会自动执行此命令

When picked up, the server will automatically execute this command

相当于给玩家发送控制台指令

Equivalent to sending console command to the player

填写要求 / Requirements

必须是已在服务器注册的有效命令

Must be a pre-registered valid server command

建议使用前缀（如css_）避免冲突

Recommended to use prefix (e.g. css_) to avoid conflicts

需要与您的插件命令系统匹配

Must match your plugin's command system

示例场景 / Example Scenario


# 当玩家获得AK-47时 / When player gets AK-47:

服务器执行 -> css_ak @玩家ID

Server executes -> css_ak @playerID

技术实现建议 / Technical Tips

在插件中预先注册这些命令：


// 注册AK-47发放命令 / Register AK-47 grant command

AddCommand("css_ak", "Grant AK-47", (player, args) => { player.GiveWeapon("weapon_ak47");});

使用玩家ID作为参数确保精准发放

Use player ID as parameter for precise targeting

概率系统说明 / Probability System

概率值	说明	Value	Description

0.3	30%几率被选中	0.3	30% selection chance

1.0	必定出现（需权重平衡）	1.0	Guaranteed spawn

多个物品时概率计算公式：

Probability calculation formula for multiple items:

当前物品概率 / 所有启用物品概率总和

Current item probability / Sum of all enabled items' probabilities

隐藏命令系统安全说明 / Hidden Command Security Guide

█ 核心安全机制 / Core Security Mechanism

AddCommand("css_HanAirDropTest", "", HanAirDropTest); 

此命令为服务器隐藏指令，具有以下特性：

This is a server-side hidden command with:

不会出现在命令列表/控制台补全

Hidden from command list/console auto-complete

执行时不显示任何反馈日志

No execution feedback in logs

仅能通过空投系统触发

Only triggerable through airdrop system

█ 密码生成 / Password 

可以使用16位随机密码

use 16-digit random password:


示例/Example: XK3Q9FGT7YH2DP4R

生成工具建议：

KeePass密码生成器

1Password生成器

⚠️ 密码泄露风险

⚠️ PASSWORD LEAK RISK

若玩家获知完整命令格式：

If players discover command format:

!XK3Q9FGT7YH2DP4R  # 可直接获取道具

!XK3Q9FGT7YH2DP4R  # Direct item access

将导致：

Consequences:

经济系统崩溃

Economy system break

服务器平衡性破坏

Game balance destruction

█ 防护措施 / Protection Measures

定期更换密码?

Regular password rotation?:

建议每周更换一次

Recommend weekly changes








