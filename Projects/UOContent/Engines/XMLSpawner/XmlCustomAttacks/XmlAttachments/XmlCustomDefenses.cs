using Server.ACC.CM;
using Server.ACC.CSS;
using Server.ACC.CSS.Modules;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Mobiles.Classi;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
** XmlCustomDefenses
** 11/26/04
** ArteGordon
**
** This attachment will allow you to create a system for adding special defenses to shields including combo defenses that require
** a series of specific special defensive moves to be executed in a timed sequence.
** This is the defensive counterpart to XmlCustomAttacks.
**
*/
namespace Server.Engines.XmlSpawner2
{
    public class XmlCustomDefenses : XmlAttachment
    {
        [Usage("difesa")]
        [Description("Usare il comando difesa seguito da un numero da 1 a 9, quel numero corrisponde alla difesa speciale presente nell'apposito gump, il primo in alto è il numero uno, seguito in ordine crescente verso il basso. Il comando può essere usato nelle macro degli assistenti. Inserendo il numero 0 verrà resettato il gump alla posizione iniziale in alto a sinistra.")]//Usare il comando difesa seguito da un numero da 1 a 9, quel numero corrisponde alla difesa speciale presente nell'apposito gump, il primo in alto è il numero uno, seguito in ordine crescente verso il basso. Il comando può essere usato nelle macro degli assistenti. Inserendo il numero 0 verrà resettato il gump alla posizione iniziale in alto a sinistra.")]
        private static void Difesa_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (e.Arguments.Length > 0)
            {
                if (int.TryParse(e.Arguments[0], out int num) && num >= 0)
                {
                    if (!(m.ShieldArmor is BaseShield shield))
                    {
                        return;
                    }

                    if (XmlAttach.FindAttachment(shield, typeof(XmlCustomDefenses)) is XmlCustomDefenses m_attachment && m_attachment.AttachedTo == shield)
                    {
                        if (num == 0)
                        {
                            if (m_attachment.m_IconInfo != null)
                            {
                                m_attachment.m_IconInfo.Location = new Point3D(0, 0, m_attachment.m_IconInfo.Location.Z);
                                m.SendLocalizedMessage(1004601);//"Posizione del GUMP resettata al valore iniziale (in alto a sinistra), chiudere e riaprire il gump per aggiornare posizione oppure riavviare il client!");
                            }
                        }
                        else if (num <= m_attachment.Specials.Count)
                        {
                            LastShieldType.TryGetValue(m, out Type t);
                            if (t != null && shield.GetType() != t)
                            {
                                m.SendLocalizedMessage(1004603);//"Senti che ci metterai un pò, prima di abituarti ad uno scudo diverso");
                                return;
                            }
                            SpecialDefense s = m_attachment.Specials[num - 1];
                            // if clicked again, then deselect
                            if (s == m_attachment.m_SelectedDefense)
                            {
                                if (s.PreInitialize)
                                {
                                    m_attachment.PreInitFailed(m, shield, 0);
                                }

                                m_attachment.m_SelectedDefense = null;
                            }
                            else
                            {
                                // see whether they have the required resources for this defense
                                if (CheckRequirements(m, s, true))
                                {
                                    bool ok = true;

                                    // check preinitialization of other attachment present, if present (null check mandatory)
                                    if (m_attachment.m_SelectedDefense != null && m_attachment.m_SelectedDefense.PreInitialize)
                                    {
                                        m_attachment.PreInitFailed(m, shield, 0);
                                    }

                                    if (s.PreInitialize)
                                    {
                                        if (m.CanBeginAction(s))
                                        {
                                            ok = PreInit(m, m.Combatant, shield, s);
                                        }
                                        else
                                        {
                                            m.SendLocalizedMessage(1004606);//"Non puoi deselezionare e riselezionare una difesa di quel tipo così rapidamente, aspetta!");
                                        }
                                    }
                                    // if so, then let them select it
                                    if (ok)
                                    {
                                        m_attachment.m_SelectedDefense = s;
                                        ColpoSpeciale.DelaySpeciali(m);
                                    }
                                }
                                else
                                {
                                    // otherwise clear it
                                    if (s.PermanentEffect && !m.CanBeginAction(s.DefenseID))
                                    {
                                        SpecialDefenses sd = s.DefenseID;
                                        Timer.DelayCall(TimeSpan.FromSeconds(Math.Max(0, m_attachment.CoolDownCalculation(s, m))), delegate
                                        {
                                            if (m != null && !m.Deleted)
                                            {
                                                m.EndAction(sd);
                                            }
                                        });
                                    }
                                    m_attachment.m_SelectedDefense = null;
                                }
                            }

                            ConstructDefenseGump(m, m_attachment);
                        }
                    }
                }
                else
                {
                    m.SendLocalizedMessage(1004282);//"Numero non valido nel comando");
                }
            }
            else
            {
                m.SendLocalizedMessage(1004611);//"Inserire un valore numerico dopo il comando da 1 a 9 per selezionare l'attacco oppure 0 per resettare la posizione del gump");
            }
        }

        [Usage("defense")]
        [Description("Use the defense command followed by a number from 1 to 9, that number corresponds to the special defense present in the special gump, the first at the top is number one, followed in ascending order downwards. The command can be used in assistant macros. Entering the number 0 will reset the gump to the initial position at the top left.")]
        private static void Defense_OnCommand(CommandEventArgs e)
        {
            Difesa_OnCommand(e);
        }

        // ------------------------------------------------------------------------------
        // BEGINNING of user-defined special defenses and combos information
        // ------------------------------------------------------------------------------

        //
        // define the Combo and special defense enums
        //
        // you must first add entries here if you wish to add new defenses
        //

        // DEFENSES
        public enum ComboDefenses
        {
            ColdWind
        }

        public enum SpecialDefenses
        {
            DifesaConScudo,
            AttaccoConScudo,
            ScudoTotale,
            DifesaFinale,
            Deviazione,
            ColpoDiScudo,
            MindDrain,
            StamDrain,
            ParalyzingFear,
            GiftOfHealth,
            SpikeShield,
            PuffOfSmoke,
            AntiFuoco,
            AntiGhiaccio,
            AntiVeleno,
            AntiEnergia
        }

