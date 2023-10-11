using Server.Items;

namespace Server.Engines.XmlSpawner2
{
    public class TemporaryDamagePlus : XmlAttachment
    {
        // a serial constructor is REQUIRED
        public TemporaryDamagePlus(ASerial serial) : base(serial)
        {
        }

        public TemporaryDamagePlus()
        {
        }

        public override void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, ref int damageGiven, int originalDamage)
        {
            // if it is still refractory then return
            damageGiven = (int)(damageGiven * Utility.RandomDouble(1.15, 1.35));
        }

        public override void Serialize(GenericWriter writer)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            Delete();
        }

        public override void OnAttach()
        {
            base.OnAttach();

            //Attach consentito solo su mobile
            if (!(AttachedTo is Mobile))
            {
                Delete();
            }
        }
    }
}