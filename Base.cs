#region Revision info
/*
 * $Author: millz $
 * $Date: 2013-07-09 11:15:47 +0200 (Tue, 09 Jul 2013) $
 * $ID: $
 * $Revision: 48 $
 * $URL: http://subversion.assembla.com/svn/iwantmovement/trunk/IWantMovement/Base.cs $
 * $LastChangedBy: millz $
 * $ChangesMade: $
 */
#endregion

using System;
using System.Windows.Forms;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Routines;
using Styx.Plugins;
using Styx.WoWInternals.WoWObjects;
using IWantMovement.Helper;
using IWantMovement.Managers;
using IWantMovement.Settings;

namespace IWantMovement
{
// ReSharper disable InconsistentNaming
    internal class IWantMovement : HBPlugin
// ReSharper restore InconsistentNaming
    {
        private Form _gui; 
        Targeting _previousTargetMethod;
        Targeting _thisTargetMethod;

        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static string SvnRevision { get { return "$Rev: 48 $"; } }
        private static IWMSettings Settings { get { return IWMSettings.Instance; } }
        //private DateTime _facingLast;
        private DateTime _pluginThrottle;
         
        private static bool _initialized;

        private ICombatRoutine _decoratedCombatRoutine;
        private ICombatRoutine _undecoratedCombatRoutine;

        #region Default Overrides
        public override string Author { get { return "Millz"; }}
        public override string ButtonText { get { return "Settings"; }}
        public override string Name { get { return "I Want Movement"; }}
        public override bool WantButton { get { return true; }}
        public override Version Version { get { return new Version(0,0,1); }}
        #endregion Default Overrides

        public override void OnButtonPress()
        {
            if (_gui == null || _gui.IsDisposed || _gui.Disposing) _gui = new GUI();
            if (_gui != null || _gui.IsDisposed) _gui.ShowDialog();
        }

        public override void Initialize()
        {
            if (!_initialized) // prevent init twice. 
            {
                Log.Info("Storing current targeting instance.");
                _previousTargetMethod = Targeting.Instance;
                Log.Info("Creating our targeting instance.");
                _thisTargetMethod = new Target();

                Log.Info("IWantMovement Initialized [ {0}]", SvnRevision.Replace("$", "")); // Will print as [ Rev: 1 ]
                Log.Info("Have you tried PureRotation yet? - http://tinyurl.com/purev2");
                Log.Info("-- Millz");
                _initialized = true;
           
                base.Initialize();
            }
        }

        public override void Dispose()
        {
            Log.Info("Removing IWantMovement Hooks");
            Targeting.Instance = _previousTargetMethod;
            RoutineManager.Current = _undecoratedCombatRoutine;
            Log.Info("Disabled IWantMovement");
            base.Dispose(); 
        }
        
        public override void Pulse() 
        {
            if (DateTime.UtcNow < _pluginThrottle.AddMilliseconds(200) 
                || Me.IsDead  
                || Me.IsFlying 
                || Me.IsOnTransport 
                || Me.Mounted) { return; } 

            if ((RoutineManager.Current != null) && (RoutineManager.Current != _decoratedCombatRoutine))
            {
                Log.Info("Installing Combat Routine Hook...");
                _undecoratedCombatRoutine = RoutineManager.Current;
                _decoratedCombatRoutine = new IWantMovementCR(RoutineManager.Current);
                RoutineManager.Current = _decoratedCombatRoutine;
                Log.Info("Combat Routine Hook Installed!");
            }
            
            if ((_thisTargetMethod != Targeting.Instance) && Settings.EnableTargeting && !Me.HasAura("Food") && !Me.HasAura("Drink"))
            {
                Log.Warning("Taking control of targeting. If this message is being spammed, something else is trying to take control.");
                Targeting.Instance = _thisTargetMethod;
            }

            if (Targeting.Instance == _thisTargetMethod && Settings.EnableTargeting && !Me.GotTarget && !Me.HasAura("Food") && !Me.HasAura("Drink"))
            {
                if (!Me.GotTarget)
                {
                    Target.AquireTarget();
                }
                
            }

            if (Me.GotTarget)
            {
                // Clear dead targets
                Target.ClearTarget();
            } 
             
            if (Settings.EnableMovement && !Me.HasAura("Food") && !Me.HasAura("Drink") && (Me.Combat || Me.PetInCombat))
            {
                if (Settings.EnableFacing && Me.CurrentTarget != null && !Me.CurrentTarget.IsDead && !Me.IsMoving && !Me.IsSafelyFacing(Me.CurrentTarget) && Me.CurrentTarget.Distance <= 50)
                {
                    Log.Info("[Facing: {0}] [Target HP: {1}] [Target Distance: {2}]", Me.CurrentTarget.Name, Me.CurrentTarget.HealthPercent, Me.CurrentTarget.Distance);
                    Me.CurrentTarget.Face();
                }
                Movement.Move();
            }

            _pluginThrottle = DateTime.UtcNow;
            

        }

    }
}
