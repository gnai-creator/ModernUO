using Server.ACC.CM;
using Server.ACC.CSS;
using Server.ACC.CSS.Modules;
using Server.ACC.CSS.Systems.Druid;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Mobiles.Classi;
using Server.Network;
using Server.Spells;
using System;
using System.Collections.Generic;
using System.Text;

/*
** Originally written by ArteGordon
*/
namespace Server.Engines.XmlSpawner2
{
    public class XmlCustomAttacks : XmlAttachment
    {
        [Usage("attacco")]
        [Description("Usare il comando attacco seguito da un numero da 1 a 8, quel numero corrisponde all'attacco speciale presente nell'apposito gump, il primo in alto è il numero uno, seguito in ordine crescente verso il basso. Il comando può essere usato nelle macro degli assistenti. Inserendo il numero 0 verrà resettato il gump alla posizione iniziale in alto a sinistra. SE userete il numero 9 verrà effettuato un cambio tra uso di FRECCE avvelenate o TUTTE (indicato a schermo con messaggio).")]//Usare il comando attacco seguito da un numero da 1 a 8, quel numero corrisponde all'attacco speciale presente nell'apposito gump, il primo in alto è il numero uno, seguito in ordine crescente verso il basso. Il comando può essere usato nelle macro degli assistenti. Inserendo il numero 0 verrà resettato il gump alla posizione iniziale in alto a sinistra. SE userete il numero 9 verrà effettuato un cambio tra uso di FRECCE avvelenate o TUTTE (indicato a schermo con messaggio).")]
        private static void Attacco_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (e.Arguments.Length > 0)
            {
                if (int.TryParse(e.Arguments[0], out int num) && num >= 0 && num < 10)
                {
                    if (num < 9)
                    {
                        BaseWeapon weapon = m.Weapon;
                        if (weapon == null)
                        {
                            return;
                        }

                        if (XmlAttach.FindAttachment(weapon, typeof(XmlCustomAttacks)) is XmlCustomAttacks m_attachment && m_attachment.AttachedTo == weapon)
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
                                LastWeapType.TryGetValue(m, out Type t);
                                if (t != null && weapon.GetType() != t)
                                {
                                    m.SendLocalizedMessage(1004602);//"Senti che ci metterai un pò, prima di abituarti ad un'arma diversa");
                                    return;
                                }
                                SpecialAttack s = m_attachment.Specials[num - 1];
                                // if clicked again, then deselect
                                if (s == m_attachment.m_SelectedAttack)
                                {
                                    if (s.PermanentEffect && !m.CanBeginAction(s.AttackID))
                                    {
                                        SpecialAttacks sa = s.AttackID;
                                        Timer.DelayCall(TimeSpan.FromSeconds(Math.Max(0, m_attachment.CoolDownCalculation(s, m, weapon))), delegate
                                        {
                                            if (m != null && !m.Deleted)
                                            {
                                                m.EndAction(sa);
                                            }
                                        });
                                    }
                                    m_attachment.m_SelectedAttack = null;
                                }
                                else
                                {
                                    // see whether they have the required resources for this attack
                                    if (CheckRequirements(m, s, true))
                                    {
                                        bool ok = true;

                                        // if so, then let them select it
                                        if (s.PreInitialize)
                                        {
                                            if (m.CanBeginAction(s))
                                            {
                                                ok = m_attachment.PreInit(m, m.Combatant, weapon, s);
                                            }
                                            else
                                            {
                                                m.SendLocalizedMessage(1004605);//"Non puoi deselezionare e riselezionare un attacco di quel tipo così rapidamente, aspetta!");
                                            }
                                        }
                                        if (ok)
                                        {
                                            m_attachment.m_SelectedAttack = s;
                                        }
                                    }
                                    else
                                    {
                                        // otherwise clear it
                                        m_attachment.m_SelectedAttack = null;
                                    }
                                }
                                ConstructAttacksGump(m, m_attachment);
                            }
                        }
                    }
                    else
                    {
                        m.PoisonArrows = !m.PoisonArrows;
                        if (m.PoisonArrows)
                        {
                            m.SendLocalizedMessage(1004608);//"Hai scelto di usare solo munizioni avvelenate");
                        }
                        else
                        {
                            m.SendLocalizedMessage(1004609);//"Hai scelto di usare qualsiasi tipo di munizione");
                        }
                    }
                }
                else
                {
                    m.SendLocalizedMessage(1004607);//"Numero non valido nel comando (validi: 0 per reset posizione gump, da 1 a 8 per il colpi, 9 per impostazione frecce avvelenate)");
                }
            }
            else
            {
                m.SendLocalizedMessage(1004610);//"Inserire un valore numerico dopo il comando: da 1 a 8 per selezionare l'attacco, 9 per impostazione frecce avvelenate oppure 0 per resettare la posizione del gump");
            }
        }

        [Usage("attack")]
        [Description("Use the attack command followed by a number from 1 to 8, that number corresponds to the special attack present in the appropriate gump, the first at the top is the number one, followed in ascending order downwards. The command can be used in assistant macros. Entering the number 0 will reset the gump to the initial position at the top left. IF you use number 9, a change will be made between use of poisoned ARROWS or ALL (indicated on the screen with message).")]//Usare il comando attacco seguito da un numero da 1 a 8, quel numero corrisponde all'attacco speciale presente nell'apposito gump, il primo in alto è il numero uno, seguito in ordine crescente verso il basso. Il comando può essere usato nelle macro degli assistenti. Inserendo il numero 0 verrà resettato il gump alla posizione iniziale in alto a sinistra. SE userete il numero 9 verrà effettuato un cambio tra uso di FRECCE avvelenate o TUTTE (indicato a schermo con messaggio).")]
        private static void Attack_OnCommand(CommandEventArgs e)
        {
            Attacco_OnCommand(e);
        }

        // ------------------------------------------------------------------------------
        // BEGINNING of user-defined special attacks and combos information
        // ------------------------------------------------------------------------------

        //
        // define the Combo and special attack enums
        //
        // you must first add entries here if you wish to add new attacks
        //

        // COMBOS
        public enum ComboAttacks
        {
            MorteFulminea,
            ThunderStrike,
            LightningRain,
            SqueezingFist,
            FuocoFiamme,
            DeadlyVortex,
            SoulSteal,
            ElectricLightning,
            FastPoison,
            IceWind,
            Daemonshot
        }
        // ATTACKS
        public enum SpecialAttacks
        {
            FrecciaAvvelenata,
            Stordente,
            ColpoRapido,
            ColpoRapidoAscia,
            Dispel,
            Lacerante,
            LaceranteMinore,
            Immobilizzante,
            Mirato,
            Perforante,
            DoppiaFreccia,
            ColpoInCorsa,
            Affondo,
            Paralizzante,
            Mandritto,
            ColpoAlleGambe,
            Disarcionare,
            TempestaArmi,
            ColpoPossente,
            ColpoPossenteNano,
            Preciso,
            Bucante,
            Stoccata,
            Mortale,
            Critico,
            CriticoAscia,
            Devastante,
            Dirompente,
            AntiMana,
            Staminante,
            TerroreOscuro,
            VortexStrike,
            DaemonWolfStrike,
            DarkPantherStrike,
            BlueWolfStrike,
            UndeadWolfStrike,
            ColpoOmbra,
            Deconcentrante,
            LamaAvvelenata,
            Disarma,
            Fulminante,
            ColpoSacro,
            AnnientaMale,
            LuceDivina,
            ColpoAlleSpalle,
            FrecciaInfuocata,
            Allungamento,
            DispelMinore,
            FrecciaElettrica,
            FrecciaRaggelante
        }

        public static new void Initialize()
        {
            CommandSystem.Register("attacco", AccessLevel.Player, new CommandEventHandler(Attacco_OnCommand), null, LanguageType.Italian);
            CommandSystem.Register("attack", AccessLevel.Player, new CommandEventHandler(Attack_OnCommand), null, LanguageType.English);
            //
            // define the special attacks and their use requirements
            //
            // ideally, you have a definition for every SpecialAttacks enum.  Although it isnt absolutely necessary,
            // if it isnt defined here, it will not be available for use
            AddSpecialAttack(1004690,//Lancia una freccia avvelenata contro il bersaglio. La potenza del veleno è variabile in base al tipo di veleno e dell'abilità di avvelenare, il colpo inoltre risulterà più potente.
                SpecialAttacks.FrecciaAvvelenata, 0x521B, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 10, 0, 20,
                80, 110, 0,
                new SkillName[] { SkillName.Archery, SkillName.Poisoning },
                new int[] { 105, 60 },
                new Type[] { typeof(PoisonPotion) },
                new int[] { 1 },
                28,
                false, true
            );
            AddSpecialAttack(1004692, //"Il bersaglio si muoverà a velocità ridotta per un periodo di tempo limitato.",
                SpecialAttacks.Stordente, 0x5001, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 10, 0, 0,
                100, 100, 0,
                new SkillName[] { SkillName.Tactics, SkillName.Anatomy },
                new int[] { 100, 100 },
                null,
                null,
                25,
                false, true
            );
            AddSpecialAttack(1004694, //"Esegue un colpo rapido che farà meno danno e verrà eseguito nella metà del tempo necessario.",
                SpecialAttacks.ColpoRapido, 0x520F, IconTypes.GumpID, TimeSpan.FromSeconds(10),
                0, 5, 10, 0,
                90, 110, 0,
                new SkillName[] { SkillName.Tactics, SkillName.ArmsLore },
                new int[] { 100, 100 },
                new Type[] { typeof(AgilityPotion) },
                new int[] { 1 },
                21,
                true, true
            );
            AddSpecialAttack(1004694, //"Esegue un colpo rapido che farà meno danno e verrà eseguito nella metà del tempo necessario.",
                SpecialAttacks.ColpoRapidoAscia, 0x520F, IconTypes.GumpID, TimeSpan.FromSeconds(10),
                0, 5, 10, 0,
                90, 110, 0,
                new SkillName[] { SkillName.Tactics, SkillName.ArmsLore },
                new int[] { 100, 100 },
                new Type[] { typeof(AgilityPotion) },
                new int[] { 1 },
                21,
                true, true
            );
            AddSpecialAttack(1004696,//"Dissolvi Magie", "Il colpo così effettuato permette, mediante uso di una scroll dispel, di annullare gli effetti magici presenti sul bersaglio",
                SpecialAttacks.Dispel, 0x8E8, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                10, 0, 0, 0,
                90, 110, 20,
                new SkillName[] { SkillName.Archery, SkillName.Magery },
                new int[] { 105, 50 },
                new Type[] { typeof(DispelScroll) },
                new int[] { 1 },
                24,
                false, true
            );
            AddSpecialAttack(1004698,//"Lacerante", "Lancia una freccia che causa il sanguinamento della vittima.",
                SpecialAttacks.Lacerante, 0x5101, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 10, 0, 0,
                90, 110, 0,
                new SkillName[] { SkillName.Archery, SkillName.Tactics },
                new int[] { 105, 100 },
                null,//new Type[] { typeof(Brimstone) },
                null,//new int[] { 1 },
                28,
                false, true
            );
            AddSpecialAttack(1004700,//"Lacerante", "Causa il sanguinamento della vittima.",
                SpecialAttacks.LaceranteMinore, 0x5101, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 5, 0, 0,
                100, 110, 0,
                new SkillName[] { SkillName.Tactics, SkillName.Anatomy },
                new int[] { 100, 100 },
                null,//new Type[] { typeof(Brimstone) },
                null,//new int[] { 1 },
                28,
                false, true
            );
            AddSpecialAttack(1004702,//"Immobilizzante", "Lancia una freccia in un punto scoperto a grande velocità, disturbandone il movimento. L'attacco farà pochissimo danno, ma permette di bloccare quasiasi azione l'avversario stesse per compiere.",
                SpecialAttacks.Immobilizzante, 0x5218, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 4, 0, 0,
                90, 110, 0,
                new SkillName[] { SkillName.Archery, SkillName.Tactics },
                new int[] { 90, 90 },
                null,
                null,
                10,
                false, true
            );
            AddSpecialAttack(1004704,//"Mirato", "Esegue un colpo mirato, che colpisce un punto vitale, questo colpo ha maggior effetto su bersagli con armature poco protettive.",
                SpecialAttacks.Mirato, 0x521A, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 4, 0, 0,
                90, 110, 0,
                new SkillName[] { SkillName.Archery, SkillName.Tactics },
                new int[] { 90, 90 },
                null,
                null,
                13,
                false, true
            );
            AddSpecialAttack(1004706,//"Perforante", "Permette di eseguire un colpo che penetra a fondo nell'armatura, causando maggiore danno. Su armature leggere o su bersagli senza armatura questo colpo non aumenta il danno",
                SpecialAttacks.Perforante, 0x5216, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 4, 0, 0,
                90, 110, 0,
                new SkillName[] { SkillName.Archery, SkillName.Tactics },
                new int[] { 100, 100 },
                null,
                null,
                17,
                false, true
            );
            AddSpecialAttack(1004708,//"Doppia Freccia", "Permette di lanciare due frecce quasi in contemporanea, aventi un unico bersaglio, dopo la prima freccia scagliata, l'effetto debilitante di questa mossa causerà una paralisi temporanea.", 
                SpecialAttacks.DoppiaFreccia, 0x5215, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 10, 0, 0,
                90, 110, 0,
                new SkillName[] { SkillName.Archery },
                new int[] { 105 },
                null,
                null,
                19,
                false, true
            );
            AddSpecialAttack(1004710,//"Colpo in Corsa", "Permette di lanciare una freccia con il normale delay di caricamento colpo, ma in corsa.", 
                SpecialAttacks.ColpoInCorsa, 0x5209, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 15, 0, 0,
                90, 110, 0,
                new SkillName[] { SkillName.Archery },
                new int[] { 105 },
                null,
                null,
                23,
                true, true
            );
            AddSpecialAttack(1004712,//"Affondo", "Esegue un colpo di punta, ottimo danno anche contro le armature. Non aumenta il normale danno contro armature di livello basso.", 
                SpecialAttacks.Affondo, 0x5200, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 4, 0, 0,
                110, 100, 0,
                new SkillName[] { SkillName.Fencing },
                new int[] { 105 },
                null,
                null,
                13,
                false, true
            );
            AddSpecialAttack(1004714,//"Paralizzante", "Un colpo ben assestato che permette di fermare qualsiasi azione il bersaglio stesse per compiere.",
                SpecialAttacks.Paralizzante, 0x5210, IconTypes.GumpID, TimeSpan.FromSeconds(10),
                0, 4, 0, 0,
                100, 110, 0,
                new SkillName[] { SkillName.Tactics },
                new int[] { 100 },
                null,
                null,
                16,
                false, true
            );
            AddSpecialAttack(1004716,//"Mandritto", "Un colpo ben assestato che permette di fermare qualsiasi azione il bersaglio stesse per compiere.",
                SpecialAttacks.Mandritto, 0x5210, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 4, 0, 0,
                100, 110, 0,
                new SkillName[] { SkillName.Tactics },
                new int[] { 100 },
                null,
                null,
                15,
                false, true
            );
            AddSpecialAttack(1004718,//"Colpo alle Gambe", "Colpo mirato alle gambe, perdita di stamina al bersaglio, che dipenderà dalla forza del colpo inferto.", 
                SpecialAttacks.ColpoAlleGambe, 0x5325, IconTypes.GumpID, TimeSpan.FromSeconds(10),
                0, 10, 0, 0,
                110, 100, 0,
                new SkillName[] { SkillName.Macing, SkillName.Wrestling },
                new int[] { 105, 100 },
                null,
                null,
                23,
                false, true
            );
            AddSpecialAttack(1004720,//"Disarcionare", "Il colpo permette di far cadere il bersaglio da cavallo. Tale mossa farà scendere da cavallo anche a te, visto lo sforzo richiesto non riuscirai a muoverti per un po di tempo, se il colpo viene eseguito a piedi l'effetto incapacitante durerà molto meno.", 
                SpecialAttacks.Disarcionare, 0x5205, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 8, 0, 0,
                110, 100, 0,
                new SkillName[] { SkillName.Fencing, SkillName.Macing },
                new int[] { 100, 100 },
                null,
                null,
                30,
                true, true
            );
            AddSpecialAttack(1004722,//"Tempesta d'Armi", "Rotea l'arma ad alta velocità, causa danno minore a chiunque sia attorno a te.", 
                SpecialAttacks.TempestaArmi, 0x520E, IconTypes.GumpID, TimeSpan.FromSeconds(10),
                0, 5, 0, 0,
                110, 110, 0,
                new SkillName[] { SkillName.Tactics },
                new int[] { 100 },
                new Type[] { typeof(AgilityPotion) },
                new int[] { 1 },
                28,
                false, true
            );
            AddSpecialAttack(1004724,//"Colpo Possente", "L'uso della forza bruta e di un'arma contundente aumenterà il danno inferto, causando però un temporaneo abbassamento della difesa.", 
                SpecialAttacks.ColpoPossente, 0x5008, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 5, 0, 0,
                110, 110, 0,
                new SkillName[] { SkillName.Tactics },
                new int[] { 100 },
                null,
                null,
                18,
                true, true
            );
            AddSpecialAttack(1004724,//"Colpo Possente", "L'uso della forza bruta e di un'arma contundente aumenterà il danno inferto, causando però un temporaneo abbassamento della difesa.", 
                SpecialAttacks.ColpoPossenteNano, 0x5008, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 5, 0, 0,
                110, 110, 0,
                new SkillName[] { SkillName.Tactics },
                new int[] { 100 },
                null,
                null,
                18,
                true, true
            );
            AddSpecialAttack(1004726,//"Colpo Preciso", "L'uso della balestra ti permette un colpo preciso, che infligge un maggiore danno, a differenza del mirato, che non penetra bene nelle armature, questo è leggermente superiore.", 
                SpecialAttacks.Preciso, 0x521A, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 4, 0, 0,
                100, 110, 0,
                new SkillName[] { SkillName.Archery, SkillName.Tactics },
                new int[] { 105, 100 },
                null,
                null,
                14,
                false, true
            );
            AddSpecialAttack(1004728,//"Colpo Bucante", "Un tiro andato a segno ti permetterà di sfondare le difese nemiche, ottimo danno contro le armature pesanti.",        // attack name, and description
                SpecialAttacks.Bucante, 0x5203, IconTypes.GumpID, TimeSpan.FromSeconds(8),                   // attack id, id of gump icon, and chaining time
                0, 5, 0, 0,                                                                    // mana, stam, hits, karma usage
                110, 110, 0,                                                                        // str, dex, int requirements
                new SkillName[] { SkillName.Tactics, SkillName.Anatomy },                                        //  skill requirement list
                new int[] { 100, 100 },                                                               // minimum skill levels
                null,                                              // reagent list
                null,                                                               // reagent quantities
                15,
                false, true
            );
            AddSpecialAttack(1004730,//"Stoccata", "Buon danno contro le armature, questo tipo di attacco ha un effetto debilitante sul nemico.", 
                SpecialAttacks.Stoccata, 0x520D, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 4, 0, 0,
                110, 110, 0,
                new SkillName[] { SkillName.Fencing, SkillName.Tactics },
                new int[] { 100, 100 },
                null,
                null,
                13,
                false, true
            );
            AddSpecialAttack(1004732,//"Colpo Mortale", "Danno molto elevato fatto dall'arma, con un costo notevole di stamina e vita.",        // attack name, and description
                SpecialAttacks.Mortale, 0x5107, IconTypes.GumpID, TimeSpan.FromSeconds(8),                   // attack id, id of gump icon, and chaining time
                0, 5, 20, 0,                                                                    // mana, stam, hits, karma usage
                100, 110, 0,                                                                        // str, dex, int requirements
                new SkillName[] { SkillName.Tactics, SkillName.ArmsLore },                                        //  skill requirement list
                new int[] { 100, 100 },                                                               // minimum skill levels
                null,                                              // reagent list
                null,                                                               // reagent quantities
                56,
                false, true
            );
            AddSpecialAttack(1004734,//"Critico", "Aumenta il danno inferto a spese di parte della propria vita e stamina.",        // attack name, and description
                SpecialAttacks.Critico, 0x5202, IconTypes.GumpID, TimeSpan.FromSeconds(10),                   // attack id, id of gump icon, and chaining time
                0, 5, 10, 0,                                                                    // mana, stam, hits, karma usage
                110, 110, 0,                                                                        // str, dex, int requirements
                new SkillName[] { SkillName.Tactics, SkillName.Anatomy },                                        //  skill requirement list
                new int[] { 100, 100 },                                                               // minimum skill levels
                null,                                              // reagent list
                null,                                                               // reagent quantities
                13,
                false, true
            );
            AddSpecialAttack(1004734,//"Critico", "Aumenta il danno inferto a spese di parte della propria vita e stamina.",        // attack name, and description
                SpecialAttacks.CriticoAscia, 0x5202, IconTypes.GumpID, TimeSpan.FromSeconds(10),                   // attack id, id of gump icon, and chaining time
                0, 5, 10, 0,                                                                    // mana, stam, hits, karma usage
                110, 110, 0,                                                                        // str, dex, int requirements
                new SkillName[] { SkillName.Tactics, SkillName.Anatomy },                                        //  skill requirement list
                new int[] { 100, 100 },                                                               // minimum skill levels
                null,                                              // reagent list
                null,                                                               // reagent quantities
                13,
                false, true
            );
            AddSpecialAttack(1004736,//"Devastante", "Sfonda le difese nemiche con la forza bruta.",        // attack name, and description
                SpecialAttacks.Devastante, 0x5203, IconTypes.GumpID, TimeSpan.FromSeconds(8),                   // attack id, id of gump icon, and chaining time
                0, 5, 0, 0,                                                                    // mana, stam, hits, karma usage
                110, 110, 0,                                                                        // str, dex, int requirements
                new SkillName[] { SkillName.Tactics, SkillName.ArmsLore },                                        //  skill requirement list
                new int[] { 100, 100 },                                                               // minimum skill levels
                null,                                              // reagent list
                null,                                                               // reagent quantities
                15,
                false, true
            );
            AddSpecialAttack(1004738,//"Dirompente", "Tende a sfiancare il nemico, colpendolo in punti nevralgici.",        // attack name, and description
                SpecialAttacks.Dirompente, 0x59DA, IconTypes.GumpID, TimeSpan.FromSeconds(8),                   // attack id, id of gump icon, and chaining time
                0, 5, 0, 0,                                                                    // mana, stam, hits, karma usage
                110, 110, 0,                                                                        // str, dex, int requirements
                new SkillName[] { SkillName.Tactics, SkillName.Anatomy, SkillName.ArmsLore },                                        //  skill requirement list
                new int[] { 100, 100, 100 },                                                               // minimum skill levels
                null,                                              // reagent list
                null,                                                               // reagent quantities
                17,
                false, true
            );
            AddSpecialAttack(1004740,//"Vuoto Mentale", "Toglie mana al bersaglio.",
                SpecialAttacks.AntiMana, 0x5007, IconTypes.GumpID, TimeSpan.FromSeconds(8),  // explicitly specifying the gump icon as a gumpid
                0, 10, 0, 0,
                80, 80, 0,
                new SkillName[] { SkillName.Tactics, SkillName.Anatomy },
                new int[] { 100, 100 },
                null,
                null,
                23,
                false, true
            );
            AddSpecialAttack(1004742,//"Colpo Vorticoso", "Evoca un energy vortex per aiutarti in battaglia",
                SpecialAttacks.VortexStrike, 0x20b9, IconTypes.ItemID, TimeSpan.FromSeconds(8), // example of using an itemid for the gump icon
                40, 10, 0, 0,
                0, 0, 30,
                new SkillName[] { SkillName.Tactics },
                new int[] { 130 },
                new Type[] { typeof(EnergyVortexScroll) },
                new int[] { 1 },
                14,
                false, true
            );
            AddSpecialAttack(1004772,//"Daemon Wolf", "Evoca un DaemonWolf per aiutarti in battaglia",
                SpecialAttacks.DaemonWolfStrike, 0x2394, IconTypes.ItemID, TimeSpan.FromSeconds(7), // example of using an itemid for the gump icon
                10, 0, 0, 0,
                90, 110, 20,
                new SkillName[] { SkillName.Archery },
                new int[] { 105 },
                new Type[] { typeof(SpringWater) },
                new int[] { 5 },
                14,
                false, true
            );
            AddSpecialAttack(1004774,//"Dark Panther", "Evoca un Dark Panther per aiutarti in battaglia",
                SpecialAttacks.DarkPantherStrike, 0x2395, IconTypes.ItemID, TimeSpan.FromSeconds(7), // example of using an itemid for the gump icon
                10, 0, 0, 0,
                90, 110, 20,
                new SkillName[] { SkillName.Archery },
                new int[] { 110 },
                new Type[] { typeof(SpringWater) },
                new int[] { 5 },
                14,
                false, true
            );
            AddSpecialAttack(1004776,//"Blue Wolf", "Evoca un Blue Wolf per aiutarti in battaglia",
                SpecialAttacks.BlueWolfStrike, 0x2396, IconTypes.ItemID, TimeSpan.FromSeconds(7), // example of using an itemid for the gump icon
                10, 0, 0, 0,
                90, 110, 20,
                new SkillName[] { SkillName.Archery },
                new int[] { 115 },
                new Type[] { typeof(SpringWater) },
                new int[] { 5 },
                14,
                false, true
            );
            AddSpecialAttack(1004778,//"Undead Wolf", "Evoca un Undead Wolf per aiutarti in battaglia",
                SpecialAttacks.UndeadWolfStrike, 0x2397, IconTypes.ItemID, TimeSpan.FromSeconds(7), // example of using an itemid for the gump icon
                10, 0, 0, 0,
                90, 110, 20,
                new SkillName[] { SkillName.Archery },
                new int[] { 120 },
                new Type[] { typeof(SpringWater) },
                new int[] { 5 },
                14,
                false, true
            );
            AddSpecialAttack(1004744,//"Colpo Staminante", "Debilita il bersaglio, togliendo parte della sua stamina.",
                SpecialAttacks.Staminante, 0x500e, IconTypes.GumpID, TimeSpan.FromSeconds(8),                // if the icon type is not specified, gump icons use gumpids
                0, 10, 0, 0,
                110, 100, 0,
                new SkillName[] { SkillName.Tactics, SkillName.Anatomy },
                new int[] { 100, 100 },
                null,
                null,
                24,
                false, true
            );
            AddSpecialAttack(1004746,//"Terrore Oscuro", "Paralizza il bersaglio.",
                SpecialAttacks.TerroreOscuro, 0x500d, IconTypes.GumpID, TimeSpan.FromSeconds(10),
                10, 0, 0, 0,
                0, 0, 0,
                new SkillName[] { SkillName.Tactics, SkillName.Magery },
                new int[] { 100, 50 },
                new Type[] { typeof(ParalyzeScroll) },
                new int[] { 1 },
                30,
                false, true
            );
            AddSpecialAttack(1004748,//"Colpo d'Ombra", "Ti rende invisibile dopo l'attacco, causa inoltre un danno lievemente maggiore, soprattutto su armature medio-alte.",
                SpecialAttacks.ColpoOmbra, 0x5DC3, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                10, 5, 0, 0,
                0, 100, 0,
                new SkillName[] { SkillName.Stealth, SkillName.Hiding },
                new int[] { 100, 100 },
                new Type[] { typeof(VolcanicAsh) },
                new int[] { 1 },
                25,
                false, true
            );
            AddSpecialAttack(1004750,//"Deconcentrante", "Un colpo ben assestato che permette di fermare qualsiasi azione il bersaglio stesse per compiere.",
                SpecialAttacks.Deconcentrante, 0x5210, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 4, 0, 0,
                100, 110, 0,
                new SkillName[] { SkillName.Tactics },
                new int[] { 100 },
                null,
                null,
                18,
                false, true
            );
            AddSpecialAttack(1004752,//"Lama Avvelenata", "Avvelena la lama prima di scagliarla contro il bersaglio. La potenza del veleno è variabile in base al tipo di veleno e dell'abilità di avvelenare, la lama rimarrà inoltre intrisa di veleno, sebbene il primo colpo risulterà meno efficace.",
                SpecialAttacks.LamaAvvelenata, 0x521B, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 10, 0, 20,
                100, 90, 0,
                new SkillName[] { SkillName.Tactics, SkillName.Poisoning },
                new int[] { 105, 60 },
                new Type[] { typeof(PoisonPotion) },
                new int[] { 1 },
                27,
                false, true
            );
            AddSpecialAttack(1004754,//"Disarma", "Con questo tipo di equipaggiamento ti è possibile tentare un colpo che disarmi l'avversario.",
                SpecialAttacks.Disarma, 0x5204, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 10, 0, 0,
                110, 20, 0,
                new SkillName[] { SkillName.Wrestling, SkillName.Tactics },
                new int[] { 100, 80 },
                null,
                null,
                29,
                false, true
            );
            AddSpecialAttack(1004756,//"Colpo Fulminante", "Con l'arma in uso il dardo verrà sparato ad una tale velocità da lasciare quasi inerme il bersaglio, facendolo muovere a velocità ridotta. Tale forza richiede comunque un enorme sforzo e quindi rimarrai inerme a tua volta per un breve periodo.",
                SpecialAttacks.Fulminante, 0x5001, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 10, 0, 0,
                120, 100, 0,
                new SkillName[] { SkillName.Tactics, SkillName.Anatomy, SkillName.ArmsLore },
                new int[] { 100, 100, 100 },
                null,
                null,
                25,
                false, true
            );
            AddSpecialAttack(1004758,//"Colpo Sacro", "Con l'arma a tua disposizione puoi effettuare un colpo che avrà maggiore efficacia contro le creature non-morte in generale.",
                SpecialAttacks.ColpoSacro, 0x59DA, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                10, 10, 0, 0,
                100, 20, 20,
                new SkillName[] { SkillName.SpiritSpeak, SkillName.Macing },
                new int[] { 95, 100 },
                null,
                null,
                20,
                false, false
            );
            AddSpecialAttack(1004760,//"Annienta-Male", "Con la forza divina puoi conferire una forza energetica all'arma, tale da riuscire a fornire un grande danno alla creatura Malvagia.",
                SpecialAttacks.AnnientaMale, 0x59E1, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                10, 10, 0, 0,
                100, 20, 20,
                new SkillName[] { SkillName.SpiritSpeak, SkillName.Chivalry },
                new int[] { 90, 90 },
                null,
                null,
                20,
                false, false
            );
            AddSpecialAttack(1004762,//"Luce Divina", "Aumentando la potenza energetica dell'arma, conferisci danno extra, accecando temporaneamente il nemico.",
                SpecialAttacks.LuceDivina, 0x59DB, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                10, 10, 0, 0,
                90, 20, 30,
                new SkillName[] { SkillName.Chivalry, SkillName.SpiritSpeak, SkillName.Focus },
                new int[] { 100, 100, 70 },
                null,
                null,
                30,
                false, false
            );
            AddSpecialAttack(1004764,//"Colpo alle Spalle", "Puoi colpire il tuo bersaglio infliggendo molto danno, purchè tu rimanga dietro le spalle di questi.",
                SpecialAttacks.ColpoAlleSpalle, 0x59E4, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                10, 10, 0, 0,
                90, 110, 30,
                new SkillName[] { SkillName.Hiding, SkillName.Stealth, SkillName.Fencing },
                new int[] { 100, 100, 105 },
                null,
                null,
                30,
                false, false
            );
            AddSpecialAttack(1004766,//"Freccia Incendiaria", "Con l'arma in uso il dardo verrà sparato ad una enorme velocità, la miscela della freccia va arricchita con una sostanza esplosiva, che la rende incendiaria.",
                SpecialAttacks.FrecciaInfuocata, 0x8EA, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 10, 0, 0,
                100, 110, 0,
                new SkillName[] { SkillName.Tactics, SkillName.ArmsLore, SkillName.Alchemy },
                new int[] { 100, 100, 90 },
                new Type[] { typeof(ExplosionPotion) },
                new int[] { 1 },
                24,
                false, true
            );

            AddSpecialAttack(1004768,//"Allungamento", "Con l'arma in uso ti è possibile impugnarla su un lato, anziché al centro, permettendoti di raggiungere punti più lontani (2 tile), questo uso inibisce le altre abilità dell'arma.",
                SpecialAttacks.Allungamento, 0x5211, IconTypes.GumpID, TimeSpan.FromSeconds(8),
                0, 2, 0, 0,
                100, 0, 0,
                new SkillName[] { SkillName.Macing, SkillName.Wrestling },
                new int[] { 80, 90 },
                null,
                null,
                16,
                false, true, true
            );
            AddSpecialAttack(1004770,//"Dissolvi Magie", "Il colpo così effettuato permette, mediante uso di una scroll dispel, di annullare gli effetti magici presenti sul bersaglio",
                SpecialAttacks.DispelMinore, 0x8E8, IconTypes.GumpID, TimeSpan.FromSeconds(10),
                10, 0, 0, 0,
                90, 110, 20,
                new SkillName[] { SkillName.Tactics, SkillName.Inscribe },
                new int[] { 105, 50 },
                new Type[] { typeof(DispelScroll) },
                new int[] { 1 },
                14,
                false, true
            );
            //ADDED FOR NEW BOW ELECTRIC
            AddSpecialAttack(1116793,//"Freccia elettrica", "Con l'arma in uso il dardo verrà potenziato diventando altamente conduttore, grazie all'applicazione di una miscela blu si ha la capacità di scaricare un colpo eletrico aumentando il danno inflitto al bersaglio.",
              SpecialAttacks.FrecciaElettrica, 0x8E9, IconTypes.GumpID, TimeSpan.FromSeconds(8),
              0, 10, 0, 0,
              100, 110, 0,
              new SkillName[] { SkillName.Tactics, SkillName.ArmsLore, SkillName.Alchemy },
              new int[] { 100, 100, 90 },
              new Type[] { typeof(AgilityPotion) },
              new int[] { 1 },
              24,
              false, true
          );
            //ADDED FOR NEW BOW ICE
            AddSpecialAttack(1116800,//"Freccia Raggelante", "Lancia una freccia in un punto scoperto a grande velocità, disturbandone il movimento. L'attacco farà pochissimo danno, ma permette di bloccare quasiasi azione l'avversario stesse per compiere.",
              SpecialAttacks.FrecciaRaggelante, 0x5218, IconTypes.GumpID, TimeSpan.FromSeconds(8),
              0, 4, 0, 0,
              90, 110, 0,
              new SkillName[] { SkillName.Archery, SkillName.Tactics },
              new int[] { 90, 90 },
              null,
              null,
              20,
              false, true
           );
            //
            // define combos and the sequence of special attacks needed to activate them
            //
            AddComboAttack(1004790, //"Fuoco e Fiamme",
                ComboAttacks.FuocoFiamme,
                new SpecialAttacks[]
                {
                    SpecialAttacks.Fulminante,
                    SpecialAttacks.ColpoRapido,
                    SpecialAttacks.FrecciaInfuocata
                }
            );
            AddComboAttack(1004791, //"Vortice di Lame",
                ComboAttacks.DeadlyVortex,
                new SpecialAttacks[]
                {
                    SpecialAttacks.TempestaArmi,
                    SpecialAttacks.Paralizzante,
                    SpecialAttacks.ColpoRapido
                }
            );
            AddComboAttack(1004792, //"Morte Fulminea",
                ComboAttacks.ThunderStrike,                       // combo name, and id
                new SpecialAttacks[]                                                           // list of special attacks needed to complete the combo
					{
                        XmlCustomAttacks.SpecialAttacks.Critico,
                        XmlCustomAttacks.SpecialAttacks.Paralizzante,
                        XmlCustomAttacks.SpecialAttacks.DispelMinore
                    }
            );
            AddComboAttack(1004793, //"Cattura Anima",
                ComboAttacks.SoulSteal,                       // combo name, and id
                new SpecialAttacks[]                                                           // list of special attacks needed to complete the combo
					{
                        XmlCustomAttacks.SpecialAttacks.Affondo,
                        XmlCustomAttacks.SpecialAttacks.LaceranteMinore,
                        XmlCustomAttacks.SpecialAttacks.AntiMana
                    }
            );
            //ADDED FOR NEW BOW ELECTRIC
            AddComboAttack(1116795, //"Fuoco elettrico",
              ComboAttacks.ElectricLightning,
              new SpecialAttacks[]
              {
                    SpecialAttacks.Fulminante,
                    SpecialAttacks.ColpoRapido,
                    SpecialAttacks.FrecciaElettrica
              }
          );
            //ADDED FOR NEW BOW ACID
            AddComboAttack(1116798, //"Fuoco Velenoso",
              ComboAttacks.FastPoison,
              new SpecialAttacks[]
              {
                  SpecialAttacks.FrecciaAvvelenata,
                  SpecialAttacks.Fulminante,
                  SpecialAttacks.ColpoRapido
              }
          );
            //ADDED FOR NEW BOW ICE
            AddComboAttack(1116803, //"Vento gelido",
              ComboAttacks.IceWind,
              new SpecialAttacks[]
              {
                  SpecialAttacks.ColpoRapido,
                  SpecialAttacks.FrecciaRaggelante,
                  SpecialAttacks.Fulminante
              }
          );
            //ADDED FOR DAEMONBOW/DREAMLINE
            AddComboAttack(1116882, //"Daemon Shot",
              ComboAttacks.Daemonshot,
              new SpecialAttacks[]
              {
                  SpecialAttacks.Immobilizzante,
                  SpecialAttacks.Mirato,
                  SpecialAttacks.Perforante
              }
          );

            /*AddComboAttack( "Morte Fulminea", ComboAttacks.MorteFulminea,
				new SpecialAttacks[]
				{
					SpecialAttacks.TripleSlash,
					SpecialAttacks.TempestaArmi,
					SpecialAttacks.TempestaArmi
				}
			);
			AddComboAttack( "Colpo Fulminante", ComboAttacks.ThunderStrike,                       // combo name, and id
				new SpecialAttacks []                                                           // list of special attacks needed to complete the combo
					{
					SpecialAttacks.TripleSlash,
					SpecialAttacks.MindDrain,
					SpecialAttacks.ParalyzingFear,
					SpecialAttacks.TripleSlash,
					SpecialAttacks.StamDrain
					}
			);

			AddComboAttack( "Pioggia di Fulmini", ComboAttacks.LightningRain,
				new SpecialAttacks []
					{
					SpecialAttacks.TripleSlash,
					SpecialAttacks.MindDrain,
					SpecialAttacks.MindDrain,
					SpecialAttacks.StamDrain
					}
			);

			AddComboAttack( "Colpo Schiacciante", ComboAttacks.SqueezingFist,
				new SpecialAttacks []
					{
					SpecialAttacks.MindDrain,
					SpecialAttacks.StamDrain
					}
			);*/

            // after deser, restore combo and specials lists to all existing CustomAttacks attachments based on these definitions
            //not needed since attach is created dinamically
            /*foreach(XmlAttachment x in XmlAttach.AllAttachments.Values)
            {
                if(x is XmlCustomAttacks)
                {
                    //((XmlCustomAttacks)x).FillComboList();
                    ((XmlCustomAttacks)x).FillSpecialsList();
                }
            }*/
            //EventSink.SetAbility += new SetAbilityEventHandler( EventSink_SetAbilityReceived );
        }

        //		private static void EventSink_SetAbilityReceived( SetAbilityEventArgs e )
        //		{
        //			if(e.Index<1000 && e.Mobile!=null)
        //			{
        //				Mobile m = e.Mobile;
        //				Gump g = m.FindGump(typeof(CustomAttacksGump));
        //				if(g!=null)
        //				{
        //					g.OnResponse(m.NetState, new RelayInfo(e.Index, null, null));
        //				}
        //			}
        //		}

        public void DelayedDefenseMalusCheck(float timer, SpecialAttack special, BaseWeapon weapon, Mobile attacker)
        {
            if (attacker != null)
            {
                if (m_SelectedAttack != null && m_SelectedAttack == special && weapon == attacker.Weapon && timer > 0)
                {
                    Timer.DelayCall(TimeSpan.FromSeconds(timer), () => DelayedDefenseMalusCheck(timer, special, weapon, attacker));
                }
                else
                {
                    attacker.ReceivedDamageMod = 1;
                }
            }
        }

        public bool PreInit(Mobile attacker, Mobile defender, BaseWeapon weapon, SpecialAttack special)
        {
            //DEFENDER PUO' ESSERE NULL!! ***ATTENZIONE!!!***
            switch (special.AttackID)
            {
                case SpecialAttacks.ColpoRapido:
                {
                    if (defender == null)
                    {
                        attacker.SendLocalizedMessage(1005059);//"Per farlo funzionare dovresti prima attaccare un bersaglio!");
                        return false;
                    }
                    attacker.SendLocalizedMessage(1005058);//"Ti prepari per l'attacco rapido...");
                    attacker.BeginAction(special);
                    Mobile trg = defender;
                    double tmnct = (attacker.NextCombatTime.Subtract(DateTime.UtcNow).TotalMilliseconds * 0.4);
                    DateTime nct = DateTime.UtcNow + TimeSpan.FromMilliseconds(tmnct);
                    SpecialAttack sa = special;
                    Timer.DelayCall(TimeSpan.FromMilliseconds(tmnct), delegate
                    {
                        if (attacker != null && !attacker.Deleted)
                        {
                            attacker.EndAction(sa);
                            if (defender != null && !defender.Deleted && attacker.Combatant == trg && attacker.NextCombatTime > nct && m_SelectedAttack == sa)
                            {
                                attacker.NextCombatTime = DateTime.UtcNow;
                                attacker.colpo_caricato_war = true;
                                attacker.RevealingAction();
                                attacker.HidingProgess = false;
                            }
                        }
                    });
                    break;
                }
                case SpecialAttacks.ColpoRapidoAscia:
                    {
                        if (defender == null)
                        {
                            attacker.SendLocalizedMessage(1005059);//"Per farlo funzionare dovresti prima attaccare un bersaglio!");
                            return false;
                        }
                        attacker.SendLocalizedMessage(1005058);//"Ti prepari per l'attacco rapido...");
                        attacker.BeginAction(special);
                        Mobile trg = defender;
                        double tmnct = (attacker.NextCombatTime.Subtract(DateTime.UtcNow).TotalMilliseconds * 0.4);
                        DateTime nct = DateTime.UtcNow + TimeSpan.FromMilliseconds(tmnct);
                        SpecialAttack sa = special;
                        Timer.DelayCall(TimeSpan.FromMilliseconds(tmnct), delegate
                        {
                            if (attacker != null && !attacker.Deleted)
                            {
                                attacker.EndAction(sa);
                                if (defender != null && !defender.Deleted && attacker.Combatant == trg && attacker.NextCombatTime > nct && m_SelectedAttack == sa)
                                {
                                    attacker.NextCombatTime = DateTime.UtcNow;
                                    attacker.colpo_caricato_war = true;
                                    attacker.RevealingAction();
                                    attacker.HidingProgess = false;
                                }
                            }
                        });
                        break;
                    }
                case SpecialAttacks.ColpoInCorsa:
                {
                    if (defender == null)
                    {
                        attacker.SendLocalizedMessage(1005057);//"Per farlo funzionare dovresti prima attaccare un bersaglio!");
                        return false;
                    }
                    attacker.SendLocalizedMessage(1005056);//Prepari il colpo in corsa...
                    attacker.BeginAction(special);
                    Mobile trg = defender;
                    DateTime nct = attacker.NextCombatTime;
                    SpecialAttack sa = special;
                    Timer.DelayCall(attacker.NextCombatTime.Subtract(DateTime.UtcNow).Subtract(TimeSpan.FromMilliseconds(100)), delegate
                    {
                        if (attacker != null && !attacker.Deleted)
                        {
                            attacker.EndAction(sa);
                            if (defender != null && !defender.Deleted && attacker.Combatant == trg && attacker.NextCombatTime >= nct && m_SelectedAttack == sa)
                            {
                                //attacker.moved_oncharging_attack=false;
                                attacker.colpo_caricato_arcer = true;
                                attacker.RevealingAction();
                                attacker.HidingProgess = false;
                            }
                        }
                    });
                    break;
                }
                case SpecialAttacks.ColpoPossente:
                {
                    attacker.ReceivedDamageMod = 1.5f;
                    Timer.DelayCall(() => DelayedDefenseMalusCheck(3, special, weapon, attacker));
                    break;
                }
                case SpecialAttacks.ColpoPossenteNano:
                    {
                        attacker.ReceivedDamageMod = 1.4f;
                        Timer.DelayCall(() => DelayedDefenseMalusCheck(3, special, weapon, attacker));
                        break;
                    }
                case SpecialAttacks.Disarcionare:
                {
                    if (!attacker.Mounted && attacker.ClasseID != (int)ClassiEnum.Paladino)
                    {
                        attacker.SendLocalizedMessage(1005055, "", 0x22);
                    }

                    break;
                }
                default:
                    attacker.EndAction(special);
                    break;
            }

            return true;
        }

        public void PreInitFailed(Mobile attacker, BaseWeapon weapon, ref int damageGiven)
        {
            switch (m_SelectedAttack.AttackID)
            {
                case SpecialAttacks.ColpoRapido:
                {
                    damageGiven = 1;
                    break;
                }
                case SpecialAttacks.ColpoRapidoAscia:
                    {
                        damageGiven = 1;
                        break;
                    }
                case SpecialAttacks.ColpoInCorsa:
                {
                    damageGiven = 1;
                    break;
                }
                default:
                    break;
            }
        }

        //
        // carry out the special attacks
        //
        // If you add a new attack, you must add the code here to define what it actually does when it hits
        //
        public void DoSpecialAttack(Mobile attacker, Mobile defender, BaseWeapon weapon, ref int damageGiven, SpecialAttack special, int originalDamage)
        {
            attacker.SendLocalizedMessage(1005001, string.Format("#{0}", special.Name));//"Esegui {0}!", special.Name);
            ColpoSpeciale.DelaySpeciali(attacker);

            // apply the special attack
            switch (special.AttackID)
            {
                case SpecialAttacks.FrecciaAvvelenata:
                {
                    if (m_ItemUsed == null || m_ItemUsed.Count == 0 || !(m_ItemUsed[0] is PoisonPotion))
                    {
                        break; //non dovrebbe mai succedere, ma il mondo è strano a volte...
                    }

                    PoisonPotion pp = (PoisonPotion)m_ItemUsed[0];
                    int level = (int)(((((Math.Min(attacker.Skills.Poisoning.Value, 100) - 80) + (pp.Effect * 0.5f)) / 10) + (Math.Min(attacker.Skills.Anatomy.Value, 200) * 0.005f)) - 1);
                    level = Math.Max(1, Math.Min(level, 4));
                    defender.ApplyPoison(attacker, Poison.GetPoison(level));
                    attacker.MovingParticles(defender, 0x389D, 7, 0, false, true, 9502, 4019, 0x160);
                    damageGiven = (int)(damageGiven * 1.3f);
                    break;
                }
                case SpecialAttacks.Stordente:
                {
                    damageGiven = (int)(damageGiven * 0.7f);
                    float duration = ((Math.Max(0, Math.Min(attacker.Skills.Tactics.Value, 200)) - 100) * 0.01f) + 0.5f;
                    Timer.DelayCall(TimeSpan.Zero, () => { if (defender != null && defender.Alive) { defender.DoSleep(TimeSpan.FromSeconds(duration)); } });
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504430);
                    //defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*STORDENTE*");
                    break;
                }
                case SpecialAttacks.ColpoRapido:
                {
                    float bas = weapon is BaseSword ? 0.90f : (weapon is BasePoleArm ? 0.75f : 0.5f);
                    damageGiven = (int)(damageGiven * bas);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504436);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*COLPO RAPIDO*");
                    break;
                }
                case SpecialAttacks.ColpoRapidoAscia:
                    {
                        float bas = weapon is BaseSword ? 0.90f : (weapon is BaseAxe ? 0.75f : 0.5f);
                        damageGiven = (int)(damageGiven * bas);
                        defender.PublicOverheadMessage(MessageType.Regular, 0, 504436);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*COLPO RAPIDO*");
                        break;
                    }
                case SpecialAttacks.DispelMinore:
                {
                    damageGiven = (int)(damageGiven * 0.3f);
                    goto case SpecialAttacks.Dispel;
                }
                case SpecialAttacks.Dispel:
                {
                    SpellHelper.DispelHelper(attacker, defender, null);
                    Spells.Fifth.MagicReflectSpell.RemoveTo(defender);
                    defender.PlaySound(0x5BD);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504437);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*DISSOLVENTE*");
                    break;
                }
                case SpecialAttacks.Lacerante:
                {
                    damageGiven = (int)(damageGiven * 0.7f);
                    int calc = (int)(damageGiven * 0.4f);
                    if (calc < 1)
                    {
                        calc = 1;
                    }

                    int time = (int)(((Math.Min(attacker.Skills.Tactics.Value, 200) - 100) * 0.03f) + 2);
                    defender.DoBleed(calc, time);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504438);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*LACERANTE*");
                    break;
                }
                case SpecialAttacks.LaceranteMinore:
                {
                    if (weapon is BaseKnife)
                    {
                        damageGiven = (int)(damageGiven * 1.45);
                    }

                    goto case SpecialAttacks.Lacerante;
                }
                case SpecialAttacks.Immobilizzante:
                {
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(0.4f, 0.6f));
                    if (damageGiven < 1)
                    {
                        damageGiven = 1;
                    }

                    defender.FrizzAll();
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504416);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*IMMOBILIZZANTE*");
                    break;
                }
                case SpecialAttacks.Mirato:
                {
                    damageGiven += (int)((damageGiven * 0.5f) - ((originalDamage - damageGiven) * 0.3f));
                    if (damageGiven < 1)
                    {
                        damageGiven = 1;
                    }
                    defender.Stam -= Math.Min(4, (int)(damageGiven * 0.2f));
                    if (damageGiven > 90)
                    {
                       damageGiven = 90;
                    }

                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504417);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*MIRATO*");
                    break;
                }
                case SpecialAttacks.Perforante:
                {
                    damageGiven += (int)((originalDamage - damageGiven) * 0.4f);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504418);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*PERFORANTE*");
                    break;
                }
                case SpecialAttacks.DoppiaFreccia:
                {
                    attacker.Freeze(TimeSpan.FromSeconds(Utility.RandomFloat(2.0f, 3.0f)));
                    Timer.DelayCall(TimeSpan.FromMilliseconds(150), delegate
                    {
                        if (attacker != null && attacker.Alive && (BaseWeapon)attacker.Weapon == weapon && defender != null && defender.Alive && attacker.Combatant == defender)
                        {
                            attacker.colpo_caricato_arcer = true;
                        }
                    });
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(0.65f, 0.85f));
                    break;
                }
                case SpecialAttacks.ColpoInCorsa:
                {
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(0.7f, 0.9f));
                    break;
                }
                case SpecialAttacks.Affondo:
                {
                    float multi = 0.70f;
                    if (weapon is BaseKnife)//knives or kryss get extra 25% bonus
                    {
                        multi = 0.95f;
                    }

                    damageGiven += (int)((originalDamage - damageGiven) * multi);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504439);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*AFFONDO*");
                    break;
                }
                case SpecialAttacks.Paralizzante:
                {
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(0.5f, 0.6f));
                    if (damageGiven < 1)
                    {
                        damageGiven = 1;
                    }

                    defender.FrizzAll();
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504424);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*PARALIZZANTE*");
                    break;
                }
                case SpecialAttacks.Mandritto:
                {
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(0.5f, 0.6f));
                    if (damageGiven < 1)
                    {
                        damageGiven = 1;
                    }

                    defender.FrizzAll();
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504427);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*MANDRITTO*");
                    break;
                }
                case SpecialAttacks.ColpoAlleGambe:
                {
                    defender.Stam -= (int)(originalDamage * Utility.RandomFloat(0.3f, 0.4f));
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504440);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*COLPO ALLE GAMBE*");
                    break;
                }
                case SpecialAttacks.Disarcionare:
                {
                    if (defender.Player && defender.Mount != null)
                    {
                        bool paladino = attacker.ClasseID == (int)ClassiEnum.Paladino;
                        if (!attacker.Mounted || paladino || Utility.RandomFloat() >= (weapon.MaxRange == 1 ? 0.18f : 0.36f)) //82% con arma a una mano - 64% successo a due mani: se a cavallo
                        {
                            damageGiven = (int)(damageGiven * Utility.RandomFloat(0.5f, 0.7f));
                            defender.Freeze(TimeSpan.FromSeconds(Utility.RandomFloat(0.8f, 1.4f)));
                            Server.Mobiles.BaseMount.Dismount(defender, true);
                            defender.PublicOverheadMessage(MessageType.Regular, 0, 504441);//, string.Format("#{0}", special.Name));//
                                                                                           //defender.PublicOverheadMessage(MessageType.Regular, 0, false, (defender.Female ? "*DISARCIONATA*" : "*DISARCIONATO*"));
                            attacker.Freeze(TimeSpan.FromSeconds(Utility.RandomFloat(0.9f, 1.4f)));
                            if (attacker.Mount != null && (attacker.ClasseID == (int)ClassiEnum.Templare ? (Utility.RandomFloat() < (weapon.MaxRange == 1 ? 0.3f : 0.15f)) : !paladino)) //85% con arma lunga - 70% successo arma corta solo se templare e  se a cavallo ovviamente...il paladino non cade, gli altri cadono sempre
                            {
                                Server.Mobiles.BaseMount.Dismount(attacker, true);
                            }
                        }
                        else
                        {
                            attacker.SendLocalizedMessage(1005054, "", 0x22);//, "Fallisci e cadi rovinosamente a terra!");
                            attacker.Freeze(TimeSpan.FromSeconds(Utility.RandomFloat(0.5f, 1.0f)));
                            Server.Mobiles.BaseMount.Dismount(attacker, true);
                        }
                    }
                    else
                    {
                        attacker.SendLocalizedMessage(1005053);//"Il colpo non ha avuto nessun effetto!");
                    }
                    break;
                }
                case SpecialAttacks.TempestaArmi:
                {
                    damageGiven = 0;
                    int damage = 0;
                    attacker.FixedEffect(0x3728, 10, 15);
                    attacker.PlaySound(0x2A1);
                    float bonus = 1.0f;
                    XmlAttach.ShieldDamageMod(attacker, (attacker.FindItemOnLayer(Layer.TwoHanded) as BaseShield), ref bonus);
                    List<Mobile> mlist = new List<Mobile>();
                    IPooledEnumerable<Mobile> pool = attacker.GetMobilesInRange(weapon.MaxRange);
                    foreach (Mobile defenders in pool)
                    {
                        if (defenders != attacker && attacker.CanBeHarmful(defenders, false) && attacker.InLOS(defenders))
                        {
                            mlist.Add(defenders);
                        }
                    }
                    pool.Free();
                    for (int i = mlist.Count - 1; i >= 0; --i)
                    {
                        Mobile defenders = mlist[i];
                        damage = (int)((weapon.ComputeDamage(attacker, defenders) * bonus) * defenders.ReceivedDamageMod);
                        attacker.DoHarmful(defenders);
                        defenders.Damage((int)(weapon.AbsorbDamage(attacker, defenders, damage) * 0.8f), attacker);
                    }
                    break;
                }
                case SpecialAttacks.ColpoPossente:
                {
                    float bas = weapon is BaseSword ? 1.30f : (weapon is BasePoleArm ? 1.20f : 1.10f);
                    damageGiven = (int)(damageGiven * (bas + Math.Min((Math.Max(attacker.Str - 100, 1) * 0.004f), 0.27f)));
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504442);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*POSSENTE*");
                    break;
                }
                case SpecialAttacks.ColpoPossenteNano:
                    {
                        float bas = weapon is BaseSword ? 1.30f : (weapon is BaseAxe ? 1.20f : 1.10f);
                        damageGiven = (int)(damageGiven * (bas + Math.Min((Math.Max(attacker.Str - 100, 1) * 0.004f), 0.27f)));
                        defender.PublicOverheadMessage(MessageType.Regular, 0, 504442);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*POSSENTE*");
                        break;
                    }
                case SpecialAttacks.Preciso:
                {
                    damageGiven += (int)((damageGiven * 0.5f) - ((originalDamage - damageGiven) * 0.20f));
                    if (damageGiven < 1)
                    {
                        damageGiven = 1;
                    }
                    defender.Stam -= Math.Min(4, (int)(damageGiven * 0.2f));
                    if (damageGiven > 90)
                    {
                       damageGiven = 90;
                    }
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504420);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*PRECISO*");
                    break;
                }
                case SpecialAttacks.Bucante:
                {
                    damageGiven += (int)((originalDamage - damageGiven) * 0.55f);
                    Dictionary<Layer, Item> equip = defender.GetEquippedWearables;
                    weapon.DamageArmor(attacker.Player && defender.Player, false, equip);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504421);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*BUCANTE*");
                    break;
                }
                case SpecialAttacks.Stoccata:
                {
                    damageGiven += (int)((originalDamage - damageGiven) * 0.6);
                    defender.Stam -= Utility.RandomMinMax(0, 6);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504425);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*STOCCATA*");
                    break;
                }
                case SpecialAttacks.Mortale:
                {
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(1.35f, 1.45f)); //...da valutare se è davvero troppo...
                    if (damageGiven < 50)
                    {
                    damageGiven = 50;
                    }
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504422);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*MORTALE*");
                    break;
                }
                case SpecialAttacks.Critico:
                {
                    float bas = weapon is BaseSword ? 1.30f : (weapon is BasePoleArm ? 1.20f : 1.10f);
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(bas, bas + 0.07f));
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504423);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*CRITICO*");
                    break;
                }
                case SpecialAttacks.CriticoAscia:
                    {
                        float bas = weapon is BaseSword ? 1.30f : (weapon is BaseAxe ? 1.20f : 1.10f);
                        damageGiven = (int)(damageGiven * Utility.RandomFloat(bas, bas + 0.07f));
                        defender.PublicOverheadMessage(MessageType.Regular, 0, 504423);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*CRITICO*");
                        break;
                    }
                case SpecialAttacks.Devastante:
                {
                    damageGiven = (int)(damageGiven * (Utility.RandomFloat(1.15f, 1.20f)));
                    Dictionary<Layer, Item> equip = defender.GetEquippedWearables;
                    bool pvp = attacker.Player && defender.Player;
                    if (weapon.DamageArmor(pvp, false, equip) == null || Utility.Random(3) > 0)
                        weapon.DamageWearings(attacker.Player && defender.Player, false, equip);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504428);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*DEVASTANTE*");
                    break;
                }
                case SpecialAttacks.Dirompente:
                {
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(1.09f, 1.13f));
                    defender.Stam -= Utility.RandomMinMax(2, 6);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504429);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*DIROMPENTE*");
                    break;
                }
                case SpecialAttacks.AntiMana:
                {
                    defender.Mana -= (int)(damageGiven * 0.8f);
                    break;
                }
                case SpecialAttacks.VortexStrike:
                {
                    attacker.PlaySound(0x217);
                    BaseCreature m = new EnergyVortex();
                    if (BaseCreature.Summon(m, false, attacker, defender.Location, 0x212, TimeSpan.FromSeconds(30.0)))
                    {
                        m.Combatant = defender;
                    }

                    break;
                }
                case SpecialAttacks.DaemonWolfStrike:
                    {
                        attacker.PlaySound(0x217);
                        BaseCreature m = new DaemonWolf();
                        if (BaseCreature.Summon(m, true, attacker, defender.Location, 0x212, TimeSpan.FromSeconds(60.0)))
                        {
                            m.Combatant = defender;
                        }

                        break;
                    }
                case SpecialAttacks.DarkPantherStrike:
                    {
                        attacker.PlaySound(0x217);
                        BaseCreature m = new DarkPanther();
                        if (BaseCreature.Summon(m, true, attacker, defender.Location, 0x212, TimeSpan.FromSeconds(60.0)))
                        {
                            m.Combatant = defender;
                        }

                        break;
                    }
                case SpecialAttacks.BlueWolfStrike:
                    {
                        attacker.PlaySound(0x217);
                        BaseCreature m = new BlueWolf();
                        if (BaseCreature.Summon(m, true, attacker, defender.Location, 0x212, TimeSpan.FromSeconds(60.0)))
                        {
                            m.Combatant = defender;
                        }

                        break;
                    }
                case SpecialAttacks.UndeadWolfStrike:
                    {
                        attacker.PlaySound(0x217);
                        BaseCreature m = new UndeadWolf();
                        if (BaseCreature.Summon(m, true, attacker, defender.Location, 0x212, TimeSpan.FromSeconds(60.0)))
                        {
                            m.Combatant = defender;
                        }

                        break;
                    }
                case SpecialAttacks.Staminante:
                {
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(0.8f, 0.9f));
                    if (damageGiven < 1)
                    {
                        damageGiven = 1;
                    }

                    defender.Stam -= (int)(damageGiven * Utility.RandomFloat(0.28f, 0.36f));
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504443);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*STAMINANTE*");
                    break;
                }
                case SpecialAttacks.ColpoOmbra:
                {
                    float dmgmod = Math.Max(0, attacker.Skills[SkillName.Tactics].Value - 100) * 0.002f + 0.3f;
                    damageGiven += (int)(originalDamage * dmgmod + (attacker.Skills[SkillName.Fencing].Value * 0.03f) + Utility.Random(6));
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504444);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*COLPO D'OMBRA*");
                    attacker.PlaySound(0x51B);
                    attacker.HiddenSpell = true;
                    attacker.AllowedStealthSteps = originalDamage / 3;
                    break;
                }
                case SpecialAttacks.TerroreOscuro:
                {
                    // flee
                    if (defender is BaseCreature bc)
                    {
                        bc.Combatant = null;
                        bc.BeginFlee(TimeSpan.FromSeconds(4));
                    }

                    damageGiven = (int)(damageGiven * 0.95f);//5% less damage
                                                            //Mobile bersaglio=defender;
                                                            //SpellHelper.CheckReflect(5, attacker, ref bersaglio);
                                                            // and become paralyzed
                    void OnEffect((Mobile, Mobile) mob)
                    {
                        if (mob.Item1 != null && mob.Item1.Alive && mob.Item2 != null)
                        {
                            mob.Item1.Paralyze(TimeSpan.FromSeconds(0.4f + Math.Max(-0.2f, (mob.Item2.Skills.Tactics.Value - 100) * 0.011f)));
                        }
                    }
                    Timer.DelayCall(TimeSpan.FromMilliseconds(100), OnEffect, (defender, attacker));
                    // lose target focus
                    defender.NextCombatTime = DateTime.UtcNow + defender.Weapon.GetDelay(defender);
                    defender.FixedEffect(0x376A, 9, 32);
                    defender.PlaySound(0x204);
                    defender.SendLocalizedMessage(1005052);//"Sei terrorizzato e non riesci a muoverti..");
                    break;
                }
                case SpecialAttacks.Deconcentrante:
                {
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(0.5f, 0.6f));
                    if (damageGiven < 1)
                    {
                        damageGiven = 1;
                    }

                    defender.FrizzAll();
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504419);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*DECONCENTRANTE*");
                    break;
                }
                case SpecialAttacks.LamaAvvelenata:
                {
                    if (m_ItemUsed == null || m_ItemUsed.Count == 0 || !(m_ItemUsed[0] is PoisonPotion))
                    {
                        break; //non dovrebbe mai succedere, ma il mondo è strano a volte...
                    }

                    PoisonPotion pp = (PoisonPotion)m_ItemUsed[0];
                    weapon.Poison = pp.PoisonLevel;
                    weapon.PoisonCharges = 18 - (pp.PoisonLevel.Level * 2);
                    damageGiven = (int)(damageGiven * 0.5f);
                    goto case SpecialAttacks.FrecciaAvvelenata;
                }
                case SpecialAttacks.Disarma:
                {
                    damageGiven = (int)(damageGiven * 0.7f);
                    Container pack = defender.Backpack;
                    BaseWeapon toDisarm = defender.Weapon;
                    if (pack != null && toDisarm != null && !toDisarm.Deleted && toDisarm.Movable && !(toDisarm is Fists))
                    {
                        pack.DropItem(toDisarm);
                        if (defender.Player)
                        {
                            BaseWeapon.BlockEquip(defender, TimeSpan.FromSeconds(1.2));
                        }
                        else
                        {
                            Timer.DelayCall(TimeSpan.FromSeconds(1.0), () => { defender.EquipItem(toDisarm); });
                        }
                    }
                    break;
                }
                case SpecialAttacks.Fulminante:
                {
                    damageGiven = (int)(damageGiven * 1.2f);
                    attacker.Freeze(TimeSpan.FromMilliseconds(550));
                    defender.Freeze(TimeSpan.FromMilliseconds(250));
                    goto case SpecialAttacks.Stordente;
                }
                case SpecialAttacks.ColpoSacro:
                {
                    if (defender is BaseUndead)
                    {
                        damageGiven *= 3;
                        defender.FixedParticles(0x3728, 10, 30, 5052, 0x480, 0, EffectLayer.LeftFoot);
                        defender.PublicOverheadMessage(MessageType.Regular, 0, 505099);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*COLPO SACRO*");
                    }
                    else
                    {
                        damageGiven = (int)(damageGiven * 1.4);
                    }

                    defender.PublicOverheadMessage(MessageType.Regular, 0, 505099);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*COLPO SACRO*");
                    break;
                }
                case SpecialAttacks.AnnientaMale:
                {
                    float karmeff = 0;
                    if (defender.Karma < 0)
                    {
                        karmeff = Math.Min(Math.Abs(defender.Karma) * 0.01f, 25.0f);
                    }

                    if (defender.Kills >= 10)
                    {
                        karmeff += Math.Min(defender.Kills * 0.1f, 10);
                    }

                    if (defender.Fame > 0)
                    {
                        karmeff += Math.Min(defender.Fame * 0.01f, 25.0f);
                    }

                    if (defender.FedeID != (int)Fedi.Idior || defender.FedeID != (int)Fedi.Hilianor)// || defender.AlignmentID == (int)Alignment.Chaos)//RIMOZIONE ALLINEAMENTO
                    {
                        karmeff += 10;
                    }

                    karmeff += 10.0f + (attacker.Skills[weapon.DefSkill].Value * 0.1f) * 1.5f;
                    defender.FixedParticles(0x3728, 10, 30, 5052, 0x480, 0, EffectLayer.LeftFoot);
                    AOS.Damage(defender, attacker, (int)karmeff, 0, 0, 0, 0, 100, 0);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 505100);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*ANNIENTA-MALE*");
                    break;
                }
                case SpecialAttacks.LuceDivina:
                {
                    defender.Send(new ScreenEffect((ScreenEffectType)Utility.RandomMinMax(0x01, 0x02)));//fadein o flash di luce. il fadeinout taglia un mare di tempo
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 505101);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*LUCE DIVINA*");
                    if (Utility.RandomFloat() < 0.3f)//30% scarso?
                    {
                        defender.FrizzAll();
                    }

                    if (!defender.Player)
                    {
                        AOS.Damage(defender, attacker, damageGiven >> 1, 0, 0, 0, 0, 100, 0);
                    }
                    else
                    {
                        AOS.Damage(defender, attacker, damageGiven >> 2, 0, 0, 0, 0, 100, 0);
                    }

                    break;
                }
                case SpecialAttacks.ColpoAlleSpalle:
                {
                    if (Utility.IsBehindMobile(attacker, defender))
                    {
                        float dmgmod = Math.Max(0, attacker.Skills[SkillName.Tactics].Value - 100) * 0.0020f + 1.20f;
                        damageGiven = (int)(originalDamage * dmgmod + (attacker.Skills[SkillName.Fencing].Value * 0.05f) + Utility.Random(5));
                        defender.PublicOverheadMessage(MessageType.Regular, 0, 504445);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*COLPO ALLE SPALLE*");
                    }
                    else
                    {
                        attacker.SendLocalizedMessage(1005060);//"Non eri dietro il bersaglio per il Colpo alle Spalle, riprova");
                        attacker.EndAction(SpecialAttacks.ColpoAlleSpalle);
                    }
                    break;
                }
                case SpecialAttacks.FrecciaInfuocata:
                {
                    if (XmlAttach.FindAttachment(weapon, typeof(XmlFire)) is XmlFire att)
                    {
                        att.ResetEndTime();
                    }

                    AOS.Damage(defender, attacker, originalDamage, 0, 100, 0, 0, 0);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 504446);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*FRECCIA DI FUOCO*");
                    break;
                }

                case SpecialAttacks.Allungamento:
                {
                    break;
                }
                //ADDED FRECCIA ELETTRICA
                case SpecialAttacks.FrecciaElettrica:
                {
                    if (XmlAttach.FindAttachment(weapon, typeof(XmlFire)) is XmlFire att)
                    {
                        att.ResetEndTime();
                    }

                    AOS.Damage(defender, attacker, originalDamage, 0, 0, 0, 0, 100);
                    attacker.MovingParticles(defender, 0x3818, 7, 0, false, false, 9502, 4019, 0x160);
                    attacker.PlaySound(0X20A);
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 1116792);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*FRECCIA ELETTRICA*");
                    break;
                }
                //ADDED FRECCIA RAGGELANTE
                case SpecialAttacks.FrecciaRaggelante:
                {
                    attacker.MovingParticles(defender, 0x37B9, 7, 0, false, false, 9502, 4019, 0x160);
                    attacker.PlaySound(0X213);
                    damageGiven = (int)(damageGiven * Utility.RandomFloat(0.4f, 0.6f));
                    if (damageGiven < 1)
                    {
                        damageGiven = 1;
                    }

                    defender.FrizzAll();
                    defender.PublicOverheadMessage(MessageType.Regular, 0, 1116802);//, string.Format("#{0}", special.Name));//defender.PublicOverheadMessage(MessageType.Regular, 0, false, "*IMMOBILIZZANTE*");
                    break;
                }
                default:
                    attacker.SendLocalizedMessage(1005014);//"no effect");
                    break;
            }


        }

        //
        // carry out the combo attacks
        //
        // If you add a new combo, you must add the code here to define what it actually does when it is activated
        //
        public void DoComboAttack(Mobile attacker, Mobile defender, BaseWeapon weapon, int damageGiven, ComboAttack combo)
        {
            if (attacker == null || defender == null || weapon == null || combo == null)
            {
                return;
            }

            attacker.SendLocalizedMessage(1005050, string.Format("#{0}", combo.Name));
            attacker.PublicOverheadMessage(MessageType.Emote, 0x020, 1005051, string.Format("#{0}*", combo.Name));

            // apply the combo attack
            switch (combo.AttackID)
            {
                case ComboAttacks.MorteFulminea:
                {
                    attacker.FixedEffect(0x3728, 10, 15);
                    attacker.PlaySound(0x2A1);
                    defender.Kill();
                    break;
                }
                case ComboAttacks.ThunderStrike:
                {
                    defender.FixedEffect(0x376A, 9, 32);
                    defender.PlaySound(0x51E);
                    defender.Damage(damageGiven, attacker);
                    defender.DoSleep(TimeSpan.FromMilliseconds(600));
                    break;
                }
                case ComboAttacks.LightningRain:
                {
                    defender.Damage(weapon.MaxDamage * 3, attacker);
                    defender.Mana -= weapon.MaxDamage * 7;
                    defender.Stam -= weapon.MaxDamage * 4;
                    break;
                }
                case ComboAttacks.SqueezingFist:
                {
                    // 5 sec paralyze
                    defender.FixedEffect(0x376A, 9, 32);
                    defender.PlaySound(0x204);
                    defender.Freeze(TimeSpan.FromSeconds(5));
                    // 7x stam drain
                    defender.Stam -= weapon.MaxDamage * 7;
                    break;
                }
                case ComboAttacks.FuocoFiamme:
                {
                    defender.FixedParticles(0x36BD, 10, 30, 5052, EffectLayer.Head);
                    defender.PlaySound(0x208);
                    AOS.Damage(defender, attacker, damageGiven, 0, 100, 0, 0, 0);
                    break;
                }
                case ComboAttacks.DeadlyVortex:
                {
                    int range = weapon.MaxRange + 1;
                    new Misc.EffectCircleCreator(TimeSpan.FromMilliseconds(200), range, attacker.Location, attacker.Map, 0x3728).Start();
                    attacker.PlaySound(0x51F);
                    float bonus = 1.0f;
                    XmlAttach.ShieldDamageMod(attacker, (attacker.FindItemOnLayer(Layer.TwoHanded) as BaseShield), ref bonus);
                    List<Mobile> mlist = new List<Mobile>();
                    IPooledEnumerable<Mobile> pool = attacker.GetMobilesInRange(range);
                    foreach (Mobile defenders in pool)
                    {
                        if (defenders != attacker && attacker.CanBeHarmful(defenders, false) && attacker.InLOS(defenders))
                        {
                            mlist.Add(defenders);
                        }
                    }
                    pool.Free();
                    for (int i = mlist.Count - 1; i >= 0; --i)
                    {
                        Mobile defenders = mlist[i];
                        int damage = (int)((weapon.ComputeDamage(attacker, defenders) * bonus) * defenders.ReceivedDamageMod);
                        attacker.DoHarmful(defenders);
                        defenders.Damage((int)(weapon.AbsorbDamage(attacker, defenders, damage) * 0.8f), attacker);
                    }
                    break;
                }
                case ComboAttacks.SoulSteal:
                {
                    defender.Damage(damageGiven, attacker);
                    Effects.SendMovingEffect(defender, attacker, 0x37B9, 5, 10, false, false);
                    attacker.FixedEffect(0x37B9, 9, 32);
                    attacker.PlaySound(0x28E);
                    attacker.Heal(damageGiven);
                    break;
                }
                //ADDED FOR NEW ELECTRICBOW 
                case ComboAttacks.ElectricLightning:
                {
                    defender.FixedParticles(0x36BD, 10, 30, 5052, EffectLayer.Head);
                    defender.PlaySound(0x5BF);
                    AOS.Damage(defender, attacker, damageGiven, 0, 0, 0, 0, 100);
                    break;
                }
                //ADDED FOR NEW ACIDBOW
                case ComboAttacks.FastPoison:
                {
                    defender.FixedParticles(0x36BD, 10, 30, 5052, EffectLayer.Head);
                    defender.PlaySound(0x5BF);
                    AOS.Damage(defender, attacker, damageGiven, 0, 0, 0, 100, 0);
                    break;
                }
                //ADDED FOR NEW ICEBOW
                case ComboAttacks.IceWind:
                {
                    defender.FixedParticles(0x36BD, 10, 30, 5052, EffectLayer.Head);
                    defender.PlaySound(0x5BF);
                    AOS.Damage(defender, attacker, damageGiven, 0, 0, 100, 0, 0);
                    break;
                }
                //ADDED FOR DAEMONBOW / DREAMLINE
                case ComboAttacks.Daemonshot:
                {
                    if (defender is BaseDaemon)
                    {
                        defender.FixedParticles(0x36BD, 10, 30, 5052, EffectLayer.Head);
                        defender.PlaySound(0x208);
                        AOS.Damage(defender, attacker, damageGiven, 0, 100, 0, 0, 0);
                    }
                    break;
                }
            }
        }

        [Attachable]
        public XmlCustomAttacks(string name)
        {
            if (string.Compare("test", name, true) == 0)
            {
                foreach (SpecialAttacks att in AllSpecials.Keys)
                {
                    AddSpecial(att);
                }
            }
            else if (string.Compare("arco", name, true) == 0)
            {
                AddSpecial(SpecialAttacks.Stordente);
                AddSpecial(SpecialAttacks.FrecciaAvvelenata);
                AddSpecial(SpecialAttacks.ColpoRapido);
                AddSpecial(SpecialAttacks.ColpoRapidoAscia);
                AddSpecial(SpecialAttacks.Dispel);
                AddSpecial(SpecialAttacks.Lacerante);
                AddSpecial(SpecialAttacks.Immobilizzante);
                AddSpecial(SpecialAttacks.Mirato);
                AddSpecial(SpecialAttacks.Perforante);
                AddSpecial(SpecialAttacks.ColpoInCorsa);
                AddSpecial(SpecialAttacks.DoppiaFreccia);
                AddSpecial(SpecialAttacks.FrecciaElettrica);
                AddSpecial(SpecialAttacks.FrecciaRaggelante);
            }
            else if (string.Compare("spada", name, true) == 0)
            {
                AddSpecial(SpecialAttacks.Affondo);
                AddSpecial(SpecialAttacks.ColpoAlleGambe);
                AddSpecial(SpecialAttacks.Disarcionare);
                AddSpecial(SpecialAttacks.TempestaArmi);
                AddSpecial(SpecialAttacks.Mortale);
            }
        }

        // this constructor is intended to be called from within scripts that wish to define custom attack configurations
        // by passing it a list of SpecialAttacks
        public XmlCustomAttacks(SpecialAttacks[] attacklist)
        {
            if (attacklist != null)
            {
                for (int i = 0; i < attacklist.Length; ++i)
                {
                    AddSpecial(attacklist[i]);
                }
            }
        }

        public XmlCustomAttacks(SpecialAttacks attack)
        {
            AddSpecial(attack);
        }

        public XmlCustomAttacks()
        {
        }
        // ------------------------------------------------------------------------------
        // END of user-defined special attacks and combos information
        // ------------------------------------------------------------------------------

        private static Dictionary<SpecialAttacks, SpecialAttack> AllSpecials = new Dictionary<SpecialAttacks, SpecialAttack>();
        public static SpecialAttack GetAttack(SpecialAttacks special)
        {
            if (AllSpecials.TryGetValue(special, out SpecialAttack s))
            {
                return s;
            }

            Console.WriteLine("Error in getting an attack in XmlCustomAttacks - > GetAttack -> {0}", special.ToString());
            throw new NullReferenceException();
        }

        private static Dictionary<ComboAttacks, ComboAttack> AllCombos = new Dictionary<ComboAttacks, ComboAttack>();

        public static void AddMobiletoDict(PlayerMobile pm)
        {
            LastWeapType[pm] = null;
            pm.RMT_attk = new XmlCustomAttacks.RemoveAttkTimer(pm);
        }

        public static void DelMobileinDict(PlayerMobile pm)
        {
            if (pm.RMT_attk != null)
            {
                pm.RMT_attk.Stop();
            }

            LastWeapType.Remove(pm);
        }

        private static Dictionary<Mobile, Type> LastWeapType = new Dictionary<Mobile, Type>();

        public enum IconTypes
        {
            GumpID,
            ItemID
        }

        public class SpecialAttack
        {
            public int Name;           // attack name
            public SpecialAttacks AttackID;  // attack id
            public TimeSpan ChainTime;    // time available until next attack in the chain must be performed
            public int Icon;                 // button icon for this attack
            public IconTypes IconType;          // what type of art to use for button icon
            public int ManaReq;             // mana usage for this attack
            public int StamReq;             // stamina usage for this attack
            public int HitsReq;             // hits usage for this attack
            public int KarmaReq;            // karma usage for this attack
            public int StrReq;             // str requirements for this attack
            public int DexReq;             // dex requirements for this attack
            public int IntReq;             // int requirements for this attack
            public Type[] Reagents;       // reagent list used for this attack
            public int[] Quantity;        // reagent quantity list
            public SkillName[] Skills;    // list of skill requirements for this attack
            public int[] MinSkillLevel;   // minimum skill levels
            public float CoolDown;         // CoolDown in seconds
            public bool PreInitialize, ClassCoolDown, PermanentEffect; // Preinizializzazione, necessaria in pochi (fortunatamente) casi.
                                                                       //ClassCoolDown specifica se dobbiamo tenere conto della differenza armi (tra +5 a +1 e armi non particolari...)

            public SpecialAttack(int name, SpecialAttacks id, int icon, IconTypes itype, TimeSpan duration,
            int mana, int stam, int hits, int karma, int minstr, int mindex, int minint,
            SkillName[] skills, int[] minlevel, Type[] reagents, int[] quantity, float cooldown, bool preinitialize, bool classcooldown, bool permanenteffect)
            {
                Name = name;
                AttackID = id;
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
                ClassCoolDown = classcooldown;
                PermanentEffect = permanenteffect;
            }
        }

        public class ComboAttack
        {
            public int Name;
            public ComboAttacks AttackID;
            public SpecialAttacks[] AttackSequence;

            public ComboAttack(int name, ComboAttacks id, SpecialAttacks[] sequence)
            {
                Name = name;
                AttackID = id;
                AttackSequence = sequence;
            }
        }

        public class ActiveCombo
        {
            public ComboAttack Combo;
            public int PositionInSequence;

            public ActiveCombo(ComboAttack c)
            {
                Combo = c;
                PositionInSequence = 0;
            }
        }

        private SpecialAttack m_SelectedAttack;
        public override object GenericInternal => m_SelectedAttack;
        private List<Item> m_ItemUsed;

        // these are the lists of special moves and combo status for each instance
        private List<SpecialAttack> Specials = new List<SpecialAttack>();
        private static Dictionary<Mobile, SpecialAttack> SelectedAttacks = new Dictionary<Mobile, XmlCustomAttacks.SpecialAttack>();
        private static Dictionary<Mobile, List<ActiveCombo>> Combos = new Dictionary<Mobile, List<XmlCustomAttacks.ActiveCombo>>();
        private static Dictionary<Mobile, ComboTimer> ComboTimers = new Dictionary<Mobile, XmlCustomAttacks.ComboTimer>();
        private IconInfo m_IconInfo = null;

        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlCustomAttacks(ASerial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
        }

        //protected override bool UnsavedAttach => true;

        private static void AddSpecialAttack(int name, SpecialAttacks id, int icon, IconTypes itype, TimeSpan duration,
        int mana, int stam, int hits, int karma, int minstr, int mindex, int minint,
        SkillName[] skills, int[] minlevel, Type[] reagents, int[] quantity, float cooldown, bool preinitializes, bool classcooldown)
        {
            AddSpecialAttack(name, id, icon, itype,
            duration, mana, stam, hits, karma,
            minstr, mindex, minint, skills, minlevel, reagents, quantity, cooldown, preinitializes, classcooldown, false);
        }

        private static void AddSpecialAttack(int name, SpecialAttacks id, int icon, IconTypes itype, TimeSpan duration,
        int mana, int stam, int hits, int karma, int minstr, int mindex, int minint,
        SkillName[] skills, int[] minlevel, Type[] reagents, int[] quantity, float cooldown, bool preinitializes, bool classcooldown, bool permanenteffect)
        {
            AllSpecials.Add(id, new SpecialAttack(name, id, icon, itype,
            duration, mana, stam, hits, karma,
            minstr, mindex, minint, skills, minlevel, reagents, quantity, cooldown, preinitializes, classcooldown, permanenteffect));
        }

        public void AddSpecial(SpecialAttacks id)
        {

            if (AllSpecials.TryGetValue(id, out SpecialAttack s))
            {
                Specials.Add(s);
            }
        }

        private static void AddComboAttack(int name, ComboAttacks id, SpecialAttacks[] sequence)
        {
            AllCombos[id] = new ComboAttack(name, id, sequence);
        }

        public static ComboAttack GetComboAttack(ComboAttacks name)
        {
            AllCombos.TryGetValue(name, out ComboAttack combo);
            return combo;
        }

        public static void AddAttack(IEntity target, SpecialAttacks attack)
        {
            // is there an existing custom attacks attachment to add to?
            XmlCustomAttacks a = (XmlCustomAttacks)XmlAttach.FindAttachment(target, typeof(XmlCustomAttacks));

            if (a == null)
            {
                // add a new custom attacks attachment
                XmlAttach.AttachTo(target, new XmlCustomAttacks(attack));
            }
            else
            {
                // add the new attack to existing attack list
                a.AddSpecial(attack);
            }
        }

        public void InitializeCombos(Mobile from, List<ActiveCombo> list)
        {
            SelectedAttacks.TryGetValue(from, out m_SelectedAttack);
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
                    Combos[from] = new List<XmlCustomAttacks.ActiveCombo>(list);
                }
                else
                {
                    Combos[from] = new List<XmlCustomAttacks.ActiveCombo>();
                }
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

        private void CheckCombos(Mobile attacker, Mobile defender, BaseWeapon weapon, int damageGiven, SpecialAttack s)
        {
            if (s == null)
            {
                return;
            }

            if (Combos.TryGetValue(attacker, out List<ActiveCombo> clist))
            {
                foreach (ActiveCombo c in clist)
                {
                    if (c != null && c.Combo != null && c.Combo.AttackSequence != null && c.PositionInSequence < c.Combo.AttackSequence.Length)
                    {
                        if (c.Combo.AttackSequence[c.PositionInSequence] == s.AttackID)
                        {
                            if (++c.PositionInSequence >= c.Combo.AttackSequence.Length)
                            {
                                // combo is complete so execute it
                                DoComboAttack(attacker, defender, weapon, damageGiven, c.Combo);
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

        public static bool CheckRequirements(Mobile from, SpecialAttack s, bool blockpermability)
        {
            List<Item> it = null;
            return CheckRequirements(from, s, blockpermability, ref it);
        }

        public static bool CheckRequirements(Mobile from, SpecialAttack s, bool blockpermability, ref List<Item> itemList)
        {
            //if(from == null || s == null) return false;
            //check cooldown
            if ((blockpermability || !s.PermanentEffect) && !from.CanBeginAction(s.AttackID))
            {
                from.SendLocalizedMessage(1005061);//"Non puoi riutilizzare quell'abilità così rapidamente");
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
                        from.SendMessage(33, "Error in skill level specification for {0}", s.AttackID);
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

            if (s.Reagents != null && s.Quantity != null)
            {
                // check for any reagents that are specified
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
                        from.SendMessage(33, "Error in quantity specification for {0}", s.AttackID);
                        return false;
                    }
                    if (itemList != null)
                    {
                        itemList.Add(ret);
                    }
                }
            }
            return true;
        }

        public override void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, ref int damageGiven, int originalDamage)
        {
            if (attacker == null || defender == null || weapon == null || m_SelectedAttack == null)
            {
                return;
            }

            m_ItemUsed = new List<Item>();
            if (!CheckRequirements(attacker, m_SelectedAttack, false, ref m_ItemUsed))
            {
                if (m_SelectedAttack.PreInitialize)
                {
                    PreInitFailed(attacker, weapon, ref damageGiven);
                    m_ItemUsed = null; // nulliamo la lista...
                }

                m_SelectedAttack = null;
                ConstructAttacksGump(attacker, this);
                return;
            }
            attacker.BeginAction(m_SelectedAttack.AttackID);
            SpecialAttacks ss = m_SelectedAttack.AttackID;
            float CCD = CoolDownCalculation(m_SelectedAttack, attacker, weapon);
            if (CCD < 0)
            {
                CCD = 0;
            }

            if (!m_SelectedAttack.PermanentEffect)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(CCD), () => { if (attacker != null && !attacker.Deleted) { attacker.EndAction(ss); } });
            }

            // take the requirements
            if (attacker.Backpack != null && m_SelectedAttack.Reagents != null && m_SelectedAttack.Quantity != null)
            {
                attacker.Backpack.ConsumeTotal(m_SelectedAttack.Reagents, m_SelectedAttack.Quantity, true);
            }

            attacker.Mana -= m_SelectedAttack.ManaReq;
            attacker.Stam -= m_SelectedAttack.StamReq;
            attacker.Hits -= m_SelectedAttack.HitsReq;
            attacker.Karma -= m_SelectedAttack.KarmaReq;

            // apply the attack
            DoSpecialAttack(attacker, defender, weapon, ref damageGiven, m_SelectedAttack, originalDamage);

            if (m_SelectedAttack.KarmaReq > 0)
            {
                attacker.SendLocalizedMessage(1019064);//Hai perso un poco di karma.
            }

            // nulliamo la lista, non ci serve più...
            m_ItemUsed = null;

            // after applying a special attack activate the specials timer for combo chaining
            DoComboTimer(attacker, weapon, m_SelectedAttack.ChainTime);

            // check all combos to see which have this attack as the next in sequence, and which might be complete
            CheckCombos(attacker, defender, weapon, damageGiven, m_SelectedAttack);


            if (!m_SelectedAttack.PermanentEffect)
            {
                // clear the selected attack
                m_SelectedAttack = null;

                // redisplay the gump
                ConstructAttacksGump(attacker, this);
            }
        }

        public float CoolDownCalculation(SpecialAttack s, Mobile m, BaseWeapon weapon)
        {
            if (s.ClassCoolDown)
            {
                return ((s.CoolDown * weapon.CoolDownFactor) - ((m.Skills[weapon.DefSkill].Value - 100) * 0.06f));
            }

            return (s.CoolDown - ((m.Skills[weapon.DefSkill].Value - 100) * 0.06f));
        }

        public override void OnEquip(Mobile from)
        {
            // open the specials gump
            if (from == null || !from.Player || Specials.Count < 1)
            {
                return;
            }

            if (m_SelectedAttack != null)
            {
                bool contains = false, checks = !(LastWeapType.TryGetValue(from, out Type t) && t != null && AttachedTo.GetType() != t);
                if (checks)
                {
                    for (int i = Specials.Count - 1; i >= 0 && !contains; --i)
                    {
                        contains = (Specials[i].AttackID == m_SelectedAttack.AttackID);
                    }
                }

                if (!contains)
                {
                    if (m_SelectedAttack.PermanentEffect && !from.CanBeginAction(m_SelectedAttack.AttackID))
                    {
                        SpecialAttacks sa = m_SelectedAttack.AttackID;
                        Timer.DelayCall(TimeSpan.FromSeconds(Math.Max(0, CoolDownCalculation(m_SelectedAttack, from, (BaseWeapon)AttachedTo))), delegate
                        {
                            if (from != null && !from.Deleted)
                            {
                                from.EndAction(sa);
                            }
                        });
                    }
                    m_SelectedAttack = null;
                }
                else if(m_SelectedAttack.PreInitialize)
                {
                    PreInit(from, from.Combatant, (BaseWeapon)AttachedTo, m_SelectedAttack);
                }
            }
            IconsModule im = (IconsModule)CentralMemory.GetModule(from.Serial, typeof(IconsModule));
            if (im != null)
            {
                if (!im.Icons.TryGetValue(typeof(XmlCustomAttacks), out m_IconInfo) || m_IconInfo == null)
                {
                    im.Icons[typeof(XmlCustomAttacks)] = m_IconInfo = new IconInfo(typeof(XmlCustomAttacks), 0x15D1, 0, 0, 0, School.Speciali);
                }
            }
            else
            {
                m_IconInfo = new IconInfo(typeof(XmlCustomAttacks), 0x15D1, 0, 0, 0, School.Speciali);
            }

            ConstructAttacksGump(from, this);
        }

        public override void OnRemoved(IEntity parent)
        {
            // close the specials gump
            if (parent is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)parent;

                if (pm.NetState != null)
                {
                    if (LastWeapType[pm] == null)
                    {
                        LastWeapType[pm] = AttachedTo.GetType();
                        pm.RMT_attk.Start();
                    }
                    else if (LastWeapType[pm] == AttachedTo.GetType())
                    {
                        pm.RMT_attk.Start();
                    }
                }

                pm.CloseGump(typeof(CustomAttacksGump));
                SelectedAttacks[pm] = m_SelectedAttack;
                Delete();
            }
        }

        public class RemoveAttkTimer : Timer
        {
            private Mobile m_from;

            public RemoveAttkTimer(Mobile from) : base(Parametri.WeaponExchangeDelay)
            {
                m_from = from;
            }

            protected override void OnTick()
            {
                LastWeapType[m_from] = null;
            }
        }

        public override LogEntry OnIdentify(Mobile from)
        {
            int num = Specials.Count - 1;
            if (num < 0)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder("#1005176");//1005176 -> Attacchi Speciali:
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

        public override void OnAttach()
        {
            base.OnAttach();

            // only allow attachment to weapons and shields
            if (!(AttachedTo is BaseWeapon))
            {
                Delete();
            }
        }

        public void DoComboTimer(Mobile from, BaseWeapon weapon, TimeSpan delay)
        {
            if (ComboTimers.TryGetValue(from, out ComboTimer t) && t != null)
            {
                t.Stop();
                ComboTimers[from] = t = new XmlCustomAttacks.ComboTimer(from, delay, weapon);
            }
            else
            {
                ComboTimers[from] = t = new XmlCustomAttacks.ComboTimer(from, delay, weapon);
            }

            t.Start();
        }

        public static void ConstructAttacksGump(Mobile from, XmlCustomAttacks a)
        {
            if (from == null || a == null || a.Deleted || a.m_IconInfo == null)
            {
                return;
            }
                
            from.SendGump(new CustomAttacksGump(from, a, a.m_IconInfo));
        }

        private class ComboTimer : Timer
        {
            private Mobile m_from;
            private BaseWeapon m_weap;

            public ComboTimer(Mobile from, TimeSpan delay, BaseWeapon weapon) : base(delay)
            {
                Priority = TimerPriority.OneSecond;
                m_from = from;
                m_weap = weapon;
            }

            protected override void OnTick()
            {
                // the combo has expired
                if (m_from != null)
                {
                    ResetCombos(m_from);

                    if (m_weap != null && !m_weap.Deleted && m_weap.Parent == m_from)
                    {
                        // refresh the gump
                        XmlCustomAttacks attk = XmlAttach.FindAttachment(m_weap, typeof(XmlCustomAttacks), null) as XmlCustomAttacks;
                        ConstructAttacksGump(m_from, attk);
                    }
                }
            }
        }

        private class CustomAttacksInfoGump : Gump
        {
            public CustomAttacksInfoGump(Mobile from, XmlCustomAttacks a, SpecialAttack s) : base(0, 0)
            {
                // prepare the page
                AddPage(0);

                AddBackground(0, 0, 400, 300, 5054);
                AddAlphaRegion(0, 0, 400, 300);
                AddHtmlLocalized(20, 2, 340, 20, s.Name, 0x77B1, false, false);
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

                int CCD, loc = 1005016;
                if (s.CoolDown > 0)
                {
                    CCD = (int)a.CoolDownCalculation(s, from, (BaseWeapon)a.AttachedTo);
                    if (CCD > 0)
                    {
                        loc++;
                    }

                    text.AppendFormat("\t{0}", CCD);
                }

                AddHtmlLocalized(20, 20, 360, 260, loc, text.ToString(), 0x1, true, true);
            }
        }

        private class CustomAttacksGump : Gump
        {
            private XmlCustomAttacks m_attachment;

            private const int vertspacing = 47;

            public override void RefreshMemo()
            {
                m_attachment.m_IconInfo.Location = new Point3D(X, Y, m_attachment.m_IconInfo.Location.Z);
            }

            public CustomAttacksGump(Mobile from, XmlCustomAttacks a, IconInfo p) : base(p.Location.X, p.Location.Y)
            {
                Closable = false;
                Disposable = false;
                Dragable = true;

                from.CloseGump(typeof(CustomAttacksGump));

                m_attachment = a;

                int specialcount = a.Specials.Count;

                // prepare the page
                AddPage(0);

                AddBackground(0, 0, 70, 75 + specialcount * vertspacing, 5054);
                AddLabel(13, 2, 55, "Attack");
                // if combos are still active then give it the red light
                if (HasActiveCombos(from))
                {
                    //active
                    AddImage(15, 25, 0x0a53);
                }
                else
                {
                    //inactive
                    //AddImage( 15, 25, 0x0a52 );
                    AddButton(15, 25, 0x0a52, 0x0a53, 9999, GumpButtonType.Reply, 0);
                }
                // go through the list of enabled moves and add buttons for them
                int y = 70;
                for (int i = 0; i < specialcount; ++i)
                {
                    SpecialAttack s = m_attachment.Specials[i];

                    // flag the attack as being selected
                    // this puts a white background behind the selected attack.  Doesnt look as nice, but works in both the
                    // 2D and 3D client.  I prefer to leave this commented out for best appearance in the 2D client but
                    // feel free to uncomment it for best client compatibility.
                    /*
					if(m_attachment != null && m_attachment.m_SelectedAttack != null && m_attachment.m_SelectedAttack == s)
					{
						AddImageTiled( 2, y-2, 66, vertspacing+2, 0xBBC );
					}
					*/

                    // add the attack button

                    if (s.IconType == IconTypes.ItemID)
                    {
                        AddButton(5, y, 0x5207, 0x5207, (int)s.AttackID + 1000, GumpButtonType.Reply, 0);
                        AddImageTiled(5, y, 44, 44, 0x283E);
                        AddItem(5, y, s.Icon);
                    }
                    else
                    {
                        AddButton(5, y, s.Icon, s.Icon, (int)s.AttackID + 1000, GumpButtonType.Reply, 0);
                    }

                    // flag the attack as being selected
                    // colors the attack icon red.  Looks better that the white background highlighting, but only supported by the 2D client.
                    if (m_attachment != null && m_attachment.m_SelectedAttack != null && m_attachment.m_SelectedAttack == s)
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
                    AddButton(52, y + 13, 0x4b9, 0x4b9, 2000 + (int)s.AttackID, GumpButtonType.Reply, 0);

                    y += vertspacing;
                }

            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                BaseWeapon weapon;
                Mobile m = state.Mobile;
                if (m_attachment == null || state == null || m == null || info == null || m.Weapon != (weapon = (BaseWeapon)m_attachment.AttachedTo) || !LastWeapType.TryGetValue(m, out Type t))
                {
                    return;
                }

                if (t != null && weapon.GetType() != t)
                {
                    m.SendLocalizedMessage(1005062);//"Senti che ci metterai un pò, prima di abituarti ad un'arma diversa");
                                                    //m.CloseGump(typeof(CustomAttacksGump));
                    ConstructAttacksGump(m, m_attachment);
                    return;
                }

                if (info.ButtonID == 9999)
                {
                    m.SendGump(new IconPlacementGump(this, m, m_attachment.m_IconInfo.Location.X, m_attachment.m_IconInfo.Location.Y, 30, 0x15D1, typeof(XmlCustomAttacks), 0, School.Speciali));
                    return;
                }

                // go through all of the possible specials and find the matching button
                int max = m_attachment.Specials.Count;
                for (int i = 0; i < max; ++i)
                {
                    SpecialAttack s = m_attachment.Specials[i];

                    if (s != null && info.ButtonID == (int)s.AttackID + 1000)
                    {
                        // if clicked again, then deselect
                        if (s == m_attachment.m_SelectedAttack)
                        {
                            CheckPermAbility(m, weapon, s);
                            m_attachment.m_SelectedAttack = null;
                        }
                        else
                        {
                            // see whether they have the required resources for this attack
                            if (CheckRequirements(m, s, true))
                            {
                                bool ok = true;

                                // if so, then let them select it
                                if (s.PreInitialize)
                                {
                                    if (m.CanBeginAction(s))
                                    {
                                        ok = m_attachment.PreInit(m, m.Combatant, weapon, s);
                                    }
                                    else
                                    {
                                        m.SendLocalizedMessage(1005063);//"Non puoi deselezionare e riselezionare un attacco di quel tipo così rapidamente, aspetta!");
                                        break;
                                    }
                                }
                                if (ok)
                                {
                                    CheckPermAbility(m, weapon, m_attachment.m_SelectedAttack);
                                    m_attachment.m_SelectedAttack = s;
                                }
                            }
                            else
                            {
                                // otherwise clear it
                                CheckPermAbility(m, weapon, m_attachment.m_SelectedAttack);
                                m_attachment.m_SelectedAttack = null;
                            }
                        }
                        //m.CloseGump(typeof(CustomAttacksGump));
                        ConstructAttacksGump(m, m_attachment);
                        break;
                    }
                    else if (s != null && info.ButtonID == (int)s.AttackID + 2000)
                    {
                        m.CloseGump(typeof(CustomAttacksInfoGump));
                        ConstructAttacksGump(state.Mobile, m_attachment);
                        m.SendGump(new CustomAttacksInfoGump(state.Mobile, m_attachment, s));
                        break;
                    }
                }
            }

            private void CheckPermAbility(Mobile m, BaseWeapon weapon, SpecialAttack s)
            {
                //null check is MANDATORY as "s" can be NULL value
                if (s != null && s.PermanentEffect && !m.CanBeginAction(s.AttackID))
                {
                    SpecialAttacks sa = s.AttackID;
                    Timer.DelayCall(TimeSpan.FromSeconds(Math.Max(0, m_attachment.CoolDownCalculation(s, m, weapon))), delegate
                    {
                        if (m != null && !m.Deleted)
                        {
                            m.EndAction(sa);
                        }
                    });
                }
            }
        }
    }
}
