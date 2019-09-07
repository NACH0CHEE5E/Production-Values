using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pipliz;
using Pipliz.JSON;
using Recipes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Chatting;
using Shared;
using NetworkUI;
using NetworkUI.Items;

namespace FoodGen
{
    [ModLoader.ModManager]
    class Class1
    {
        public const string MOD_VERSION = "0.1.0";

        public const string NAME = "NACH0";
        public const string MODNAME = "ProductionStats";
        public const string MODNAMESPACE = NAME + "." + MODNAME + ".";

        public static string GAMEDATA_FOLDER = @"";
        public static string GAME_SAVES = @"";
        public static string GAME_SAVEFILE = @"";
        public static string GAME_ROOT = @"";
        public static string MOD_FOLDER = @"gamedata/mods/NACH0/Decor";

        public static string FILE_NAME = "ProductionStats.json";
        public static string FILE_PATH = @"";

        public static Dictionary<string, Dictionary<string, Dictionary<string, int>>> ProductionItems = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();
        //public static Dictionary<string, int> Settings = new Dictionary<string, int>();
        static bool WasDay = false;
        static int day = 0;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAssemblyLoaded, MODNAMESPACE + "OnAssemblyLoaded")]
        public static void OnAssemblyLoaded(string path)
        {
            MOD_FOLDER = Path.GetDirectoryName(path) + "/";

            GAME_ROOT = path.Substring(0, path.IndexOf("gamedata")).Replace("\\", "/") + "/";
            GAMEDATA_FOLDER = path.Substring(0, path.IndexOf("gamedata") + "gamedata".Length).Replace("\\", "/") + "/";
            GAME_SAVES = GAMEDATA_FOLDER + "savegames/";
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterSelectedWorld, MODNAMESPACE + "AfterSelectedWorld")]
        public static void AfterSelectedWorld()
        {
            GAME_SAVEFILE = GAME_SAVES + ServerManager.WorldName + "/";
            FILE_PATH = GAME_SAVEFILE + FILE_NAME;

        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, MODNAMESPACE + "AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            day = Pipliz.Math.RoundToInt(System.Math.Floor(TimeCycle.TotalHours / 24));
            if (TimeCycle.IsDay)
            {
                WasDay = true;
            }
            if (!File.Exists(FILE_PATH))
            {
                File.Create(FILE_PATH);
                //FoodValues.Add("default", "0", 0)

            }
            else
            {
                var FILE_CONTENTS = File.ReadAllText(FILE_PATH);
                if (FILE_CONTENTS != "")
                {
                    ProductionItems = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, int>>>>(FILE_CONTENTS);
                }
            }
        }
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, MODNAMESPACE + "OnUpdate")]
        public static void OnUpdate()
        {
            if (!WasDay && TimeCycle.IsDay)
            {
                WasDay = true;
                day = Pipliz.Math.RoundToInt(System.Math.Floor(TimeCycle.TotalHours / 24));
                return;
            }
            else if (WasDay && !TimeCycle.IsDay)
            {
                day = Pipliz.Math.RoundToInt(System.Math.Floor(TimeCycle.TotalHours / 24));
                foreach (Colony colony in ServerManager.ColonyTracker.ColoniesByID.Values)
                {
                    if (!ProductionItems.ContainsKey(colony.ColonyID.ToString()))
                    {
                        ProductionItems[colony.ColonyID.ToString()] = new Dictionary<string, Dictionary<string, int>>();
                    }
                    foreach (var type in ProductionItems[colony.ColonyID.ToString()])
                    {
                        ProductionItems[colony.ColonyID.ToString()][type.Key].Add(day.ToString(), colony.Stockpile.Items.GetValueOrDefault(ItemTypes.GetType(type.Key).ItemIndex, 0));
                    }
                    /*if (!FoodValues[colony.ColonyID.ToString()].ContainsKey(day.ToString()))
                    {
                        FoodValues[colony.ColonyID.ToString()].Add(day.ToString(), Pipliz.Math.RoundToInt(colony.Stockpile.TotalFood));
                    }
                    List<string> keysToRemove = new List<string>();
                    foreach (var dict in FoodValues[colony.ColonyID.ToString()])
                    {
                        if (Int32.Parse(dict.Key) < day - 10)
                        {
                            keysToRemove.Add(dict.Key);
                        }
                    }
                    foreach (var key in keysToRemove)
                    {
                        FoodValues[colony.ColonyID.ToString()].Remove(key);
                    }*/
                }
                WasDay = false;
            }
        }
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAutoSaveWorld, MODNAMESPACE + "OnAutoSaveWorld")]
        public static void OnAutoSaveWorld()
        {
            var fileContnets = JsonConvert.SerializeObject(ProductionItems, Formatting.Indented);
            File.WriteAllText(FILE_PATH, fileContnets);
            return;
        }
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnSaveWorldMisc, MODNAMESPACE + "OnSaveWorldMisc")]
        public static void OnSaveWorldMisc(JObject j)
        {
            var fileContnets = JsonConvert.SerializeObject(ProductionItems, Formatting.Indented);
            File.WriteAllText(FILE_PATH, fileContnets);
            return;
        }
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, MODNAMESPACE + "OnPlayerClick")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData data)
        {
            if (data.TypeSelected == ItemTypes.GetType("NACH0.Types." + MODNAME).ItemIndex)
            {
                if (data.ClickType == PlayerClickedData.EClickType.Left)
                {
                    SendUI(player);
                }
            }
        }
        public static void SendUI(Players.Player player)
        {
            day = Pipliz.Math.RoundToInt(System.Math.Floor(TimeCycle.TotalHours / 24));
            NetworkMenu ProductionUI = new NetworkMenu();
            ProductionUI.Identifier = "ProductionUI";
            ProductionUI.LocalStorage.SetAs("header", "Production Stats");
            ProductionUI.Width = 600;
            ProductionUI.Height = 200;

            Label itemLabel = new Label("Item: ");
            Label changeFromYesterdayLabel = new Label("Change From Yesterday: ");
            Label changeFrom5DaysAgoLabel = new Label("Average Change per day over 5 days: ");
            List<(IItem, int)> horizontalRowItems = new List<(IItem, int)>();

            horizontalRowItems.Add((itemLabel, 64));
            horizontalRowItems.Add((changeFromYesterdayLabel, 200));
            horizontalRowItems.Add((changeFrom5DaysAgoLabel, 275));
            HorizontalRow horizontalRow = new HorizontalRow(horizontalRowItems);
            ProductionUI.Items.Add(horizontalRow);

            foreach (var type in ProductionItems[player.ActiveColony.ColonyID.ToString()])
            {
                ItemIcon icon = new ItemIcon(type.Key);
                Label yesterday = new Label("No Data");
                Label fivedays = new Label("No Data");
                if (ProductionItems[player.ActiveColony.ColonyID.ToString()][type.Key].ContainsKey((day - 1).ToString()))
                {
                    yesterday = new Label((player.ActiveColony.Stockpile.Items.GetValueOrDefault(ItemTypes.GetType(type.Key).ItemIndex, 0) - ProductionItems[player.ActiveColony.ColonyID.ToString()][type.Key][(day - 1).ToString()]).ToString());
                }
                if (ProductionItems[player.ActiveColony.ColonyID.ToString()][type.Key].ContainsKey((day - 5).ToString()))
                {
                    fivedays = new Label((player.ActiveColony.Stockpile.Items.GetValueOrDefault(ItemTypes.GetType(type.Key).ItemIndex, 0) - ProductionItems[player.ActiveColony.ColonyID.ToString()][type.Key][(day - 1).ToString()] / 5).ToString());
                }

                horizontalRowItems = new List<(IItem, int)>();

                horizontalRowItems.Add((icon, 64));
                horizontalRowItems.Add((yesterday, 200));
                horizontalRowItems.Add((fivedays, 275));
                horizontalRow = new HorizontalRow(horizontalRowItems);
                ProductionUI.Items.Add(horizontalRow);
            }

            NetworkMenuManager.SendServerPopup(player, ProductionUI);
        }
        [ChatCommandAutoLoader]
        public class AddTypesCommand : IChatCommand
        {
            public bool TryDoCommand(Players.Player player, string chat, List<string> splits)
            {
                //ServerLog.LogAsyncMessage(new LogMessage("Command Imput: " + chat, LogType.Log));
                if (player == null)
                {
                    return false;
                }
                if (chat.StartsWith("/production"))
                {
                    var inventorySlot1 = player.Inventory.Items[0];
                    var typeName = ItemTypes.GetType(inventorySlot1.Type);
                    if (!ProductionItems.ContainsKey(player.ActiveColony.ColonyID.ToString()))
                    {
                        ProductionItems[player.ActiveColony.ColonyID.ToString()] = new Dictionary<string, Dictionary<string, int>>();
                    }
                    if (!ProductionItems[player.ActiveColony.ColonyID.ToString()].ContainsKey(typeName.Name))
                    {
                        ProductionItems[player.ActiveColony.ColonyID.ToString()][typeName.Name] = new Dictionary<string, int>();
                        return true;
                    }
                }
                return false;
            }

        }
    }
}
