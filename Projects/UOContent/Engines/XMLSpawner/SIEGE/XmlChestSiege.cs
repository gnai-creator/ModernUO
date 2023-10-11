using Server.Items;
using System.Collections.Generic;

namespace Server.Engines.XmlSpawner2
{
    public class XmlChestSiege : XmlSiege
    {
        public override int LightDamageEffectID => 14201;  // 14201 = sparkle
        public override int MediumDamageEffectID => 14201;
        public override int HeavyDamageEffectID => 14201;

        // a serial constructor is REQUIRED
        public XmlChestSiege(ASerial serial)
            : base(serial)
        {
        }

        [Attachable]
        public XmlChestSiege()
        {
            HitsMax = 100;
        }

        [Attachable]
        public XmlChestSiege(int hitsmax)
        {
            HitsMax = hitsmax;
            Hits = HitsMax;
        }

        [Attachable]
        public XmlChestSiege(int hitsmax, int resistfire, int resistphysical)
        {
            HitsMax = hitsmax;
            Hits = HitsMax;
            ResistPhysical = resistphysical;
            ResistFire = resistfire;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // only allow attachment to containers
            if (!(AttachedTo is Container))
            {
                Delete();
            }
        }

        public override void OnDestroyed()
        {
            if (AttachedTo is Container chest && chest.Map != null && chest.Map != Map.Internal)
            {
                List<Item> movelist = new List<Item>(chest.Items);

                foreach (Item i in movelist)
                {
                    // spill the contents out onto the ground
                    i.MoveToWorld(chest.Location, chest.Map);
                }

                // and permanently destroy the container
                chest.Delete();
            }
        }

    }
}
