using Server.ACC.CSS.Systems.Undead;
using System;

namespace Server.Engines.XmlSpawner2
{
    public class XmlSpeed : XmlAttachment
    {
        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlSpeed(ASerial serial) : base(serial)
        {
        }

        public XmlSpeed(TimeSpan duration)
        {
            Expiration = duration;
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (AttachedTo is Mobile m)
            {
                m.SwitchSpeedControl(RunningFlags.SpeedPotion, true);
                UndeadSpellbook vs = UndeadSpell.SpecialEquip(m);
                vs.Attributes.CastSpeed = VampireBloodSpeed.c_CastSpeed;
                vs.Attributes.WeaponSpeed = VampireBloodSpeed.c_WeaponSpeed;
            }
            else
            {
                Delete();
            }
        }

        public override void OnDelete()
        {
            if (AttachedTo is Mobile m && !m.Deleted)
            {
                m.SwitchSpeedControl(RunningFlags.SpeedPotion, false);
                m.EndAction(typeof(VampireBloodSpeed));
                UndeadSpellbook vs = UndeadSpell.SpecialEquip(m);
                vs.Attributes.CastSpeed = 0;
                vs.Attributes.WeaponSpeed = 0;
                if (m.RazzaID != UtilityRazzeClassi.VAMPIRO)
                    vs.Delete();
                m.SendLocalizedMessage(505346);// "L'effetto di una pozione di velocità è terminato.");
            }
        }
    }
}
