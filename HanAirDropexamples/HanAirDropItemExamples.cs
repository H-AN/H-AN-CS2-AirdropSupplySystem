using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;



public class HanAirDropTestPlugin : BasePlugin
{
    public override string ModuleName => "[华仔]空投道具示例";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "By : 华仔H-AN";
    public override string ModuleDescription => "创建指令空投道具示例";

    public override void Load(bool hotReload)
    {
        //Creates a command. The command can generate a 16-digit password. Add this command password to HanItemCFG.json using the same format.
        AddCommand("css_HanAirDropTest", "", HanAirDropTest); //创建指令 指令可以用密码生成16位数 将 指令密码 根据 HanItemCFG.json 的格式 填写到 里面
    }

    private void HanAirDropTest(CCSPlayerController? client, CommandInfo info)
    {
        if (client == null || !client.IsValid || !client.PawnIsAlive || client.TeamNum != 3)
            return;

        var playerPawn = client.PlayerPawn.Value;

        if(playerPawn == null || !playerPawn.IsValid )
            return;

        //编写你想要给予的道具 不限于 武器  道具  状态 任何 
        // Define items to grant - can be weapons, items, status effects, or any other type
 
        string ItemName = "你的道具";  // YourItem

        client.PrintToChat($"[华仔空投]玩家{client.PlayerName} 获得了道具{ItemName}");
    }
}