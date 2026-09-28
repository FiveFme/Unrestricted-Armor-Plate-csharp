using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using System.Reflection;

namespace FiveF_UnrestrictedArmorPlates;

/// <summary>
/// This is the replacement for the former package.json data. This is required for all mods.
///
/// This is where we define all the metadata associated with this mod.
/// You don't have to do anything with it, other than fill it out.
/// All properties must be overriden, properties you don't use may be left null.
/// It is read by the mod loader when this mod is loaded.
/// </summary>
public record ModMetadata : IModMetadata
{
	/// <summary>
	/// Any string can be used for a modId, but it should ideally be unique and not easily duplicated
	/// a 'bad' ID would be: "mymod", "mod1", "questmod"
	/// It is recommended (but not mandatory) to use the reverse domain name notation,
	/// see: https://docs.oracle.com/javase/tutorial/java/package/namingpkgs.html
	/// </summary>
	public string ModGuid { get; init; } = "com.fivef.unrestrictedarmorplate";
	public string Name { get; init; } = "UnrestrictedArmorPlates";
	public string Author { get; init; } = "FiveF";
	public List<string>? Contributors { get; init; }
	public SemanticVersioning.Version Version { get; init; } = new("2.1.0");
	public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
	public List<string>? Incompatibilities { get; init; }
	public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
	public string? Url { get; init; }
	public string License { get; init; } = "MIT";
	public bool HasPrepatcher { get; init; } = false;
}

