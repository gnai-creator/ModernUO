using Server.Engines.XmlSpawner2;
using Server.Targeting;
using System;

namespace Server.Items
{

    public class SiegeRepairTool : Item
    {
        public override int LabelNumber => 504506;
        public const double RepairDestroyedResourcePenalty = 2; // additional resource  factor for repairing destroyed structures
        public const double RepairDestroyedTimePenalty = 2; // additional time factor for repairing destroyed structures
        public const int RepairRange = 1;   // number of tiles away to search for repairable objects 

        private int m_UsesRemaining;                // if set to less than zero, becomes unlimited uses
        private int m_HitPerRepair = 100;                   // number of hits repaired per use

        public virtual double BaseRepairTime => 8.0;  // base time in seconds required to repair

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get => m_UsesRemaining;
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitsPerRepair
        {
            get => m_HitPerRepair;
            set { m_HitPerRepair = value; InvalidateProperties(); }
        }

        [Constructable]
        public SiegeRepairTool()
            : this(50)
        {
        }

        [Constructable]
        public SiegeRepairTool(int nuses)
            : base(0x0FB4)
        {
            Name = "";
            Hue = 0;
            UsesRemaining = nuses;
        }

        public SiegeRepairTool(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_UsesRemaining >= 0)
            {
                list.Add(1060584, m_UsesRemaining.ToString()); // uses remaining: ~1_val~
            }

            list.Add(504508, HitsPerRepair.ToString()); // ~1_val~: ~2_val

        }

        public override void OnDoubleClick(Mobile from)
        {
            if (UsesRemaining == 0)
            {
                from.SendLocalizedMessage(504507);//"Quest'attrezzo è inutile ora.");
                return;
            }
            if (IsChildOf(from.Backpack) || Parent == from)
            {
                from.Target = new SiegeRepairTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            writer.Write(m_UsesRemaining);
            writer.Write(m_HitPerRepair);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    m_UsesRemaining = reader.ReadInt();
                    m_HitPerRepair = reader.ReadInt();
                    break;
            }

        }

        public static void SiegeRepair_Callback((Mobile, XmlSiege, int, SiegeComponent) args)
        {
            Mobile from = args.Item1;
            XmlSiege a = args.Item2;
            int nhits = args.Item3;
            SiegeComponent targeted = args.Item4;

            if (a != null && targeted != null && !targeted.Deleted && from != null && from.Alive)
            {
                if (from.InRange(targeted.Location, (RepairRange + 1)))
                {
                    a.Hits += nhits;
                    from.SendLocalizedMessage(504508, nhits.ToString());//"{0} punti ripristinati", nhits);
                    a.BeingRepaired = false;
                }
                else
                {
                    from.SendLocalizedMessage(504509);//"Sei troppo distante ed hai perso i materiali!");
                    a.BeingRepaired = false;
                }
            }
            else if (a != null)
            {
                from.SendLocalizedMessage(500949);//"Non puoi ripararla da morto!");
                a.BeingRepaired = false;
            }

        }

        private class SiegeRepairTarget : Target
        {
            private SiegeRepairTool m_tool;

            public SiegeRepairTarget(SiegeRepairTool tool)
                : base(2, true, TargetFlags.None)
            {
                m_tool = tool;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || m_tool == null || from.Map == null)
                {
                    return;
                }
                // find any xmlsiege attachment on the target
                XmlSiege a = XmlAttach.FindAttachment(targeted as IEntity, typeof(XmlSiege)) as XmlSiege;
                // if it isnt on the target, but the target is an addon, then check the addon
                if (a == null && targeted is AddonComponent addon)
                {
                    a = XmlAttach.FindAttachment(addon.Addon, typeof(XmlSiege)) as XmlSiege;
                }

                // if it still isnt found, the look for nearby targets
                if (a == null)
                {
                    Point3D loc = Point3D.Zero;
                    if (targeted is IEntity)
                    {
                        loc = ((IEntity)targeted).Location;
                    }
                    else
                        if (targeted is StaticTarget)
                    {
                        loc = ((StaticTarget)targeted).Location;
                    }
                    else
                            if (targeted is LandTarget)
                    {
                        loc = ((LandTarget)targeted).Location;
                    }

                    if (loc != Point3D.Zero)
                    {
                        foreach (Item p in from.Map.GetItemsInRange(loc, RepairRange))
                        {
                            a = (XmlSiege)XmlAttach.FindAttachment(p, typeof(XmlSiege));
                            if (a != null)
                            {
                                break;
                            }
                        }
                    }

                }