        public static new void Initialize()
        {
            CommandSystem.Register("difesa", AccessLevel.Player, new CommandEventHandler(Difesa_OnCommand), null, LanguageType.Italian);//, new CommandCheckHandler(Difesa_CheckerITA));
            CommandSystem.Register("defense", AccessLevel.Player, new CommandEventHandler(Defense_OnCommand), null, LanguageType.English);//, new CommandCheckHandler(Difesa_CheckerITA));
                                                                                                             //CommandSystem.Register("defence", AccessLevel.Player, new CommandEventHandler(Difesa_OnCommand), new CommandCheckHandler(Difesa_CheckerENG));
                                                                                                             //
                                                                                                             // define the special defenses and their use requirements
                                                                                                             //
                                                                                                             // ideally, you have a definition for every SpecialDefenses enum.  Although it isnt absolutely necessary,
                                                                                                             // if it isnt defined here, it will not be available for use
            AddSpecialDefense(1004612,
                SpecialDefenses.DifesaConScudo, 0x5105, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 5, 0, 0,
                100, 100, 0,
                new SkillName[] { SkillName.Parry },
                new int[] { 110 },
                null,
                null,
                20,
                true,
                true
            );
            AddSpecialDefense(1004614,
                SpecialDefenses.AttaccoConScudo, 0x8CE, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 5, 0, 0,
                100, 100, 0,
                new SkillName[] { SkillName.Parry, SkillName.Tactics },
                new int[] { 110, 110 },
                null,
                null,
                20,
                true,
                true
            );
            AddSpecialDefense(1004616,
                SpecialDefenses.ScudoTotale, 0x59D8, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 5, 0, 0,
                100, 100, 0,
                new SkillName[] { SkillName.Parry },
                new int[] { 110 },
                null,
                null,
                20,
                true,
                false
            );
            AddSpecialDefense(1004618,
                SpecialDefenses.DifesaFinale, 0x5325, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 20, 50, 0,
                100, 100, 0,
                new SkillName[] { SkillName.Parry, SkillName.Wrestling, SkillName.Tactics },
                new int[] { 90, 100, 105 },
                null,
                null,
                56,
                false,
                false
            );
            AddSpecialDefense(1004620,
                SpecialDefenses.Deviazione, 0x59DD, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 5, 0, 0,
                110, 100, 0,
                new SkillName[] { SkillName.Parry, SkillName.Wrestling },
                new int[] { 105, 100 },
                null,
                null,
                15,
                false,
                false
            );
            AddSpecialDefense(1004622,
                SpecialDefenses.ColpoDiScudo, 0x5425, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 5, 0, 0,
                110, 100, 0,
                new SkillName[] { SkillName.Parry, SkillName.Wrestling },
                new int[] { 100, 100 },
                null,
                null,
                15,
                true,
                false
            );
            AddSpecialDefense(1004624,
                SpecialDefenses.MindDrain, 0x5007, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                10, 4, 5, 0,
                0, 0, 40,
                new SkillName[] { SkillName.Magery },
                new int[] { 50 },
                null,
                null,
                10,
                false,
                false
            );
            AddSpecialDefense(1004626,
                SpecialDefenses.StamDrain, 0x500e, IconTypes.GumpID, TimeSpan.FromSeconds(8),                // if the icon type is not specified, gump icons use gumpids
                30, 4, 0, 0,
                40, 40, 0,
                null,
                null,
                new Type[] { typeof(Ginseng), typeof(Garlic) },
                new int[] { 1, 2 },
                10,
                false,
                false
            );
            AddSpecialDefense(1004628,
                SpecialDefenses.ParalyzingFear, 0x500d, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                10, 5, 5, 10,
                0, 0, 40,
                new SkillName[] { SkillName.Necromancy },
                new int[] { 30 },
                new Type[] { typeof(Head) },
                new int[] { 1 },
                10,
                false,
                false
            );
            AddSpecialDefense(1004630,
                SpecialDefenses.GiftOfHealth, 0x500c, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                40, 15, 0, 0,
                0, 0, 30,
                null,
                null,
                new Type[] { typeof(Ginseng), typeof(MandrakeRoot) },
                new int[] { 4, 4 },
                10,
                false,
                false
            );
            AddSpecialDefense(1004632,
                SpecialDefenses.SpikeShield, 0x2086, IconTypes.ItemID, TimeSpan.FromSeconds(8), // example of using an itemid for the gump icon
                0, 10, 0, 0,
                30, 30, 0,
                null,
                null,
                new Type[] { typeof(PigIron) },
                new int[] { 3 },
                10,
                false,
                false
            );
            AddSpecialDefense(1004634,
                SpecialDefenses.PuffOfSmoke, 0x520b, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                20, 20, 0, 0,
                0, 40, 0,
                new SkillName[] { SkillName.Stealth, SkillName.Hiding },
                new int[] { 50, 50 },
                new Type[] { typeof(SpidersSilk) },
                new int[] { 2 },
                10,
                false,
                false
            );
            AddSpecialDefense(1004636,
                SpecialDefenses.AntiFuoco, 0x8CE, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 5, 0, 0,
                100, 100, 0,
                new SkillName[] { SkillName.Parry, SkillName.Tactics },
                new int[] { 110, 110 },
                null,
                null,
                20,
                true,
                true
            );
            AddSpecialDefense(1004638,
                SpecialDefenses.AntiGhiaccio, 0x8CE, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 5, 0, 0,
                100, 100, 0,
                new SkillName[] { SkillName.Parry, SkillName.Tactics },
                new int[] { 110, 110 },
                null,
                null,
                20,
                true,
                true
            );
            AddSpecialDefense(1004640,
                SpecialDefenses.AntiVeleno, 0x8CE, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 5, 0, 0,
                100, 100, 0,
                new SkillName[] { SkillName.Parry, SkillName.Tactics },
                new int[] { 110, 110 },
                null,
                null,
                20,
                true,
                true
            );
            AddSpecialDefense(1004642,
                SpecialDefenses.AntiEnergia, 0x8CE, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 5, 0, 0,
                100, 100, 0,
                new SkillName[] { SkillName.Parry, SkillName.Tactics },
                new int[] { 110, 110 },
                null,
                null,
                20,
                true,
                true
            );

            //
            // define combos and the sequence of special defenses needed to activate them
            //
            /*AddComboDefense( "Vento Freddo", ComboDefenses.ColdWind,
				new SpecialDefenses []
					{
					SpecialDefenses.SpikeShield,
					SpecialDefenses.SpikeShield,
					SpecialDefenses.StamDrain
					}
			);*/

            // after deser, restore combo and specials lists to all existing CustomDefenses attachments based on these definitions
            //AddComboDefense("Pirito Spaziale", ComboDefenses.ColdWind, new SpecialDefenses[]{SpecialDefenses.Deviazione, SpecialDefenses.DifesaFinale});
            //EventSink.SetAbility += new SetAbilityEventHandler( EventSink_SetAbilityReceived );
        }

        //		private static void EventSink_SetAbilityReceived( SetAbilityEventArgs e )
        //		{
        //			if(e.Index>=1000 && e.Mobile!=null)
        //			{
        //				Mobile m = e.Mobile;
        //				Gump g = m.FindGump(typeof(CustomDefenseGump));
        //				if(g!=null)
        //				{
        //					g.OnResponse(m.NetState, new RelayInfo(e.Index - 1000, null, null));
        //				}
        //			}
        //		}

        public static bool PreInit(Mobile defender, Mobile attacker, Item shield, SpecialDefense special)
        {
            if (defender == null || shield == null || special == null)
            {
                return false;
            }

            //ATTACKER PUO' ESSERE NULL!! ***ATTENZIONE!!!***
            switch (special.DefenseID)
            {
                case SpecialDefenses.ColpoDiScudo:
                    goto case SpecialDefenses.ScudoTotale;
                case SpecialDefenses.AttaccoConScudo:
                    goto case SpecialDefenses.ScudoTotale;
                case SpecialDefenses.ScudoTotale:
                {
                    defender.NextCombatTime = DateTime.MaxValue.Subtract(TimeSpan.FromHours(1));
                    defender.colpo_caricato_war = false;
                    break;
                }
                default:
                    break;
            }

            return true;
        }