// We want to load after PostDBModLoader is complete, so we set our type priority to that, plus 1.
[Injectable(TypePriority = OnLoadOrder.PostLoad + 3)]
public class UnrestrictedArmorPlates(
	GlobalTable globalTable,
	TemplateTable templateTable,
	ModHelper modHelper,
	BotConfig botConfig) : IOnLoad // Implement the `IOnLoad` interface so that this mod can do something
{

	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		// This will get us the full path to the mod, e.g. C:\spt\user\mods\5ReadCustomJsonConfig-0.0.1
		var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

		// We give the path to the mod folder and the file we want to get, giving us the config, supply the files 'type' between the diamond brackets
		var modConfig = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config/config.json5");

		if (modConfig.EnableMod)
		{
			ArmorPlates();

			if (modConfig.EnableBuiltInInsert)
			{
				BuiltInInsert();

				//disable RandomisedArmorSlots - armor
				if (modConfig.DisableRandomisedArmorSlots)
				{
					foreach (var randomArmorSlot in botConfig.Equipment["pmc"].Randomisation)
					{
						if (randomArmorSlot.RandomisedArmorSlots != null)
						{
							randomArmorSlot.RandomisedArmorSlots.Remove("TacticalVest");
							randomArmorSlot.RandomisedArmorSlots.Remove("ArmorVest");
						}
					}
				}
			}

			if (modConfig.EnableBuiltInInsert_helmet)
			{
				BuiltInInsert_helmet();

				//disable RandomisedArmorSlots - helmet
				if (modConfig.DisableRandomisedArmorSlots)
				{
					foreach (var randomArmorSlot in botConfig.Equipment["pmc"].Randomisation)
					{
						if (randomArmorSlot.RandomisedArmorSlots != null)
						{
							randomArmorSlot.RandomisedArmorSlots.Remove("Headwear");
						}
					}
				}
			}

			if (modConfig.FilterPlatesByLevel_config)
			{
				botConfig.Equipment["pmc"].FilterPlatesByLevel = false;
			}
		}

		return Task.CompletedTask;
	}

	private void ArmorPlates()
	{
		var itemsTable = templateTable.Items;

		var armorPlate_frontback = new List<string>();
		var armorPlate_side = new List<string>();

		var armorPlateColliders_frontback = new List<string>();
		var armorPlateColliders_side = new List<string>();

		// creating lists
		foreach (var item in itemsTable)
		{
			var itemId = item.Value.Id;
			var itemParent = item.Value.Parent;
			var itemProps = item.Value.Properties;
			var itemName = item.Value.Name;

			// ArmorPlate
			if (itemParent == "644120aa86ffbe10ee032b6f")
			{
				if (itemName.StartsWith("item_equipment_plate_", StringComparison.OrdinalIgnoreCase)) // exclusive start _name for Armor Plates
				{
					// front / back
					if (itemName.EndsWith("_frontback", StringComparison.OrdinalIgnoreCase) || itemName.EndsWith("_front", StringComparison.OrdinalIgnoreCase) || itemName.EndsWith("_back", StringComparison.OrdinalIgnoreCase))
					{
						if (!armorPlate_frontback.Contains(itemId))
						{
							armorPlate_frontback.Add(itemId);
						}
						foreach (var collider in itemProps.ArmorPlateColliders ?? Enumerable.Empty<string>())
						{
							if (!armorPlateColliders_frontback.Contains(collider))
							{
								armorPlateColliders_frontback.Add(collider);
							}
						}
					}
					// side
					else if (itemName.EndsWith("_side", StringComparison.OrdinalIgnoreCase))
					{
						if (!armorPlate_side.Contains(itemId))
						{
							armorPlate_side.Add(itemId);
						}
						foreach (var collider in itemProps.ArmorPlateColliders ?? Enumerable.Empty<string>())
						{
							if (!armorPlateColliders_side.Contains(collider))
							{
								armorPlateColliders_side.Add(collider);
							}
						}
					}
				}
			}
		}

		//push lists to filters
		foreach (var item in itemsTable)
		{
			var itemId = item.Value.Id;
			var itemParent = item.Value.Parent;
			var itemProps = item.Value.Properties;
			var itemName = item.Value.Name;

			// Armor / Vest
			if (itemParent == "5448e54d4bdc2dcc718b4568" || itemParent == "5448e5284bdc2dcb718b4567")
			{
				foreach (var slot in itemProps.Slots)
				{
					foreach (var filter in slot.Properties.Filters)
					{
						// front / back
						if (slot.Name.Equals("front_plate", StringComparison.OrdinalIgnoreCase) || slot.Name.Equals("back_plate", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in armorPlate_frontback)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
						// side
						else if (slot.Name.Equals("left_side_plate", StringComparison.OrdinalIgnoreCase) || slot.Name.Equals("right_side_plate", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in armorPlate_side)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
					}
				}
			}

			// ArmorPlate
			if (itemParent == "644120aa86ffbe10ee032b6f")
			{
				if (itemName.StartsWith("item_equipment_plate_", StringComparison.OrdinalIgnoreCase))
				{
					// creating new list with elements from ArmorPlateColliders.
					var originalArmorPlateColliders = itemProps.ArmorPlateColliders?.ToList() ?? new List<string>();
					// front / back
					if (itemName.EndsWith("_frontback", StringComparison.OrdinalIgnoreCase) || itemName.EndsWith("_front", StringComparison.OrdinalIgnoreCase) || itemName.EndsWith("_back", StringComparison.OrdinalIgnoreCase))
					{
						////simple and fast version because of IEnumerable
						////this version seems to be sufficient because it comes out to the same thing
						//itemProps.ArmorPlateColliders = armorPlateColliders_frontback;

						foreach (var collider in armorPlateColliders_frontback)
						{
							if (!originalArmorPlateColliders.Contains(collider))
							{
								originalArmorPlateColliders.Add(collider);
							}
						}
					}
					// side
					if (itemName.EndsWith("_side", StringComparison.OrdinalIgnoreCase))
					{
						//itemProps.ArmorPlateColliders = armorPlateColliders_side;

						foreach (var collider in armorPlateColliders_side)
						{
							if (!originalArmorPlateColliders.Contains(collider))
							{
								originalArmorPlateColliders.Add(collider);
							}
						}
					}

					// assign ArmorPlateColliders back.
					itemProps.ArmorPlateColliders = originalArmorPlateColliders;
				}
			}
		}

		/*
		Plate_Granit_SAPI_chest
		Plate_Granit_SAPI_back
		Plate_Granit_SSAPI_side_right_low
		Plate_Granit_SSAPI_side_right_high
		Plate_Granit_SSAPI_side_left_high
		Plate_Granit_SSAPI_side_left_low
		Plate_Korund_chest
		Plate_6B13_back
		Plate_Korund_side_left_high
		Plate_Korund_side_right_high
		Plate_Korund_side_left_low
		Plate_Korund_side_right_low
		*/
	}

	private void BuiltInInsert()
	{
		var itemsTable = templateTable.Items;

		var builtInInsert_frontback = new List<string>();
		var builtInInsert_side = new List<string>();
		var builtInInsert_groin = new List<string>();
		var builtInInsert_collar = new List<string>();
		var builtInInsert_shoulder = new List<string>();

		var builtInInsertColliders_frontback = new List<string>();
		var builtInInsertColliders_side = new List<string>();
		var builtInInsertColliders_groin = new List<string>();
		var builtInInsertColliders_collar = new List<string>();
		var builtInInsertColliders_shoulder = new List<string>();

		// creating lists
		foreach (var item in itemsTable)
		{
			var itemId = item.Value.Id; var itemParent = item.Value.Parent; var itemProps = item.Value.Properties; var itemName = item.Value.Name;

			if (itemParent == "65649eb40bf0ed77b8044453")
			{
				// front / back
				if (itemName.EndsWith("soft_armor_front", StringComparison.OrdinalIgnoreCase) || itemName.EndsWith("soft_armor_back", StringComparison.OrdinalIgnoreCase))
				{
					if (!builtInInsert_frontback.Contains(itemId))
					{
						builtInInsert_frontback.Add(itemId);
					}
					foreach (var collider in itemProps.ArmorColliders ?? Enumerable.Empty<string>())
					{
						if (!builtInInsertColliders_frontback.Contains(collider))
						{
							builtInInsertColliders_frontback.Add(collider);
						}
					}
				}
				// side
				if (itemName.EndsWith("soft_armor_right_side", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_left_side", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_left", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_right", StringComparison.OrdinalIgnoreCase))
				{
					if (!builtInInsert_side.Contains(itemId))
					{
						builtInInsert_side.Add(itemId);
					}
					foreach (var collider in itemProps.ArmorColliders ?? Enumerable.Empty<string>())
					{
						if (!builtInInsertColliders_side.Contains(collider))
						{
							builtInInsertColliders_side.Add(collider);
						}
					}
				}
				// groin
				if (itemName.EndsWith("soft_armor_groin", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_groin_front", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_groin_back", StringComparison.OrdinalIgnoreCase))
				{
					if (!builtInInsert_groin.Contains(itemId))
					{
						builtInInsert_groin.Add(itemId);
					}
					foreach (var collider in itemProps.ArmorColliders ?? Enumerable.Empty<string>())
					{
						if (!builtInInsertColliders_groin.Contains(collider))
						{
							builtInInsertColliders_groin.Add(collider);
						}
					}
				}
				// collar
				if (itemName.EndsWith("soft_armor_collar", StringComparison.OrdinalIgnoreCase))
				{
					if (!builtInInsert_collar.Contains(itemId))
					{
						builtInInsert_collar.Add(itemId);
					}
					foreach (var collider in itemProps.ArmorColliders ?? Enumerable.Empty<string>())
					{
						if (!builtInInsertColliders_collar.Contains(collider))
						{
							builtInInsertColliders_collar.Add(collider);
						}
					}
				}
				// shoulder
				if (itemName.EndsWith("soft_armor_right_arm", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_left_arm", StringComparison.OrdinalIgnoreCase))
				{
					if (!builtInInsert_shoulder.Contains(itemId))
					{
						builtInInsert_shoulder.Add(itemId);
					}
					foreach (var collider in itemProps.ArmorColliders ?? Enumerable.Empty<string>())
					{
						if (!builtInInsertColliders_shoulder.Contains(collider))
						{
							builtInInsertColliders_shoulder.Add(collider);
						}
					}
				}
			}
		}

		// adding BuiltInInserts and armorColliders from created lists to existing Filters in armors/rigs.
		foreach (var item in itemsTable)
		{
			var itemId = item.Value.Id; var itemParent = item.Value.Parent; var itemProps = item.Value.Properties; var itemName = item.Value.Name;

			// adding BuiltInInserts to existing Filters in armors/rigs.
			if (itemParent == "5448e54d4bdc2dcc718b4568" || itemParent == "5448e5284bdc2dcb718b4567")
			{
				foreach (var slot in itemProps.Slots)
				{
					foreach (var filter in slot.Properties.Filters)
					{
						// front / back
						if (slot.Name.Equals("soft_armor_front", StringComparison.OrdinalIgnoreCase) || slot.Name.Equals("soft_armor_back", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in builtInInsert_frontback)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
						// side
						if (slot.Name.Equals("soft_armor_left", StringComparison.OrdinalIgnoreCase) || slot.Name.Equals("soft_armor_right", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in builtInInsert_side)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
						// groin
						if (slot.Name.Equals("groin", StringComparison.OrdinalIgnoreCase) || slot.Name.Equals("groin_back", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in builtInInsert_groin)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
						// collar
						if (slot.Name.Equals("collar", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in builtInInsert_collar)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
						// shoulder
						if (slot.Name.Equals("shoulder_r", StringComparison.OrdinalIgnoreCase) || slot.Name.Equals("shoulder_l", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in builtInInsert_shoulder)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
					}
				}
			}

			// adding armorColliders to existing Filters in BuiltInInserts.
			if (itemParent == "65649eb40bf0ed77b8044453")
			{

				// creating new list with elements from ArmorColliders.
				var originalArmorColliders = itemProps.ArmorColliders?.ToList() ?? new List<string>();

				// front / back
				if (itemName.EndsWith("soft_armor_front", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_back", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var collider in builtInInsertColliders_frontback)
					{
						if (!originalArmorColliders.Contains(collider))
						{
							originalArmorColliders.Add(collider);
						}
					}
				}
				// side
				if (itemName.EndsWith("soft_armor_right_side", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_left_side", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_left", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_right", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var collider in builtInInsertColliders_side)
					{
						if (!originalArmorColliders.Contains(collider))
						{
							originalArmorColliders.Add(collider);
						}
					}
				}
				// groin
				if (itemName.EndsWith("soft_armor_groin", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_groin_front", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_groin_back", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var collider in builtInInsertColliders_groin)
					{
						if (!originalArmorColliders.Contains(collider))
						{
							originalArmorColliders.Add(collider);
						}
					}
				}
				// collar
				if (itemName.EndsWith("soft_armor_collar", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var collider in builtInInsertColliders_collar)
					{
						if (!originalArmorColliders.Contains(collider))
						{
							originalArmorColliders.Add(collider);
						}
					}
				}
				// shoulder
				if (itemName.EndsWith("soft_armor_right_arm", StringComparison.OrdinalIgnoreCase) ||
					itemName.EndsWith("soft_armor_left_arm", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var collider in builtInInsertColliders_shoulder)
					{
						if (!originalArmorColliders.Contains(collider))
						{
							originalArmorColliders.Add(collider);
						}
					}
				}

				// assign ArmorPlateColliders back.
				itemProps.ArmorColliders = originalArmorColliders;
			}
		}

		// unlock Soft Armors for Armor and Chest rig
		foreach (var item in itemsTable)
		{
			var itemId = item.Value.Id; var itemParent = item.Value.Parent; var itemProps = item.Value.Properties; var itemName = item.Value.Name;

			if (itemParent == "5448e54d4bdc2dcc718b4568" || itemParent == "5448e5284bdc2dcb718b4567")
			{
				foreach (var slot in itemProps.Slots)
				{
					foreach (var filter in slot.Properties.Filters)
					{
						if (filter.Locked != null && filter.Locked != false)
						{
							filter.Locked = false;
						}
					}
				}
			}
		}
	}

	private void BuiltInInsert_helmet()
	{
		var itemsTable = templateTable.Items;

		var builtInInsert_top = new List<string>();
		var builtInInsert_back = new List<string>();
		var builtInInsert_ears = new List<string>();

		var builtInInsertColliders_top = new List<string>();
		var builtInInsertColliders_back = new List<string>();
		var builtInInsertColliders_ears = new List<string>();

		// creating lists
		foreach (var item in itemsTable)
		{
			var itemId = item.Value.Id; var itemParent = item.Value.Parent; var itemProps = item.Value.Properties; var itemName = item.Value.Name;

			if (itemParent == "65649eb40bf0ed77b8044453")
			{
				// top
				// ITEM _name
				if (itemName.EndsWith("helmet_armor_top", StringComparison.OrdinalIgnoreCase))
				{
					if (!builtInInsert_top.Contains(itemId))
					{
						builtInInsert_top.Add(itemId);
					}
					foreach (var collider in itemProps.ArmorColliders ?? Enumerable.Empty<string>())
					{
						if (!builtInInsertColliders_top.Contains(collider))
						{
							builtInInsertColliders_top.Add(collider);
						}
					}
				}
				// back
				// ITEM _name
				if (itemName.EndsWith("helmet_armor_nape", StringComparison.OrdinalIgnoreCase))
				{
					if (!builtInInsert_back.Contains(itemId))
					{
						builtInInsert_back.Add(itemId);
					}
					foreach (var collider in itemProps.ArmorColliders ?? Enumerable.Empty<string>())
					{
						if (!builtInInsertColliders_back.Contains(collider))
						{
							builtInInsertColliders_back.Add(collider);
						}
					}
				}
				// ears
				// ITEM _name
				if (itemName.EndsWith("helmet_armor_ears", StringComparison.OrdinalIgnoreCase))
				{
					if (!builtInInsert_ears.Contains(itemId))
					{
						builtInInsert_ears.Add(itemId);
					}
					foreach (var collider in itemProps.ArmorColliders ?? Enumerable.Empty<string>())
					{
						if (!builtInInsertColliders_ears.Contains(collider))
						{
							builtInInsertColliders_ears.Add(collider);
						}
					}
				}
			}
		}

		// adding BuiltInInserts and armorColliders from created lists to existing Filters in Headwear.
		foreach (var item in itemsTable)
		{
			var itemId = item.Value.Id; var itemParent = item.Value.Parent; var itemProps = item.Value.Properties; var itemName = item.Value.Name;

			// adding BuiltInInserts to existing Filters in Headwear.
			if (itemParent == "5a341c4086f77401f2541505")
			{
				foreach (var slot in itemProps.Slots)
				{
					foreach (var filter in slot.Properties.Filters)
					{
						// top
						// SLOT _name
						if (slot.Name.Equals("helmet_top", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in builtInInsert_top)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
						// back
						// SLOT _name
						if (slot.Name.Equals("helmet_back", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in builtInInsert_back)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
						// ears
						// SLOT _name
						if (slot.Name.Equals("helmet_ears", StringComparison.OrdinalIgnoreCase))
						{
							foreach (var armor in builtInInsert_ears)
							{
								if (!filter.Filter.Contains(armor))
								{
									filter.Filter.Add(armor);
								}
							}
						}
					}
				}
			}

			// adding armorColliders to existing Filters in BuiltInInserts.
			if (itemParent == "65649eb40bf0ed77b8044453")
			{
				// creating new list with elements from ArmorColliders.
				var originalArmorColliders = itemProps.ArmorColliders?.ToList() ?? new List<string>();

				// top
				// ITEM _name
				if (itemName.EndsWith("helmet_armor_top", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var collider in builtInInsertColliders_top)
					{
						if (!originalArmorColliders.Contains(collider))
						{
							originalArmorColliders.Add(collider);
						}
					}
				}
				// back
				// ITEM _name
				if (itemName.EndsWith("helmet_armor_ears", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var collider in builtInInsertColliders_ears)
					{
						if (!originalArmorColliders.Contains(collider))
						{
							originalArmorColliders.Add(collider);
						}
					}
				}
				// back
				// ITEM _name
				if (itemName.EndsWith("helmet_armor_top", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var collider in builtInInsertColliders_top)
					{
						if (!originalArmorColliders.Contains(collider))
						{
							originalArmorColliders.Add(collider);
						}
					}
				}

				// assign ArmorPlateColliders back.
				itemProps.ArmorColliders = originalArmorColliders;
			}
		}

		// unlock Soft Armors for Helmets
		foreach (var item in itemsTable)
		{
			var itemId = item.Value.Id; var itemParent = item.Value.Parent; var itemProps = item.Value.Properties; var itemName = item.Value.Name;

			if (itemParent == "5a341c4086f77401f2541505")
			{
				foreach (var slot in itemProps.Slots)
				{
					if (slot.Name.Equals("helmet_top", StringComparison.OrdinalIgnoreCase) ||
						slot.Name.Equals("helmet_back", StringComparison.OrdinalIgnoreCase) ||
						slot.Name.Equals("helmet_ears", StringComparison.OrdinalIgnoreCase))
					{
						foreach (var filter in slot.Properties.Filters)
						{
							if (filter.Locked != null && filter.Locked != false)
							{
								filter.Locked = false;
							}
						}
					}
				}
			}
		}


		//SLOTS
		//helmet_top
		//helmet_back
		//helmet_eyes - 5b4329f05acfc47a86086aa1, 5c08f87c0db8340019124324, 5c0d2727d174af02a012cf58, 66bdc28a0b603c26902b2011
		//helmet_jaw - 5b4329f05acfc47a86086aa1, 66bdc28a0b603c26902b2011
		//helmet_ears

		//ITEMS
		//helmet_armor_top
		//helmet_armor_nape
		//helmet_armor_eyes - 5b4329f05acfc47a86086aa1, 5c08f87c0db8340019124324, 5c0d2727d174af02a012cf58, 66bdc28a0b603c26902b2011
		//helmet_armor_jaw - 5b4329f05acfc47a86086aa1, 66bdc28a0b603c26902b2011
		//helmet_armor_ears
	}


	public record ModConfig
	{
		public required bool EnableMod { get; set; }
		public required bool EnableBuiltInInsert { get; set; }
		public required bool EnableBuiltInInsert_helmet { get; set; }
		public required bool DisableRandomisedArmorSlots { get; set; }
		public required bool FilterPlatesByLevel_config { get; set; }
	}
}