                // repair the target
                if (a != null && targeted is SiegeComponent component)
                {
                    if (a.Hits >= a.HitsMax)
                    {
                        from.SendLocalizedMessage(504510);//"Non richiede riparazioni.");
                        return;
                    }

                    if (a.BeingRepaired)
                    {
                        from.SendLocalizedMessage(1004136);//"Devi aspettare prima di poter riparare ancora.");
                        return;
                    }
                    Container pack = from.Backpack;

                    // does the player have it?
                    if (pack != null && from.InRange(component.Location, (RepairRange + 1)))
                    {
                        int nhits = 0;

                        double resourcepenalty = 1;

                        // require more resources for repairing destroyed structures
                        if (a.Hits == 0)
                        {
                            resourcepenalty = RepairDestroyedResourcePenalty;
                        }

                        // dont consume resources for staff
                        if (from.AccessLevel > AccessLevel.Player)
                        {
                            resourcepenalty = 0;
                        }

                        int requirediron = (int)(a.Iron * resourcepenalty);
                        int requiredstone = (int)(a.Stone * resourcepenalty);
                        int requiredwood = (int)(a.Wood * resourcepenalty);

                        int niron = pack.GetAmount(typeof(IronIngot), true);
                        int nwood = pack.GetAmount(typeof(Log), true);
                        int nstone = pack.GetAmount(typeof(BaseGranite), true);

                        if (niron < requirediron || nstone < requiredstone || nwood < requiredwood)
                        {
                            if (requirediron > 0 && requiredwood > 0)
                            {
                                from.SendLocalizedMessage(504511, string.Format("{0}\t{1}", requirediron, requiredwood));//"Occorrono {0} ferro e {1} legna per riparare!", requirediron, requiredwood);
                            }
                            else if (requirediron == 0 && requiredwood > 0)
                            {
                                from.SendLocalizedMessage(504512, requiredwood.ToString());//"Occorre {0} legna per riparare!", requiredwood);
                            }
                            else if (requirediron > 0 && requiredwood == 0)
                            {
                                from.SendLocalizedMessage(504513, requirediron.ToString());//"Ti servirà {0} ferro per riparare!", requirediron);
                            }
                            else
                            {
                                from.SendLocalizedMessage(504514);//"Non è riparabile!");
                            }

                            return;
                        }
                        pack.ConsumeTotal(typeof(BaseGranite), requiredstone, true);
                        pack.ConsumeTotal(typeof(Log), requiredwood, true);
                        pack.ConsumeTotal(typeof(IronIngot), requirediron, true);

                        nhits += m_tool.HitsPerRepair;
                        from.PlaySound(0x2A); // play anvil sound
                        from.SendLocalizedMessage(504515);//"Inizi a riparare");

                        a.BeingRepaired = true;

                        double smithskill = from.Skills[SkillName.Blacksmith].Value;
                        double carpentryskill = from.Skills[SkillName.Carpentry].Value;

                        double timepenalty = 1;
                        if (a.Hits == 0)
                        {
                            // repairing destroyed structures requires more time
                            timepenalty = RepairDestroyedTimePenalty;
                        }

                        // compute repair speed with modifiers
                        TimeSpan repairtime = TimeSpan.FromSeconds(m_tool.BaseRepairTime * timepenalty - from.Dex / 40.0 - smithskill / 50.0 - carpentryskill / 50.0);

                        m_tool.UsesRemaining--;
                        if (m_tool.UsesRemaining < 1)
                        {
                            from.SendLocalizedMessage(1044038); // You have worn out your tool!
                            m_tool.Delete();
                        }

                        // allow staff instant repair
                        if (from.AccessLevel > AccessLevel.Player)
                        {
                            repairtime = TimeSpan.Zero;
                        }

                        // setup for the delayed repair
                        Timer.DelayCall(repairtime, SiegeRepair_Callback, (from, a, nhits, component));
                    }
                    else
                    {
                        from.SendLocalizedMessage(504501);//"Sei troppo distante!");
                        return;
                    }
                }
                else
                {
                    from.SendLocalizedMessage(504472);//"Bersaglio non valido");
                    return;
                }
            }
        }
    }
}
