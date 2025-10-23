// =================================================
// System_BBB - By Alphadin, LegoPepper, Nobot | Edited by Zinlock
// =================================================
// File Name: server.cs
// Description: What? This has to start somewhere.
// =================================================
// Table of Contents:
//
// 1. Preferences
// 2. Scripts
// 3. Minigame
// =================================================

// =================================================
// 1. Preferences
// =================================================
// Enable
$BBB::Enable = true;

// Maps
$BBB::Start::Map		= "Add-Ons/BBB_Roy_The_Shipper/save.bls";
$BBB::Map::Rounds		= 6;

// Time
// $BBB::Time::Base		= 3 * 60 * 1000; // In MS
$BBB::Time::PreRound 	= "30000";
$BBB::Time::PostRound	= "12000";
$BBB::Time::Shock		= "1500"; // The time between the round ends and the winner is shown. Should be lower than Time::PostRound.

$BBB::Time::MapVote		= "30000"; // Map vote time
$BBB::Time::Bonus		= "10000"; // Bonus time for every time an innocent is killed.

// Other
$BBB::Announce::BodyFound = true; // Announce to the server when a body is found.
$BBB::UsingSMMBodies = false;
$BBB::RoundMusic = ""; // HL1_-_Military_Precision";

$Game::Item::PopTime = 2000;
// =================================================
// 2. Scripts
// =================================================
// Script Paths
$BBB::Path = "Add-Ons/System_BBB/";
$BBB::Path::Saving	= "config/server/BBB/";
// Script Execution
$pattern = $BBB::Path @ "scripts/*.cs";
for ( $file = findFirstFile( $pattern ) ; $file !$= "" ; $file = findNextFile( $pattern ) )
	exec( $file );

$pattern = $BBB::Path @ "weapons/*.cs";
for ( $file = findFirstFile( $pattern ) ; $file !$= "" ; $file = findNextFile( $pattern ) )
	exec( $file );
$pattern = "";

//exec prefs to load after files have been executed
schedule(10000,0,"exec","add-ons/system_bbb/prefs.cs");

function BBB_RebuildItemTable()
{
	deleteVariables("$BBB::Weapons_*");

	%pattern = $BBB::Path @ "weapons_*.txt";
	for ( %file = findFirstFile( %pattern ) ; %file !$= "" ; %file = findNextFile( %pattern ) )
	{
		%fileName = fileBase(%file);

		%table = getSubStr(%fileName, 8, strLen(%fileName) - 7);
		if(%table !$= "")
		{
			%fileObj = new FileObject();

			if(%fileObj.openForRead(%file))
			{
				while(!%fileObj.isEOF())
				{
					%str = %fileObj.readLine();

					if(%str $= "")
						continue;
					
					%item = $uiNameTable_items[getField(%str,0)];
					%price = getField(%str,1);
					%stock = getField(%str,2);

					if(isObject(%item))
					{
						$BBB::Weapons_[%table] = $BBB::Weapons_[%table] TAB %item.getName();
						$BBB::WeaponPrice[%table,%item.getName()] = %price;
						$BBB::WeaponStock[%table,%item.getName()] = %stock;
					}
					else
						warn("BBB_RebuildItemTable() - Couldn't find weapon \"" @ %str @ "\" for table \"" @ %table @ "\"");

					$BBB::Weapons_[%table] = trim($BBB::Weapons_[%table]);
				}
			}

			%fileObj.close();
			%fileobj.delete();
		}
	}
}

$pattern = "";
$file = "";

registerBBBPrefs();

// Events
registerInputEvent("fxDTSBrick", "onCorpseTouch", "Self fxDTSBrick" TAB "Corpse Player" TAB "MiniGame MiniGame");
registerInputEvent("fxDTSBrick", "onInnocentActivate", "Self fxDTSBrick" TAB "Player Player" TAB "Client GameConnection" TAB "MiniGame MiniGame");
registerInputEvent("fxDTSBrick", "onInnocentTouch", "Self fxDTSBrick" TAB "Player Player" TAB "Client GameConnection" TAB "MiniGame MiniGame");
registerInputEvent("fxDTSBrick", "onTraitorActivate", "Self fxDTSBrick" TAB "Player Player" TAB "Client GameConnection" TAB "MiniGame MiniGame");
registerInputEvent("fxDTSBrick", "onTraitorTouch", "Self fxDTSBrick" TAB "Player Player" TAB "Client GameConnection" TAB "MiniGame MiniGame");
registerOutputEvent("Player", "DeleteCorpse", "", false);

// =================================================
// 3. Minigame
// =================================================
function BBB() // -- The only non-packaged function not in functions.cs! Wow! (Its here for easy editing) [This is a lie, not the only one.]
{
	if(!isObject(BBB_Minigame) && $BBB::Enable)
	{
		new ScriptObject(BBB_Minigame)
		{
			class = "MiniGameSO";
			owner = -1;
			numMembers = 0;

			title = "Betrayal in Block Boulevard";
			colorIdx = "3";
			inviteOnly = false;
			UseAllPlayersBricks = true;
			PlayersUseOwnBricks = false;

			Points_BreakBrick = 0;
			Points_PlantBrick = 0;
			Points_KillPlayer = 0;
			Points_KillSelf = 0;
			Points_Die = 0;

			respawnTime = "-1";
			vehiclerespawntime = "10000";
			brickRespawnTime = "30000";
			playerDatablock = "BBB_Standard_Armor";

			useSpawnBricks = true;
			fallingdamage = true;
			weapondamage = true;
			SelfDamage = true;
			VehicleDamage = true;
			brickDamage = false;

			enableWand = false;
			EnableBuilding = false;
			enablePainting = false;

			StartEquip0 = 0;
			StartEquip1 = 0;
			StartEquip2 = 0;
			StartEquip3 = 0;
			StartEquip4 = 0;

			isBBB = true;
		};

		MinigameGroup.add(BBB_Minigame);
		$MiniGameColorTaken[BBB_Minigame.colorIdx] = 1;
		$DefaultMinigame = BBB_Minigame;
		commandToAll('AddMinigameLine', BBB_Minigame.getLine(), BBB_Minigame.getId(), BBB_Minigame.colorIdx);

		BBB_RebuildItemTable();
		BBB_BuildMapList();
		BBB_BuildShopList();
		TTTInventoryV2_Init();
		TTT_CreateGroups();
		BBB_Minigame.rolegroup = RoleGroup_Find("Default");
		BBB_Minigame.nameList = NameList_Find("Colors");

		BBB_LoadMap(SelectMaps(1));

		for(%a = 0; %a < ClientGroup.getCount(); %a++)
			BBB_Minigame.addMember(ClientGroup.getObject(%a));
	}
}

schedule(10, 0, "BBB");
