using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;


public static class SoundSystem {

    public static MemoryFunctionVoid<CBaseEntity, string, int, float, float> CBaseEntity_EmitSoundParamsFunc = new(GameData.GetSignature("CBaseEntity_EmitSoundParams")); 


    public static void EmitSound(this CBaseEntity entity, string soundEventName, int pitch = 1, float volume = 1f, float delay = 1f)
    {
        if (entity is null 
        || entity.IsValid is not true
        || string.IsNullOrEmpty(soundEventName) is true 
        || CBaseEntity_EmitSoundParamsFunc is null) return;

        //invoke play sound from an entity
        CBaseEntity_EmitSoundParamsFunc.Invoke(entity, soundEventName, pitch, volume, delay);
    }
    

    public static void EmitSoundGlobal(string soundEventName)
    {
        //get the world entity so we can emit global sounds
        var worldEntity = Utilities.GetEntityFromIndex<CBaseEntity>(0);

        if (worldEntity is null 
        || worldEntity.IsValid is not true
        || string.IsNullOrEmpty(soundEventName) is true  
        || worldEntity.DesignerName.Contains("world") is not true) return;

        //emit sound from the worldent
        worldEntity.EmitSound(soundEventName);
        
    }



}