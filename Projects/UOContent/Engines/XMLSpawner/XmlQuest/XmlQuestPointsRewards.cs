using Server.ACC.CSS;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using Server.ACC.CSS.Systems.Druid;

/*
** XmlQuestPointsRewards
** ArteGordon
** updated 9/18/05
**
** this class lets you specify rewards that can be purchased for XmlQuestPoints quest Credits.
** The items will be displayed in the QuestPointsRewardGump that is opened by the QuestPointsRewardStone
*/

namespace Server.Engines.XmlSpawner2
{
    public class XmlQuestPointsRewards
    {
        public int Cost;       // cost of the reward in credits
        public Type RewardType;   // this will be used to create an instance of the reward
        public int Name;         // used to describe the reward in the gump
        public string NameArgs;
        public int ItemID;     // used for display purposes
        public int ItemHue;
        public int yOffset;
        public object[] RewardArgs; // arguments passed to the reward constructor
        public int MinPoints;   // the minimum points requirement for the reward

        public static List<XmlQuestPointsRewards> RewardsList { get; } = new List<XmlQuestPointsRewards>();
        public static List<XmlQuestPointsRewards> FenixList { get; } = new List<XmlQuestPointsRewards>();
        public static List<XmlQuestPointsRewards> ExpList { get; } = new List<XmlQuestPointsRewards>();
        public static List<XmlQuestPointsRewards> ExpListAnimali { get; } = new List<XmlQuestPointsRewards>();
        public static List<XmlQuestPointsRewards> NewRewardsList { get; } = new List<XmlQuestPointsRewards>();
        public static List<XmlQuestPointsRewards> PointsRewardList { get; } = new List<XmlQuestPointsRewards>();

        public XmlQuestPointsRewards(int minpoints, Type reward, int name, int cost, int id, int hue, int yoffset, object[] args, string nameargs = null)
        {
            RewardType = reward;
            Cost = cost;
            ItemID = id;
            ItemHue = hue;
            Name = name;
            RewardArgs = args;
            MinPoints = minpoints;
            yOffset = yoffset;
            NameArgs = nameargs;
        }

