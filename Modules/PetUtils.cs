﻿namespace TownOfHost;

public static class PetUtils
{
    public static void SetPet(PlayerControl player, string petId, bool applyNow = false, string prefix = null, float extraTime = 0f)
    {
        if (player.Is(CustomRoles.GM)) return;
        if (player.AmOwner) {
            player.SetPet(petId);
            return;
        }

        var outfit = player.Data.Outfits[PlayerOutfitType.Default];
        outfit.PetId = petId;
        RPC.SendGameData(player.GetClientId());
    }
}