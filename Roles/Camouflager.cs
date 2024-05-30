using System.Collections.Generic;
using UnityEngine;
using TownOfHost.PrivateExtensions;
using HarmonyLib;

namespace TownOfHost
{
    public static class Camouflager
    {
        static readonly int Id = 2500;

        public static CustomOption CamouflagerCamouflageCoolDown;
        public static CustomOption CamouflagerCamouflageDuration;
        public static CustomOption CamouflagerCanVent;
        public static void SetupCustomOption()
        {
            Options.SetupSingleRoleOptions(Id, CustomRoles.Camouflager, 1, AmongUsExtensions.OptionType.Impostor);
            CamouflagerCamouflageCoolDown = CustomOption.Create(Id + 10, Color.white, "CamouflagerCamouflageCoolDown", AmongUsExtensions.OptionType.Impostor, 30f, 2.5f, 60f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
            CamouflagerCamouflageDuration = CustomOption.Create(Id + 11, Color.white, "CamouflagerCamouflageDuration", AmongUsExtensions.OptionType.Impostor, 15f, 2.5f, 60f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
            CamouflagerCanVent = CustomOption.Create(Id + 12, Color.white, "CamouflagerCanVent", AmongUsExtensions.OptionType.Impostor, true, Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
        }
        public static void Init()
        {
            DidCamo = false;
        }
        public static bool DidCamo = false;
        public static bool CanVent()
        {
            return CamouflagerCanVent.GetBool();
        }
        public static void ShapeShiftState(PlayerControl shifter, bool shapeshifting, PlayerControl shiftinginto)
        {
            

            if (DidCamo)
            {
                if (shapeshifting) return;
                if (shifter == null) return;
                if (shiftinginto == null) return;
                Logger.Info($"Camouflager Revert ShapeShift", "Camouflager");
                foreach (PlayerControl revert in PlayerControl.AllPlayerControls)
                {
                    if (revert.Is(CustomRoles.Phantom) || revert == null || revert.Data.Disconnected || revert.PlayerId == shifter.PlayerId) continue;
                    if (revert.Data.IsDead)
                    {
                        Utils.SendMessage("We were unable to shift you back into your regular self because of the Innersloth AntiCheat. Sorry!", revert.PlayerId);
                        continue;
                    }
                    if (revert.inVent)
                        revert.MyPhysics.ExitAllVents();
                    revert.RpcShapeshift(revert, false); // Shift each player back to themselves
                }
                DidCamo = false;
            }
            if (shapeshifting)
            {
                if (shifter == shiftinginto) // Camouflager is shifting back to themselves
                {
                    // Revert all other players to their original form
                    foreach (PlayerControl target in PlayerControl.AllPlayerControls)
                    {
                        if (target == shifter) continue; // Skip the Camouflager
                        if (target.Is(CustomRoles.Phantom)) continue; // Skip Phantoms
                        if (target.Data.IsDead) continue; // Skip dead players
                        if (target.inVent)
                            target.MyPhysics.ExitAllVents();
                        target.RpcShapeshift(target, false); // Shift each player back to themselves
                    }

                    // Reset the DidCamo flag
                    DidCamo = false;
                }
                else
                {
                    if (shifter == null || shifter.Data.IsDead || shiftinginto == null) return;
                    Logger.Info($"Camouflager ShapeShift", "Camouflager");
                    foreach (PlayerControl target in PlayerControl.AllPlayerControls)
                    {
                        if (target == shifter) continue;
                        if (target == shiftinginto) continue;
                        if (target.Is(CustomRoles.Phantom)) continue;
                        if (target.Data.IsDead)
                        {
                            Utils.SendMessage("We were unable to shift you back into your regular self because of the Innersloth AntiCheat. Sorry!", target.PlayerId);
                            continue;
                        }
                        if (target.inVent)
                            target.MyPhysics.ExitAllVents();

                        target.RpcShapeshift(shiftinginto, false);
                    }
                    DidCamo = true;
                }
            }
            else if (!shapeshifting)
            {
                if (DidCamo)
                {
                    // Revert all other players to their original form
                    foreach (PlayerControl target in PlayerControl.AllPlayerControls)
                    {
                        if (target == shifter) continue; // Skip the Camouflager
                        if (target.Is(CustomRoles.Phantom)) continue; // Skip Phantoms
                        if (target.Data.IsDead) continue; // Skip dead players
                        if (target.inVent)
                            target.MyPhysics.ExitAllVents();
                        target.RpcShapeshift(target, false); // Shift each player back to themselves
                    }

                    // Reset the DidCamo flag
                    DidCamo = false;
                }
            }
           
        }
    }
}
