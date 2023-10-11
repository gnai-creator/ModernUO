using Server.Mobiles;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlFrostEffect : XmlAttachment
    {
        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments
        private float m_Force;
        [CommandProperty(AccessLevel.GameMaster)]
        public override object GenericInternal => m_Force;

        // a serial constructor is REQUIRED
        public XmlFrostEffect(ASerial serial) : base(serial)
        {
        }

        public XmlFrostEffect(float force, TimeSpan duration)
        {
            Expiration = duration;
            m_Force = force;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
            // version 0
            writer.Write(m_Force);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            // version 0
            if(reader.ReadInt() < 1)
                m_Force = (float)reader.ReadDouble();
            else
                m_Force = reader.ReadFloat();
        }

        public override void OnDelete()
        {
            base.OnDelete();
            if (AttachedTo is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)AttachedTo;
                bc.CurrentSpeed -= m_Force;
                bc.ActiveSpeed -= m_Force;
                bc.PassiveSpeed -= m_Force;
                bc.HueMod = -1;
            }
            else if (AttachedTo is PlayerMobile)
            {
                PlayerMobile pg = (PlayerMobile)AttachedTo;
                pg.SwitchSpeedControl(RunningFlags.FrostMovement, false);
                pg.HueMod = -1;
            }
        }

        public override void OnAttach()
        {
            base.OnAttach();
            if (AttachedTo is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)AttachedTo;
                bc.CurrentSpeed += m_Force;
                bc.ActiveSpeed += m_Force;
                bc.PassiveSpeed += m_Force;
                bc.HueMod = 3;
            }
            else if (AttachedTo is PlayerMobile)
            {
                PlayerMobile pg = (PlayerMobile)AttachedTo;
                pg.SwitchSpeedControl(RunningFlags.FrostMovement, true);
                pg.HueMod = 3;
            }
        }
    }
}