        public void PreInitFailed(Mobile defender, BaseShield shield, int toCalculate)
        {
            //attenzione, il preinitfailed SOLO NEL DEFENSES viene chiamato se la selezione preinizializza e:
            // 1- un utente preme di nuovo il tasto per il colpo speciale deselezionandolo
            // 2- un utente preme un altro colpo speciale, di fatto invalidando il precedente
            // 3- il colpo non può riuscire perchè mancano i requisiti durante la sua esecuzione
            // TENETENE CONTO!!

            switch (m_SelectedDefense.DefenseID)
            {
                case SpecialDefenses.ColpoDiScudo:
                    goto case SpecialDefenses.ScudoTotale;
                case SpecialDefenses.ScudoTotale:
                {
                    defender.NextCombatTime = DateTime.UtcNow + defender.Weapon.GetDelay(defender);
                    break;
                }
                case SpecialDefenses.AttaccoConScudo:
                {
                    SpecialDefenses sd = SpecialDefenses.AttaccoConScudo;
                    if (!defender.CanBeginAction(sd))
                    {
                        defender.NextCombatTime = DateTime.UtcNow + defender.Weapon.GetDelay(defender);
                        Timer.DelayCall(TimeSpan.FromSeconds(Math.Max(0, CoolDownCalculation(AllSpecials[sd], defender))), delegate
                        {
                            if (defender != null && !defender.Deleted)
                            {
                                defender.EndAction(sd);
                            }
                        });
                    }
                    break;
                }
                case SpecialDefenses.DifesaConScudo:
                {
                    SpecialDefenses sd = SpecialDefenses.DifesaConScudo;
                    if (!defender.CanBeginAction(sd))
                    {
                        Timer.DelayCall(TimeSpan.FromSeconds(Math.Max(0, CoolDownCalculation(AllSpecials[sd], defender))), delegate
                        {
                            if (defender != null && !defender.Deleted)
                            {
                                defender.EndAction(sd);
                            }
                        });
                    }
                    break;
                }
                default:
                    break;
            }
        }

        //
        // carry out the special defenses
        //
        // If you add a new defense, you must add the code here to define what it actually does when it hits
        // can optionally return a value that will be used to reduce damage
        //
        public int DoSpecialDefense(Mobile attacker, Mobile defender, BaseWeapon weapon, BaseShield shield, int damageGiven, SpecialDefense special, ref bool ResourceAndEnd)
        {
            //NON usare return qui sotto, per interrompere l'esecuzione settare il break e modificate toReturn secondo danno da sottrarre
            // settare inoltre sendmsg se volete che appaia il messaggio del colpo o meno (se magari non rispetta condizioni, etc)

            ColpoSpeciale.DelaySpeciali(defender);
            int toReturn = 0;
            bool sendmsg = true;

            // apply the special defense
            switch (special.DefenseID)
            {
                case SpecialDefenses.DifesaConScudo:
                {
                    float protect = 0.40f;
                    if (shield.ProtectionLevel > ArmorProtectionLevel.Regular)
                    {
                        protect += shield.GetProtOffset() * 0.025f * ((int)shield.ShieldType * 0.01f);
                    }
                    else
                    {
                        protect += (int)shield.Resource * 0.0077f * ((int)shield.ShieldType * 0.01f);
                    }

                    toReturn = (int)(damageGiven * protect);

                    if (toReturn < 1)
                    {
                        toReturn = 1;
                    }

                    break;
                }
                case SpecialDefenses.AttaccoConScudo:
                {
                    if (defender.NextCombatTime == DateTime.MaxValue.Subtract(TimeSpan.FromHours(1)) && !defender.colpo_caricato_war)
                    {
                        float protect = 0.40f;
                        if (shield.ProtectionLevel > ArmorProtectionLevel.Regular)
                        {
                            protect += shield.GetProtOffset() * 0.025f * ((int)shield.ShieldType * 0.01f);
                        }
                        else
                        {
                            protect += (int)shield.Resource * 0.0077f * ((int)shield.ShieldType * 0.01f);
                        }

                        toReturn = (int)(damageGiven * protect);

                        if (damageGiven <= 1)
                        {
                            toReturn = 0;
                        }

                        if (attacker.InRange(defender.Location, 1) && defender.LastActionTime.Subtract(DateTime.UtcNow) >= TimeSpan.FromSeconds(2))
                        {
                            attacker.Damage(toReturn, defender);
                            defender.PublicOverheadMessage(MessageType.Regular, 0, 1004670);//"*ATTACCO CON SCUDO*");
                        }
                    }
                    else
                    {
                        defender.NextCombatTime = DateTime.MaxValue.Subtract(TimeSpan.FromHours(1));
                        defender.colpo_caricato_war = false;
                        sendmsg = false;
                    }

                    break;
                }
                case SpecialDefenses.ScudoTotale:
                {
                    if (defender.NextCombatTime == DateTime.MaxValue.Subtract(TimeSpan.FromHours(1)) && !defender.colpo_caricato_war)
                    {
                        defender.NextCombatTime = DateTime.UtcNow + defender.Weapon.GetDelay(defender);
                        int removedDMG = (int)(damageGiven * DefenseLVL);

                        if (damageGiven > 1 && (damageGiven - removedDMG) < 1)
                        {
                            toReturn = damageGiven - 1;
                        }
                    }
                    else
                    {
                        defender.SendLocalizedMessage(1004671);//Non puoi effettuare attacchi durante l'esecuzione di questa mossa
                        sendmsg = false;
                    }
                    break;
                }
                case SpecialDefenses.DifesaFinale:
                {
                    if (attacker.InRange(defender.Location, 1))
                    {
                        defender.Freeze(TimeSpan.FromSeconds(2));
                        attacker.Freeze(TimeSpan.FromSeconds(3));
                        attacker.Stam /= 3;
                        if (attacker.Stam < 1)
                        {
                            attacker.Stam = 1;
                        }

                        attacker.Damage(damageGiven, defender);
                        //TODO: graphical & sound effects
                    }
                    else
                    {
                        sendmsg = false;
                        ResourceAndEnd = false;
                    }
                    break;
                }
                case SpecialDefenses.Deviazione:
                {
                    int toRemove = 0;
                    List<Mobile> near = new List<Mobile>(defender.GetMobilesInRange(1).Cast<Mobile>());
                    near.Remove(attacker);
                    near.Remove(defender);

                    //sino al 55% di danno parato, skill e fortuna, fortuna per il 20%, parry 25%, fisso 10%
                    float removed = ((Utility.RandomFloat() * 0.2f) + (Math.Min(defender.Skills[SkillName.Parry].Value, 200) * 0.00125f) + 0.1f);
                    toRemove = (int)(damageGiven * removed);
                    if (near.Count > 0)
                    {
                        int toGive = damageGiven - toRemove;
                        int choosen = Utility.Random(near.Count);
                        near[choosen].Damage(toGive, defender);
                    }
                    defender.PlaySound(0x520);
                    toReturn = toRemove;
                    break;
                }
                case SpecialDefenses.ColpoDiScudo:
                {
                    if (defender.NextCombatTime == DateTime.MaxValue.Subtract(TimeSpan.FromHours(1)) && !defender.colpo_caricato_war)
                    {
                        defender.NextCombatTime = DateTime.UtcNow + defender.Weapon.GetDelay(defender);
                        int toGive = (int)(damageGiven * 0.4f * ((int)shield.ShieldType * 0.01f));
                        attacker.Damage(toGive, defender);
                        attacker.Freeze(TimeSpan.FromSeconds(1 * ((int)shield.ShieldType * 0.01f)));
                        attacker.Combatant = null;
                        attacker.SendLocalizedMessage(1004672);//Il colpo ricevuto ti stordisce!
                        attacker.PublicOverheadMessage(MessageType.Regular, 0, 1004673);//*COLPO DI SCUDO*
                    }
                    else
                    {
                        defender.SendLocalizedMessage(1004671);//Non puoi effettuare attacchi durante l'esecuzione di questa mossa
                        sendmsg = false;
                    }
                    break;
                }
                case SpecialDefenses.MindDrain:
                {
                    attacker.Mana -= damageGiven;
                    defender.FixedEffect(0x375A, 10, 15);
                    break;
                }
                case SpecialDefenses.StamDrain:
                {
                    attacker.Stam -= damageGiven;
                    defender.FixedEffect(0x374A, 10, 15);
                    // absorb no damage
                    break;
                }
                case SpecialDefenses.SpikeShield:
                {
                    // return the damage to attacker
                    attacker.Damage(damageGiven, defender);
                    defender.SendLocalizedMessage(1004674, damageGiven.ToString());
                    // absorb all of the damage you would have taken
                    toReturn = damageGiven;
                    break;
                }
                case SpecialDefenses.PuffOfSmoke:
                {
                    defender.Hidden = true;
                    break;
                }
                case SpecialDefenses.GiftOfHealth:
                {
                    defender.FixedEffect(0x376A, 9, 32);
                    defender.PlaySound(0x202);
                    defender.Hits += damageGiven;
                    defender.SendLocalizedMessage(1004644, damageGiven.ToString());//Delle ferite sono state curate: ~1_val~
                    break;
                }
                case SpecialDefenses.ParalyzingFear:
                {
                    // lose target focus
                    attacker.Combatant = null;
                    // flee
                    if (attacker is BaseCreature)
                    {
                        ((BaseCreature)attacker).BeginFlee(TimeSpan.FromSeconds(6));
                    }
                    // and become paralyzed
                    attacker.Freeze(TimeSpan.FromSeconds(5));
                    attacker.FixedEffect(0x376A, 9, 32);
                    attacker.PlaySound(0x204);
                    attacker.SendLocalizedMessage(1004645);//"Sei paralizzato dal terrore..");
                    break;
                }
                default:
                {
                    sendmsg = false;
                    defender.SendLocalizedMessage(1005014);//"Nessun effetto");
                    break;
                }
            }

            if (sendmsg)
            {
                defender.SendLocalizedMessage(1005001, string.Format("#{0}", special.Name));//Esegui ~1_val~!
            }

            return toReturn;
        }

