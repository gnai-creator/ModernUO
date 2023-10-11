using Server.Commands;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlSpawnerDefaults
    {
        public bool AllowGhostTrig = false;
        public bool AllowNPCTrig = false;
        public string ConfigFile = null;
        public TimeSpan DespawnTime = TimeSpan.FromHours(0);
        public TimeSpan Duration = TimeSpan.FromMinutes(0);
        public bool ExternalTriggering = false;
        public bool FreeRun = false;
        public bool Group = false;
        public int HomeRange = 5;
        public bool HomeRangeIsRelative = true;
        public int KillReset = 1;
        public TimeSpan MaxDelay = TimeSpan.FromMinutes(10);
        public TimeSpan MinDelay = TimeSpan.FromMinutes(5);
        public string MobTriggerName = null;
        public string MobTriggerProp = null;
        public string NoTriggerOnCarried = null;
        public string PlayerTriggerProp = null;
        public string ProximityMsg = null;
        public int ProximityRange = -1;
        public int ProximitySound = 0x1F4;
        public TimeSpan RefractMax = TimeSpan.FromMinutes(0);
        public TimeSpan RefractMin = TimeSpan.FromMinutes(0);
        public string RegionName = null;
        public bool Running = true;
        public int SequentialSpawn = -1;
        public Item SetItem = null;
        public bool ShowBounds = false;
        public string SkillTrigger = null;
        public bool SmartSpawning = false;
        public bool SpawnOnTrigger = false;
        public int SpawnRange = 5;
        public string SpeechTrigger = null;
        public int StackAmount = 1;
        public int Team = 0;
        public TimeSpan TODEnd = TimeSpan.FromMinutes(0);
        public XmlSpawner.TODModeType TODMode = XmlSpawner.TODModeType.Realtime;
        public TimeSpan TODStart = TimeSpan.FromMinutes(0);
        public AccessLevel TriggerAccessLevel = AccessLevel.Player;
        public Mobile TriggerMob = null;
        public Item TriggerObject = null;
        public string TriggerObjectProp = null;
        public string TriggerOnCarried = null;
        public double TriggerProbability = 1;
        public WayPoint WayPoint = null;

        public static XmlSpawnerDefaults Instance = new XmlSpawnerDefaults();
        public static Type TypeInstance = typeof(XmlSpawnerDefaults);
        public XmlSpawnerDefaults()
        {
        }

        public static void Initialize()
        {
            CommandSystem.Register("XmlAdd", AccessLevel.Owner, new CommandEventHandler(XmlAdd_OnCommand));
        }

        [Usage("XmlAdd type [method_args]")]
        [Description("Permette di aggiungere oggetti che normalmente non sono impostati come costruibili, il livello minimo di costruzione è richiesto")]
        public static void XmlAdd_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m.Backpack == null)
            {
                return;
            }

            Type t = ScriptCompiler.FindTypeByName(e.GetString(0), true);
            if (t != null)
            {
                string[] args = new string[e.Arguments.Length - 1];
                for (int i = 1; i < args.Length; ++i)
                {
                    args[i] = e.Arguments[i];
                }

                object o = XmlSpawner.CreateObject(t, args, false, false, m.AccessLevel);
                if (o is Item)
                {
                    m.Backpack.AddItem((Item)o);
                }
                else if (o is Mobile)
                {
                    ((Mobile)o).MoveToWorld(m.Location, m.Map);
                }
                else
                {
                    m.SendMessage("Impossibile costruire l'oggetto richiesto (verificare di aver scritto gli eventuali argomenti necessari)");
                }
            }
            else
            {
                m.SendMessage("Il tipo indicato non è valido");
            }
        }
    }
}
