using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;

public class Mod
{
    public static Mod LoadModFromJson(string modpath)
    {
        Mod mod = new Mod
        {
            id = Path.GetFileName(modpath),
            name = Path.GetFileName(modpath),
            version = "",
            hideVersion = false,
            targetGameVersion = "v1.9.06",
            authors = "Unknown",
            description = "No Description.",
            path = modpath,
            consolePath = "",
            checksum = "",
            checksumChanged = false,
            checksumOverrideVersion = false,
            requirements = new string[0],
            requirementsNames = new string[0],
            tags = new string[0],
            modifiesRegions = false,
            workshopId = 0UL,
            workshopMod = false,
            hasDLL = false,
            loadOrder = 0,
            enabled = false,
            WorkshopData = new WorkshopDataClass()
        };
        if (File.Exists(modpath + Path.DirectorySeparatorChar.ToString() + "modinfo.json"))
        {
            Dictionary<string, object> dictionary = File.ReadAllText(modpath + Path.DirectorySeparatorChar.ToString() + "modinfo.json").dictionaryFromJson();
            if (dictionary == null)
            {
                Console.WriteLine("FAILED TO DESERIALIZE MOD FROM JSON! " + modpath);
                return null;
            }

            LoadFromDictionary(dictionary, ref mod);
        }
        if (Directory.Exists((modpath + Path.DirectorySeparatorChar.ToString() + "world").ToLowerInvariant()))
        {
            mod.modifiesRegions = true;
        }
        if (Directory.Exists(Path.Combine(modpath, "plugins")) || Directory.Exists(Path.Combine(modpath, "patchers")))
        {
            mod.hasDLL = true;
        }
        if (File.Exists(modpath + Path.DirectorySeparatorChar.ToString() + "workshopdata.json"))
        {
            try
            {
                mod.WorkshopData = JsonSerializer.Deserialize<WorkshopDataClass>(File.ReadAllText(modpath + Path.DirectorySeparatorChar.ToString() + "workshopdata.json"));
            }
            catch
            {
                mod.WorkshopData = null;
            }

            if (mod.WorkshopData == null)
            {
                mod.WorkshopData = new WorkshopDataClass();
                Console.WriteLine("FAILED TO DESERIALIZE WORKSHOPDATA FROM JSON! " + modpath);
            }
        }

        return mod;
    }

    public static void LoadFromDictionary(Dictionary<string, object> dictionary, ref Mod mod)
    {
        if (dictionary.ContainsKey("id"))
        {
            mod.id = dictionary["id"].ToString();
        }
        if (dictionary.ContainsKey("name"))
        {
            mod.name = dictionary["name"].ToString();
        }
        if (dictionary.ContainsKey("version"))
        {
            mod.version = dictionary["version"].ToString();
        }
        if (dictionary.ContainsKey("hide_version"))
        {
            mod.hideVersion = (bool)dictionary["hide_version"];
        }
        if (dictionary.ContainsKey("target_game_version"))
        {
            mod.targetGameVersion = dictionary["target_game_version"].ToString();
        }
        if (dictionary.ContainsKey("authors"))
        {
            mod.authors = dictionary["authors"].ToString();
        }
        if (dictionary.ContainsKey("description"))
        {
            mod.description = dictionary["description"].ToString();
        }
        if (dictionary.ContainsKey("youtube_trailer_id"))
        {
            mod.trailerID = dictionary["youtube_trailer_id"].ToString();
        }
        if (dictionary.ContainsKey("requirements"))
        {
            mod.requirements = ((List<object>)dictionary["requirements"]).ConvertAll<string>((object x) => x.ToString()).ToArray();
        }
        if (dictionary.ContainsKey("requirements_names"))
        {
            mod.requirementsNames = ((List<object>)dictionary["requirements_names"]).ConvertAll<string>((object x) => x.ToString()).ToArray();
        }
        if (dictionary.ContainsKey("tags"))
        {
            mod.tags = ((List<object>)dictionary["tags"]).ConvertAll<string>((object x) => x.ToString()).ToArray();
        }
        if (dictionary.ContainsKey("checksum_override_version"))
        {
            mod.checksumOverrideVersion = (bool)dictionary["checksum_override_version"];
        }
    }

    public string LocalizedName
    {
        get
        {
            if (!string.IsNullOrEmpty(this.name))
            {
                return this.name;
            }
            return this.id;
        }
    }

    public string LocalizedDescription
    {
        get
        {
            return this.description;
        }
    }

    public bool DLCMissing
    {
        get
        {
            return false;
        }
    }

    public string id = "Unknown";

    public string name = "Unknown";

    public string path = "Unknown";

    public string consolePath = "Unknown";

    public string version = "";

    public string targetGameVersion = "v1.9.06";

    public string authors = "Unknown";

    public string description = "No Description.";

    public string descBlank;

    public string trailerID;

    public string[] requirements = new string[0];

    public string[] requirementsNames = new string[0];

    public string[] tags = new string[0];

    public bool enabled;

    public string checksum = "";

    public bool checksumChanged;

    public bool checksumOverrideVersion;

    public bool hideVersion;

    public int loadOrder;

    public bool modifiesRegions;

    public bool workshopMod;

    public ulong workshopId;

    public bool hasDLL;

    public const string authorBlank = "Unknown";

    public WorkshopDataClass? WorkshopData;
}