        public static void Initialize()
        {
            //			PointsRewardList.Add( new XmlQuestPointsRewards( 500, typeof(AncientSmithyHammer), "+20 Ancient Smithy Hammer, 50 uses", 500, 0x13E4, new object[] { 20, 50 }));
            //			PointsRewardList.Add( new XmlQuestPointsRewards( 200, typeof(ColoredAnvil), "Colored Anvil", 400, 0xFAF, null ));
            //			PointsRewardList.Add( new XmlQuestPointsRewards( 100, typeof(PowderOfTemperament), "Powder Of Temperament, 10 uses", 300, 4102, new object[] { 10 }));
            //			PointsRewardList.Add( new XmlQuestPointsRewards( 100, typeof(LeatherGlovesOfMining), "+20 Leather Gloves Of Mining", 200, 0x13c6, new object[] { 20 }));
            //double large = 0;
            // this is an example of adding a mobile as a reward
            RewardsList.Add(new XmlQuestPointsRewards(500, typeof(Chocobo), 505760, 400, 0x213B, 0, -15, new object[] { true }));//Chocobo (il colore può variare)
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(Raptalon), 505761, 300, 0x2D95, 0, -15, null));//"Raptalon"
            RewardsList.Add(new XmlQuestPointsRewards(0, typeof(PergamenaNome), 504556, 100, 0x14F0, 0, 6, null, "#505762\t#505963"));//"Deed Cambio Nome (Servono 20kk)"
            RewardsList.Add(new XmlQuestPointsRewards(0, typeof(LightBracelet), 505763, 50, 0x1F06, 0, 5, new object[] { true }));//"Bracciale della Luce"
            RewardsList.Add(new XmlQuestPointsRewards(0, typeof(TalismanoDellaLuce), 505764, 50, 0x2F5B, 0, 5, new object[] { true }));//"Talismano della Luce"
            RewardsList.Add(new XmlQuestPointsRewards(20, typeof(SpeedPotion), 505765, 10, 0xF06, 0x33, 6, null));//"Pozione della Velocità (durata minima 3 minuti)"
            RewardsList.Add(new XmlQuestPointsRewards(50, typeof(CassaMisteriosa), 505766, 150, 0xe40, 0x489, 0, null));//"Cassa Misteriosa"
            RewardsList.Add(new XmlQuestPointsRewards(100, typeof(XmlEnemyMastery), 505767, 50, 0, 0, 0, new object[] { "BaseDaemon", 50, 50, 2880.0, 1004402 }));//+50% Maestria Demoni per 48 ore
            RewardsList.Add(new XmlQuestPointsRewards(100, typeof(XmlEnemyMastery), 505768, 50, 0, 0, 0, new object[] { "BaseUndead", 50, 50, 2880.0, 1004401 }));//+50% Maestria Non Morti per 48 ore
            RewardsList.Add(new XmlQuestPointsRewards(200, typeof(SocketHammer), 505769, 200, 0x13E4, 0x55, 5, null));//"Incastonatore (per mettere/levare i cristalli su item e mob)"
            //m_RewardList.Add( new XmlQuestPointsRewards( 250, typeof(RunaDiLuce), "Runa della Luce", 200, 0x1f14, 289, 5, null));
            RewardsList.Add(new XmlQuestPointsRewards(250, typeof(MageCrystal), 505770, 200, 0xF8E, 8, 5, null));//"Cristallo della Magia (Item - tieni in mano item mentre casti)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(MythicAmethyst), 505771, 300, 0xF2D, 11, 5, null));//"Ametista del Mito (Creature +30 danno/+900 HP/3 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(LegendaryAmethyst), 505772, 200, 0xF2D, 12, 5, null));//"Ametista Leggendaria (Creature +20 danno/+600 HP/2 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(AncientAmethyst), 505773, 100, 0xF2D, 15, 5, null));//"Ametista Antica (Creature +10 danno/+200 HP/1 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(MythicDiamond), 505774, 300, 0xF2D, 1153, 5, null));//"Diamante del Mito (Creature +120 Str/3 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(LegendaryDiamond), 505775, 200, 0xF2D, 1150, 5, null));//"Diamante Leggendario (Creature +80 Str/2 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(AncientDiamond), 505776, 100, 0xF2D, 1151, 5, null));//"Diamante Antico (Creature +40 Str/1 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(MythicEmerald), 505777, 300, 0xF2D, 1267, 5, null));//"Smeraldo del Mito (Creature +120 Dex/3 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(LegendaryEmerald), 505778, 200, 0xF2D, 1268, 5, null));//"Smeraldo Leggendario (Creature +80 Dex/2 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(AncientEmerald), 505779, 100, 0xF2D, 76, 5, null));//"Smeraldo Antico (Creature +40 Dex/1 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(MythicRuby), 505780, 300, 0xF2D, 32, 5, null));//"Rubino del Mito (Creature +600 Armatura/3 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(LegendaryRuby), 505781, 200, 0xF2D, 33, 5, null));//"Rubino Leggendario (Creature +400 Armatura/2 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(AncientRuby), 505782, 100, 0xF2D, 30, 5, null));//"Rubino Antico (Creature +200 Armatura/1 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(MythicTourmaline), 505783, 300, 0xF2D, 1161, 5, null));//"Tormalina del Mito (Creature +30 Elemental Resist/3 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(LegendaryTourmaline), 505784, 200, 0xF2D, 53, 5, null));//"Tormalina Leggendaria (Creature +20 Elemental Resist/2 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(AncientTourmaline), 505785, 100, 0xF2D, 56, 5, null));//"Tormalina Antica (Creature +10 Elemental Resist/1 slot)"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(FurnitureDyeTub), 505786, 300, 0xFAB, 0, 5, null));//"Tintura per Mobili"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(ColoraArmi), 505787, 300, 0xFAB, 0, 5, null));//"Colore per Armi (191 tinkering o bowcraft)"
            RewardsList.Add(new XmlQuestPointsRewards(500, typeof(BondingStone), 505788, 500, 0xFCC, 35, 5, new object[] { 200.0 }));//"Bonding Stone (Creature - resuscitabile per 200 ore)"
            RewardsList.Add(new XmlQuestPointsRewards(500, typeof(ClothingBlessDeed), 505789, 500, 0x14F0, 0, 0, null));//"Deed della Benedizione (rende blessed un oggetto)"
            RewardsList.Add(new XmlQuestPointsRewards(200, typeof(XmlQuestBook), 505790, 200, 0x2259, 0, -5, null));//"Libro delle Quest (raccoglitore di tutte le quest)"
            RewardsList.Add(new XmlQuestPointsRewards(450, typeof(RidableTigre), 505791, 450, 0x2116, 0, -15, null));//"Tigre Cavalcabile"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(RidableKarsten), 505792, 400, 0x2128, 0, -15, null));//"Karsten Cavalcabile"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(RidableRaptor), 505793, 400, 0x2146, 0, -15, null));//"Raptor Cavalcabile"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(RidableStegosauro), 505794, 400, 0x2139, 0, -15, null));//"Stregosauro Cavalcabile"
            RewardsList.Add(new XmlQuestPointsRewards(550, typeof(RidableCavalloCorazzato), 505795, 500, 0x2145, 0, -15, null));//"Cavallo Corazzato"
            RewardsList.Add(new XmlQuestPointsRewards(100, typeof(GobletOfCelebration), 505796, 100, 0x09B3, 1174, 0, null));//"Calice della Celebrazione (500 cariche acqua)"
            RewardsList.Add(new XmlQuestPointsRewards(100, typeof(CassettaAttrezzi), 505797, 400, 7866, 0, 0, null));//"Cassetta degli Attrezzi"
            RewardsList.Add(new XmlQuestPointsRewards(100, typeof(DeedCambioSesso), 505798, 100, 0x14F0, 0, 0, null));//"Deed Cambio Sesso (Servono 2,5kk)"
            //Added Flegias 02/05/2018
            RewardsList.Add(new XmlQuestPointsRewards(500, typeof(Zebra), 505799, 400, 0x2155, 0, -15, null));//"Zebra Cavalcabile"
            RewardsList.Add(new XmlQuestPointsRewards(500, typeof(CavalloMaculato), 505800, 400, 0x215E, 0, -15, null));//"Cavallo Maculato"
            RewardsList.Add(new XmlQuestPointsRewards(550, typeof(CavalloBardato), 505801, 500, 0x2160, 0, -15, null));//"Cavallo Bardato"
            //Added Jumala 21/02/2020
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(PianoforteNorthDeed), 1116880, 300, 0x14F0, 0, 0, null));//"Pianoforte Nord deed"
            RewardsList.Add(new XmlQuestPointsRewards(300, typeof(pianoforteOvestDeed), 1116881, 300, 0x14F0, 0, 0, null));//"Pianoforte Ovest deed"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(LampadaArcimago), 1149548, 400, 0x2DBA, 0, 0, null));//"LampadaArcimago"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(LampadaHalloweenEst), 1149551, 400, 0x2DB8, 0, 0, null));//"LampadaHalloweenEst"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(LampadaHalloweenSud), 1149552, 400, 0, 0, 0, null));//"LampadaHalloweenSud"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(LampadaOrnamentoEst), 1149553, 400, 0x2DB0, 0, -15, null));//"LampadaOrnamentoEst"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(LampadaOrnamentoSud), 1149554, 400, 0, 0, 0, null));//"LampadaOrnamentoSud"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(LampadaRegaleEst), 1149555, 400, 0x2DBC, 0, -15, null));//"LampadaRegaleEst"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(LampadaRegaleSud), 1149556, 400, 0, 0, 0, null));//"LampadaRegaleSud"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(LampadaDragoEst), 1149549, 400, 0x2DB4, 0, -15, null));//"LampadaDragoEst"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(LampadaDragoSud), 1149550, 400, 0, 0, 5, null));//"LampadaDragoSud"
            RewardsList.Add(new XmlQuestPointsRewards(400, typeof(TigerMask), 1149547, 300, 0x263C, 0, 5, null));//"Tiger Mask"

            //definizioni per item da donazione / FENIX
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(AutoPremiumDonorSys), 505802, 700, 0, 0, 0, new object[] { 90 }));//Bonus Account per 3 Mesi
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(AutoPremiumDonorSys), 505803, 1300, 0, 0, 0, new object[] { 180 }));//Bonus Account per 6 Mesi
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(AutoPremiumDonorSys), 505804, 2400, 0, 0, 0, new object[] { 360 }));//Bonus Account per 12 Mesi
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(CasaccaEsotica), 505805, 150, 0x2695, 0, -10, new object[] { 0, 0, 0 }));//Casacca Esotica
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(VesteEsotica), 505806, 150, 0x2696, 0, -10, new object[] { 0, 0, 0 }));//Veste Esotica
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(QuestCreditPoints), 505807, 100, 0, 0, 0, new object[] { 300 }));//300 *Crediti* Quest
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(SocketHammer), 505769, 150, 0x13E4, 0x55, 5, null));//Incastonatore (per mettere/levare i cristalli su item e mob)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(BondingStone), 505788, 180, 0xFCC, 35, 5, new object[] { 200.0 }));//Bonding Stone (Creature - resuscitabile per 200 ore)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(SkillBall), 505808, 100, 0xE73, 2222, 5, null));//Skill Ball (50 punti)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(SkillBall), 505809, 200, 0xE73, 2222, 5, new object[] { 100 }));//Skill Ball (100 punti)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(TransferBall), 505810, 500, 0x1870, 1152, 5, new object[] { TransferBall.VoidName }));//Transfer Ball (uso libero)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(StatBall), 505811, 50, 0x1870, 1153, 5, null));//Stat Ball (imposti stat, nei limiti razza)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(SpecialDyeTub), 505812, 150, 0xFAB, 0, 0, null));//Dye Tub Speciale (colori a scelta)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(ColoriBrillanti), 505813, 300, 0xFAB, 0, 0, null));//Dye Tub Brillanti (a scelta - 6 cariche)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(ColoriAnticati), 505814, 300, 0xFAB, 0, 0, null));//Dye Tub Anticati (a scelta - 6 cariche)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(LeatherBrillantiDyeTub), 505815, 300, 0xFAB, 0, 0, null));//Dye Tub Brillanti per pelli (richiede 120 tailoring - 6 cariche)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(LeatherAnticatiDyeTub), 505816, 300, 0xFAB, 0, 0, null));//Dye Tub Anticati per pelli (richiede 120 tailoring - 6 cariche)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(Spellbook), 505817, 150, 0xEFA, 2962, 0, new object[] { ulong.MaxValue, 0xEFA, 2962 }));//Spellbook Colorato
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(Spellbook), 505817, 150, 0xEFA, 2246, 0, new object[] { ulong.MaxValue, 0xEFA, 2246 }));//Spellbook Colorato
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(Spellbook), 505817, 150, 0xEFA, 2241, 0, new object[] { ulong.MaxValue, 0xEFA, 2241 }));//Spellbook Colorato
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(Spellbook), 505817, 150, 0xEFA, 2064, 0, new object[] { ulong.MaxValue, 0xEFA, 2064 }));//Spellbook Colorato
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(DruidSpellbook), 505832, 150, 0xEFA, 2962, 0, new object[] { true, 2962 }));//Druid Spellbook Colorato
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(DruidSpellbook), 505832, 150, 0xEFA, 2246, 0, new object[] { true, 2246 }));//Druid Spellbook Colorato
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(DruidSpellbook), 505832, 150, 0xEFA, 2241, 0, new object[] { true, 2241 }));//Druid Spellbook Colorato
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(DruidSpellbook), 505832, 150, 0xEFA, 2064, 0, new object[] { true, 2064 }));//Druid Spellbook Colorato
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(Kasa), 505818, 100, 0x2798, 1161, 0, new object[] { LootType.Blessed, 1161, 0, 0 }));//Cappello di Paglia
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(BackpackDyeTub), 505819, 100, 0xFAB, 1287, 0, new object[] { 1287 }));//BackPack Dye Tub
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(BackpackDyeTub), 505819, 100, 0xFAB, 2100, 0, new object[] { 2100 }));//BackPack Dye Tub
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(Lantern), 505820, 100, 0xA25, 1287, 0, new object[] { LootType.Blessed, 1287 }));//Lanterna Colorata
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(Lantern), 505820, 100, 0xA25, 2100, 0, new object[] { LootType.Blessed, 2100 }));//Lanterna Colorata
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(WildStaff), 505821, 100, 11557, 1161, -10, new object[] { LootType.Blessed, 1161, 0, 0 }));//WildStaff Colorata
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(Torch), 505822, 100, 2578, 1161, -10, new object[] { LootType.Blessed, 1161 }));//Torcia Colorata
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(LeatherGloves), 505823, 150, 5062, 1287, 0, new object[] { LootType.Blessed, 1287, 0, 0 }));//Guanti di Cuoio Colorati
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(Boots), 505824, 150, 5899, 1287, 0, new object[] { LootType.Blessed, 1287, 0, 0 }));//Stivali Colorati
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(TribalMask), 505825, 150, 0x154B, 2100, 4, new object[] { LootType.Blessed, 2100, 0, 0 }));//Maschera Tribale Colorata
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(ColorePerDraghi), 505826, 1000, 4011, 1799, 0, new object[] { 1799 }));//Colore per Draghi (1 carica)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(ColorePerDraghi), 505826, 1000, 4011, 2100, 0, new object[] { 2348 }));//Colore per Draghi (1 carica)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(ColorePerDraghi), 505826, 1000, 4011, 2963, 0, new object[] { 2963 }));//Colore per Draghi (1 carica)
            //Added Flegias 23/01/2018
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(ColorePerDraghi), 505826, 1000, 4011, 2326, 0, new object[] { 2326 }));//Colore per Draghi (1 carica)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(ColorePerDraghi), 505826, 1000, 4011, 2177, 0, new object[] { 2177 }));//Colore per Draghi (1 carica)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(ColorePerDraghi), 505826, 1000, 4011, 1927, 0, new object[] { 1927 }));//Colore per Draghi (1 carica)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(FireworksWand), 1079804, 150, 0xDF2, 0, 0, null));//Fireworks Wand
            //Added Jumala 02/09/2021
            //FenixList.Add(new XmlQuestPointsRewards(0, typeof(Stallone), 505834, 500, 9716, 0, -5, null));//Stallone
            //FenixList.Add(new XmlQuestPointsRewards(0, typeof(Purosangue), 505833, 500, 9717, 0, -5, null));//Purosangue
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(AnelloDellaMagia), 1116811, 300, 0x1F09, 0, -5, null));//Anello della magia +10  mage; regenhits da 1 a 5 random
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(AnelloDellaForesta), 1116812, 300, 0x1F09, 0, -5, null));//Anello della foresta +10 arche; regenhits da 1 a 5 random
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(AnelloDelloSpadaccino), 1116813, 300, 0x1F09, 0, -5, null));//Anello dello spadaccino +10 sword;  regenhits da 1 a 5 random
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(AnelloDelGuerriero), 1116814, 300, 0x1F09, 0, -5, null));//Anello del guerriero +10 tactis;  regenhits da 1 a 5 random
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(AnelloDelFuoco), 1116815, 300, 0x1F09, 0, -5, null));//Anello del fuoco +10 magic Res;  regenhits da 1 a 5 random
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(CollanaDellaSapienza), 1116816, 300, 0x1085, 0, -5, null));//Collana della sapienza +10 anatomy
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(CollanaDellaConoscenza), 1116817, 300, 0x1085, 0, -5, null));//Collana della conoscenza +10 evalInt
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(GlovesOfThePugilist), 1070690, 400, 0x13D5, 0, -5, null));//[Rare] Gloves Of The Pugilist +20 wrestling
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(CollanaDelMentalista), 1116818, 300, 0x1085, 0, -5, null));//Collana del mentalista +10 Meditation
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(BerserkHatchet), 1116891, 600, 0xF43, 2590, -5, null));//Berserk's Corroded Hatchet +5 Lumber
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(BerserkBreathOfTheDead), 1116893, 600, 0x26BB, 2590, -5, null));//Falcetto +5 Herb.
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(HunterBoots), 1116895, 600, 0x317A, 0, -5, null));//[Rare] Hunter's Boots +10 Traking
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(TinkerFullApron), 1116896, 600, 0x153d, 1284, -5, null));//Grembiule da Armaiolo +5 Tinker.
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(TailorFullApron), 1116897, 600, 0x153d, 1284, -5, null));//Grembiule da Sarto +5 Tailor.
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(AlchemyFullApron), 1116898, 600, 0x153d, 1284, -5, null));//Grembiule da Alchimista +5 Alche.
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(BowcraftFullApron), 1116899, 600, 0x153d, 1284, -5, null));//Grembiule da Arcaiolo +5 Bow.
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(BlacksmithFullApron), 1116902, 600, 0x153d, 1284, -5, null));//Grembiule da Fabbro +5 Blacks.
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(CarpentryFullApron), 1116903, 600, 0x153d, 1284, -5, null));//Grembiule da Carpentiere +5 Carpe.
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(JackalsCollar), 1061594, 600, 0x2B76, 2133, -5, null));//[Rare] Jackal's Collar +5 Stealth, +5 Hiding (Solo Bardo)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(JackalsSleeves), 1116900, 600, 0x2B77, 2133, -5, null));//[Rare] Jackal's Sleeves +5 Stealth, +5 Hiding (Solo Bardo)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(JackalsGloves), 1116901, 600, 0x13D5, 2133, -5, null));//[Rare] Jackal's Gloves +10 Hiding (Solo Bardo)
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(BerserkStuddedGloves), 1116888, 600, 0x13D5, 2590, -5, null));//[Base] Berserk's Gloves +20 Armor
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionPamamino), 505851, 800, 9717, 51, -5, null));//New Mustang Panamino
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionGrey), 505852, 900, 9717, 999, -5, null));//New Mustang Grey
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionSky), 505853, 1100, 9717, 611, -5, null));//New Mustang Sky
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionRedRoan), 505854, 1200, 9717, 633, -5, null));//New Mustang Red Roan
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionCrimson), 505855, 1400, 9717, 438, -5, null));//New Mustang Crimson
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(RidableBlackBear), 1116894, 1600, 0x2118, 0, -5, null));//Ridable Black Bear
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionBlack), 505856, 2000, 9717, 1109, -5, null));//New Mustang Black
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(RidableGrifoneBianco), 505858, 3000, 8799, 0, -5, null));//Grifone Bianco
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(BloodyDinosauroCorazzato), 1064035, 3000, 8554, 0, -5, null));//Bloody Dinosauro Corazzato
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(RidableRaptorSella), 1064036, 2500, 8555, 0, -5, null));//Foliage Raptor
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(RidableBloodyRaptor), 505838, 2000, 8549, 0, -5, null));//Ridable Bloody Raptor
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(RidableBloodyStegosauro), 505836, 2000, 8548, 0, -5, null));//Ridable Bloody Stegosauro
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(RidableNightmare), 505857, 3500, 8893, 0, -5, null));//Nightmare
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(RidableEowmu), 505947, 4000, 8471, 0, -5, null));//Eowmu
            FenixList.Add(new XmlQuestPointsRewards(0, typeof(CharacterStatueDeed), 1076189, 3000, 0x14F0, 0, 6, null));//deed statua personale MARMO
            /*large = Gumps.FenixRewardGump.s_LargestString;
			for(int i=m_FenixList.Count - 1; i>=0; --i)
			{
				if((m_FenixList[i].Name.Length*6.1)>large)
					large=m_FenixList[i].Name.Length*6.1;
			}
			Gumps.FenixRewardGump.s_LargestString=(int)Math.Ceiling(large);*/

            //item per EXP
            ExpList.Add(new XmlQuestPointsRewards(0, typeof(SkillBall), 505808, 900, 0xE73, 2222, 5, null));//Skill Ball (50 punti)
            ExpList.Add(new XmlQuestPointsRewards(0, typeof(QuestCreditPoints), 505807, 900, 0, 0, 0, new object[] { 300 }));//300 *Crediti* Quest
            ExpList.Add(new XmlQuestPointsRewards(0, typeof(AutoPremiumDonorSys), 505802, 6000, 0, 0, 0, new object[] { 90 }));//Bonus Account per 3 Mesi
            ExpList.Add(new XmlQuestPointsRewards(0, typeof(RidableBlackBear), 1116894, 2000, 0x2118, 0, -15, null));//Ridable Black Bear
            ExpList.Add(new XmlQuestPointsRewards(0, typeof(ColorePerDraghi), 505826, 1000, 4011, 2590, 0, new object[] { 2590 }));//Colore per Draghi (1 carica)
            ExpList.Add(new XmlQuestPointsRewards(0, typeof(ColorePerDraghi), 505826, 1000, 4011, 1284, 0, new object[] { 1284 }));//Colore per Draghi (1 carica)
            ExpList.Add(new XmlQuestPointsRewards(0, typeof(SoulSeeker), 1075046, 500, 0x2D27, 1424, -15, null));//[Epic] Soul Seeker + 50% hits nazgul (Solo Bardo)
            ExpList.Add(new XmlQuestPointsRewards(0, typeof(CharacterStatueDeed), 1076189, 30000, 0x14F0, 0, 6, null));//deed statua personale MARMO

            //Animali e famili per EXP --vendor 
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(Lion), 505829, 4000, 8541, 0, -5, null));//Leone (Famiglio)
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(TigreDelNord), 505830, 4000, 10094, 0, -5, null));//Tigre del Nord (Famiglio)
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(LeopardoBianco), 505831, 4000, 9653, 2875, 0, null));//Leopardo Bianco (Famiglio)
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(GrifoneCarrot), 505827, 1200, 8504, 2175, 0, null));//Grifone Carrot
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(MustangCarrot), 505828, 1200, 9624, 2175, -5, null));//Mustang Carrot
            //ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(Stallone), 505834, 500, 9716, 0, -5, null));//Stallone
            //ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(Purosangue), 505833, 500, 9717, 0, -5, null));//Purosangue
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionPamamino), 505851, 800, 9717, 51, -5, null));//New Mustang Panamino
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionGrey ), 505852, 900, 9717, 999, -5, null));//New Mustang Grey
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionSky), 505853, 1100, 9717, 611, -5, null));//New Mustang Sky
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionRedRoan), 505854, 1200, 9717, 633, -5, null));//New Mustang Red Roan
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionCrimson), 505855, 1400, 9717, 438, -5, null));//New Mustang Crimson
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(RidableBlackBear), 1116894, 1600, 0x2118, 0, -5, null));//Ridable Black Bear
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(MustangStallionBlack), 505856, 2000, 9717, 1109, -5, null));//New Mustang Black
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(RidableGrifoneBianco), 505858, 3000, 8799, 0, -5, null));//Grifone Bianco
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(RidableBloodyRaptor), 505838, 30000, 8549, 0, -5, null));//Ridable Bloody Raptor
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(RidableBloodyStegosauro), 505836, 30000, 8548, 0, -5, null));//Ridable Bloody Stegosauro
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(RidableRaptorSella), 1064036, 35000, 8555, 0, -5, null));//Foliage Raptor
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(BloodyDinosauroCorazzato), 1064035, 40000, 8554, 0, -5, null));//Bloody Dinosauro Corazzato
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(RidableNightmare), 505857, 50000, 8893, 0, -5, null));//Nightmare
            ExpListAnimali.Add(new XmlQuestPointsRewards(0, typeof(RidableEowmu), 505947, 60000, 8471, 0, -5, null));//Eowmu


            //Animali per punti quest --vendor 
            NewRewardsList.Add(new XmlQuestPointsRewards(500, typeof(Chocobo), 505760, 400, 0x213B, 0, -15, new object[] { true }));//Chocobo (il colore può variare)
            NewRewardsList.Add(new XmlQuestPointsRewards(400, typeof(Raptalon), 505761, 300, 0x2D95, 0, -15, null));//"Raptalon"
            NewRewardsList.Add(new XmlQuestPointsRewards(450, typeof(RidableTigre), 505791, 450, 0x2116, 0, -15, null));//"Tigre Cavalcabile"
            NewRewardsList.Add(new XmlQuestPointsRewards(400, typeof(RidableKarsten), 505792, 400, 0x2128, 0, -15, null));//Karsten Cavalcabile
            NewRewardsList.Add(new XmlQuestPointsRewards(400, typeof(RidableRaptor), 505793, 400, 0x2146, 0, -15, null));//"Raptor Cavalcabile"
            NewRewardsList.Add(new XmlQuestPointsRewards(400, typeof(RidableStegosauro), 505794, 400, 0x2139, 0, -15, null));//"Stregosauro Cavalcabile"
            NewRewardsList.Add(new XmlQuestPointsRewards(550, typeof(RidableCavalloCorazzato), 505795, 500, 0x2145, 0, -15, null));//"Cavallo Corazzato"
            NewRewardsList.Add(new XmlQuestPointsRewards(500, typeof(Zebra), 505799, 400, 0x2155, 0, -15, null));//"Zebra Cavalcabile"
            NewRewardsList.Add(new XmlQuestPointsRewards(500, typeof(CavalloMaculato), 505800, 400, 0x215E, 0, -15, null));//"Cavallo Maculato"
            NewRewardsList.Add(new XmlQuestPointsRewards(550, typeof(CavalloBardato), 505801, 500, 0x2160, 0, -15, null));//"Cavallo Bardato"

            //Ricompense dei PUNTI-KILL
            //*PointsRewardList.Add(new XmlQuestPointsRewards(0, typeof(XmlLifeDrain), 505843, 10, 0, 0, 0, new object[] { 5, 3.0, 5.0, "XmlPointsExtra" }));//+5 Life Drain for 5 minutes
            //*PointsRewardList.Add(new XmlQuestPointsRewards(0, typeof(XmlSpeed), 505846, 10, 0, 0, 0, new object[] { TimeSpan.FromMinutes(10.0) }));//Speed for 10 minutes
            PointsRewardList.Add(new XmlQuestPointsRewards(100, typeof(BerserkHalfApron), 1116885, 200, 0x153b, 2590, 0, null));//[Base] Berserk's Half Apron
            PointsRewardList.Add(new XmlQuestPointsRewards(100, typeof(BerserkBoots), 1116886, 200, 0x170B, 2590, 0, null));//[Base] Berserk's Boots
            PointsRewardList.Add(new XmlQuestPointsRewards(100, typeof(HoodedShroudOfBerserk), 1116887, 500, 0x2684, 2590, 0, null));//[Base] Berserk's Hooded Shroud
            PointsRewardList.Add(new XmlQuestPointsRewards(100, typeof(BerserkDoublet), 1116889, 300, 0x1F7B, 2590, 0, null));//[Base] Berserk's Doublet
            PointsRewardList.Add(new XmlQuestPointsRewards(100, typeof(BerserkKilt), 1116890, 300, 0x1537, 2590, 0, null));//[Base] Berserk's Kilt
            PointsRewardList.Add(new XmlQuestPointsRewards(200, typeof(BackpackDyeTub), 505819, 100, 0xFAB, 2590, 0, new object[] { 2590 }));//Backpack DyeTub
            PointsRewardList.Add(new XmlQuestPointsRewards(200, typeof(BackpackDyeTub), 505819, 100, 0xFAB, 1284, 0, new object[] { 1284 }));//Backpack DyeTub
            PointsRewardList.Add(new XmlQuestPointsRewards(300, typeof(TalismanoDelVolatile), 504399, 100, 0x2F5A, 0, 0, null));//Backpack DyeTub
            PointsRewardList.Add(new XmlQuestPointsRewards(300, typeof(BerserkHairDye), 1041088, 300, 0xEFF, 2590, 0, null));//Berserk's Hair Dye
            PointsRewardList.Add(new XmlQuestPointsRewards(400, typeof(ElectricBlueDyeTub), 1116904, 600, 0xFAB, 1284, 0, null));//Electric Blue Dye Tub
            PointsRewardList.Add(new XmlQuestPointsRewards(200, typeof(SoulSeeker), 1075046, 500, 0x2D27, 1424, -15, null));//[Epic] Soul Seeker + 50% hits nazgul (Solo Bardo)
        }
    }
}