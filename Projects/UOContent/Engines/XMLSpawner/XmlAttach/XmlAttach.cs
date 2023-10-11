#define RESTRICTCONSTRUCTABLE
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Reflection;

namespace Server.Engines.XmlSpawner2
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class Attachable : Attribute
    {
        private AccessLevel m_AccessLevel;

        public AccessLevel AccessLevel
        {
            get => m_AccessLevel;
            set => m_AccessLevel = value;
        }

        public Attachable() : this(AccessLevel.GameMaster)
        {
        }

        public Attachable(AccessLevel access)
        {
            m_AccessLevel = access;
        }
    }

    public class ASerial
    {
        private int m_SerialValue;

        public int Value => m_SerialValue;

        public ASerial(int serial)
        {
            m_SerialValue = serial;
        }

        private static int m_GlobalSerialValue;

        public static bool serialInitialized = false;

        public static ASerial NewSerial()
        {
            // it is possible for new attachments to be constructed before existing attachments are deserialized and the current m_globalserialvalue
            // restored.  This creates a possible serial conflict, so dont allow assignment of valid serials until proper deser of m_globalserialvalue
            // Resolve unassigned serials in initialization
            if (!serialInitialized)
            {
                return new ASerial(0);
            }

            if (m_GlobalSerialValue == int.MaxValue || m_GlobalSerialValue < 0)
            {
                m_GlobalSerialValue = 0;
            }

            // try the next serial number in the series
            int newserialno = m_GlobalSerialValue + 1;

            // check to make sure that it is not in use
            while (XmlAttach.AllAttachments.ContainsKey(newserialno))
            {
                newserialno++;
                if (newserialno == int.MaxValue || newserialno < 0)
                {
                    newserialno = 1;
                }
            }

            m_GlobalSerialValue = newserialno;

            return new ASerial(m_GlobalSerialValue);
        }

        internal static void GlobalSerialize(GenericWriter writer)
        {
            writer.Write(m_GlobalSerialValue);
        }

        internal static void GlobalDeserialize(GenericReader reader)
        {
            m_GlobalSerialValue = reader.ReadInt();
        }
    }

    public class XmlAttach
    {
        private static string m_FilePath = Path.Combine(string.Format("{0}/Attachments", World.PrimaryName), "Attachments.bin");        // the attachment serializations
        private static string m_ImaPath = Path.Combine(string.Format("{0}/Attachments", World.PrimaryName), "Attachments.ima");         // the item/mob attachment tables
        private static string m_FpiPath = Path.Combine(string.Format("{0}/Attachments", World.PrimaryName), "Attachments.fpi");        // the file position indices
        private static Type m_AttachableType = typeof(Attachable);

        public static bool IsAttachable(ConstructorInfo ctor)
        {
            return ctor.IsDefined(m_AttachableType, false);
        }

        public static bool IsAttachable(ConstructorInfo ctor, AccessLevel accessLevel)
        {
            object[] attrs = ctor.GetCustomAttributes(m_AttachableType, false);

            if (attrs.Length == 0)
            {
                return false;
            }

            return accessLevel >= ((Attachable)attrs[0]).AccessLevel;
        }

        public static void HashSerial(ASerial key, XmlAttachment o)
        {
            if (key.Value != 0)
            {
                AllAttachments[key.Value] = o;//.Add(key.Value, o);
            }
            else
            {
                UnassignedAttachments.Add(o);
            }
        }

        // each entry in the hashtable is an array of XmlAttachments that is keyed by an object.
        public static Dictionary<IEntity, List<XmlAttachment>> EntityAttachments = new Dictionary<IEntity, List<XmlAttachment>>();
        public static Dictionary<int, XmlAttachment> AllAttachments = new Dictionary<int, XmlAttachment>();
        private static List<XmlAttachment> UnassignedAttachments = new List<XmlAttachment>();

        public static bool HasAttachments(IEntity o)
        {
            if (o == null)
            {
                return false;
            }

            if(EntityAttachments.TryGetValue(o, out List<XmlAttachment> alist))
            {
                return alist != null && alist.Count > 0;
            }
 
            return false;
        }

        public static XmlAttachment[] Values
        {
            get
            {
                XmlAttachment[] valuearray = new XmlAttachment[AllAttachments.Count];
                XmlAttach.AllAttachments.Values.CopyTo(valuearray, 0);
                return valuearray;
            }
        }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
            EventSink.WorldPostLoad += new WorldPostLoadEventHandler(XmlAttachment.PostLoadChecks);
        }

        public static void Initialize()
        {
            ASerial.serialInitialized = true;
            //CommandSystem.Register( "RecuperoBless", AccessLevel.Developer, new CommandEventHandler( XmlBlessItem.Recupero_OnCommand ) );
            //CommandSystem.Register( "RecuperoBlessLoad", AccessLevel.Owner, new CommandEventHandler( XmlBlessItem.RecuperoLoad_OnCommand ) );
            // resolve unassigned serials
            foreach (XmlAttachment a in UnassignedAttachments)
            {
                // get the next unique serial id
                ASerial serial = ASerial.NewSerial();
                a.Serial = serial;

                // register the attachment in the serial keyed hashtable
                XmlAttach.HashSerial(serial, a);
            }
            UnassignedAttachments = null;

            // Register our speech handler
            EventSink.Speech += new SpeechEventHandler(EventSink_Speech);

            // Register our movement handler
            EventSink.Movement += new MovementEventHandler(EventSink_Movement);

            //CommandSystem.Register( "AllAtt", AccessLevel.GameMaster, new CommandEventHandler( ListItemAttachments_OnCommand ) );
            //CommandSystem.Register( "MobAtt", AccessLevel.GameMaster, new CommandEventHandler( ListMobileAttachments_OnCommand ) );
            CommandSystem.Register("GetAtt", AccessLevel.GameMaster, new CommandEventHandler(GetAttachments_OnCommand));
            //CommandSystem.Register( "DelAtt", AccessLevel.GameMaster, new CommandEventHandler( DeleteAttachments_OnCommand ) );
            //CommandSystem.Register( "TrigAtt", AccessLevel.GameMaster, new CommandEventHandler( ActivateAttachments_OnCommand ) );
            //CommandSystem.Register( "AddAtt", AccessLevel.GameMaster, new CommandEventHandler( AddAttachment_OnCommand ) );
            TargetCommands.Register(new AddAttCommand());
            TargetCommands.Register(new DelAttCommand());
            TargetCommands.Register(new SetAttCommand());
            CommandSystem.Register("AvailAtt", AccessLevel.GameMaster, new CommandEventHandler(ListAvailableAttachments_OnCommand));
        }

        public class AddAttCommand : BaseCommand
        {
            public AddAttCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.All;
                Commands = new string[] { "AddAtt" };
                ObjectTypes = ObjectTypes.Both;
                Usage = "AddAtt type [args]";
                Description = "Adds an attachment to the targeted object.";
                ListOptimized = true;
            }

            public override bool ValidateArgs(BaseCommandImplementor impl, CommandEventArgs e)
            {
                if (e.Arguments.Length >= 1)
                {
                    return true;
                }

                e.Mobile.SendMessage("Usage: " + Usage);
                return false;
            }

            public override void ExecuteList(CommandEventArgs e, List<object> list)
            {
                if (e != null && list != null && e.Length >= 1)
                {

                    // create a new attachment and add it to the item
                    int nargs = e.Arguments.Length - 1;

                    string[] args = new string[nargs];

                    for (int j = 0; j < nargs; j++)
                    {
                        args[j] = e.Arguments[j + 1];
                    }

                    Type attachtype = SpawnerType.GetType(e.Arguments[0]);

                    if (attachtype != null && attachtype.IsSubclassOf(typeof(XmlAttachment)))
                    {
                        // go through all of the objects in the list
                        int count = 0;

                        for (int i = 0; i < list.Count; ++i)
                        {

                            XmlAttachment o = (XmlAttachment)XmlSpawner.CreateObject(attachtype, args, false, true, e.Mobile.AccessLevel);

                            if (o == null)
                            {
                                AddResponse(string.Format("Unable to construct {0} with specified args", attachtype.Name));
                                break;
                            }

                            if (AttachTo(null, list[i] as IEntity, o, true))
                            {
                                if (list.Count < 10)
                                {
                                    AddResponse(string.Format("Added {0} to {1}", attachtype.Name, list[i]));
                                }
                                ++count;
                            }
                            else
                            {
                                LogFailure(string.Format("Attachment {0} not added to {1}", attachtype.Name, list[i]));
                            }
                        }
                        if (count > 0)
                        {
                            AddResponse(string.Format("Attachment {0} has been added [{1}]", attachtype.Name, count));
                        }
                        else
                        {
                            AddResponse(string.Format("Attachment {0} not added", attachtype.Name));
                        }
                    }
                    else
                    {
                        AddResponse(string.Format("Invalid attachment type {0}", e.Arguments[0]));
                    }
                }
            }
        }

        public class DelAttCommand : BaseCommand
        {
            public DelAttCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.All;
                Commands = new string[] { "DelAtt" };
                ObjectTypes = ObjectTypes.Both;
                Usage = "DelAtt type";
                Description = "Deletes an attachment on the targeted object.";
                ListOptimized = true;
            }

            public override bool ValidateArgs(BaseCommandImplementor impl, CommandEventArgs e)
            {
                if (e.Arguments.Length >= 1)
                {
                    return true;
                }

                e.Mobile.SendMessage("Usage: " + Usage);
                return false;
            }

            public override void ExecuteList(CommandEventArgs e, List<object> list)
            {
                if (e != null && list != null && e.Length >= 1)
                {
                    Type attachtype = SpawnerType.GetType(e.Arguments[0]);

                    if (attachtype != null && attachtype.IsSubclassOf(typeof(XmlAttachment)))
                    {

                        // go through all of the objects in the list
                        int count = 0;

                        for (int i = 0; i < list.Count; ++i)
                        {
                            List<XmlAttachment> alist = XmlAttach.FindAttachments(list[i] as IEntity, attachtype);

                            if (alist != null)
                            {
                                // delete the attachments
                                foreach (XmlAttachment a in alist)
                                {
                                    a.Delete();
                                    if (list.Count < 10)
                                    {
                                        AddResponse(string.Format("Deleted {0} from {1}", attachtype.Name, list[i]));
                                    }
                                    ++count;
                                }
                            }
                        }

                        if (count > 0)
                        {
                            AddResponse(string.Format("Attachment {0} has been deleted [{1}]", attachtype.Name, count));
                        }
                        else
                        {
                            AddResponse(string.Format("Attachment {0} not deleted", attachtype.Name));
                        }
                    }
                    else
                    {
                        AddResponse(string.Format("Invalid attachment type {0}", e.Arguments[0]));
                    }
                }
            }
        }

        public static void CleanUp()
        {
            // clean up any unowned attachments
            XmlAttachment[] arr = Values;
            foreach (XmlAttachment a in arr)
            {
                if (a.OwnedBy == null || a.OwnedBy.Deleted)
                {
                    a.Delete();
                }
            }
        }

        public static void Save(WorldSaveEventArgs e)
        {
            if (EntityAttachments == null)
            {
                return;
            }

            CleanUp();

            if (!Directory.Exists(string.Format("{0}/Attachments", World.PrimaryName)))
            {
                Directory.CreateDirectory(string.Format("{0}/Attachments", World.PrimaryName));
            }

            BinaryFileWriter writer = new BinaryFileWriter(m_FilePath, true); ;
            BinaryFileWriter imawriter = new BinaryFileWriter(m_ImaPath, true); ;
            BinaryFileWriter fpiwriter = new BinaryFileWriter(m_FpiPath, true); ;

            if (writer != null && imawriter != null && fpiwriter != null)
            {
                // save the current global attachment serial state
                ASerial.GlobalSerialize(writer);

                // remove all deleted attachments
                FullDefrag();

                // save the attachments themselves
                if (AllAttachments != null)
                {
                    writer.Write(AllAttachments.Count);

                    XmlAttachment[] valuearray = new XmlAttachment[AllAttachments.Count];
                    AllAttachments.Values.CopyTo(valuearray, 0);

                    int[] keyarray = new int[AllAttachments.Count];
                    AllAttachments.Keys.CopyTo(keyarray, 0);

                    for (int i = 0; i < keyarray.Length; ++i)
                    {
                        // write the key
                        writer.Write(keyarray[i]);

                        XmlAttachment a = valuearray[i];

                        // write the value type
                        writer.Write(a.GetType().ToString());

                        // serialize the attachment itself
                        a.Serialize(writer);

                        // save the fileposition index
                        fpiwriter.Write(writer.Position);
                    }
                }
                else
                {
                    writer.Write(0);
                }

                writer.Close();

                // save the dictionary table info for items and mobiles
                // all attachments
                if (EntityAttachments != null)
                {
                    imawriter.Write(EntityAttachments.Count);
                    List<XmlAttachment>[] valuearray = new List<XmlAttachment>[EntityAttachments.Count];
                    EntityAttachments.Values.CopyTo(valuearray, 0);

                    IEntity[] keyarray = new IEntity[EntityAttachments.Count];
                    EntityAttachments.Keys.CopyTo(keyarray, 0);

                    for (int i = 0; i < keyarray.Length; ++i)
                    {
                        // write the key
                        imawriter.Write(keyarray[i]);

                        // write out the attachments
                        List<XmlAttachment> alist = valuearray[i];

                        imawriter.Write(alist.Count);
                        foreach (XmlAttachment a in alist)
                        {
                            // write the attachment serial
                            imawriter.Write(a.Serial.Value);

                            // write the value type
                            imawriter.Write(a.GetType().ToString());

                            // save the fileposition index
                            fpiwriter.Write(imawriter.Position);
                        }
                    }
                }
                else
                {
                    // no attachments
                    imawriter.Write(0);
                }
                //old writer for items
                imawriter.Write(0);

                imawriter.Close();
                fpiwriter.Close();
            }
        }

        public static void Load()
        {
            if (!File.Exists(m_FilePath))
            {
                return;
            }
            FileStream fs;
            BinaryFileReader reader;
            FileStream imafs;
            BinaryFileReader imareader;
            FileStream fpifs;
            BinaryFileReader fpireader;

            try
            {
                fs = new FileStream(m_FilePath, (FileMode)3, (FileAccess)1, (FileShare)1);
                reader = new BinaryFileReader(new BinaryReader(fs));
                imafs = new FileStream(m_ImaPath, (FileMode)3, (FileAccess)1, (FileShare)1);
                imareader = new BinaryFileReader(new BinaryReader(imafs));
                fpifs = new FileStream(m_FpiPath, (FileMode)3, (FileAccess)1, (FileShare)1);
                fpireader = new BinaryFileReader(new BinaryReader(fpifs));
            }
            catch (Exception e)
            {
                ErrorReporter.GenerateErrorReport(e.ToString());
                return;
            }

            if (reader != null && imareader != null && fpireader != null)
            {
                // restore the current global attachment serial state
                try
                {
                    ASerial.GlobalDeserialize(reader);
                }
                catch (Exception e)
                {
                    ErrorReporter.GenerateErrorReport(e.ToString());
                    return;
                }

                ASerial.serialInitialized = true;

                // read in the serial attachment dict table information
                int count;
                try
                {
                    count = reader.ReadInt();
                }
                catch (Exception e)
                {
                    ErrorReporter.GenerateErrorReport(e.ToString());
                    return;
                }

                for (int i = 0; i < count; ++i)
                {
                    // read the serial
                    ASerial serialno;
                    try
                    {
                        serialno = new ASerial(reader.ReadInt());
                    }
                    catch (Exception e)
                    {
                        ErrorReporter.GenerateErrorReport(e.ToString());
                        return;
                    }

                    // read the attachment type
                    string valuetype;
                    try
                    {
                        valuetype = reader.ReadString();
                    }
                    catch (Exception e)
                    {
                        ErrorReporter.GenerateErrorReport(e.ToString());
                        return;
                    }

                    // read the position of the beginning of the next attachment deser within the .bin file
                    long position;
                    try
                    {
                        position = fpireader.ReadLong();

                    }
                    catch (Exception e)
                    {
                        ErrorReporter.GenerateErrorReport(e.ToString());
                        return;
                    }

                    bool skip = false;

                    XmlAttachment o = null;
                    try
                    {
                        o = (XmlAttachment)Activator.CreateInstance(Type.GetType(valuetype), new object[] { serialno });
                    }
                    catch
                    {
                        skip = true;
                    }

                    if (skip)
                    {
                        if (!AlreadyReported(valuetype))
                        {
                            Console.WriteLine("\nError deserializing attachments {0}.\nMissing a serial constructor?\n", valuetype);
                            ReportDeserError(valuetype, "Missing a serial constructor?");
                        }
                        // position the .ima file at the next deser point
                        try
                        {
                            reader.Seek(position, SeekOrigin.Begin);
                        }
                        catch
                        {
                            ErrorReporter.GenerateErrorReport("Error deserializing. Attachments save file corrupted. Attachment load aborted.");
                            return;
                        }
                        continue;
                    }
                    //reader.InsideObj=o;

                    try
                    {
                        o.Deserialize(reader);
                    }
                    catch
                    {
                        skip = true;
                    }

                    // confirm the read position
                    if (reader.Position != position || skip)
                    {
                        if (!AlreadyReported(valuetype))
                        {
                            Console.WriteLine("\nError deserializing attachments {0}\n", valuetype);
                            ReportDeserError(valuetype, "save file corruption or incorrect Serialize/Deserialize methods?");
                        }
                        // position the .ima file at the next deser point
                        try
                        {
                            reader.Seek(position, SeekOrigin.Begin);
                        }
                        catch
                        {
                            ErrorReporter.GenerateErrorReport("Error deserializing. Attachments save file corrupted. Attachment load aborted.");
                            return;
                        }
                        continue;
                    }

                    // add it to the hash table
                    try
                    {
                        AllAttachments.Add(serialno.Value, o);
                    }
                    catch
                    {
                        ErrorReporter.GenerateErrorReport(string.Format("\nError deserializing {0} serialno {1}. Attachments save file corrupted. Attachment load aborted.\n",
                        valuetype, serialno.Value));
                        return;
                    }
                }

                // read in the global attachment dict table information
                //two iterations
                for (int num = 0; num < 2; num++)
                {
                    try
                    {
                        count = imareader.ReadInt();
                    }
                    catch (Exception e)
                    {
                        ErrorReporter.GenerateErrorReport(e.ToString());
                        return;
                    }

                    for (int i = 0; i < count; ++i)
                    {
                        IEntity key;
                        try
                        {
                            key = imareader.ReadEntity();
                        }
                        catch (Exception e)
                        {
                            ErrorReporter.GenerateErrorReport(e.ToString());
                            return;
                        }

                        int nattach;
                        try
                        {
                            nattach = imareader.ReadInt();
                        }
                        catch (Exception e)
                        {
                            ErrorReporter.GenerateErrorReport(e.ToString());
                            return;
                        }

                        for (int j = 0; j < nattach; j++)
                        {
                            // and serial
                            ASerial serialno;
                            try
                            {
                                serialno = new ASerial(imareader.ReadInt());
                            }
                            catch (Exception e)
                            {
                                ErrorReporter.GenerateErrorReport(e.ToString());
                                return;
                            }

                            // read the attachment type
                            string valuetype;
                            try
                            {
                                valuetype = imareader.ReadString();
                            }
                            catch (Exception e)
                            {
                                ErrorReporter.GenerateErrorReport(e.ToString());
                                return;
                            }

                            // read the position of the beginning of the next attachment deser within the .bin file
                            long position;
                            try
                            {
                                position = fpireader.ReadLong();
                            }
                            catch (Exception e)
                            {
                                ErrorReporter.GenerateErrorReport(e.ToString());
                                return;
                            }

                            XmlAttachment o = FindAttachmentBySerial(serialno.Value);

                            if (o == null || imareader.Position != position)
                            {
                                if (!AlreadyReported(valuetype))
                                {
                                    Console.WriteLine("\nError deserializing attachments of type {0}.\n", valuetype);
                                    ReportDeserError(valuetype, "save file corruption or incorrect Serialize/Deserialize methods?");
                                }
                                // position the .ima file at the next deser point
                                try
                                {
                                    imareader.Seek(position, SeekOrigin.Begin);
                                }
                                catch
                                {
                                    ErrorReporter.GenerateErrorReport("Error deserializing. Attachments save file corrupted. Attachment load aborted.");
                                    return;
                                }
                                continue;
                            }

                            // attachment successfully deserialized so attach it
                            AttachTo(key, o, false);
                        }
                    }
                }

                if (fs != null)
                {
                    fs.Close();
                }

                if (imafs != null)
                {
                    imafs.Close();
                }

                if (fpifs != null)
                {
                    fpifs.Close();
                }

                if (desererror != null)
                {
                    ErrorReporter.GenerateErrorReport("Error deserializing particular attachments.");
                }
            }
        }

        private class DeserErrorDetails
        {
            public string Type;
            public string Details;

            public DeserErrorDetails(string type, string details)
            {
                Type = type;
                Details = details;
            }

        }
        private static List<DeserErrorDetails> desererror = null;
        private static void ReportDeserError(string typestr, string detailstr)
        {
            if (desererror == null)
            {
                desererror = new List<DeserErrorDetails>();
            }

            desererror.Add(new DeserErrorDetails(typestr, detailstr));
        }
        private static bool AlreadyReported(string typestr)
        {
            if (desererror == null)
            {
                return false;
            }

            foreach (DeserErrorDetails s in desererror)
            {
                if (s.Type == typestr)
                {
                    return true;
                }
            }
            return false;
        }

        public static void CheckOnBeforeKill(Mobile m_killed, Mobile m_killer)
        {

            // do not register creature vs creature kills, nor any kills involving staff
            //            if (m_killer == null || m_killed == null || !(m_killer.Player || m_killed.Player) /*|| (m_killer.AccessLevel > AccessLevel.Player) || (m_killed.AccessLevel > AccessLevel.Player) */)
            //				return;

            if (m_killer != null)
            {
                // check the killer
                List<XmlAttachment> alist = XmlAttach.FindAttachments(m_killer);
                if (alist != null)
                {
                    foreach (XmlAttachment a in alist)
                    {
                        if (a != null && !a.Deleted && a.HandlesOnKill)
                        {
                            a.OnBeforeKill(m_killed, m_killer);
                        }
                    }
                }

                // check any equipped items
                List<Item> equiplist = m_killer.Items;
                if (equiplist != null)
                {
                    foreach (Item i in equiplist)
                    {
                        if (i == null || i.Deleted)
                        {
                            continue;
                        }

                        alist = FindAttachments(i);
                        if (alist != null)
                        {
                            foreach (XmlAttachment a in alist)
                            {
                                if (a != null && !a.Deleted && a.CanActivateEquipped && a.HandlesOnKill)
                                {
                                    a.OnBeforeKill(m_killed, m_killer);
                                }
                            }
                        }
                    }
                }
            }

            if (m_killed != null)
            {
                // check the killed
                List<XmlAttachment> alist = XmlAttach.FindAttachments(m_killed);
                if (alist != null)
                {
                    foreach (XmlAttachment a in alist)
                    {
                        if (a != null && !a.Deleted && a.HandlesOnKilled)
                        {
                            a.OnBeforeKilled(m_killed, m_killer);
                        }
                    }
                }
            }
        }


        public static void CheckOnKill(Mobile m_killed, Mobile m_killer, bool last)
        {

            // do not register creature vs creature kills, nor any kills involving staff
            //            if (m_killer == null || m_killed == null || !(m_killer.Player || m_killed.Player) /*|| (m_killer.AccessLevel > AccessLevel.Player) || (m_killed.AccessLevel > AccessLevel.Player) */)
            //				return;

            if (m_killer != null)
            {
                // check the killer
                List<XmlAttachment> alist = XmlAttach.FindAttachments(m_killer);
                if (alist != null)
                {
                    foreach (XmlAttachment a in alist)
                    {
                        if (a != null && !a.Deleted && a.HandlesOnKill)
                        {
                            a.OnKill(m_killed, m_killer);
                        }
                    }
                }

                // check any equipped items
                List<Item> equiplist = m_killer.Items;
                if (equiplist != null)
                {
                    foreach (Item i in equiplist)
                    {
                        if (i == null || i.Deleted)
                        {
                            continue;
                        }

                        alist = XmlAttach.FindAttachments(i);
                        if (alist != null)
                        {
                            foreach (XmlAttachment a in alist)
                            {
                                if (a != null && !a.Deleted && a.CanActivateEquipped && a.HandlesOnKill)
                                {
                                    a.OnKill(m_killed, m_killer);
                                }
                            }
                        }
                    }
                }
            }

            if (m_killed != null)
            {
                // check the killed
                List<XmlAttachment> alist = FindAttachments(m_killed);
                if (alist != null)
                {
                    foreach (XmlAttachment a in alist)
                    {
                        if (a != null && !a.Deleted && a.HandlesOnKilled)
                        {
                            a.OnKilled(m_killed, m_killer, last);
                        }
                    }
                }
            }
        }

        public static void EventSink_Movement(MovementEventArgs args)
        {
            Mobile from = args.Mobile;

            if (!from.Player /* || from.AccessLevel > AccessLevel.Player */)
            {
                return;
            }

            // check for any items in the same sector
            if (from.Map != null)
            {
                IPooledEnumerable<Item> itemlist = from.Map.GetItemsInRange(from.Location, Map.SectorSize);
                if (itemlist != null)
                {
                    foreach (Item i in itemlist)
                    {
                        if (i == null || i.Deleted)
                        {
                            continue;
                        }

                        List<XmlAttachment> alist = FindAttachments(i);
                        if (alist != null)
                        {
                            foreach (XmlAttachment a in alist)
                            {
                                if (a != null && !a.Deleted && a.HandlesOnMovement)
                                {
                                    a.OnMovement(args);
                                }
                            }
                        }
                    }
                    itemlist.Free();
                }


                // check for mobiles
                IPooledEnumerable<Mobile> moblist = from.Map.GetMobilesInRange(from.Location, Map.SectorSize);
                if (moblist != null)
                {
                    foreach (Mobile i in moblist)
                    {
                        // dont respond to self motion
                        if (i == null || i.Deleted || i == from)
                        {
                            continue;
                        }

                        List<XmlAttachment> alist = FindAttachments(i);
                        if (alist != null)
                        {
                            foreach (XmlAttachment a in alist)
                            {
                                if (a != null && !a.Deleted && a.HandlesOnMovement)
                                {
                                    a.OnMovement(args);
                                }
                            }
                        }
                    }
                    moblist.Free();
                }
            }
        }

        public static void EventSink_Speech(SpeechEventArgs args)
        {
            Mobile from = args.Mobile;

            if (from == null || from.Map == null /*|| from.AccessLevel > AccessLevel.Player */)
            {
                return;
            }

            // check the mob for any attachments that might handle speech
            List<XmlAttachment> alist = FindAttachments(from);
            if (alist != null)
            {
                foreach (XmlAttachment a in alist)
                {
                    if (a != null && !a.Deleted && a.HandlesOnSpeech)
                    {
                        a.OnSpeech(args);
                    }
                }
            }

            // check for any nearby items
            IPooledEnumerable<Item> itemlist = from.Map.GetItemsInRange(from.Location, Map.SectorSize);
            if (itemlist != null)
            {
                foreach (Item i in itemlist)
                {
                    if (i == null || i.Deleted)
                    {
                        continue;
                    }

                    alist = FindAttachments(i);
                    if (alist != null)
                    {
                        foreach (XmlAttachment a in alist)
                        {
                            if (a != null && !a.Deleted && a.CanActivateInWorld && a.HandlesOnSpeech)
                            {
                                a.OnSpeech(args);
                            }
                        }
                    }
                }
                itemlist.Free();
            }


            // check for any nearby mobs
            IPooledEnumerable<Mobile> moblist = from.Map.GetMobilesInRange(from.Location, Map.SectorSize);
            if (moblist != null)
            {
                foreach (Mobile i in moblist)
                {
                    if (i == null || i.Deleted)
                    {
                        continue;
                    }

                    alist = FindAttachments(i);
                    if (alist != null)
                    {
                        foreach (XmlAttachment a in alist)
                        {
                            if (a != null && !a.Deleted && a.HandlesOnSpeech)
                            {
                                a.OnSpeech(args);
                            }
                        }
                    }
                }
                moblist.Free();
            }



            // also check for any items in the mobs toplevel backpack
            if (from.Backpack != null)
            {
                List<Item> packlist = from.Backpack.Items;
                if (packlist != null)
                {
                    foreach (Item i in packlist)
                    {
                        if (i == null || i.Deleted)
                        {
                            continue;
                        }

                        alist = FindAttachments(i);
                        if (alist != null)
                        {
                            foreach (XmlAttachment a in alist)
                            {
                                if (a != null && !a.Deleted && a.CanActivateInBackpack && a.HandlesOnSpeech)
                                {
                                    a.OnSpeech(args);
                                }
                            }
                        }
                    }
                }
            }

            // check any equipped items
            List<Item> equiplist = from.Items;
            if (equiplist != null)
            {
                foreach (Item i in equiplist)
                {
                    if (i == null || i.Deleted)
                    {
                        continue;
                    }

                    alist = FindAttachments(i);
                    if (alist != null)
                    {
                        foreach (XmlAttachment a in alist)
                        {
                            if (a != null && !a.Deleted && a.CanActivateEquipped && a.HandlesOnSpeech)
                            {
                                a.OnSpeech(args);
                            }
                        }
                    }
                }
            }
        }

        public static XmlAttachment FindAttachmentOnMobile(Mobile from, Type type, string name)
        {
            if (from == null)
            {
                return null;
            }
            // check the mob for any attachments
            List<XmlAttachment> alist = FindAttachments(from);
            if (alist != null)
            {
                foreach (XmlAttachment a in alist)
                {
                    if (a != null && !a.Deleted && (type == null || (a.GetType() == type || a.GetType().IsSubclassOf(type))) && (name == null || name == a.Name))
                    {
                        return a;
                    }
                }
            }

            // also check for any items in the mobs toplevel backpack
            if (from.Backpack != null)
            {
                List<Item> itemlist = from.Backpack.Items;
                if (itemlist != null)
                {
                    foreach (Item i in itemlist)
                    {
                        if (i == null || i.Deleted)
                        {
                            continue;
                        }

                        alist = FindAttachments(i);
                        if (alist != null)
                        {
                            foreach (XmlAttachment a in alist)
                            {
                                if (a != null && !a.Deleted && (type == null || (a.GetType() == type || a.GetType().IsSubclassOf(type))) && (name == null || name == a.Name))
                                {
                                    return a;
                                }
                            }
                        }
                    }
                }
            }

            // check any equipped items
            List<Item> equiplist = from.Items;
            if (equiplist != null)
            {
                foreach (Item i in equiplist)
                {
                    if (i == null || i.Deleted)
                    {
                        continue;
                    }

                    alist = FindAttachments(i);

                    if (alist != null)
                    {
                        foreach (XmlAttachment a in alist)
                        {
                            if (a != null && !a.Deleted && (type == null || (a.GetType() == type || a.GetType().IsSubclassOf(type))) && (name == null || name == a.Name))
                            {
                                return a;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private class AttachTarget : Target
        {
            private CommandEventArgs m_e;
            private string m_set = null;

            public AttachTarget(CommandEventArgs e, string sset)
                : base(30, false, TargetFlags.None)
            {
                m_e = e;
                m_set = sset;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || !(targeted is IEntity ie))
                {
                    return;
                }

                Type type = null;
                string name = null;

                if (m_e.Arguments.Length > 0)
                {
                    type = SpawnerType.GetType(m_e.Arguments[0]);
                }
                if (m_e.Arguments.Length > 1)
                {
                    name = m_e.Arguments[1];
                }

                Defrag(ie);

                List<XmlAttachment> plist = FindAttachments(ie, type);

                if (plist == null && m_set != "add")
                {
                    from.SendMessage("No attachments");
                    return;
                }

                switch (m_set)
                {
                    case "add":

                        if (m_e.Arguments.Length < 1)
                        {
                            from.SendMessage("Must specify an attachment type.");
                            return;
                        }

                        // create a new attachment and add it to the item
                        int nargs = m_e.Arguments.Length - 1;

                        string[] args = new string[nargs];

                        for (int j = 0; j < nargs; j++)
                        {
                            args[j] = m_e.Arguments[j + 1];
                        }


                        XmlAttachment o = null;

                        Type attachtype = SpawnerType.GetType(m_e.Arguments[0]);

                        if (attachtype != null && attachtype.IsSubclassOf(typeof(XmlAttachment)))
                        {

                            o = (XmlAttachment)XmlSpawner.CreateObject(attachtype, args, false, true, from.AccessLevel);
                        }

                        if (o != null)
                        {
                            //o.Name = aname;
                            if (AttachTo(from, ie, o, true))
                            {
                                from.SendMessage("Added attachment {2} : {0} to {1}", m_e.Arguments[0], ie, o.Serial.Value);
                            }
                            else
                            {
                                from.SendMessage("Attachment not added: {0}", m_e.Arguments[0]);
                            }
                        }
                        else
                        {
                            from.SendMessage("Unable to construct attachment {0}", m_e.Arguments[0]);
                        }

                        break;
                    case "get":
                        /*
							foreach(XmlAttachment p in plist)
							{
								if(p == null || p.Deleted || (name != null && name != p.Name) || (type != null && type != p.GetType())) continue;

								from.SendMessage("Found attachment {3} : {0} : {1} : {2}",p.GetType().Name,p.Name,p.OnIdentify(from), p.Serial.Value);

							}
							*/
                        from.SendGump(new XmlGetAttGump(from, ie, 0, 0));

                        break;
                    case "delete":
                        /*
							foreach(XmlAttachment p in plist)
							{
								if(p == null || p.Deleted || (name != null && name != p.Name) || (type != null && type != p.GetType())) continue;

								from.SendMessage("Deleting attachment {3} : {0} : {1} : {2}",p.GetType().Name,p.Name,p.OnIdentify(from), p.Serial.Value);
								p.Delete();
							}
							*/
                        from.SendGump(new XmlGetAttGump(from, ie, 0, 0));

                        break;
                    case "activate":
                        foreach (XmlAttachment p in plist)
                        {
                            if (p == null || p.Deleted || (name != null && name != p.Name) || (type != null && type != p.GetType()))
                            {
                                continue;
                            }

                            LogEntry le = p.OnIdentify(from);
                            from.SendMessage("Activating attachment {0} : {1} : {2}", p.Serial.Value, p.GetType().Name, p.Name);
                            if (le != null)
                            {
                                from.SendLocalizedMessage(le.Number, le.Args);
                            }

                            p.OnTrigger(null, from);
                        }

                        break;
                }
            }
        }

        [Usage("GetAtt [type [nome]] OPPURE [seriale [true/false DEFAULT false]]")]
        [Description("Ritorna la descrizione/gump degli attachment contenuti in un oggetto di cui si può specificare il tipo (ed un eventuale nome), valido ovviamente solo dopo aver targettato chi contiene un attachment, OPPURE un seriale (seguito, volendo, da una booleana, in cui true significa restituire il gump di tutti gli attachment contenuti all'interno dell'oggetto, ossia mostri o item, false per le props dell'attachment). Senza nessun argomento o utilizzando un nome come argomento mostrerà un target con il quale scegliere l'oggetto padre...usando il seriale non aprirà alcun target")]
        public static void GetAttachments_OnCommand(CommandEventArgs e)
        {
            int ser = -1;
            if (e.Arguments.Length > 0)
            {
                if ((e.Arguments[0].StartsWith("0x", StringComparison.Ordinal) && int.TryParse(e.Arguments[0].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ser)) || int.TryParse(e.Arguments[0], out ser)) //il primo è un numero
                {
                    bool gump = false;
                    if (e.Arguments.Length > 1)//try to get the bool
                    {
                        bool.TryParse(e.Arguments[1], out gump);
                    }

                    XmlAttachment a = FindAttachmentBySerial(ser);
                    if (a != null)
                    {
                        if (!gump)
                        {
                            e.Mobile.SendGump(new PropertiesGump(e.Mobile, a));
                        }
                        else if (a.AttachedTo != null)
                        {
                            e.Mobile.SendGump(new XmlGetAttGump(e.Mobile, a.AttachedTo, 0, 0));
                        }
                        else
                        {
                            e.Mobile.SendMessage("L'attachment non ha un oggetto padre ed è quindi da considerarsi invalido");
                        }
                    }
                    else
                    {
                        e.Mobile.SendMessage("Attachment con seriale {0} non esiste", e.Arguments[0]);
                    }
                    return;
                }
                else
                {
                    ser = -1;
                }
            }
            if (ser == -1)
            {
                e.Mobile.Target = new AttachTarget(e, "get");
            }
        }

        public class SetAttCommand : BaseCommand
        {
            private static Type s_AttachType = typeof(XmlAttachment);

            public SetAttCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.All;
                Commands = new string[] { "SetAtt" };
                ObjectTypes = ObjectTypes.All;
                Usage = "SetAtt <attachmenttype> <propertyName> <value>";
                Description = "Sets one or more property values by type of attachment and name of a targeted object.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (e.Length >= 3)
                {
                    for (int i = 0; (i + 2) < e.Length; i += 3)
                    {
                        Type t = ScriptCompiler.FindTypeByName(e.GetString(i), true);
                        if (t != null && (t == s_AttachType || t.IsSubclassOf(s_AttachType)))
                        {
                            if (!(obj is XmlAttachment))
                            {
                                obj = FindAttachment(obj as IEntity, t);
                            }

                            string result;
                            if (obj == null)
                            {
                                result = "Not found or Invalid attachment";
                            }
                            else
                            {
                                result = Properties.SetValue(e.Mobile, obj, e.GetString(i + 1), e.GetString(i + 2));
                            }

                            if (result == "Property has been set.")
                            {
                                AddResponse(result);
                            }
                            else
                            {
                                LogFailure(result);
                            }
                        }
                        else
                        {
                            LogFailure("Format: SetAtt <attachmenttype> <propertyName> <value>");
                        }
                    }
                }
                else
                {
                    LogFailure("Format: SetAtt <attachmenttype> <propertyName> <value>");
                }
            }
        }

        [Usage("AddAtt type [args]")]
        [Description("Adds an attachment to the targeted object.")]
        public static void AddAttachment_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new AttachTarget(e, "add");
        }

        [Usage("DelAtt [type/serialno [name]]")]
        [Description("Deletes attachments on the targeted object.")]
        public static void DeleteAttachments_OnCommand(CommandEventArgs e)
        {
            int ser = -1;
            if (e.Arguments.Length > 0)
            {
                // is this a numeric arg?
                char c = e.Arguments[0][0];
                if (c >= '0' && c <= '9')
                {
                    try
                    {
                        ser = int.Parse(e.Arguments[0]);
                    }
                    catch { }
                    XmlAttachment a = FindAttachmentBySerial(ser);
                    if (a != null)
                    {
                        e.Mobile.SendMessage("Deleting attachment {0} : {1}", ser, a);
                        a.Delete();
                    }
                    else
                    {
                        e.Mobile.SendMessage("Attachment {0} does not exist", ser);
                    }
                }
            }

            if (ser == -1)
            {
                e.Mobile.Target = new AttachTarget(e, "delete");
            }
        }

        [Usage("TrigAtt [type [name]]")]
        [Description("Triggers attachments on the targeted object.")]
        public static void ActivateAttachments_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new AttachTarget(e, "activate");
        }

        private static void Match(Type matchtype, Type[] types, List<Type> results)
        {
            if (matchtype == null)
            {
                return;
            }

            for (int i = 0; i < types.Length; ++i)
            {
                Type t = types[i];

                if (t.IsSubclassOf(matchtype))
                {
                    results.Add(t);
                }
            }
        }


        private static List<Type> Match(Type matchtype)
        {
            List<Type> results = new List<Type>();
            Type[] types = ScriptCompiler.TypeCache.Types;

            /*Assembly[] asms = ScriptCompiler.Assemblies;

            for (int i = 0; i < asms.Length; ++i)
            {
                types = ScriptCompiler.GetTypeCache(asms[i]).Types;
                Match(matchtype, types, results);
            }*/
            Match(matchtype, types, results);

            results.Sort(TypeNameComparer.Instance);

            return results;
        }

        private class TypeNameComparer : IComparer<Type>
        {
            public static TypeNameComparer Instance = new TypeNameComparer();
            public TypeNameComparer()
            {
            }

            public int Compare(Type a, Type b)
            {
                return a.Name.CompareTo(b.Name);
            }
        }


        [Usage("AvailAtt")]
        [Description("Lists all available attachments.")]
        public static void ListAvailableAttachments_OnCommand(CommandEventArgs e)
        {
            List<Type> attachtypes = Match(typeof(XmlAttachment));

            string parmliststr = null;

            foreach (Type attachtype in attachtypes)
            {
                // get all constructors derived from the XmlAttachment class
                ConstructorInfo[] ctors = attachtype.GetConstructors();

                for (int i = 0; i < ctors.Length; ++i)
                {
                    ConstructorInfo ctor = ctors[i];
#if (RESTRICTCONSTRUCTABLE)
                    if (!IsAttachable(ctor, e.Mobile.AccessLevel))
                    {
                        continue;
                    }
#else
					if (!IsAttachable(ctor))
					{
						continue;
					}
#endif

                    ParameterInfo[] paramList = ctor.GetParameters();

                    if (paramList != null)
                    {
                        string parms = attachtype.Name;


                        for (int j = 0; j < paramList.Length; j++)
                        {
                            parms += ", " + paramList[j].Name;
                        }

                        parmliststr += parms + "\n";
                    }
                }
            }
            e.Mobile.SendGump(new ListAttachmentsGump(parmliststr, 20, 20));
        }

        private class ListAttachmentsGump : Gump
        {
            public ListAttachmentsGump(string attachmentlist, int X, int Y)
                : base(X, Y)
            {
                AddPage(0);

                AddBackground(20, 0, 330, 480, 5054);

                AddPage(1);

                AddImageTiled(20, 0, 330, 480, 0x52);

                AddLabel(27, 2, 0x384, "Available Attachments");
                AddHtml(25, 22, 320, 458, attachmentlist, false, true);
            }
        }

        private class DisplayAttachmentGump : Gump
        {
            public DisplayAttachmentGump(Mobile from, List<LogEntry> text, int X, int Y)
                : base(X, Y)
            {
                // prepare the page
                AddPage(0);
                int max = text.Count;
                AddBackground(0, 0, 400, 125 * max, 5054);
                AddAlphaRegion(0, 0, 400, 125 * max);
                AddLabel(20, 2, 55, "Attachment Description(s)");
                for (int i = 0; i < max; ++i)
                {
                    AddHtmlLocalized(20, 125 * i + 20, 360, 100, text[i].Number, text[i].Args, 0x1, true, true);
                }
            }
        }

        public static void RevealAttachments(Mobile from, Item it)
        {
            if (from == null || it == null)
            {
                return;
            }
            List<XmlAttachment> plist = XmlAttach.FindAttachments(it);
            FinalRevealAttachments(from, plist);
        }

        public static void RevealAttachments(Mobile from, Mobile m)
        {
            if (from == null || m == null)
            {
                return;
            }
            List<XmlAttachment> plist = XmlAttach.FindAttachments(m);
            FinalRevealAttachments(from, plist);
        }

        private static void FinalRevealAttachments(Mobile from, List<XmlAttachment> plist)
        {
            if (plist == null)
            {
                return;
            }

            List<LogEntry> msg = new List<LogEntry>();

            foreach (XmlAttachment p in plist)
            {
                if (p != null && !p.Deleted)
                {
                    LogEntry le = p.OnIdentify(from);
                    if (le != null)
                    {
                        msg.Add(le);
                    }
                }
            }
            if (msg.Count > 0)
            {
                from.CloseGump(typeof(DisplayAttachmentGump));
                from.SendLocalizedMessage(505353);// "Individui delle proprietà nascoste!");
                from.SendGump(new DisplayAttachmentGump(from, msg, 0, 0));
            }
        }

        public static bool AttachTo(IEntity ie, XmlAttachment attachment, bool first = true)
        {
            return AttachTo(null, ie, attachment, first);
        }

        public static bool AttachTo(IEntity from, IEntity ie, XmlAttachment attachment, bool first = true)
        {
            if (attachment == null || ie == null)
            {
                return false;
            }

            Defrag(EntityAttachments, ie);
            //List<XmlAttachment> copy = new List<XmlAttachment>();
            List<XmlAttachment> attachmententry = FindAttachments(ie, original: true);

            // see if there is already an attachment list for the object
            if (attachmententry != null)
            {
                // if an existing entry list was found then just add the attachment to that list after making sure there is not a duplicate
                for(int x = attachmententry.Count - 1; x >= 0; --x)
                {
                    XmlAttachment i = attachmententry[x];
                    // and attachment is considered a duplicate if both the type and name match
                    if (i != null && !i.Deleted && i.GetType() == attachment.GetType() && i.Name == attachment.Name)
                    {
                        // duplicate found so replace it
                        attachment.OnBeforeReattach(i);
                        i.Delete();
                    }
                }

                attachmententry.Add(attachment);
            }
            else
            {
                // otherwise make a new entry list
                attachmententry = new List<XmlAttachment>
                {

                    // containing the attachment
                    attachment
                };

                // and add it to the correct dictionary table
                EntityAttachments[ie] = attachmententry;
            }

            attachment.AttachedTo = ie;
            attachment.OwnedBy = ie;

            if (from != null)
            {
                if (from is Mobile)
                {
                    attachment.SetAttachedBy(((Mobile)from).Name);
                }
                else if (from is Item)
                {
                    attachment.SetAttachedBy(((Item)from).Name);
                }
            }

            // if this is being attached for the first time, then call the OnAttach method
            // if it is being reattached due to deserialization then dont
            if (first)
            {
                attachment.OnAttach();
            }
            else
            {
                attachment.OnReattach();
            }

            return !attachment.Deleted;
        }

        public static List<T> SpecificAttachments<T>(IEntity o)
        {
            List<T> newlist = new List<T>();
            if (o == null || EntityAttachments == null || o.Deleted || typeof(T) == null)
            {
                return newlist;
            }

            if (!EntityAttachments.TryGetValue(o, out List<XmlAttachment> list) || list == null)
            {
                return newlist;
            }

            foreach (XmlAttachment i in list)
            {
                // see if it is deleted
                if (i == null || i.Deleted)
                {
                    continue;
                }

                if (i is T t)
                {
                    newlist.Add(t);
                }
            }

            return newlist;
        }

        public static List<XmlAttachment> FindAttachments(IEntity ie, Type type = null, string name = null, bool original = false)
        {
            return FindAttachments(EntityAttachments, ie, type, name, original);
        }

        public static List<XmlAttachment> FindAttachments(Dictionary<IEntity, List<XmlAttachment>> attachments, IEntity o, Type type, string name, bool original)
        {
            if (o == null || attachments == null || o.Deleted)
            {
                return null;
            }

            if (!attachments.TryGetValue(o, out List<XmlAttachment> list) || list == null)
            {
                return null;
            }

            if (type == null && name == null)
            {
                if (original)
                {
                    return list;
                }
                else
                {
                    return list.GetRange(0, list.Count);
                }
            }
            else
            {
                // just get those of a particular type and/or name
                List<XmlAttachment> newlist = new List<XmlAttachment>();

                foreach (XmlAttachment i in list)
                {
                    // see if it is deleted
                    if (i == null || i.Deleted)
                    {
                        continue;
                    }

                    Type itype = i.GetType();

                    if ((type == null || (itype != null && (itype == type || itype.IsSubclassOf(type)))) && (name == null || (name == i.Name)))
                    {
                        newlist.Add(i);
                    }
                }

                return newlist;
            }
        }

        public static XmlAttachment FindAttachment(IEntity ie, Type type = null, string name = null)
        {
            return FindAttachment(EntityAttachments, ie, type, name);
        }

        private static XmlAttachment FindAttachment(Dictionary<IEntity, List<XmlAttachment>> attachments, IEntity ie, Type type, string name)
        {
            if (ie == null || attachments == null || ie.Deleted || !attachments.TryGetValue(ie, out List<XmlAttachment> list) || list == null)
            {
                return null;
            }

            if (type == null && name == null)
            {
                // return the first valid attachment
                foreach (XmlAttachment i in list)
                {
                    if (i != null && !i.Deleted)
                    {
                        return i;
                    }
                }
            }
            else
            {
                // just get those of a particular type and/or name
                foreach (XmlAttachment i in list)
                {
                    // see if it is deleted
                    if (i == null || i.Deleted)
                    {
                        continue;
                    }

                    Type itype = i.GetType();

                    if ((type == null || (itype != null && (itype == type || itype.IsSubclassOf(type)))) && (name == null || (name == i.Name)))
                    {
                        return i;
                    }
                }
            }
            return null;
        }

        public static XmlAttachment FindAttachmentBySerial(int serialno)
        {
            if (serialno <= 0)
            {
                return null;
            }

            AllAttachments.TryGetValue(serialno, out XmlAttachment a);
            return a;
        }


        private static void FullDefrag()
        {
            // defrag the mobile/item tables
            FullDefrag(EntityAttachments);
            // defrag the serial table
            FullSerialDefrag(AllAttachments);
        }

        private static void FullDefrag(Dictionary<IEntity, List<XmlAttachment>> attachments)
        {
            IEntity[] keyarray = new IEntity[attachments.Count];

            attachments.Keys.CopyTo(keyarray, 0);
            for (int i = keyarray.Length - 1; i >= 0; --i)
            {
                Defrag(attachments, keyarray[i]);
            }
        }

        private static void FullSerialDefrag(Dictionary<int, XmlAttachment> attachments)
        {
            // go through the item attachments
            int[] keyarray = new int[attachments.Count];

            attachments.Keys.CopyTo(keyarray, 0);
            for (int i = 0; i < keyarray.Length; ++i)
            {
                XmlAttachment a = attachments[keyarray[i]];

                if (a == null || a.Deleted)
                {
                    attachments.Remove(keyarray[i]);
                }
            }
        }

        /// <summary>
        /// To be used if you are not sure of the object type passed to defrag, if you are sure you should specify the dictionary type and always pass the exact type
        /// </summary>
        /// <param name="ie">the generic IEntity passed, allowed are Mobile or Item</param>
        public static void Defrag(IEntity ie)
        {
            Defrag(EntityAttachments, ie);
        }

        private static void Defrag(Dictionary<IEntity, List<XmlAttachment>> attachments, IEntity ie)
        {
            if (ie == null || attachments == null)
            {
                return;
            }

            bool removeall = false;
            if (EntityAttachments.TryGetValue(ie, out List<XmlAttachment> list))
            {
                removeall = ie.Deleted;
            }

            if (list != null)
            {
                if (removeall)
                {
                    attachments.Remove(ie);
                }
                else
                {
                    for (int i = list.Count - 1; i >= 0; --i)
                    {
                        XmlAttachment x = list[i];
                        if (x == null || x.Deleted)
                        {
                            list.Remove(x);
                        }
                    }
                    if (list.Count == 0)
                    {
                        attachments.Remove(ie);
                    }
                }
            }
            else
            {
                attachments.Remove(ie);
            }
        }

        public static bool CheckCanEquip(Item item, Mobile from)
        {
            // call the CanEquip method on any attachments on the item
            // look for attachments on the item
            List<XmlAttachment> attachments = FindAttachments(item);

            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted)
                    {
                        if (!a.CanEquip(from))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static void CheckOnEquip(Item item, Mobile from)
        {
            // look for attachments on the item
            List<XmlAttachment> attachments = FindAttachments(item);

            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted)
                    {
                        a.OnEquip(from);
                    }
                }
            }
        }

        public static void CheckOnRemoved(Item item, IEntity parent)
        {
            // look for attachments on the item
            List<XmlAttachment> attachments = FindAttachments(item);

            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted)
                    {
                        a.OnRemoved(parent);
                    }
                }
            }
        }

        public static void OnSpellDamage(Item augmenter, Mobile caster, Mobile target, ref int spelldamage, int phys, int fire, int cold, int pois, int nrgy)
        {
            // look for attachments on the augmenter
            List<XmlAttachment> attachments = FindAttachments(augmenter);
            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted)
                    {
                        a.OnSpellDamage(augmenter, caster, target, ref spelldamage, phys, fire, cold, pois, nrgy);
                    }
                }
            }

            // also support OnSpellDamage for the mobile owner
            attachments = FindAttachments(caster);
            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted)
                    {
                        a.OnSpellDamage(augmenter, caster, target, ref spelldamage, phys, fire, cold, pois, nrgy);
                    }
                }
            }
        }

        public static void OnHitBySpell(Mobile caster, Mobile defender, ref int spelldamage, int phys, int fire, int cold, int pois, int nrgy)
        {
            // look for attachments on the augmenter
            List<XmlAttachment> attachments = FindAttachments(defender);

            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted)
                    {
                        a.OnHitBySpell(caster, defender, ref spelldamage, phys, fire, cold, pois, nrgy);
                    }
                }
            }
        }

        public static void OnWeaponHit(BaseWeapon weapon, Mobile attacker, Mobile defender, ref int damage, int originalDamage)
        {
            // look for attachments on the weapon
            List<XmlAttachment> attachments = FindAttachments(weapon);

            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted)
                    {
                        a.OnWeaponHit(attacker, defender, weapon, ref damage, originalDamage);
                    }
                }
            }

            // also support OnWeaponHit for the mobile owner
            attachments = FindAttachments(attacker);

            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted)
                    {
                        a.OnWeaponHit(attacker, defender, weapon, ref damage, originalDamage);
                    }
                }
            }
        }

        public static int OnArmorHit(Mobile attacker, Mobile defender, Item armor, BaseWeapon weapon, int damage)
        {
            int damageTaken = 0;

            // figure out who the attacker and defender are based upon who is carrying the armor/weapon

            // look for attachments on the armor
            if (armor != null)
            {
                List<XmlAttachment> attachments = FindAttachments(armor);

                if (attachments != null)
                {
                    foreach (XmlAttachment a in attachments)
                    {
                        if (a != null && !a.Deleted)
                        {
                            damageTaken += a.OnArmorHit(attacker, defender, armor, weapon, damage);
                        }
                    }
                }
            }

            return damageTaken;
        }

        public static void ShieldDamageMod(Mobile attacker, BaseShield shield, ref float bonus)
        {
            if (shield != null)
            {
                List<XmlAttachment> attachments = FindAttachments(shield);

                if (attachments != null)
                {
                    foreach (XmlAttachment a in attachments)
                    {
                        if (a != null && !a.Deleted)
                        {
                            a.ShieldDamageMod(attacker, shield, ref bonus);
                        }
                    }
                }
            }
        }

        public static void AddAttachmentProperties(IEntity parent, ObjectPropertyList list)
        {
            if (parent == null)
            {
                return;
            }

            //string propstr = null;
            List<XmlAttachment> plist = FindAttachments(parent);
            if (plist != null && plist.Count > 0)
            {
                //int more=0;
                for (int i = 0; i < plist.Count; ++i)
                {
                    XmlAttachment a = plist[i];

                    if (a != null && !a.Deleted)
                    {
                        // give the attachment an opportunity to modify the properties list of the parent
                        a.AddProperties(list);
                        // get any displayed properties on the attachment
                        if (list != null)
                        {
                            LogEntry str = a.DisplayedProperties(null);
                            if (str != null)
                            {
                                list.Add(str.Number, str.Args);
                                /*if(more>0)
									propstr +="\n";
								more++;
								propstr += str;*/
                                //the method below don't work well in some cases
                                //if (i < plist.Count - 1) propstr += "\n";
                            }
                        }
                    }
                }
            }

            /*if (propstr != null && list != null)
				list.Add(1062613, propstr);*/
        }

        public static void UseReq(NetState state, PacketReader pvSrc)
        {
            Mobile from = state.Mobile;

            if (from.AccessLevel >= AccessLevel.GameMaster || DateTime.UtcNow >= from.NextActionTime)
            {
                int value = pvSrc.ReadInt32();

                if ((value & ~0x7FFFFFFF) != 0)
                {
                    from.OnPaperdollRequest();
                }
                else
                {
                    Serial s = value;

                    bool blockdefaultonuse = false;

                    if (s.IsMobile)
                    {
                        Mobile m = World.FindMobile(s);

                        if (m != null && !m.Deleted)
                        {
                            // get attachments on the mobile doing the using
                            List<XmlAttachment> fromlist = FindAttachments(from);
                            if (fromlist != null)
                            {
                                foreach (XmlAttachment a in fromlist)
                                {
                                    if (a != null && !a.Deleted)
                                    {
                                        if (a.BlockDefaultOnUse(from, m))
                                        {
                                            blockdefaultonuse = true;
                                        }

                                        a.OnUser(m);
                                    }
                                }
                            }

                            // get attachments on the mob
                            List<XmlAttachment> alist = FindAttachments(m);
                            if (alist != null)
                            {
                                foreach (XmlAttachment a in alist)
                                {
                                    if (a != null && !a.Deleted)
                                    {
                                        if (a.BlockDefaultOnUse(from, m))
                                        {
                                            blockdefaultonuse = true;
                                        }

                                        a.OnUse(from);
                                    }
                                }
                            }

                            if (!blockdefaultonuse)
                            {
                                from.Use(m);
                            }
                        }
                    }
                    else if (s.IsItem)
                    {
                        Item item = World.FindItem(s);

                        if (item != null && !item.Deleted)
                        {
                            // get attachments on the mobile doing the using
                            List<XmlAttachment> fromlist = FindAttachments(from);
                            if (fromlist != null)
                            {
                                foreach (XmlAttachment a in fromlist)
                                {
                                    if (a != null && !a.Deleted)
                                    {
                                        if (a.BlockDefaultOnUse(from, item))
                                        {
                                            blockdefaultonuse = true;
                                        }

                                        a.OnUser(item);
                                    }
                                }
                            }

                            // get attachments on the mob
                            List<XmlAttachment> alist = FindAttachments(item);
                            if (alist != null)
                            {
                                foreach (XmlAttachment a in alist)
                                {
                                    if (a != null && !a.Deleted)
                                    {
                                        if (a.BlockDefaultOnUse(from, item))
                                        {
                                            blockdefaultonuse = true;
                                        }

                                        a.OnUse(from);
                                    }
                                }
                            }
                            /*else if(item is AddonComponent ba)//siccome la lista di prima è vuota, vediamo se ha un componente primario...
							{
								if(ba.Addon != null && !ba.Addon.Deleted)
								{
									alist = FindAttachments(ba.Addon);
									if (alist != null)
									{
										foreach (XmlAttachment a in alist)
										{
											if (a != null && !a.Deleted)
											{
												if (a.BlockDefaultOnUse(from, item))
													blockdefaultonuse = true;
												a.OnUse(from);
											}
										}
									}
								}
							}*/
                            // need to check the item again in case it was modified in the OnUse or OnUser method
                            if (from.Warmode)
                            {
                                if (!HandSiegeAttack.SelectTarget(from, from.Weapon, item) && !blockdefaultonuse)
                                    from.Use(item);
                            }
                            else if (!blockdefaultonuse)
                                from.Use(item);
                        }
                    }
                }
            }
            else
            {
                from.SendActionMessage();
            }
        }

        public static bool OnDragLift(Mobile from, Item item)
        {
            // look for attachments on the item
            if (item != null)
            {
                List<XmlAttachment> attachments = FindAttachments(item);

                if (attachments != null)
                {
                    foreach (XmlAttachment a in attachments)
                    {
                        if (a != null && !a.Deleted && !a.OnDragLift(from, item))
                        {
                            return false;
                        }
                    }
                }
            }

            // allow lifts by default
            return true;
        }

        public class ErrorReporter
        {
            private static void SendEmail(string filePath)
            {
                Console.Write("XmlSpawner2 Attachment error: Sending email...");

                Email.Send("Automated XmlSpawner2 Attachment Report. See attachment for details.", Email.MailType.XmlAttachError, new Attachment(filePath));
            }

            private static string GetRoot()
            {
                try
                {
                    return Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                }
                catch
                {
                    return "";
                }
            }

            private static string Combine(string path1, string path2)
            {
                if (path1 == "")
                {
                    return path2;
                }

                return Path.Combine(path1, path2);
            }


            private static void CreateDirectory(string path)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            private static void CreateDirectory(string path1, string path2)
            {
                CreateDirectory(Combine(path1, path2));
            }

            private static void CopyFile(string rootOrigin, string rootBackup, string path)
            {
                string originPath = Combine(rootOrigin, path);
                string backupPath = Combine(rootBackup, path);

                try
                {
                    if (File.Exists(originPath))
                    {
                        File.Copy(originPath, backupPath);
                    }
                }
                catch
                {
                }
            }

            public static void GenerateErrorReport(string error)
            {
                Console.Write("\nXmlSpawner2 Attachment Error:\n{0}\nGenerating report...", error);

                try
                {
                    string timeStamp = GetTimeStamp();
                    string fileName = string.Format("Attachment Error {0}.log", timeStamp);

                    string root = GetRoot();
                    string filePath = Combine(root, fileName);

                    using (StreamWriter op = new StreamWriter(filePath))
                    {
                        Version ver = Core.Assembly.GetName().Version;

                        op.WriteLine("XmlSpawner2 Attachment Error Report");
                        op.WriteLine("===================");
                        op.WriteLine();
                        op.WriteLine("RunUO Version {0}.{1}.{3}, Build {2}", ver.Major, ver.Minor, ver.Revision, ver.Build);
                        op.WriteLine("Operating System: {0}", Environment.OSVersion);
                        op.WriteLine(".NET Framework: {0}", Environment.Version);
                        op.WriteLine("XmlSpawner2: {0}", XmlSpawner.Version);
                        op.WriteLine("Time: {0}", Core.MistedDateTime);

                        op.WriteLine();

                        op.WriteLine("Error:");
                        op.WriteLine(error);

                        op.WriteLine();
                        op.WriteLine("Specific Attachment Errors:");
                        foreach (DeserErrorDetails s in XmlAttach.desererror)
                        {
                            op.WriteLine("{0} - {1}", s.Type, s.Details);
                        }
                    }

                    Console.WriteLine("done");

                    if (Email.CrashAddresses != null && !Core.LocalHost)
                    {
                        SendEmail(filePath);
                    }
                }
                catch
                {
                    Console.WriteLine("failed");
                }
            }

            private static string GetTimeStamp()
            {
                DateTime now = DateTime.UtcNow;

                return string.Format("{0}-{1}-{2}-{3}-{4}-{5}",
                    now.Day,
                    now.Month,
                    now.Year,
                    now.Hour,
                    now.Minute,
                    now.Second
                    );
            }
        }
    }
}
