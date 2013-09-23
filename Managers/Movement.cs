#region Revision info
/*
 * $Author: millz $
 * $Date: 2013-07-09 11:15:47 +0200 (Tue, 09 Jul 2013) $
 * $ID: $
 * $Revision: 48 $
 * $URL: http://subversion.assembla.com/svn/iwantmovement/trunk/IWantMovement/Managers/Movement.cs $
 * $LastChangedBy: millz $
 * $ChangesMade: $
 */
#endregion


/*
 * 
 * A lot of code in here was taken from CLU's Movement.cs with permission from Wulf.
 * Big credits/thanks go to the CLU/PureRotation team (past and present) for code in here.
 * 
 * -- Millz
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using IWantMovement.Helper;
using Styx;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals;

namespace IWantMovement.Managers
{
    public static class Movement
    {

        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        //private static int MaxDistance { get { return Settings.IWMSettings.Instance.MaxDistance; } }
        //private static int StopDistance { get { return Settings.IWMSettings.Instance.StopDistance; } }
        //public static bool ValidatedSettings = false;
        //private static bool _displayWarning = false;
        private static bool MoveBehindTarget { get { return Settings.IWMSettings.Instance.MoveBehindTarget; } }
        private static DateTime _movementLast;
        //public delegate WoWPoint LocationRetriever(object context);
        //public delegate float DynamicRangeRetriever(object context);
        private static float StopDistance { get { return Me.IsMelee() ? 1f : 33f; } }
        private static float MaxDistance { get { return Me.IsMelee() ? MeleeRange : 35f; } }
        private static DateTime _movementSuspendedTime;
        private static bool _movementSuspended;

        private static bool CanMove()
        {
            return (DateTime.UtcNow > _movementLast.AddMilliseconds(Settings.IWMSettings.Instance.MovementThrottleTime)) 
                && !Me.Stunned 
                && !Me.Rooted 
                && !Me.HasAnyAura("Food", "Drink") 
                && !Me.IsDead 
                && !Me.IsFlying 
                && !Me.IsOnTransport;
        }

        /*
        private static void ValidateSettings()
        {
            if (!ValidatedSettings)
            {
                if (Me.IsMelee() && MaxDistance > 5f)
                {
                    _displayWarning = true;
                    Log.Warning("Your max distance setting({0}) is too high for a melee class. Reduce it!", MaxDistance);
                }

                if (MaxDistance > 40)
                {
                    _displayWarning = true;
                    Log.Warning("Max distance is {0}! This is very high for any class!", MaxDistance);
                }
                if (StopDistance < 2)
                {
                    _displayWarning = true;
                    Log.Warning("Stop distance is {0}! This is very low for any class!", StopDistance);
                }
                if (StopDistance >= MaxDistance)
                {
                    _displayWarning = true;
                    Log.Warning("Your stop distance [{0}] should be lower than max distance [{1}].", StopDistance, MaxDistance);
                }

                if (MaxDistance - StopDistance > 7)
                {
                    _displayWarning = true;
                    Log.Warning("Your Max Distance [{0}] is much higher than your Stop Distance [{1}]. Consider lowering the difference between these 2 values.", MaxDistance, StopDistance);
                }
                if (_displayWarning)
                {
                    Log.Warning("This warning will not prevent the plugin from working, however it may not work as expected.");
                }

                ValidatedSettings = true;

            }
        }*/

        private static bool NeedToMove()
        {
            return Me.CurrentTarget != null && (Me.CurrentTarget.Distance > MaxDistance || !Me.CurrentTarget.InLineOfSpellSight)   /* && !Me.IsMoving*/;
        }

        private static bool NeedToStop()
        {
           if (Me.CurrentTarget != null && (!Me.IsMelee() && (Me.CurrentTarget.Distance <= StopDistance && Me.CurrentTarget.InLineOfSpellSight) || Me.CurrentTarget.Distance <= 1.5 || Me.IsMelee() && Me.CurrentTarget.IsWithinMeleeRange) && Me.IsMoving)
           {
               return true;
           }

            return false;
        }

        #region Key States
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetAsyncKeyState(int vkey);

        static bool IsKeyDown(Keys key)
        {
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        private static bool UserIsMoving()
        {
            return  IsKeyDown(Keys.A) || 
                    IsKeyDown(Keys.S) || 
                    IsKeyDown(Keys.D) || 
                    IsKeyDown(Keys.W) || 
                    IsKeyDown(Keys.Q) ||
                    IsKeyDown(Keys.E) ||
                    IsKeyDown(Keys.Up) ||
                    IsKeyDown(Keys.Down) ||
                    IsKeyDown(Keys.Left) ||
                    IsKeyDown(Keys.Right)
                    ;
        }

        #endregion

        private static void SuspendMovement()
        {
            if (!Settings.IWMSettings.Instance.AllowSuspendMovement) {return;}

            if (UserIsMoving())
            {
                if (!_movementSuspended)
                {
                    Log.Warning("Suspending movement, user in control");
                }
                _movementSuspended = true;
                _movementSuspendedTime = DateTime.UtcNow;
            }
            
            if (_movementSuspended && !UserIsMoving() && DateTime.UtcNow > _movementSuspendedTime.AddMilliseconds(Settings.IWMSettings.Instance.SuspendDuration))
            {
                if (_movementSuspended)
                {
                    Log.Info("Taking control of movement");
                }
                _movementSuspended = false;

            }

        }

        public static void Move()
        {
            
            // Check we don't have bad settings
            //ValidateSettings();

            // Check if we want to allow movement.
            SuspendMovement();

            // If we don't have a target to move to
            if (Me.CurrentTarget == null || _movementSuspended) return;

            // Check if we're close enough.
            if (NeedToStop() && CanMove()) WoWMovement.MoveStop();

            // Check if we need to move
            if (!MoveBehindTarget && NeedToMove() && CanMove())
            {
                Log.Info("Moving to current target at {0}", Me.CurrentTarget.Location);
                _movementLast = DateTime.UtcNow;

                Navigator.MoveTo(Me.CurrentTarget.Location);
            }

            // Check if need to move, and want to be behind our target.
            if (MoveBehindTarget && NeedToMove() && CanMove())
            {
                Log.Info("Moving BEHIND target at {0}", Me.CurrentTarget.Location);
                _movementLast = DateTime.UtcNow;
                Navigator.MoveTo(PointBehindTarget());
            }
        }


        #region Extentions

        public static bool HasAnyAura(this WoWUnit unit, params string[] auraNames)
        {
            var auras = unit.GetAllAuras();
            var hashes = new HashSet<string>(auraNames);
            return auras.Any(a => hashes.Contains(a.Name));
        }

        private static bool IsMelee(this WoWUnit unit)
        {
            {
                if (unit != null)
                {
                    switch (StyxWoW.Me.Class)
                    {
                        case WoWClass.Warrior:
                            return true;
                        case WoWClass.Paladin:
                            return StyxWoW.Me.Specialization != WoWSpec.PaladinHoly;
                        case WoWClass.Hunter:
                            return false;
                        case WoWClass.Rogue:
                            return true;
                        case WoWClass.Priest:
                            return false;
                        case WoWClass.DeathKnight:
                            return true;
                        case WoWClass.Shaman:
                            return StyxWoW.Me.Specialization == WoWSpec.ShamanEnhancement;
                        case WoWClass.Mage:
                            return false;
                        case WoWClass.Warlock:
                            return false;
                        case WoWClass.Druid:
                            return StyxWoW.Me.Specialization != WoWSpec.DruidRestoration &&
                                   StyxWoW.Me.Specialization != WoWSpec.DruidBalance;
                        case WoWClass.Monk:
                            return StyxWoW.Me.Specialization != WoWSpec.MonkMistweaver;
                    }
                }
                return false;
            }
        }

        /*
        private static bool IsFlyingUnit
        {
            get
            {
                return StyxWoW.Me.CurrentTarget != null &&
                       (StyxWoW.Me.CurrentTarget.IsFlying ||
                        StyxWoW.Me.CurrentTarget.Distance2DSqr < 5 * 5 &&
                        Math.Abs(StyxWoW.Me.Z - StyxWoW.Me.CurrentTarget.Z) >= 5);
            }
        }
        */

        #endregion

        #region Calculations
        private static WoWPoint PointBehindTarget()
        {
            return
                StyxWoW.Me.CurrentTarget.Location.RayCast(
                    StyxWoW.Me.CurrentTarget.Rotation + WoWMathHelper.DegreesToRadians(150), MeleeRange - 2f);
        }

        /// <summary>
        ///     Returns the current Melee range for the player Unit.DistanceToTargetBoundingBox(target)
        /// </summary>
        private static float MeleeRange
        {
            get
            {
                // If we have no target... then give nothing.
                // if (StyxWoW.Me.CurrentTargetGuid == 0)  // chg to GotTarget due to non-zero vals with no target in Guid
                if (!StyxWoW.Me.GotTarget)
                    return 3.5f;

                if (StyxWoW.Me.CurrentTarget.IsPlayer)
                    return 3.5f;

                return Math.Max(5f, StyxWoW.Me.CombatReach + 1.3333334f + StyxWoW.Me.CurrentTarget.CombatReach);
            }
        }

        /*
        public static float DistanceToTargetBoundingBox()
        {
            return
                (float)
                (StyxWoW.Me.CurrentTarget == null
                     ? 999999f
                     : Math.Round(DistanceToTargetBoundingBox(Me.CurrentTarget), 0));
        }
        
        public static float DistanceToTargetBoundingBox(WoWUnit target)
        {
            if (target != null)
            {
                return (float)Math.Max(0f, target.Distance - target.BoundingRadius);
            }
            return 99999;
        }
        
        public static bool PlayerIsChanneling
        {
            get { return StyxWoW.Me.ChanneledCastingSpellId != 0; }
        }
        */
        #endregion

    }
}