        //
        // carry out the combo defenses
        //
        // If you add a new combo, you must add the code here to define what it actually does when it is activated
        //
        public void DoComboDefense(Mobile attacker, Mobile defender, BaseWeapon weapon, int damageGiven, ComboDefense combo)
        {
            if (attacker == null || defender == null || weapon == null || combo == null)
            {
                return;
            }

            defender.SendLocalizedMessage(1005002, combo.Name);//Termini la combo di difesa ~1_val~!

            // apply the combo defense
            switch (combo.DefenseID)
            {
                case ComboDefenses.ColdWind:
                {
                    // 5 sec paralyze
                    attacker.FixedEffect(0x376A, 9, 32);
                    attacker.PlaySound(0x204);
                    attacker.Freeze(TimeSpan.FromSeconds(5));
                    // 7x stam drain
                    attacker.Stam -= weapon.MaxDamage * 7;
                    break;
                }
            }
        }

        // this constructor is intended to be called from within scripts that wish to define custom defense configurations
        // by passing it a list of SpecialDefenses
        public XmlCustomDefenses(SpecialDefenses[] defenselist, float defenselevel)
        {
            if (defenselist != null)
            {
                foreach (SpecialDefenses sid in defenselist)
                {
                    AddSpecial(sid);
                }
                DefenseLVL = defenselevel;
            }
        }

        public XmlCustomDefenses(SpecialDefenses defense)
        {
            AddSpecial(defense);
        }

        [Attachable]
        public XmlCustomDefenses(string name)
        {
            if (string.Compare("brogan", name, true) == 0)
            {
                AddSpecial(SpecialDefenses.SpikeShield);
                AddSpecial(SpecialDefenses.MindDrain);
                AddSpecial(SpecialDefenses.StamDrain);
                AddSpecial(SpecialDefenses.ParalyzingFear);
            }
            else if (string.Compare("test", name, true) == 0)
            {
                foreach (SpecialDefenses id in AllSpecials.Keys)
                {
                    AddSpecial(id);
                }
            }
        }

        // ------------------------------------------------------------------------------
        // END of user-defined special defenses and combos information
        // ------------------------------------------------------------------------------

        private static Dictionary<SpecialDefenses, SpecialDefense> AllSpecials = new Dictionary<SpecialDefenses, SpecialDefense>();
        private static Dictionary<ComboDefenses, ComboDefense> AllCombos = new Dictionary<ComboDefenses, ComboDefense>();
        private IconInfo m_IconInfo = null;

        public static void AddMobiletoDict(PlayerMobile pm)
        {
            LastShieldType[pm] = null;
            pm.RMT_def = new XmlCustomDefenses.RemoveDefsTimer(pm);
        }

        public static void DelMobileinDict(PlayerMobile pm)
        {
            if (pm.RMT_def != null)
            {
                pm.RMT_def.Stop();
            }

            LastShieldType.Remove(pm);
        }

        private static Dictionary<Mobile, Type> LastShieldType = new Dictionary<Mobile, Type>();

        public enum IconTypes
        {
            GumpID,
            ItemID
        }

        public class SpecialDefense
        {
            public int Name;           // attack name
            public SpecialDefenses DefenseID;  // defense id
            public TimeSpan ChainTime;    // time available until next defense in the chain must be performed
            public int Icon;                 // button icon for this defense
            public IconTypes IconType;          // what type of art to use for button icon
            public int ManaReq;             // mana usage for this defense
            public int StamReq;             // stamina usage for this defense
            public int HitsReq;             // hits usage for this defense
            public int KarmaReq;            // karma usage for this defense
            public int StrReq;             // str requirements for this defense
            public int DexReq;             // dex requirements for this defense
            public int IntReq;             // int requirements for this defense
            public Type[] Reagents;       // reagent list used for this defense
            public int[] Quantity;        // reagent quantity list
            public SkillName[] Skills;    // list of skill requirements for this defense
            public int[] MinSkillLevel;   // minimum skill levels
            public float CoolDown;
            public bool PreInitialize;
            public bool PermanentEffect;

