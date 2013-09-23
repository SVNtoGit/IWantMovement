#region Revision info
/*
 * $Author: millz $
 * $Date: 2013-07-02 09:57:48 +0200 (Tue, 02 Jul 2013) $
 * $ID: $
 * $Revision: 47 $
 * $URL: http://subversion.assembla.com/svn/iwantmovement/trunk/IWantMovement/Managers/Target.cs $
 * $LastChangedBy: millz $
 * $ChangesMade: $
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using IWantMovement.Helper;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;
using Styx.CommonBot.POI;

namespace IWantMovement.Managers
{
    class Target : Targeting
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private readonly static Map Map = Me.CurrentMap;
        private static DateTime _targetLast;

        private static bool WantTarget()
        {
            return (DateTime.UtcNow > _targetLast.AddMilliseconds(Settings.IWMSettings.Instance.TargetingThrottleTime)) 
                && !Me.GotTarget 
                && !Me.Stunned && !Me.Rooted 
                && !Me.HasAnyAura("Food", "Drink") 
                && !Me.IsDead 
                && !Me.IsFlying && !Me.IsOnTransport;
        }


        public static void AquireTarget()
        {
            //Log.Debug("[Want A Target:{0}]", WantTarget());
            if (!WantTarget()) return;
            
            WoWUnit unit;
            _targetLast = DateTime.UtcNow;
            if (Map.IsBattleground || Map.IsArena)
            {
                
                if (Me.IsActuallyInCombat || (Me.GotAlivePet && Me.Pet.PetInCombat))
                {
                    // get a pvp unit attacking me
                    unit = NearbyAttackableUnitsAttackingMe(Me.Location, 40).FirstOrDefault(u => u != null && u.IsPlayer && u.IsHostile && u.Attackable);
                    if (unit != null) 
                    {
                        unit.Target();
                        Log.Info("[Targetting: {0}] [Target HP: {1}] [Target Distance: {2}]", unit.Name, unit.HealthPercent, unit.Distance);
                        return;
                    }
                    
                }

                // return closest pvp unit
                unit = NearbyAttackableUnits(Me.Location, 40).FirstOrDefault(u => u != null && u.IsPlayer && u.IsHostile && u.InLineOfSpellSight && u.Attackable);
                if (unit != null)
                {
                    unit.Target();
                    Log.Info("[Targetting: {0}] [Target HP: {1}] [Target Distance: {2}]", unit.Name, unit.HealthPercent, unit.Distance);
                    return;
                }
                

            }

            if (Map.IsInstance || Map.IsDungeon || Map.IsRaid)
            {
                if (Me.IsActuallyInCombat || (Me.GotAlivePet && Me.Pet.PetInCombat))
                {
                    // get unit attacking party
                    unit = NearbyAttackableUnitsAttackingUs(Me.Location, 40).FirstOrDefault(u => u != null && u.IsHostile && u.InLineOfSpellSight && u.Attackable);
                    if (unit != null)
                    {
                        unit.Target();
                        Log.Info("[Targetting: {0}] [Target HP: {1}] [Target Distance: {2}]", unit.Name, unit.HealthPercent, unit.Distance);
                        return;
                    }
                    
                }

            }

            unit = NearbyAttackableUnits(Me.Location, 25).FirstOrDefault(u => u != null && u.IsHostile && u.Attackable && ((Me.IsSafelyFacing(u) || u.IsTargetingMeOrPet) && u.InLineOfSpellSight));
            if (unit != null)
            {
                unit.Target();
                Log.Info("[Targetting: {0}] [Target HP: {1}] [Target Distance: {2}]", unit.Name, unit.HealthPercent, unit.Distance);
            }
        }

        public static void ClearTarget()
        {
            if (Me.CurrentTarget == null) { return; } 
            
            if (Me.CurrentTarget.IsDead && !Me.Looting && BotPoi.Current.Type != PoiType.Loot) 
            {
                Log.Info("[Clearing {0}] [Reason: Dead]", Me.CurrentTarget.Name);
                Me.ClearTarget();
            }

            //if (!Me.CurrentTarget.IsTargetingMeOrPet && Me.CurrentTarget.Distance > 70)
            //{
            //    Log.Info("[Clearing {0}] [Reason: Long Distance: {1}]", Me.CurrentTarget.Name, Me.CurrentTarget.Distance);
            //    Me.ClearTarget();
            //}

            /*
            if (!Me.CurrentTarget.Attackable && (!Me.CurrentTarget.IsHostile || (Me.CurrentTarget.IsFriendly && Me.CurrentTarget.IsPlayer)))
            {
                Log.Info("[Clearing {0}] [Reason: Target Not Hostile]", Me.CurrentTarget.Name);
                Me.ClearTarget();
            }*/

            if (Settings.IWMSettings.Instance.ClearTargetIfNotTargetingGroup && (Me.Combat || Me.PetInCombat) && !Me.CurrentTarget.IsDead && !IsTargetingUs(Me.CurrentTarget))
            {
                Log.Info("[Clearing {0}] [Reason: In combat - target isn't targeting us or group member]", Me.CurrentTarget.Name);
                Me.ClearTarget();
            }

        }

        private static bool IsTargetingUs(WoWUnit unit)
        {
            return unit.GotTarget && (
                unit.IsTargetingAnyMinion || unit.IsTargetingMeOrPet || unit.IsTargetingMyPartyMember ||
                   unit.IsTargetingMyRaidMember);
        }

        #region Core Unit Checks
        internal static IEnumerable<WoWUnit> AttackableUnits
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>(true, false).Where(u => u.Attackable && u.CanSelect && !u.IsFriendly && !u.IsDead && !u.IsNonCombatPet && !u.IsCritter && u.Distance <= 50); }
        }

        internal static IEnumerable<WoWUnit> NearbyAttackableUnits(WoWPoint fromLocation, double radius)
        {
            var hostile = AttackableUnits;
            var maxDistance = radius * radius;
            return hostile.Where(x => x.Location.DistanceSqr(fromLocation) < maxDistance);
        }

        internal static IEnumerable<WoWUnit> NearbyAttackableUnitsAttackingUs(WoWPoint fromLocation, double radius)
        {
            var hostile = AttackableUnits;
            var maxDistance = radius * radius;
            return hostile.Where(x => x.Location.DistanceSqr(fromLocation) < maxDistance && (x.IsTargetingMyPartyMember || x.IsTargetingMeOrPet || x.IsTargetingAnyMinion || x.IsTargetingMyRaidMember || x.IsTargetingPet));
        }

        internal static IEnumerable<WoWUnit> NearbyAttackableUnitsAttackingMe(WoWPoint fromLocation, double radius)
        {
            var hostile = AttackableUnits;
            var maxDistance = radius * radius;
            return hostile.Where(x => x.Location.DistanceSqr(fromLocation) < maxDistance && x.IsTargetingMeOrPet);
        }
        #endregion Core Unit Checks
    }
}
