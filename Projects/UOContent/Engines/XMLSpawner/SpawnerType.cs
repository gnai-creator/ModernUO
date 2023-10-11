using System;

namespace Server.Mobiles
{
    public class SpawnerType
    {
        public static Type GetType(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return ScriptCompiler.FindTypeByName(name);
        }
    }
}