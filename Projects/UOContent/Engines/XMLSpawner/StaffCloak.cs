using Server.Misc;
using Server.Mobiles;
using System;
using UltimaLive;
/*
** Allows staff to quickly switch between player and their assigned staff levels by equipping or removing the cloak
** Also allows instant teleportation to a specified destination when double-clicked by the staff member.
*/

namespace Server.Items
{
    public class StaffCloak : Cloak
    {
        public static void Configure()
        {
            PlayerMobile.FastwalkThreshold = TimeSpan.FromMilliseconds(380);
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            if (parent != null && !parent.Deleted)
            {
                if (parent.Backpack == null || parent.Backpack.Deleted)
                {
                    parent.AddItem(new Backpack());
                }

                parent.Backpack.DropItem(this);
            }
            return DeathMoveResult.MoveToBackpack;
        }

        private Point3D m_HomeLoc;
        private Map m_HomeMap;
        private AccessLevel m_OriginalAccessLevel;

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D HomeLoc { get => m_HomeLoc; set => m_HomeLoc = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map HomeMap { get => m_HomeMap; set => m_HomeMap = value; }

        [CommandProperty(AccessLevel.Owner)]
        public string CrashAddresses
        {
            get => Email.CrashAddresses;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Email.CrashAddresses = value;
                }
            }
        }
        [CommandProperty(AccessLevel.Owner)]
        public string SpeechLogAddresses
        {
            get => Email.SpeechLogPageAddresses;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Email.SpeechLogPageAddresses = value;
                }
            }
        }
        [CommandProperty(AccessLevel.Owner)]
        public string LogAlertAddresses
        {
            get => Email.LogAlertAddresses;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Email.LogAlertAddresses = value;
                }
            }
        }
        [CommandProperty(AccessLevel.Owner)]
        public string MailPassword
        {
            get => Utility.MailPasswd;
            set => Utility.MailPasswd = value;
        }

        [CommandProperty(AccessLevel.Developer)]
        public uint ClassicUOEXE
        {
            get => FeatureEnforcer.CUO_Exe;
            set => FeatureEnforcer.CUO_Exe = value;
        }
        [CommandProperty(AccessLevel.Developer)]
        public uint MobileUOPrevAndroid
        {
            get => FeatureEnforcer.MobileUO_AndroidPrev;
            set => FeatureEnforcer.MobileUO_AndroidPrev = value;
        }
        [CommandProperty(AccessLevel.Developer)]
        public uint MobileUOPreviOS
        {
            get => FeatureEnforcer.MobileUO_iOSPrev;
            set => FeatureEnforcer.MobileUO_iOSPrev = value;
        }
        [CommandProperty(AccessLevel.Developer)]
        public uint MobileUOPrevDesk
        {
            get => FeatureEnforcer.MobileUO_DeskPrev;
            set => FeatureEnforcer.MobileUO_DeskPrev = value;
        }
        [CommandProperty(AccessLevel.Developer)]
        public uint MobileUOAndroid
        {
            get => FeatureEnforcer.MobileUO_Android;
            set
            {
                FeatureEnforcer.MobileUO_Android = value;
            }
        }
        [CommandProperty(AccessLevel.Developer)]
        public uint MobileUOiOS
        {
            get => FeatureEnforcer.MobileUO_iOS;
            set
            {
                FeatureEnforcer.MobileUO_iOS = value;
            }
        }
        [CommandProperty(AccessLevel.Developer)]
        public uint MobileUODesk
        {
            get => FeatureEnforcer.MobileUO_Desk;
            set
            {
                FeatureEnforcer.MobileUO_Desk = value;
            }
        }
        [CommandProperty(AccessLevel.Developer)]
        public uint ClassicUOAPI
        {
            get => FeatureEnforcer.CUO_API;
            set => FeatureEnforcer.CUO_API = value;
        }
        [CommandProperty(AccessLevel.Developer)]
        public uint ClassicUORazor
        {
            get => FeatureEnforcer.CUORazor;
            set => FeatureEnforcer.CUORazor = value;
        }
        [CommandProperty(AccessLevel.Developer)]
        public int FastWalkMaxSteps
        {
            get => Mobile.FwdMaxSteps;
            set
            {
                if (value > 0 && value <= 6)
                    Mobile.FwdMaxSteps = value;
            }
        }
        [CommandProperty(AccessLevel.Developer)]
        public int FastwalkThreshold
        {
            get => (int)PlayerMobile.FastwalkThreshold.TotalMilliseconds;
            set
            {
                PlayerMobile.FastwalkThreshold = TimeSpan.FromMilliseconds(value);
            }
        }

        [CommandProperty(AccessLevel.Developer)]
        public bool ArtFileCRC
        {
            get => false;
            set
            {
                if (value)
                {
                    UltimaLiveSettings.PopulateVars(false);
                }
            }
        }
        [CommandProperty(AccessLevel.Developer)]
        public TimeSpan TempoNewbieTotale
        {
            get => PlayerMobile.TempoNewbie;
            set => PlayerMobile.TempoNewbie = value;
        }
        [CommandProperty(AccessLevel.Developer)]
        public bool ReloadEnforcerFeatureFile
        {
            get => false;
            set
            {
                if (value)
                {
                    FeatureEnforcer.Configure();
                }
            }
        }

        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan RestartDelay
        {
            get => AutoRestart.RestartDelay;
            set
            {
                if(value >= TimeSpan.Zero)
                {
                    AutoRestart.RestartDelay = value;
                }
            }
        }

        /*public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties(list);
			list.Add( 1060658, "Level\t{0}", StaffLevel ); // ~1_val~: ~2_val~
		}*/

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            // delete this if someone without the necessary access level picks it up or tries to equip it
            if (RootParent is Mobile)
            {
                if (((Mobile)RootParent).AccessLevel < AccessLevel.Counselor)
                {
                    Delete();
                    return;
                }
            }

            // when equipped, change access level to player
            if (parent is Mobile)
            {
                Mobile m = (Mobile)parent;

                if (m.AccessLevel >= AccessLevel.Counselor)
                {
                    m_OriginalAccessLevel = m.AccessLevel;
                    m.AccessLevel = AccessLevel.Player;
                    m.ComputeLightLevels(out int global, out _);
                    m.LightLevel = global;
                    // and make vuln
                    m.Blessed = false;
                    // and remove title
                    m.Title = null;
                }
                m.Items.Remove(this);
                m.Items.Insert(0, this);//verrà controllato per primo all'atto della morte!
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            base.OnRemoved(parent);

            // restore access level to the specified level
            if (parent is Mobile && !Deleted)
            {
                Mobile m = (Mobile)parent;
                // restore their assigned staff level
                m.AccessLevel = m_OriginalAccessLevel;//StaffLevel;
                m_OriginalAccessLevel = AccessLevel.Player;
                // and make invuln
                m.Blessed = true;
                // make hidden
                m.Hidden = true;
                // restore title
                switch (m.AccessLevel)
                {
                    case AccessLevel.Counselor: m.Title = @"[Counselor]"; break;
                    case AccessLevel.GameMaster: m.Title = @"[GameMaster]"; break;
                    case AccessLevel.Seer: m.Title = @"[Seer/GameMaster]"; break;
                    case AccessLevel.Administrator: m.Title = @"[Admin/GameMaster]"; break;
                    case AccessLevel.Developer: m.Title = @"[Devel/GameMaster]"; break;
                    case AccessLevel.Owner: m.Title = @"[Owner/GameMaster]"; break;
                }
                m.LightLevel = 40;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
            {
                return;
            }

            if (HomeMap != Map.Internal && HomeMap != null && from.AccessLevel >= AccessLevel.Counselor)
            {
                // teleport them to the specific location
                from.MoveToWorld(HomeLoc, HomeMap);
            }
        }

        public override string DefaultName => "Staff Cloak";
        [Constructable]
        public StaffCloak() : base()
        {
            LootType = LootType.Blessed;
            Weight = 0f;
        }

        public StaffCloak(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write(3);
            // version >=1 - preveniamo perdita status staff in caso lasciamo mantello messo e riavviamo il server
            writer.Write((int)m_OriginalAccessLevel);
            //version 2 - rimosso staff level - var inutilizzata ed inutile
            //writer.Write( (int) m_StaffLevel );
            // version 0
            writer.Write(m_HomeLoc);
            //version 2 - usato metodo standard
            writer.Write(m_HomeMap);
            /*string mapname = null;
			if(m_HomeMap != null)
			{
				mapname = m_HomeMap.Name;
			}
			writer.Write( mapname );*/
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                {
                    m_OriginalAccessLevel = (AccessLevel)reader.ReadInt();
                    m_HomeLoc = reader.ReadPoint3D();
                    m_HomeMap = reader.ReadMap();
                    break;
                }
                case 2:
                {
                    Name = null;
                    m_OriginalAccessLevel = (AccessLevel)reader.ReadInt();
                    m_HomeLoc = reader.ReadPoint3D();
                    m_HomeMap = reader.ReadMap();
                    break;
                }
                case 1:
                {
                    m_OriginalAccessLevel = (AccessLevel)reader.ReadInt();
                    goto case 0;
                }
                case 0:
                {
                    Name = null;
                    reader.ReadInt();
                    m_HomeLoc = reader.ReadPoint3D();
                    string mapname = reader.ReadString();

                    if (!string.IsNullOrEmpty(mapname))
                    {
                        try { m_HomeMap = Map.Parse(mapname); } catch { }
                    }

                    break;
                }
            }
        }
    }
}
