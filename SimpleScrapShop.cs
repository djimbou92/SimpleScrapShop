using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SimpleScrapShop", "djimbou", "1.0.1")]
    [Description("A simple shop that uses scrap as currency and supports weapons, ammo, food, and building items.")]
    public class SimpleScrapShop : RustPlugin
    {
        // Scrap item ID in Rust
        private const int ScrapItemId = -932201673;

        // Shop data (prices are x10 for x10 scrap servers)
        private Dictionary<string, List<ShopItem>> shopCategories = new Dictionary<string, List<ShopItem>>
        {
            ["Weapons"] = new List<ShopItem>
            {
                new ShopItem("Assault Rifle", "rifle.ak", 5000),
                new ShopItem("Custom SMG", "smg.2", 3000),
                new ShopItem("Python Revolver", "pistol.python", 2000),
                new ShopItem("Bolt Action Rifle", "rifle.bolt", 4500)
            },
            ["Ammo"] = new List<ShopItem>
            {
                new ShopItem("5.56 Rifle Ammo", "ammo.rifle", 100, 20),
                new ShopItem("Pistol Bullet", "ammo.pistol", 80, 20),
                new ShopItem("12 Gauge Buckshot", "ammo.shotgun", 120, 10)
            },
            ["Food"] = new List<ShopItem>
            {
                new ShopItem("Can of Beans", "can.beans", 150),
                new ShopItem("Cooked Chicken", "chicken.cooked", 70),
                new ShopItem("Pumpkin", "pumpkin", 50)
            },
            ["Building"] = new List<ShopItem>
            {
                new ShopItem("Wood", "wood", 10, 100),
                new ShopItem("Stone", "stones", 20, 100),
                new ShopItem("Metal Fragments", "metal.fragments", 50, 50),
                new ShopItem("Sheet Metal Door", "door.hinged.metal", 1000)
            }
        };

        #region Commands

        [ChatCommand("shop")]
        private void CmdShop(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendReply(player, "<color=#ffd700>Shop Categories:</color> Weapons, Ammo, Food, Building");
                SendReply(player, "Example: /shop Weapons");
                return;
            }

            string category = args[0].ToLowerInvariant();
            if (!shopCategories.ContainsKey(FirstUpper(category)))
            {
                SendReply(player, $"No such category. Available: Weapons, Ammo, Food, Building");
                return;
            }

            ShowCategory(player, FirstUpper(category));
        }

        [ChatCommand("buy")]
        private void CmdBuy(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 2)
            {
                SendReply(player, "Usage: /buy <Category> <Number>");
                return;
            }

            string cat = FirstUpper(args[0]);
            int num;
            if (!shopCategories.ContainsKey(cat) || !int.TryParse(args[1], out num) || num < 1 || num > shopCategories[cat].Count)
            {
                SendReply(player, "Invalid category or item number.");
                return;
            }

            var item = shopCategories[cat][num - 1];
            TryBuy(player, item);
        }

        #endregion

        #region Core Logic

        private void ShowCategory(BasePlayer player, string cat)
        {
            var list = shopCategories[cat];
            SendReply(player, $"<color=#00ffff>{cat} Shop</color> (use /buy {cat} <number>):");
            for (int i = 0; i < list.Count; i++)
            {
                var si = list[i];
                SendReply(player, $"{i + 1}. {si.Name} x{si.Amount} - <color=#ffd700>{si.Price} scrap</color>");
            }
        }

        private void TryBuy(BasePlayer player, ShopItem item)
        {
            int totalScrap = GetItemCount(player, ScrapItemId);
            if (totalScrap < item.Price)
            {
                SendReply(player, $"Not enough scrap! You need {item.Price} scrap.");
                return;
            }
            TakeItem(player, ScrapItemId, item.Price);
            player.GiveItem(ItemManager.CreateByName(item.ShortName, item.Amount));
            SendReply(player, $"Purchased <color=#00ff00>{item.Name} x{item.Amount}</color> for <color=#ffd700>{item.Price} scrap</color>.");
        }

        private int GetItemCount(BasePlayer player, int itemId)
        {
            int count = 0;
            foreach (var item in player.inventory.AllItems())
                if (item.info.itemid == itemId)
                    count += item.amount;
            return count;
        }

        private void TakeItem(BasePlayer player, int itemId, int amount)
        {
            int remaining = amount;
            foreach (var item in player.inventory.AllItems())
            {
                if (item.info.itemid != itemId) continue;
                int toTake = Mathf.Min(item.amount, remaining);
                item.UseItem(toTake);
                remaining -= toTake;
                if (remaining <= 0) break;
            }
        }

        private string FirstUpper(string str)
        {
            if (str.Length == 0) return str;
            return char.ToUpper(str[0]) + str.Substring(1).ToLower();
        }

        #endregion

        #region Helper

        private class ShopItem
        {
            public string Name;
            public string ShortName;
            public int Price;
            public int Amount;

            public ShopItem(string name, string shortName, int price, int amount = 1)
            {
                Name = name;
                ShortName = shortName;
                Price = price;
                Amount = amount;
            }
        }

        #endregion
    }
}
