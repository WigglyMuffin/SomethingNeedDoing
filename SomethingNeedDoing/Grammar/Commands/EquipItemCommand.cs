using ECommons.Automation;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;
using SomethingNeedDoing.Misc;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /equipitem command.
/// </summary>
internal class EquipItemCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/equipitem(?:\s+(?<itemid>\d+))?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly EchoModifier echoMod;
    private readonly uint itemID;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquipItemCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="itemID">Item ID.</param>
    /// <param name="wait">Wait value.</param>
    /// <param name="echo">Echo value.</param>
    private EquipItemCommand(string text, uint itemID, WaitModifier wait, EchoModifier echo) : base(text, wait)
    {
        this.itemID = itemID;
        echoMod = echo;
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static EquipItemCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);
        _ = EchoModifier.TryParse(ref text, out var echoModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var countGroup = match.Groups["itemid"];
        var itemID = countGroup.Success ? uint.Parse(countGroup.Value, CultureInfo.InvariantCulture) : uint.MinValue;

        return new EquipItemCommand(text, itemID, waitModifier, echoModifier);
    }

    /// <inheritdoc/>
    public override async Task Execute(ActiveMacro macro, CancellationToken token)
    {
        EquipItem(itemID);
        await Task.Delay(10, token);
        await PerformWait(token);
    }

    private static int EquipAttemptLoops = 0;
    private static unsafe void EquipItem(uint itemId)
    {
        var pos = FindItemInInventory(itemId, [
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4,
            InventoryType.ArmoryMainHand,
            InventoryType.ArmoryOffHand,
            InventoryType.ArmoryHead,
            InventoryType.ArmoryBody,
            InventoryType.ArmoryHands,
            InventoryType.ArmoryLegs,
            InventoryType.ArmoryFeets,
            InventoryType.ArmoryEar,
            InventoryType.ArmoryNeck,
            InventoryType.ArmoryWrist,
            InventoryType.ArmoryRings,
            InventoryType.ArmorySoulCrystal
            ]);
        if (pos == null)
        {
            DuoLog.Error($"Failed to find item {Svc.Data.GetExcelSheet<Item>()?.GetRow(itemId)?.Name} (ID: {itemId}) in inventory");
            return;
        }

        var agentId = pos.Value.inv
            is InventoryType.ArmoryMainHand
            or InventoryType.ArmoryOffHand
            or InventoryType.ArmoryHead
            or InventoryType.ArmoryBody
            or InventoryType.ArmoryHands
            or InventoryType.ArmoryLegs
            or InventoryType.ArmoryFeets
            or InventoryType.ArmoryEar
            or InventoryType.ArmoryNeck
            or InventoryType.ArmoryWrist
            or InventoryType.ArmoryRings
            or InventoryType.ArmorySoulCrystal ? AgentId.ArmouryBoard : AgentId.Inventory;
        var addonId = AgentModule.Instance()->GetAgentByInternalId(agentId)->GetAddonId();
        var ctx = AgentInventoryContext.Instance();
        ctx->OpenForItemSlot(pos.Value.inv, pos.Value.slot, addonId);

        var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu");
        if (contextMenu != null)
        {
            for (var i = 0; i < contextMenu->AtkValuesCount; i++)
            {
                var firstEntryIsEquip = ctx->EventIds[i] == 25; // i'th entry will fire eventid 7+i; eventid 25 is 'equip'
                if (firstEntryIsEquip)
                {
                    Svc.Log.Debug($"Equipping item #{itemId} from {pos.Value.inv} @ {pos.Value.slot}, index {i}");
                    Callback.Fire(contextMenu, true, 0, i - 7, 0, 0, 0); // p2=-1 is close, p2=0 is exec first command
                }
            }
            Callback.Fire(contextMenu, true, 0, -1, 0, 0, 0);
            EquipAttemptLoops++;

            if (EquipAttemptLoops >= 5)
            {
                DuoLog.Error($"Equip option not found after 5 attempts. Aborting.");
                return;
            }
        }
    }

    private static unsafe (InventoryType inv, int slot)? FindItemInInventory(uint itemId, IEnumerable<InventoryType> inventories)
    {
        foreach (var inv in inventories)
        {
            var cont = InventoryManager.Instance()->GetInventoryContainer(inv);
            for (var i = 0; i < cont->Size; ++i)
            {
                if (cont->GetInventorySlot(i)->ItemId == itemId)
                {
                    return (inv, i);
                }
            }
        }
        return null;
    }
}
