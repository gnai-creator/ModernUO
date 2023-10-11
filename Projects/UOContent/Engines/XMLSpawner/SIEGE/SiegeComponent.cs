using Server.ContextMenus;
using Server.Engines.XmlSpawner2;
using Server.Mobiles;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Items
{
    public class SiegeComponent : AddonComponent
    {
        public override bool ForceShowProperties => true;
        public override bool SeMovable => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsDraggable
        {
            get
            {
                if (Addon is ISiegeWeapon)
                {
                    return ((ISiegeWeapon)Addon).IsDraggable;
                }
                return false;
            }
            set
            {
                if (Addon is ISiegeWeapon)
                {
                    ((ISiegeWeapon)Addon).IsDraggable = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsPackable
        {
            get
            {
                if (Addon is ISiegeWeapon)
                {
                    return ((ISiegeWeapon)Addon).IsPackable;
                }
                return false;
            }
            set
            {
                if (Addon is ISiegeWeapon)
                {
                    ((ISiegeWeapon)Addon).IsPackable = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool FixedFacing
        {
            get
            {
                if (Addon is ISiegeWeapon)
                {
                    return ((ISiegeWeapon)Addon).FixedFacing;
                }
                return false;
            }
            set
            {
                if (Addon is ISiegeWeapon)
                {
                    ((ISiegeWeapon)Addon).FixedFacing = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Facing
        {
            get
            {
                if (Addon is ISiegeWeapon)
                {
                    return ((ISiegeWeapon)Addon).Facing;
                }
                return 0;
            }
            set
            {
                if (Addon is ISiegeWeapon)
                {
                    ((ISiegeWeapon)Addon).Facing = value;
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Addon != null)
            {
                Addon.OnDoubleClick(from);
            }
        }

        public SiegeComponent(int itemID)
            : base(itemID)
        {
        }

        public SiegeComponent(int itemID, string name)
            : base(itemID)
        {
            Name = name;
        }

        public SiegeComponent(Serial serial)
            : base(serial)
        {
        }

        private class RotateNextEntry : ContextMenuEntry
        {
            private ISiegeWeapon m_weapon;
            private Mobile m_from;

            public RotateNextEntry(Mobile from, ISiegeWeapon weapon)
                : base(417)
            {
                m_weapon = weapon;
                m_from = from;
            }

            public override void OnClick()
            {
                if (m_weapon == null)
                {
                    return;
                }

                IPooledEnumerable<Item> ie = m_from.GetItemsInRange(m_weapon.MinStorageRange);
                bool found = false;
                foreach (Item it in ie)
                {
                    if (m_weapon.Components.Contains(it as AddonComponent))
                    {
                        if (m_from.InRange(it.Z, BaseSiegeWeapon.MaxStorageAltitude))
                        {
                            found = true;
                        }

                        break;
                    }
                }
                ie.Free();

                if (found)
                {
                    m_weapon.Facing++;
                }
                else
                {
                    m_from.SendLocalizedMessage(504501);//"Sei troppo lontano!");
                }
            }
        }

        private class RotatePreviousEntry : ContextMenuEntry
        {
            private ISiegeWeapon m_weapon;
            private Mobile m_from;

            public RotatePreviousEntry(Mobile from, ISiegeWeapon weapon) : base(416)
            {
                m_weapon = weapon;
                m_from = from;
            }

            public override void OnClick()
            {
                if (m_weapon == null)
                {
                    return;
                }

                IPooledEnumerable<Item> ie = m_from.GetItemsInRange(m_weapon.MinStorageRange);
                bool found = false;
                foreach (Item it in ie)
                {
                    if (m_weapon.Components.Contains(it as AddonComponent))
                    {
                        if (m_from.InRange(it.Z, BaseSiegeWeapon.MaxStorageAltitude))
                        {
                            found = true;
                        }

                        break;
                    }
                }
                ie.Free();

                if (found)
                {
                    m_weapon.Facing--;
                }
                else
                {
                    m_from.SendLocalizedMessage(504501);//"Sei troppo lontano!");
                }
            }
        }

        private class BackpackEntry : ContextMenuEntry
        {
            private ISiegeWeapon m_weapon;
            private Mobile m_from;

            public BackpackEntry(Mobile from, ISiegeWeapon weapon) : base(2139)
            {
                m_weapon = weapon;
                m_from = from;
            }

            public override void OnClick()
            {
                if (m_weapon == null)
                {
                    return;
                }

                IPooledEnumerable<Item> ie = m_from.GetItemsInRange(m_weapon.MinStorageRange);
                bool found = false;
                foreach (Item it in ie)
                {
                    if (m_weapon.Components.Contains(it as AddonComponent) && m_from.InLOS(it.Location))
                    {
                        if (m_from.InRange(it.Z, BaseSiegeWeapon.MaxStorageAltitude))
                        {
                            found = true;
                        }

                        break;
                    }
                }
                ie.Free();

                if (found)
                {
                    m_weapon.StoreWeapon(m_from);
                }
            }
        }

        private class ReleaseEntry : ContextMenuEntry
        {
            private Mobile m_from;
            private XmlDrag m_drag;
            private ISiegeWeapon m_weapon;

            public ReleaseEntry(Mobile from, XmlDrag drag, ISiegeWeapon weapon) : base(6118)
            {
                m_from = from;
                m_drag = drag;
                m_weapon = weapon;
            }

            public override void OnClick()
            {
                if (m_drag == null)
                {
                    return;
                }

                BaseCreature pet = m_drag.DraggedBy as BaseCreature;

                // only allow the person dragging it or their pet to release
                if (m_drag.DraggedBy == m_from || (pet != null && (pet.ControlMaster == m_from || pet.ControlMaster == null)))
                {
                    m_drag.DraggedBy = null;
                }
            }
        }

        private class ConnectEntry : ContextMenuEntry
        {
            private Mobile m_from;
            private XmlDrag m_drag;
            private ISiegeWeapon m_weapon;

            public ConnectEntry(Mobile from, XmlDrag drag, ISiegeWeapon weapon)
                : base(5119)
            {
                m_from = from;
                m_drag = drag;
                m_weapon = weapon;
            }

            public override void OnClick()
            {
                if (m_drag == null || m_from == null || m_weapon == null)
                {
                    return;
                }

                m_from.SendLocalizedMessage(504502);//"Selezionare chi deve trainare");
                m_from.Target = new DragTarget(m_drag, m_weapon);
            }
        }

        private class SetupEntry : ContextMenuEntry
        {
            private ISiegeWeapon m_weapon;
            private Mobile m_from;

            public SetupEntry(Mobile from, ISiegeWeapon weapon)
                : base(97)
            {
                m_weapon = weapon;
                m_from = from;
            }

            public override void OnClick()
            {
                if (m_weapon != null && m_from != null)
                {
                    m_weapon.PlaceWeapon(m_from, m_from.Location, m_from.Map);
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (Addon is ISiegeWeapon)
            {
                ISiegeWeapon weapon = (ISiegeWeapon)Addon;

                if (!weapon.FixedFacing)
                {
                    list.Add(new RotateNextEntry(from, weapon));
                    list.Add(new RotatePreviousEntry(from, weapon));
                }

                if (weapon.IsPackable)
                {
                    list.Add(new BackpackEntry(from, weapon));
                }

                if (weapon.IsDraggable)
                {
                    // does it support dragging?
                    XmlDrag a = (XmlDrag)XmlAttach.FindAttachment(weapon, typeof(XmlDrag));
                    if (a != null)
                    {
                        // is it currently being dragged?
                        if (a.DraggedBy != null && !a.DraggedBy.Deleted)
                        {
                            list.Add(new ReleaseEntry(from, a, weapon));
                        }
                        else
                        {
                            list.Add(new ConnectEntry(from, a, weapon));
                        }
                    }
                }

            }
            base.GetContextMenuEntries(from, list);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);


            if (!(Addon is ISiegeWeapon weapon))
            {
                return;
            }

            if (weapon.Projectile == null || weapon.Projectile.Deleted)
            {
                //list.Add(1061169, "empty"); // range ~1_val~
                list.Add(1042975); // It's empty
            }
            else
            {
                list.Add(500767); // Reloaded
                list.Add(504521, weapon.Projectile.GetNameString()); // Tipo: ~1_val~

                if (weapon.Projectile is ISiegeProjectile projectile)
                {
                    list.Add(1061169, projectile.Range.ToString()); // range ~1_val~
                }
            }
        }

        private class DragTarget : Target
        {
            private XmlDrag m_attachment;
            private ISiegeWeapon m_weapon;

            public DragTarget(XmlDrag attachment, ISiegeWeapon weapon)
                : base(2, false, TargetFlags.None)
            {
                m_attachment = attachment;
                m_weapon = weapon;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_attachment == null || from == null || m_weapon == null)
                {
                    return;
                }

                if (!(targeted is Mobile))
                {
                    from.SendLocalizedMessage(504503);//"Devi selezionare un essere vivente!");
                    return;
                }

                IPooledEnumerable<Item> ie = from.GetItemsInRange(m_weapon.MinStorageRange);
                bool found = false;
                foreach (Item it in ie)
                {
                    if (m_weapon.Components.Contains(it as AddonComponent))
                    {
                        found = true;
                        break;
                    }
                }
                ie.Free();

                if (found)
                {
                    if (targeted is BaseCreature)
                    {
                        BaseCreature m = (BaseCreature)targeted;
                        if (m.Controlled && m.ControlMaster == from)
                        {
                            m_attachment.DraggedBy = m;
                        }
                        else
                        {
                            from.SendLocalizedMessage(504504);//"Non è tuo quell'animale.");
                        }
                    }
                    else
                    {
                        m_attachment.DraggedBy = (Mobile)targeted;
                    }
                }
                else
                {
                    from.SendLocalizedMessage(504505);//"Sei troppo lontano dall'arma d'assedio");
                }
            }
        }

        public override void OnMapChange()
        {
            if (Addon != null && Map != Map.Internal)
            {
                Addon.Map = Map;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version =*/
            reader.ReadInt();
        }
    }
}