            public SpecialDefense(int name, SpecialDefenses id, int icon, IconTypes itype, TimeSpan duration,
            int mana, int stam, int hits, int karma, int minstr, int mindex, int minint,
            SkillName[] skills, int[] minlevel, Type[] reagents, int[] quantity, float cooldown, bool preinitialize, bool permanenteffect)
            {
                Name = name;
                DefenseID = id;
                ChainTime = duration;
                Icon = icon;
                IconType = itype;
                ManaReq = mana;
                StamReq = stam;
                HitsReq = hits;
                KarmaReq = karma;
                StrReq = minstr;
                DexReq = mindex;
                IntReq = minint;
                Reagents = reagents;
                Quantity = quantity;
                Skills = skills;
                MinSkillLevel = minlevel;
                CoolDown = cooldown;
                PreInitialize = preinitialize;
                PermanentEffect = permanenteffect;
            }
        }

        public class ComboDefense
        {
            public string Name;
            public ComboDefenses DefenseID;
            public SpecialDefenses[] DefenseSequence;

            public ComboDefense(string name, ComboDefenses id, SpecialDefenses[] sequence)
            {
                Name = name;
                DefenseID = id;
                DefenseSequence = sequence;
            }
        }

        public class ActiveCombo
        {
            public ComboDefense Combo;
            public int PositionInSequence;

            public ActiveCombo(ComboDefense c)
            {
                Combo = c;
                PositionInSequence = 0;
            }
        }

        private ComboTimer m_ComboTimer;
        private SpecialDefense m_SelectedDefense;

        // these are the lists of special moves and combo status for each instance
        private List<SpecialDefense> Specials = new List<SpecialDefense>();
        private static Dictionary<Mobile, List<ActiveCombo>> Combos = new Dictionary<Mobile, List<XmlCustomDefenses.ActiveCombo>>();
        private static Dictionary<Mobile, SpecialDefense> SelectedDefenses = new Dictionary<Mobile, XmlCustomDefenses.SpecialDefense>();

        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlCustomDefenses(ASerial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            /*base.Serialize(writer);

			writer.Write( (int) 0 );

			// version 0
			// save the specials for this instance
			int max=Specials.Count;
			writer.Write(max);
			for(int i=0; i<max; ++i)
			{
				writer.Write(Specials[i].DefenseID.ToString());
			}*/
        }

        private List<SpecialDefenses> tmpSpecialsList = new List<SpecialDefenses>();

        public override void Deserialize(GenericReader reader)
        {
        }

        //protected override bool UnsavedAttach => true;

        private static void AddSpecialDefense(int name, SpecialDefenses id, int icon, IconTypes itype, TimeSpan duration,
        int mana, int stam, int hits, int karma, int minstr, int mindex, int minint,
        SkillName[] skills, int[] minlevel, Type[] reagents, int[] quantity, float cooldown, bool preinitialize, bool permanenteffect)
        {
            AllSpecials[id] = new SpecialDefense(name, id, icon, itype,
            duration, mana, stam, hits, karma, minstr, mindex, minint,
            skills, minlevel, reagents, quantity, cooldown, preinitialize, permanenteffect);
        }

        private void AddSpecial(SpecialDefenses id)
        {

            if (AllSpecials.TryGetValue(id, out SpecialDefense s))
            {
                Specials.Add(s);
            }
        }

        private static void AddComboDefense(string name, ComboDefenses id, SpecialDefenses[] sequence)
        {
            AllCombos[id] = new ComboDefense(name, id, sequence);
        }

        public static ComboDefense GetComboDefense(ComboDefenses name)
        {
            return AllCombos[name];
        }

        public static void AddDefense(IEntity target, SpecialDefenses defense)
        {
            // is there an existing custom attacks attachment to add to?
            XmlCustomDefenses a = (XmlCustomDefenses)XmlAttach.FindAttachment(target, typeof(XmlCustomDefenses));

            if (a == null)
            {
                // add a new custom attacks attachment
                XmlAttach.AttachTo(target, new XmlCustomDefenses(defense));
            }
            else
            {
                // add the new attack to existing attack list
                a.AddSpecial(defense);
            }
        }

        private static void ResetCombos(Mobile from)
        {
            if (Combos.TryGetValue(from, out List<ActiveCombo> clist))
            {
                foreach (ActiveCombo c in clist)
                {
                    c.PositionInSequence = 0;
                }
            }
        }

        private static bool HasActiveCombos(Mobile from)
        {
            if (Combos.TryGetValue(from, out List<ActiveCombo> clist))
            {
                foreach (ActiveCombo c in clist)
                {
                    if (c.PositionInSequence > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void CheckCombos(Mobile attacker, Mobile defender, BaseWeapon weapon, int damageGiven, SpecialDefense s)
        {
            if (Combos.TryGetValue(defender, out List<ActiveCombo> clist))
            {
                foreach (ActiveCombo c in clist)
                {
                    if (c != null && c.Combo != null && c.Combo.DefenseSequence != null && c.PositionInSequence < c.Combo.DefenseSequence.Length)
                    {
                        if (c.Combo.DefenseSequence[c.PositionInSequence] == s.DefenseID)
                        {
                            if (++c.PositionInSequence >= c.Combo.DefenseSequence.Length)
                            {
                                // combo is complete so execute it
                                DoComboDefense(attacker, defender, weapon, damageGiven, c.Combo);

                                // and reset it
                                c.PositionInSequence = 0;
                            }
                        }
                        else
                        {
                            // out of sequence so reset the combo
                            c.PositionInSequence = 0;
                        }
                    }
                }
            }
        }

        public static bool CheckRequirements(Mobile from, SpecialDefense s, bool blockpermability)
        {
            //check non necessario
            //if(from == null || s == null) return false; 

            if ((blockpermability || !s.PermanentEffect) && !from.CanBeginAction(s.DefenseID))
            {
                from.SendLocalizedMessage(1004136);//Devi aspettare prima di poterlo rifare
                return false;
            }
            // test for str, dex, int requirements
            if (from.Str < s.StrReq)
            {
                from.SendLocalizedMessage(1005003, string.Format("{0}\t#{1}", s.StrReq, s.Name));
                return false;
            }
            if (from.Dex < s.DexReq)
            {
                from.SendLocalizedMessage(1005004, string.Format("{0}\t#{1}", s.DexReq, s.Name));
                return false;
            }
            if (from.Int < s.IntReq)
            {
                from.SendLocalizedMessage(1005005, string.Format("{0}\t#{1}", s.IntReq, s.Name));
                return false;
            }

            // test for skill requirements
            if (s.Skills != null && s.MinSkillLevel != null)
            {
                if (from.Skills == null)
                {
                    return false;
                }

                for (int i = 0; i < s.Skills.Length; ++i)
                {
                    // and check level
                    if (i < s.MinSkillLevel.Length)
                    {
                        Skill skill = from.Skills[s.Skills[i]];
                        if (skill != null && s.MinSkillLevel[i] > skill.Base)
                        {
                            from.SendLocalizedMessage(1005006, string.Format("{0}\t{1}\t#{2}", s.MinSkillLevel[i], s.Skills[i].ToString(), s.Name));
                            return false;
                        }
                    }
                    else
                    {
                        from.SendMessage(33, "Error in skill level specification for {0}", s.DefenseID);
                        return false;
                    }
                }
            }


            // test for mana, stam, and hits requirements
            if (from.Mana < s.ManaReq)
            {
                from.SendLocalizedMessage(1005011, string.Format("{0}\t#{1}", s.ManaReq, s.Name));
                return false;
            }
            if (from.Stam < s.StamReq)
            {
                from.SendLocalizedMessage(1005012, string.Format("{0}\t#{1}", s.StamReq, s.Name));
                return false;
            }
            if (from.Hits < s.HitsReq)
            {
                // clear the selected defense
                from.SendLocalizedMessage(1005013, string.Format("{0}\t#{1}", s.HitsReq, s.Name));
                return false;
            }


            // check for any reagents that are specified
            if (s.Reagents != null && s.Quantity != null)
            {
                if (from.Backpack == null)
                {
                    return false;
                }

                for (int i = 0; i < s.Reagents.Length; ++i)
                {
                    // go through each reagent
                    Item ret = from.Backpack.FindItemByType(s.Reagents[i], true);

                    // and check quantity
                    if (i < s.Quantity.Length)
                    {
                        if (ret == null || s.Quantity[i] > ret.Amount)
                        {
                            from.SendLocalizedMessage(1005006, string.Format("{0}\t{1}\t#{2}", s.Quantity[i], s.Reagents[i].Name, s.Name));
                            return false;
                        }
                    }
                    else
                    {
                        from.SendMessage(33, "Error in quantity specification for {0}", s.DefenseID);
                        return false;
                    }
                }
            }
            return true;
        }

        public void InitializeCombos(Mobile from, List<ActiveCombo> list)
        {
            SelectedDefenses.TryGetValue(from, out m_SelectedDefense);
            if (Combos.TryGetValue(from, out List<ActiveCombo> clist))
            {
                if (!clist.Equals(list))
                {
                    clist.Clear();
                    if (list != null)
                    {
                        clist.AddRange(list);
                    }
                }
                //else if same list of combos already inside, no change
            }
            else
            {
                if (list != null)
                {
                    Combos[from] = new List<XmlCustomDefenses.ActiveCombo>(list);
                }
                else
                {
                    Combos[from] = new List<XmlCustomDefenses.ActiveCombo>();
                }
            }
        }

        public override int OnArmorHit(Mobile attacker, Mobile defender, Item armor, BaseWeapon weapon, int damageGiven)
        {
            if (attacker == null || defender == null || weapon == null || !(armor is BaseShield shield) || m_SelectedDefense == null)
            {
                return 0;
            }

            if (!CheckRequirements(defender, m_SelectedDefense, false))
            {
                if (m_SelectedDefense.PreInitialize)
                {
                    PreInitFailed(defender, shield, damageGiven);
                }

                m_SelectedDefense = null;
                ConstructDefenseGump(defender, this);

                return 0;
            }

            defender.BeginAction(m_SelectedDefense.DefenseID);
            SpecialDefenses ss = m_SelectedDefense.DefenseID;
            float CCD = CoolDownCalculation(m_SelectedDefense, defender);
            if (CCD < 0)
            {
                CCD = 0;
            }

            if (!m_SelectedDefense.PermanentEffect)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(CCD), delegate
                {
                    if (defender != null && !defender.Deleted)
                    {
                        defender.EndAction(ss);
                    }
                });
            }

            bool ResourceAndEnd = true;
            // apply the defense and check if we have ended it (for special combats or conditions, etc!)
            int damage = DoSpecialDefense(attacker, defender, weapon, shield, damageGiven, m_SelectedDefense, ref ResourceAndEnd);

            if (ResourceAndEnd)
            {
                // take the requirements
                if (defender.Backpack != null && m_SelectedDefense.Reagents != null && m_SelectedDefense.Quantity != null)
                {
                    defender.Backpack.ConsumeTotal(m_SelectedDefense.Reagents, m_SelectedDefense.Quantity, true);
                }

                defender.Mana -= m_SelectedDefense.ManaReq;
                defender.Stam -= m_SelectedDefense.StamReq;
                defender.Hits -= m_SelectedDefense.HitsReq;
                defender.Karma -= m_SelectedDefense.KarmaReq;

                if (m_SelectedDefense.KarmaReq > 0)
                {
                    defender.SendLocalizedMessage(1019064);//Hai perso un poco di karma.
                }

                // after applying a special defense activate the specials timer for combo chaining
                DoComboTimer(defender, m_SelectedDefense.ChainTime);

                // check all combos to see which have this defense as the next in sequence, and which might be complete
                CheckCombos(attacker, defender, weapon, damageGiven, m_SelectedDefense);

                //if this is not a "permanent" effect, then clear and resend..
                if (!m_SelectedDefense.PermanentEffect)
                {
                    // clear the selected defense
                    m_SelectedDefense = null;
                    // redisplay the gump
                    ConstructDefenseGump(defender, this);
                }

                return damage;
            }

            return 0;
        }

        public override void ShieldDamageMod(Mobile attacker, BaseShield shield, ref float bonus)
        {
            if (m_SelectedDefense == null)
            {
                return;
            }
            //troppi check qui sotto
            //if(shield == null || m_SelectedDefense == null || attacker == null) return;

            switch (m_SelectedDefense.DefenseID)
            {
                case SpecialDefenses.DifesaConScudo:
                {
                    if (CheckRequirements(attacker, m_SelectedDefense, false))
                    {
                        bonus *= 0.5f;
                    }
                    break;
                }
                default:
                    break;
            }
        }

        public float CoolDownCalculation(SpecialDefense s, Mobile m)
        {
            if (m is PlayerMobile pm)
            {
                /*if (pm.ClassePg == ClassiEnum.Paladino)
                {
                    return (s.CoolDown - ((m.Skills[SkillName.Parry].Value - 100) * 0.06f));
                }
                else*/ 
                if (pm.ClassePg == ClassiEnum.Paladino || pm.ClassePg == ClassiEnum.Templare || pm.ClassePg == ClassiEnum.Chierico)
                {
                    return ((s.CoolDown * 1.1f) - ((m.Skills[SkillName.Parry].Value - 100) * 0.06f));
                }

                return ((s.CoolDown * 2.5f) - ((m.Skills[SkillName.Parry].Value - 100) * 0.06f));
            }

            return s.CoolDown;
        }

        public override void OnEquip(Mobile from)
        {
            // open the specials gump
            if (from == null || !from.Player || Specials.Count < 1)
            {
                return;
            }

            if (m_SelectedDefense != null)
            {
                bool contains = false, checks = !(LastShieldType.TryGetValue(from, out Type t) && t != null && AttachedTo.GetType() != t);
                if (checks)
                {
                    for (int i = Specials.Count - 1; i >= 0 && !contains; --i)
                    {
                        contains = (Specials[i].DefenseID == m_SelectedDefense.DefenseID);
                    }
                }

                if (!contains)
                {
                    if (m_SelectedDefense.PermanentEffect && !from.CanBeginAction(m_SelectedDefense.DefenseID))
                    {
                        SpecialDefenses sd = m_SelectedDefense.DefenseID;
                        Timer.DelayCall(TimeSpan.FromSeconds(Math.Max(0, CoolDownCalculation(m_SelectedDefense, from))), delegate
                        {
                            if (from != null && !from.Deleted)
                            {
                                from.EndAction(sd);
                            }
                        });
                    }
                    m_SelectedDefense = null;
                }
                else if(m_SelectedDefense.PreInitialize)
                {
                    PreInit(from, null, (Item)AttachedTo, m_SelectedDefense);
                }
            }
            IconsModule im = (IconsModule)CentralMemory.GetModule(from.Serial, typeof(IconsModule));
            if (im != null)
            {
                if (!im.Icons.TryGetValue(typeof(XmlCustomDefenses), out m_IconInfo) || m_IconInfo == null)
                {
                    im.Icons[typeof(XmlCustomDefenses)] = m_IconInfo = new IconInfo(typeof(XmlCustomDefenses), 0x15D3, 0, 0, 0, School.Speciali);
                }
            }
            else
            {
                m_IconInfo = new IconInfo(typeof(XmlCustomDefenses), 0x15D3, 0, 0, 0, School.Speciali);
            }

            ConstructDefenseGump(from, this);
        }

        public override void OnRemoved(IEntity parent)
        {
            if (parent is PlayerMobile pm)
            {
                if (pm.NetState != null)
                {
                    if (LastShieldType[pm] == null)
                    {
                        LastShieldType[pm] = AttachedTo.GetType();
                        pm.RMT_def.Start();
                    }
                    else if (LastShieldType[pm] == AttachedTo.GetType())
                    {
                        pm.RMT_def.Start();
                    }
                }

                pm.CloseGump(typeof(CustomDefenseGump));
                SelectedDefenses[pm] = m_SelectedDefense;
                Delete();
            }
        }

        public class RemoveDefsTimer : Timer
        {
            private Mobile m_from;

            public RemoveDefsTimer(Mobile from) : base(Parametri.ShieldExchangeDelay)
            {
                m_from = from;
            }

            protected override void OnTick()
            {
                LastShieldType[m_from] = null;
            }
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            int num = Specials.Count - 1;
            if (num < 0)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder("#1005175");//1005175 -> Difese Speciali:
            for (int i = 0; i <= num; ++i)
            {
                sb.AppendFormat("\t#{0}", Specials[i].Name);
            }
            if (Expiration > TimeSpan.Zero)
            {
                sb.AppendFormat("\texpires in {0:F1} minutes", Expiration.TotalMinutes);
                num++;
            }
            return new LogEntry(1005177 + num, sb.ToString());
        }

        private float DefenseLVL = 0;
        public override void OnAttach()
        {
            base.OnAttach();

            // only allow attachment to shields (for now)
            if (AttachedTo is BaseShield)
            {
                if (DefenseLVL == 0)
                {
                    DefenseLVL = ((BaseShield)AttachedTo).DefenseLevel();
                }
            }
            else
            {
                Delete();
            }
        }

        public void DoComboTimer(Mobile from, TimeSpan delay)
        {
            if (m_ComboTimer != null)
            {
                m_ComboTimer.Stop();
            }

            m_ComboTimer = new ComboTimer(from, this, delay);

            m_ComboTimer.Start();
        }

        public static void ConstructDefenseGump(Mobile from, XmlCustomDefenses a)
        {
            if (from == null || a == null || a.Deleted || a.m_IconInfo == null)
            {
                return;
            }

            from.SendGump(new CustomDefenseGump(from, a, a.m_IconInfo));
        }

        private class ComboTimer : Timer
        {
            private XmlCustomDefenses m_attachment;
            private Mobile m_from;

            public ComboTimer(Mobile from, XmlCustomDefenses a, TimeSpan delay) : base(delay)
            {
                Priority = TimerPriority.OneSecond;
                m_attachment = a;
                m_from = from;
            }

            protected override void OnTick()
            {
                if (m_attachment == null || m_attachment.Deleted)
                {
                    return;
                }
                // the combo has expired
                if (m_from != null)
                {
                    ResetCombos(m_from);
                    if (m_attachment.AttachedTo is BaseShield shield && !shield.Deleted && shield.Parent == m_from)
                    {
                        // refresh the gump
                        ConstructDefenseGump(m_from, m_attachment);
                    }
                }
            }
        }

        private class CustomDefenseInfoGump : Gump
        {
            private XmlCustomDefenses m_attachment;
            private SpecialDefense m_special;

            public CustomDefenseInfoGump(Mobile from, XmlCustomDefenses a, SpecialDefense s) : base(0, 0)
            {

                m_attachment = a;
                m_special = s;

                // prepare the page
                AddPage(0);

                AddBackground(0, 0, 400, 300, 5054);
                AddAlphaRegion(0, 0, 400, 300);
                AddHtmlLocalized(20, 2, 340, 20, s.Name, 0x77B1, false, false);
                //AddLabel( 20, 2, 55, String.Format("{0}",s.Name) );
                //text.AppendFormat("\n{0}", s.Description );
                StringBuilder text = new StringBuilder();
                text.AppendFormat("#{0}\t", s.Name + 1);

                //text.AppendFormat("Stat/Skill Minime:" );
                if (s.StrReq > 0)
                {
                    text.AppendFormat("<br>     {0} Str", s.StrReq);
                }
                if (s.DexReq > 0)
                {
                    text.AppendFormat("<br>     {0} Dex", s.DexReq);
                }
                if (s.IntReq > 0)
                {
                    text.AppendFormat("<br>     {0} Int", s.IntReq);
                }

                if (s.Skills != null)
                {
                    for (int i = 0; i < s.Skills.Length; ++i)
                    {
                        if (i < s.MinSkillLevel.Length)
                        {
                            text.AppendFormat("<br>     {1} {0}", s.Skills[i].ToString(), s.MinSkillLevel[i]);
                        }
                        else
                        {
                            text.AppendFormat("<br>     {1} {0}", s.Skills[i].ToString(), "???");
                        }
                    }
                }

                text.AppendFormat("\t");
                // generate the text requirements
                if (s.ManaReq > 0)
                {
                    text.AppendFormat("<br>     {0} Mana", s.ManaReq);
                }
                if (s.StamReq > 0)
                {
                    text.AppendFormat("<br>     {0} Stamina", s.StamReq);
                }
                if (s.HitsReq > 0)
                {
                    text.AppendFormat("<br>     {0} Hits", s.HitsReq);
                }
                if (s.KarmaReq > 0)
                {
                    text.AppendFormat("<br>     {0} Karma", s.KarmaReq);
                }

                if (s.Reagents != null)
                {
                    for (int i = 0; i < s.Reagents.Length; ++i)
                    {
                        if (i < s.Quantity.Length)
                        {
                            text.AppendFormat("<br>     {1} {0}", s.Reagents[i].Name, s.Quantity[i]);
                        }
                        else
                        {
                            text.AppendFormat("<br>     {1} {0}", s.Reagents[i].Name, "???");
                        }
                    }
                }

                int CCD = 0, loc = 1005016;
                if (s.CoolDown > 0)
                {
                    CCD = (int)a.CoolDownCalculation(s, from);
                    if (CCD > 0)
                    {
                        loc++;
                    }

                    text.AppendFormat("\t{0}", CCD);
                }

                AddHtmlLocalized(20, 20, 360, 260, loc, text.ToString(), 0x1, true, true);
            }
        }

        private class CustomDefenseGump : Gump
        {
            private XmlCustomDefenses m_attachment;
            private const int vertspacing = 47;

            public override void RefreshMemo()
            {
                m_attachment.m_IconInfo.Location = new Point3D(X, Y, m_attachment.m_IconInfo.Location.Z);
            }

            public CustomDefenseGump(Mobile from, XmlCustomDefenses a, IconInfo p) : base(p.Location.X, p.Location.Y)
            {
                Closable = false;
                Disposable = false;
                Dragable = true;

                from.CloseGump(typeof(CustomDefenseGump));

                m_attachment = a;

                int specialcount = a.Specials.Count;

                // prepare the page
                AddPage(0);

                AddBackground(0, 0, 70, 75 + specialcount * vertspacing, 5054);
                AddLabel(13, 2, 55, "Defence");
                // if combos are still active then give it the green light
                if (HasActiveCombos(from))
                {
                    // green button
                    //AddImage( 20, 10, 0x2a4e );
                    AddImage(15, 25, 0x0a53);
                }
                else
                {
                    // red button
                    //AddImage( 20, 10, 0x2a62 );
                    //AddImage( 15, 25, 0x0a52 );
                    AddButton(15, 25, 0x0a52, 0x0a53, 9999, GumpButtonType.Reply, 0);
                }
                // go through the list of enabled moves and add buttons for them
                int y = 70;
                for (int i = 0; i < specialcount; ++i)
                {
                    SpecialDefense s = m_attachment.Specials[i];

                    // flag the defense as being selected
                    // this puts a white background behind the selected defense.  Doesnt look as nice, but works in both the
                    // 2D and 3D client.  I prefer to leave this commented out for best appearance in the 2D client but
                    // feel free to uncomment it for best client compatibility.
                    /*
					if(m_attachment != null && m_attachment.m_SelectedDefense != null && m_attachment.m_SelectedDefense == s)
					{
						AddImageTiled( 2, y-2, 66, vertspacing+2, 0xBBC );
					}
					*/

                    // add the defense button

                    if (s.IconType == IconTypes.ItemID)
                    {
                        AddButton(5, y, 0x5207, 0x5207, (int)s.DefenseID + 1000, GumpButtonType.Reply, 0);
                        AddImageTiled(5, y, 44, 44, 0x283E);
                        AddItem(5, y, s.Icon);
                    }
                    else
                    {
                        AddButton(5, y, s.Icon, s.Icon, (int)s.DefenseID + 1000, GumpButtonType.Reply, 0);
                    }

                    // flag the defense as being selected
                    // colors the defense icon red.  Looks better that the white background highlighting, but only supported by the 2D client.
                    if (m_attachment != null && m_attachment.m_SelectedDefense != null && m_attachment.m_SelectedDefense == s)
                    {
                        if (s.IconType == IconTypes.ItemID)
                        {
                            AddItem(5, y, s.Icon, 33);
                        }
                        else
                        {
                            AddImage(5, y, s.Icon, 33);
                        }
                    }


                    // add the info button
                    AddButton(52, y + 13, 0x4b9, 0x4b9, 2000 + (int)s.DefenseID, GumpButtonType.Reply, 0);

                    y += vertspacing;
                }

            }
            public override void OnResponse(NetState state, RelayInfo info)
            {
                BaseShield shield;
                Mobile m = state.Mobile;
                if (m_attachment == null || state == null || m == null || info == null || m.ShieldArmor != (shield = (BaseShield)m_attachment.AttachedTo) || !LastShieldType.TryGetValue(m, out Type t))
                {
                    return;
                }

                if (t != null && shield.GetType() != t)
                {
                    m.SendLocalizedMessage(1004603);//"Senti che ci metterai un pò, prima di abituarti ad uno scudo diverso");
                    ConstructDefenseGump(m, m_attachment);
                    return;
                }

                if (info.ButtonID == 9999)
                {
                    m.SendGump(new IconPlacementGump(this, m, m_attachment.m_IconInfo.Location.X, m_attachment.m_IconInfo.Location.Y, 30, 0x15D3, typeof(XmlCustomDefenses), 0, School.Speciali));
                    return;
                }

                int max = m_attachment.Specials.Count;
                // go through all of the possible specials and find the matching button
                for (int i = 0; i < max; ++i)
                {
                    SpecialDefense s = m_attachment.Specials[i];

                    if (s != null && info.ButtonID == (int)s.DefenseID + 1000)
                    {
                        // if clicked again, then deselect
                        if (s == m_attachment.m_SelectedDefense)
                        {
                            if (s.PreInitialize)
                            {
                                m_attachment.PreInitFailed(m, shield, 0);
                            }

                            m_attachment.m_SelectedDefense = null;
                        }
                        else
                        {
                            // see whether they have the required resources for this defense
                            if (CheckRequirements(m, s, true))
                            {
                                bool ok = true;

                                // check preinitialization of other attachment present, if present (null check mandatory)
                                if (m_attachment.m_SelectedDefense != null && m_attachment.m_SelectedDefense.PreInitialize)
                                {
                                    m_attachment.PreInitFailed(m, shield, 0);
                                }

                                if (s.PreInitialize)
                                {
                                    if (m.CanBeginAction(s))
                                    {
                                        ok = PreInit(m, m.Combatant, shield, s);
                                    }
                                    else
                                    {
                                        m.SendLocalizedMessage(1004606);//"Non puoi deselezionare e riselezionare un attacco di quel tipo così rapidamente, aspetta!");
                                        break;
                                    }
                                }
                                // if so, then let them select it
                                if (ok)
                                {
                                    m_attachment.m_SelectedDefense = s;
                                    ColpoSpeciale.DelaySpeciali(m);
                                }
                            }
                            else
                            {
                                // otherwise clear it
                                if (s.PermanentEffect && !m.CanBeginAction(s.DefenseID))
                                {
                                    SpecialDefenses sd = s.DefenseID;
                                    Timer.DelayCall(TimeSpan.FromSeconds(Math.Max(0, m_attachment.CoolDownCalculation(s, m))), delegate
                                    {
                                        if (m != null && !m.Deleted)
                                        {
                                            m.EndAction(sd);
                                        }
                                    });
                                }
                                m_attachment.m_SelectedDefense = null;
                            }
                        }

                        ConstructDefenseGump(m, m_attachment);
                        break;
                    }
                    else if (s != null && info.ButtonID == (int)s.DefenseID + 2000)
                    {
                        m.CloseGump(typeof(CustomDefenseInfoGump));
                        ConstructDefenseGump(m, m_attachment);
                        m.SendGump(new CustomDefenseInfoGump(m, m_attachment, s));
                        break;
                    }
                }
            }
        }
    }
}
