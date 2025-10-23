// =================================================
// System_BBB - By Alphadin, LegoPepper, Nobot
// =================================================
// File Name: functions.cs
// Description: Functions for misc. things.
// =================================================
// Table of Contents:
//
// 1. Namespaceless
// 2. Armor
// 3. fxDTSBrick
// 4. GameConnection
// 5. Player
// 6. MinigameSO
// 7. ServerCMD
// =================================================

// =================================================
// 1. Namespaceless
// =================================================
// Code stolen directly from speedkart. Sorry Badspot :<
function BBB_BuildMapList()
{
	%pattern = "Add-Ons/BBB_*/save.bls";

	$BBB::numMaps = 0;

	%file = findFirstFile(%pattern);
	while(%file !$= "")
	{
	  $BBB::Map[$BBB::numMaps] = %file;
	  $BBB::numMaps++;

	  %file = findNextFile(%pattern);
	}
}

function BBB_BuildShopList()
{
	// Detective
	%counter = 0;
	while(%counter < getFieldCount($BBB::Weapons_Detective))
	{
		%item = getField($BBB::Weapons_Detective, %counter);
		$BBB::Shop_Detective_[%counter] = %item.getID();
		%item.price = $BBB::WeaponPrice["Detective",%item];
		%item.stock = $BBB::WeaponStock["Detective",%item];
		
		%counter++;
	}

	$BBB::Shop_Detective_[%counter] = -1;

	// Traitor
	%counter = 0;
	while(%counter < getFieldCount($BBB::Weapons_Traitor))
	{
		%item = getField($BBB::Weapons_Traitor, %counter);
		$BBB::Shop_Traitor_[%counter] = %item.getID();
		%item.price = $BBB::WeaponPrice["Traitor",%item];
		%item.stock = $BBB::WeaponStock["Traitor",%item];

		%counter++;
	}
	
	$BBB::Shop_Traitor_[%counter] = -1;
}

// Also stolen from speedkart... ;)
function BBB_LoadMap(%filename)
{
	//put everyone in observer mode
	%mg = BBB_Minigame;
	if(!isObject(BBB_Minigame))
	{
	  error("ERROR: BBB_LoadMap( " @ %filename  @ " ) - BBB minigame does not exist");
	  return;
	}
	for(%i = 0; %i < %mg.numMembers; %i++)
	{
	  %client = %mg.member[%i];
	  %player = %client.player;
	  if(isObject(%player))
		 %player.delete();

	  %camera = %client.camera;
	  %camera.setFlyMode();
	  %camera.mode = "Observer";

	  if(isObject(%client.getControlObject()) && %client.getControlObject().getDatablock().getName() $= "BillboardLoadingCamera")
	  {
		continue;
	  }

	  %client.setControlObject(%camera);
	}

	$BBB::CurrentMap = %filename;

	//clear all bricks
	// note: this function is deferred, so we'll have to set a callback to be triggered when it's done
	BrickGroup_888888.chaindeletecallback = "BBB_LoadMapP2(\"" @ %filename @ "\");";
	BrickGroup_888888.chaindeleteall();
}

// ;;;)
function BBB_LoadMapP2(%filename)
{
	echo("Loading BBB Map: " @ %filename);

	BBB_Minigame.setGlobalPrint("Loading...");
	$BBB::Round::Phase = "Loading";
	BBB_TimerLoop();

	%displayName = FilePath2MapName(%filename);

	%loadMsg = "\c4Now loading \c6" @ %displayName;

	//read and display credits file, if it exists
	// limited to one line
	%creditsFilename = filePath(%fileName) @ "/credits.txt";
	if(isFile(%creditsFilename))
	{
	  %file = new FileObject();
	  %file.openforRead(%creditsFilename);

	  %line = %file.readLine();
	  %line = stripMLControlChars(%line);
	  %loadMsg = %loadMsg @ "\c4, created by \c3" @ %line; 

	  %file.close();
	  %file.delete();
	}

	messageAll('', %loadMsg);

	//load environment if it exists
	%envFile = filePath(%fileName) @ "/environment.txt";
	if(isFile(%envFile))
	{
	  //echo("parsing env file " @ %envFile);
	  //usage: GameModeGuiServer::ParseGameModeFile(%filename, %append);
	  //if %append == 0, all minigame variables will be cleared
	  %res = GameModeGuiServer::ParseGameModeFile(%envFile, 1);

	  EnvGuiServer::getIdxFromFilenames();
	  EnvGuiServer::SetSimpleMode();

	  if(!$EnvGuiServer::SimpleMode)
	  {
		 EnvGuiServer::fillAdvancedVarsFromSimple();
		 EnvGuiServer::SetAdvancedMode();
	  }
	}
	
	$GameModeDisplayName = "TTT: " @ %displayName;

	//load save file
	schedule(10, 0, serverDirectSaveFileLoad, %fileName, 3, "", 2, 1);
}

$Pref::BBB::OverheadNames = false;

function registerBBBPrefs()
{
	RTB_registerPref("Overhead Names", "BBB", "$Pref::BBB::OverheadNames", "bool", "System_BBB", false, 0, 0);
	RTB_registerPref("Overhead Name Distance", "BBB", "$Pref::BBB::OHNDist", "int 5 200", "System_BBB", 30, 0, 0);
}

function checkForTPS(%client, %obj)
{
	if(!%obj.isFirstPerson() && %client.getControlObject() == %obj)
	{
		if(!%client.tpschecked)
		{
			%client.tpschecked = true;
			for(%i = 0; %i < clientGroup.getCount(); %i++)
			{
				%cc = clientGroup.getObject(%i);
				if(%cc.isAdmin)
				{
					messageClient(%cc, '', "<font:Palatino Linotype:18><color:494949>" @ %client.name SPC "switched to third person...");
				}
			}
		}
	}
	else if(%client.tpschecked)
	{
		%client.tpschecked = false;
		for(%i = 0; %i < clientGroup.getCount(); %i++)
		{
			%cc = clientGroup.getObject(%i);
			if(%cc.isAdmin)
			{
				messageClient(%cc, '', "<font:Palatino Linotype:18><color:494949>" @ %client.name SPC "switched back to first person.");
			}
		}
	}
}

function serverCmdFPS(%cl, %cl2)
{
	if(!%cl.isAdmin)
		return;
	
	if(!isObject(%tr = findClientByName(%cl2)) || !isObject(%tr.player))
		return;
	
	messageClient(%cl, '', "<font:Palatino Linotype:22><color:494949>" @ %tr.name SPC "is in" SPC (%tr.player.isFirstPerson() ? "first person" : "third person"));
}

function BBB_LookLoop()
{
	cancel($BBB::LookLoop::Schedule);
	if($BBB::Round::Phase !$= "Round")
		return;
	
	for(%i = 0; %i < BBB_Minigame.numMembers; %i++)
	{
		%client = BBB_Minigame.member[%i];
		%obj = %client.player;

		

		if(isObject(%obj))
		{
			%health = mCeil(100 - %obj.getDamageLevel());
			if(%health > 80)
				%obj.setShapeNameColor("0 1 0");
			else if(%health > 60)
				%obj.setShapeNameColor("0.7 1 0.1");
			else if(%health > 40)
				%obj.setShapeNameColor("1 1 0");
			else if(%health > 20)
				%obj.setShapeNameColor("1 0.5 0");
			else
				%obj.setShapeNameColor("1 0 0");
				
			checkForTPS(%client, %obj);

			%obj.setShapeNameDistance(12);

			%start = %obj.getEyePoint();
			%targets = ($TypeMasks::FxBrickObjectType | $TypeMasks::PlayerObjectType | $TypeMasks::StaticObjectType | $TypeMasks::TerrainObjectType | $TypeMasks::VehicleObjectType);
			%vec = %obj.getEyeVector();
			%end = vectorAdd(%start,vectorScale(%vec,100));
			%ray = containerRaycast(%start,%end,%targets,%obj);
			%col = firstWord(%ray);

			if(!isObject(%col) || (%col.getClassName() !$= "Player" && %col.getClassName() !$= "AIPlayer"))
				continue;
			
			if($tempdebug)
				%client.bottomPrint("col:" @ %col @ ", type: " @ %col.getType() @ ", typecheck: " @ %col.getType() & $TypeMasks::PlayerObjectType, 1, 1, 1);

			// WE FOUND SOMEONE
			if(%col.displayName !$= "")
			{
				%obj.targetLastCheck = $Sim::Time;
				%obj.targetName = %col.displayName;
			}

			Oopsies_DoCallout(%col,%obj,$ValidState::Criminal);

			if(isObject(%col) && %col.getDataBlock().maxDamage)
				%col.health = %col.getDataBlock().maxDamage - %col.getDamageLevel();

			%client.centerPrint(%col.client.inspectInfo[%client] @ %col.displayName @ "<br>\c2HP: \c6" @ mCeil(%col.health), 0.3);
		}
	}
	$BBB::LookLoop::Schedule = schedule(33, BBB_Minigame, "BBB_LookLoop");
}

// function BBB_PrintCorpseData(%client, %corpse)
// {
	// cancel(%client.corpseClickSched);
	// %time = getSimTime() - %corpse.deadTime;
	// %minutes = mFloor(%time / 60000);
	// %seconds = mFloor((%time - %minutes * 60000) / 1000);
	// if(%seconds < 10)
		// %seconds = "0" @ %seconds;
	// messageClient(%client, '', "\c6[\c4Corpse Data\c6]");
	// messageClient(%client, '', "\c6 - \c3Cause of death\c6: [" @ %corpse.SOD @ "]");
	// messageClient(%client, '', "\c6 - \c3Dead for\c6: " @ %minutes @ ":" @ %seconds);
// }
function FilePath2MapName(%file)
{
	%displayName = strReplace(%file, "Add-Ons/BBB_", "");
	%displayName = strReplace(%displayName, "/save.bls", "");
	%displayName = strReplace(%displayName, "_", " ");
	return %displayName;
}

function SelectMaps(%n,%ignore)
{
	%selected = "";
	%temp = "";
	%count = $BBB::numMaps;
	for(%i = 0; %i < %count; %i++)
	{
		%map = $BBB::Map[%i];
		if(%map $= %ignore && %count > 1)
		{
			continue;
		}
		%temp = %temp TAB %map;
	}
	%temp = ltrim(%temp);
	
	%n = getMin(%n, getFieldCount(%temp));

	for(%i = 0; %i < %n; %i++)
	{
		%choice = getRandom(0,getFieldCount(%temp) - 1);
		%selected = %selected TAB getField(%temp,%choice);
		%temp = removeField(%temp,%choice);
	}
	return ltrim(%selected);
}

function BBB_MapVote_P1()
{
	$BBB::Vote::Data = "";
	$BBB::Round = 0;

	BBB_Minigame.setGlobalPrint("VOTING...");

	messageAll('', "<font:Palatino linotype:35>\c6[\c4MAP VOTE LIST\c6]");
	///choose 3 random maps and have a vote between them
	$BBB::SelectedCount = 3;
	$BBB::SelectedMaps = SelectMaps($BBB::SelectedCount, $BBB::CurrentMap);
	$BBB::SelectedCount = getFieldCount($BBB::SelectedMaps);
	for(%i = 0; %i < $BBB::SelectedCount; %i++)
	{
		%filename = getField($BBB::SelectedMaps,%i);
		$BBB::Map::Votes[%i] = 0;

		%displayName = FilePath2MapName(%filename);

		messageAll('', "\c6" @ %i + 1 @ ". \c2" @ %displayName);
	}
	messageAll(''," <font:Palatino linotype:20>\c6To vote, type \c3/voteMap [Number].");
	$BBB::Round::Phase = "MapVote";

	$BBB::rTimeLeft = $BBB::Time::MapVote;
	BBB_TimerLoop();

	BBB_Minigame.playGlobalSound(BBB_Chat_Sound);
}

function BBB_MapVote_P2()
{
	%maxVoted = getField($BBB::SelectedMaps,0);
	%maxVotes = 0;
	for(%i = 0; %i < $BBB::SelectedCount; %i++)
	{
		if($BBB::Map::Votes[%i] > %maxVotes)
		{
			%maxVoted = "";
		}

		if($BBB::Map::Votes[%i] >= %maxVotes)
		{
			%maxVoted = lTrim(%maxVoted TAB getField($BBB::SelectedMaps,%i));
			%maxVotes = $BBB::Map::Votes[%i];
		}
	}
	%file = getField(%maxVoted,getRandom(0,getFieldCount(%maxVoted) - 1));

	%displayName = FilePath2MapName(%file);
	messageAll('', "<font:Palatino linotype:35>\c6 " @ %displayName @ " <font:Palatino linotype:30>\c4WON WITH \c6" @ %maxVotes @ " \c4" @ (%maxVotes > 1 ? "VOTES" : "VOTE") @ ".");
	BBB_Minigame.playGlobalSound(BBB_Chat_Sound);
	BBB_LoadMap(%file);
}

function BBB_SpawnItems()
{
	for(%i = 0; %i < BrickGroup_888888.getCount(); %i++)
	{
		%brick = BrickGroup_888888.getObject(%i);

		%name = %brick.getName();

		%field = $BBB::Weapons[%name];
		if(%field  !$= "")
		{
			%item = getField(%field, getRandom(0, getFieldCount(%field) - 1));
			if(isObject(%item))
			{
				%brick.setItem(%item);
			}
		}	
	}
}

function BBB_TimerLoop(%rCounter)
{
	cancel($BBB::TimerLoop::Schedule);
	if(%rCounter $= "")
		%rCounter = 1;
	if($BBB::Round::Phase $= "Round")
	{
		$BBB::bTimeLeft -= 1000;
		if($BBB::bTimeLeft < 0)
			$BBB::bTimeLeft = 0;

	// real Time Counter
	// If 3 seconds have passed, show traitors the real time for 5 seconds and revert.

		if(%rCounter > 4 && %rCounter < 10)
		{
			%showRTime = true;
			%rCounter++;
		}
		else if(%rCounter >= 10)
		{
			%showRTime = false;
			%rCounter = 0;
		}
		else
		{
			%showRTime = false;
			%rCounter++;
		}

		if($BBB::bTimeLeft > 0)
			%bTimeString = getStringFromTime($BBB::bTimeLeft);
		else
			%bTimeString = "HASTE MODE";
	}
	else
		%showRTime = false;

	$BBB::rTimeLeft -= 1000;
	if($BBB::rTimeLeft < 0)
		$BBB::rTimeLeft = 0;

	%rTimeString = getStringFromTime($BBB::rTimeLeft);

	for(%i = 0; %i < BBB_Minigame.numMembers; %i++)
	{
		%client = BBB_Minigame.member[%i];

		%client.setScore(%client.getPing());

		if(isObject(%client.player))
			%health = mCeil(100 - %client.player.getDamageLevel());
		else
			%health = 0;

		if($BBB::Round::Phase $= "Round" && isobject(%client.player) && %client.credits > 0)
			%tip = "<just:right><font:Palatino Linotype:34>\c3" @ %client.credits @ "c";
		else
			%tip = " ";

		if($BBB::Round::Phase $= "Round" && isObject(%client.player))
		{
			if(%showRTime && (%client.role.data.hasteMode || !isObject(%client.player)))
				%timeString = "\c0" @ %rTimeString;
			else
				%timeString = %bTimeString;
		}
		else
			%timeString = %rTimeString;

		%client.bottomPrint = "<br><font:Palatino Linotype:34><just:left>\c6HP: \c4" @ %health @ " " @ "<just:right>\c3" @ %timeString;
		%client.topPrint = %client.print @ %tip;
		%text = %client.topPrint @ %client.bottomPrint;
		bottomPrint(%client, %text, 2, false);
	}
	if($BBB::rTimeLeft <= 0)
	{
		%type = $BBB::Round::Phase;
		switch$(%type)
		{
			case "PreRound":
				BBB_Minigame.roundStart();
				return;
			case "Round":
				BBB_Minigame.doWinCheck();
				return;
			case "PostRound":
				if($BBB::Round >= $BBB::Map::Rounds)
					BBB_MapVote_P1();
				else
					BBB_Minigame.roundSetup();
				return;
			case "MapVote":
				BBB_MapVote_P2();
				return;
		}
	}
	$BBB::TimerLoop::Schedule = scheduleNoQuota(1000, BBB_Minigame, "BBB_TimerLoop", %rCounter);
}
function BBB_TimerLoop_ForceUpdate(%client, %line, %word, %replace)
{
	if(!%client.inBBB)
		return;

	if(%line $= "topPrint")
		%client.topPrint = setWord(%client.topPrint, %word, %replace);
	else
		%client.bottomPrint = setWord(%client.bottomPrint, %word, %replace);

	%text = %client.topPrint @ %client.bottomPrint;

	bottomPrint(%client, %text, 2);
}

function BBB_UpdateSlayList(%slayList, %type, %targetBLID, %amount)
{
	// Check if %amount is ACTUALLY A NUMBER.
	if(%amount !$= "" && !(%amount > 0 || %amount < 0))
		return;

	%count = getFieldCount(%slayList);

	// Do different tasks depending on the type.
	if(%type $= "decrementAll")
	{
		for(%i = 0; %i < %count; %i++)
		{
			%field = getField(%slayList, %i);
			%BLID = getWord(%field, 0);
			%rounds = getWord(%field, 1);

			%rounds--;

			// Slay the person.
			if(isObject(%obj = findClientByBLID(%targetBLID).player))
			{
				%obj.kill();
				%obj.deleteCorpse();
			}

			// Remove the field if its <= 0.
			if(%rounds <= 0)
				%slayList = removeField(%slayList, %i);
		}
	}
	else if(%type $= "addToTarget")
	{
		%found = false;
		for(%i = 0; %i < %count; %i++)
		{
			%field = getField(%slayList, %i);
			%BLID = getWord(%field, 0);
			if(%BLID == %targetBLID)
			{
				%found = true;
				break;
			}
		}
		// Is he already in the list?
		if(%found)
		{
			%rounds = getWord(%field, 1);
			%rounds += %amount;
			if(%rounds <= 0)
				%slayList = removeField(%slayList, %i);
			else
			{
				%newField = %BLID SPC %rounds;
				%slayList = setField(%slayList, %i, %newField);
			}
		}
		else // If not, let's add him.
		{
			if(%amount > 0)
			{
				%newField = %targetBLID SPC %amount;
				%slayList = setField(%slayList, %count, %newField);
			}
		}
	}
	else if(%type $= "targetInList")
	{
		%found = false;
		for(%i = 0; %i < %count; %i++)
		{
			%field = getField(%slayList, %i);
			%BLID = getWord(%field, 0);
			if(%BLID == %targetBLID)
			{
				%found = true;
				break;
			}
		}
		// Return bool
		return %found;
	}
	// Return the modified slaylist.
	return %slayList;
}

// in MS
function getStringFromTime(%time)
{
	%minutes = mFloor(%time / 60000);
	%seconds = mFloor((%time - %minutes * 60000) / 1000);
	if(%seconds < 10)
		%seconds = "0" @ %seconds;

	return %minutes @ ":" @ %seconds;
}

// =================================================
// 2. Armor
// =================================================

// =================================================
// 3. fxDTSBrick
// =================================================
// Base event code from ZAPT
function fxDTSBrick::onCorpseTouch(%this, %obj)
{
	$InputTarget_["Self"] = %this;

	if(isObject(%obj))
	{
		$InputTarget_["Corpse"] = %obj;

		%mini1 = getMiniGameFromObject(%this);
		%mini2 = getMiniGameFromObject(%client);
		if(isObject(%mini1) && isObject(%mini2) && %mini1.getID() == %mini2.getID())
		{
			$InputTarget_["MiniGame"] = %mini1;
		}
		else
		{
			$InputTarget_["MiniGame"] = -1;
		}
	}
	else
	{
		$InputTarget_["Corpse"] = -1;
		$InputTarget_["MiniGame"] = -1;
	}

	%this.processInputEvent("onCorpseTouch", %obj);
}

function fxDTSBrick::onInnocentActivate(%this, %client)
{
	$InputTarget_["Self"] = %this;

	if(isObject(%client))
	{
		if(isObject(%client.player))
		{
			$InputTarget_["Player"] = %client.player;
		}

		$InputTarget_["Client"] = %client;

		%mini1 = getMiniGameFromObject(%this);
		%mini2 = getMiniGameFromObject(%client);
		if(isObject(%mini1) && isObject(%mini2) && %mini1.getID() == %mini2.getID())
		{
			$InputTarget_["MiniGame"] = %mini1;
		}
		else
		{
			$InputTarget_["MiniGame"] = -1;
		}
	}
	else
	{
		$InputTarget_["Player"] = -1;
		$InputTarget_["Client"] = -1;
		$InputTarget_["MiniGame"] = -1;
	}

	%this.processInputEvent("onInnocentActivate", %client);
}

function fxDTSBrick::onInnocentTouch(%this, %obj)
{
	$InputTarget_["Self"] = %this;
	%client = %obj.client;

	if(isObject(%obj))
	{
		$InputTarget_["Client"] = %client;
		$InputTarget_["Player"] = %obj;

		%mini1 = getMiniGameFromObject(%this);
		%mini2 = getMiniGameFromObject(%client);
		if(isObject(%mini1) && isObject(%mini2) && %mini1.getID() == %mini2.getID())
		{
			$InputTarget_["MiniGame"] = %mini1;
		}
		else
		{
			$InputTarget_["MiniGame"] = -1;
		}
	}
	else
	{
		$InputTarget_["Player"] = -1;
		$InputTarget_["Client"] = -1;
		$InputTarget_["MiniGame"] = -1;
	}

	%this.processInputEvent("onInnocentTouch", %obj);
}

function fxDTSBrick::onTraitorActivate(%this, %client)
{
	$InputTarget_["Self"] = %this;

	if(isObject(%client))
	{
		if(isObject(%client.player))
		{
			$InputTarget_["Player"] = %client.player;
		}

		$InputTarget_["Client"] = %client;

		%mini1 = getMiniGameFromObject(%this);
		%mini2 = getMiniGameFromObject(%client);
		if(isObject(%mini1) && isObject(%mini2) && %mini1.getID() == %mini2.getID())
		{
			$InputTarget_["MiniGame"] = %mini1;
		}
		else
		{
			$InputTarget_["MiniGame"] = -1;
		}
	}
	else
	{
		$InputTarget_["Player"] = -1;
		$InputTarget_["Client"] = -1;
		$InputTarget_["MiniGame"] = -1;
	}

	%this.processInputEvent("onTraitorActivate", %client);
}

function fxDTSBrick::onTraitorTouch(%this, %obj)
{
	$InputTarget_["Self"] = %this;
	%client = %obj.client;

	if(isObject(%obj))
	{
		$InputTarget_["Client"] = %client;
		$InputTarget_["Player"] = %obj;

		%mini1 = getMiniGameFromObject(%this);
		%mini2 = getMiniGameFromObject(%client);
		if(isObject(%mini1) && isObject(%mini2) && %mini1.getID() == %mini2.getID())
		{
			$InputTarget_["MiniGame"] = %mini1;
		}
		else
		{
			$InputTarget_["MiniGame"] = -1;
		}
	}
	else
	{
		$InputTarget_["Player"] = -1;
		$InputTarget_["Client"] = -1;
		$InputTarget_["MiniGame"] = -1;
	}

	%this.processInputEvent("onTraitorTouch", %obj);
}
// =================================================
// 4. GameConnection
// =================================================
function GameConnection::BBB_DisplayShop(%this, %type)
{
	%counter = 0;
	if(!isObject($BBB::Shop_[%type, %counter]))
		return;
	messageClient(%this, '', "<font:Palatino Linotype:30>\c3" @ strUpr(%type) @ " SHOP\c6:");

	while(isObject($BBB::Shop_[%type, %counter]))
	{
		%item = $BBB::Shop_[%type, %counter];
		messageClient(%this, '', "\c6" @ %counter @ ". \c4" @ %item.uiName);
		%counter++;

		if(%counter > 256)
		{
			talk("!!!");
			talk("displayshop just went over 256!");
			break;
		}
	}

	messageClient(%this,'', "<font:Palatino linotype:20>\c6You have \c3" @ %this.credits @ " \c6credits.");
	messageClient(%this,'', "<font:Palatino linotype:20>\c6To buy an item, type \c3/buy [Number] \c6or \c3/buy [Item Name].");
	%this.play2D(BBB_Chat_Sound);
}

// function GameConnection::BBB_Give_Role(%client, %role)
// {
// 	%player = %client.player;
// 	if(!isObject(%player.roleBBM))
// 	{
// 		%player.roleBBM = OverheadBillboardMount.Make();
// 	}
// 	%player.mountObject(%player.roleBBM,8);
// 	switch$(%role)
// 	{
// 	    case "Detective":
// 			Billboard_ClearGhost(BillboardMount_AddBillboard(%player.roleBBM,detectiveBillboard),%client);
// 			%client.print = "<just:left><font:Palatino Linotype:22>\c3ROLE\c6: <font:Palatino Linotype:45>\c1D<font:Palatino Linotype:43>\c1ETECTIVE";
// 			%client.credits = $BBB::Detective::StartingCredits;
// 	    case "Traitor":
// 			%client.print = "<just:left><font:Palatino Linotype:22>\c3ROLE\c6: <font:Palatino Linotype:45>\c0T<font:Palatino Linotype:43>\c0RAITOR";
// 			%client.credits = $BBB::Traitor::StartingCredits;
// 		case "Innocent":
// 			%client.print = "<just:left><font:Palatino Linotype:22>\c3ROLE\c6: <font:Palatino Linotype:45>\c2I<font:Palatino Linotype:43>\c2NNOCENT";
// 			%client.credits = 0;
// 	    default:
// 			return;
// 	}
// }
// =================================================
// 5. Player
// =================================================
// Outfit code from Iban's CityRPG mod
// function Player::BBB_ApplyOutfit(%obj, %num)
// {
	// %obj.hideNode("ALL");

	// %outfit = "BBB_Outfit" @ %num;

	// for(%i = 0; %i < %outfit.numNodes; %i++)
	// {
		// %node = %outfit.node[%i];
		// %color = %outfit.color[%i];

		// %obj.unHideNode(%node);
		// %obj.setNodeColor(%node, %color);
	// }
	// %obj.setDecalName(%outfit.decal);
	// %obj.setFaceName(%outfit.face);
// }

// Outfit code from Iban's CityRPG mod
function Player::BBB_ApplyOutfit(%obj)
{
	%obj.hideNode("ALL");

	for(%i = 0; %i <= 12; %i++)
	{

		%node = $BBB::Outfit::Node_[%i];
		if(%node $= "")
			continue;

		%color = $BBB::Outfit::Color_[%i];
		if(getWord(%color, 3 < 1))
			%color = setWord(%color, 3, 1);
		%obj.unHideNode(%node);
		%obj.setNodeColor(%node, %color);
	}

	%obj.client.tpschecked = false;

	%obj.setDecalName($BBB::Outfit::Decal);
	%obj.setFaceName($BBB::Outfit::Face);
}

function ItemData::getSlot(%db)
{
	%dbName = %db.getName();
	// Get slot
	for(%i = 0; %i < 4; %i++)
	{
		switch(%i)
		{
			case 0:
				%name = "Primary";
			case 1:
				%name = "Secondary";
			case 2:
				%name = "Other";
			case 3:
				%name = "Grenade";
		}

		%fields = $BBB::Weapons_[%name];
		%fieldCount = getFieldCount(%fields);
		for(%j = 0; %j < %fieldCount; %j++)
		{
			%field = getField(%fields, %j);
			if(%field $= %dbName)
			{
				return %i;
			}
		}
	}
	return "";
}

function ItemData::onPickup (%this, %obj, %user, %amount)
{
	if(!%user.client.inBBB)
	{
		parent::onPickup (%this, %obj, %user, %amount);
	}

	if (%obj.canPickup == 0)
	{
		return 0;
	}

	// Get slot
	%slot = %this.getSlot();
	if(%slot $= "")
	{
		//look for a not used slot
		for(%i = 4; %i < 7; %i++)
		{
			if(!isObject(%user.tool[%i]))
			{
				%slot = %i;
				break;
			}
		}
	}

	if(!%obj.ammoDrop && %user.tool[%slot] == 0 && %slot != 7)
	{
		
		%obj.delete ();

		%user.tool[%slot] = %this;
		if (%user.client)
		{
			messageClient (%user.client, 'MsgItemPickup', '', %slot, %this.getId());
		}
		return 1;
	}
	return 0;
}

function Player::BBB_TargetAPlayer(%obj)
{
	// Look for players
	%start = %obj.getEyePoint();
	%targets = ($TypeMasks::FxBrickObjectType | $TypeMasks::PlayerObjectType | $TypeMasks::StaticObjectType | $TypeMasks::TerrainObjectType | $TypeMasks::VehicleObjectType);
	%vec = %obj.getEyeVector();
	%end = vectorAdd(%start,vectorScale(%vec,100));
	%ray = containerRaycast(%start,%end,%targets,%obj);
	%col = firstWord(%ray);

	// WE FOUND SOMEONE
	if(%col.displayName !$= "")
	{
		%obj.targetLastCheck = $Sim::Time;
		%obj.targetName = %col.displayName;
		%obj.targetObj = %col;
		return %obj.targetName;
	}

	// Look for corpses
	%corpse = %obj.findCorpseRayCast(true);
	if(isObject(%corpse))
	{
		if(%corpse.getClassname() !$= "AiPlayer")
		{
			%obj.targetLastCheck = $Sim::Time;
			%obj.targetName = %corpse.displayName;
			%obj.targetObj = %col;
			return %obj.targetName;
		}
	}

	%cts = 0;

	// Finally, FOV Check
	%angle = 90;
	initContainerRadiusSearch(%obj.getPosition(), 64, $TypeMasks::PlayerObjectType); // could use  $TypeMasks::CorpseObjectType, but what if the corpse is purposely hidden?
	%result = "";
	while(isObject(%col = containerSearchNext()))
	{
		%cts++;
		%product = vectorDot(%vec, vectorNormalize(vectorSub(%col.getEyePoint(), %start)));
		%result =  %product >= 1 - (%angle / 360) * 2;
		if(%result && %col.getClassname() !$= "AiPlayer")
		{
			%end = %col.getEyePoint();
			%targets = ($TypeMasks::FxBrickObjectType | $TypeMasks::StaticObjectType | $TypeMasks::TerrainObjectType | $TypeMasks::VehicleObjectType);
			%ray = containerRaycast(%start,%end,%targets,%obj);

			%hit = firstWord(%ray);
			if(%hit.getClassName() $= "AIPlayer")
			{

			}

			if(!isObject(%hit))
			{
				%obj.targetLastCheck = $Sim::Time;
				%obj.targetName = %col.displayName;
				%obj.targetObj = %col;
				return %obj.targetName;
			}
		}

		if(%cts > 256)
		{
			talk("!!!");
			talk("targetaplayer just went over 256!");
			break;
		}
	}

	// Stickiness if no other target is detected.
	if($Sim::Time - %obj.targetLastCheck > 3)
		%obj.targetObj = "";
		return "Nobody";
	
	return %obj.targetName;
}

function Player::deleteCorpse(%obj)
{
	%obj.isBody = false;
	%obj.removeBody();
}

function Player::dropCorpse(%obj)
{
	if (!isObject(%corpse = %obj.heldCorpse))
		return 0;

	%obj.playThread(2, "shiftDown");
	%a = %obj.getPosition();
	%b = vectorAdd(vectorScale(%obj.getForwardVector(), 3), %a);
	%ray = containerRayCast(%a, %b, $TypeMasks::All ^ $TypeMasks::fxAlwaysBrickObjectType, %obj);
	if(%ray)
		%b = getWords(%ray, 1, 3);
	%pos = vectorScale(vectorAdd(%a, %b), 0.5); //Get middle of raycast

	%obj.mountObject(%corpse, 8);
	%corpse.dismount();
	%corpse.setTransform(%pos);
	%corpse.setVelocity(%obj.getVelocity());

	corpseInventory_UnDisplay(%obj.client);
	return 1;
}

function Player::findCorpseRayCast(%obj, %long)
{
	%a = %obj.getEyePoint();
	if(!%long)
		%b = vectorAdd(vectorScale(%obj.getEyeVector(), 5), %a);
	else
		%b = vectorAdd(vectorScale(%obj.getEyeVector(), 100), %a);

	%ray = containerRayCast(%a, %b, $TypeMasks::PlayerObjectType | $TypeMasks::fxAlwaysBrickObjectType, %obj);
	if(%ray)
		%b = getWords(%ray, 1, 3);
	%center = vectorScale(vectorAdd(%a, %b), 0.5); //Get middle of raycast
	%length = vectorDist(%a, %b) / 2;

	%safe = 0;

	%maxdist = 1; //how fatass our fat raycast is
	initContainerRadiusSearch(%center, %length + %maxdist, $TypeMasks::CorpseObjectType); //Scale radius search so it searches the entirety of raycast
	while (isObject(%col = containerSearchNext()))
	{
		%safe++;
		if (!%col.isBody || %col.getClassName() $= "AIPlayer")
			continue;
		%p = %col.getHackPosition();
		%ab = vectorSub(%b, %a);
		%ap = vectorSub(%p, %a);

		%project = vectorDot(%ap, %ab) / vectorDot(%ab, %ab); //Projection, aka "check against closest point on raycast" or something.

		if (%project < 0 || %project > 1)
			continue;

		%j = vectorAdd(%a, vectorScale(%ab, %project));
		%distance = vectorDist(%p, %j);
		if (%distance <= %maxdist) //Give 'em the corpse!
		{
			return %col;
		}

		if(%safe > 256)
		{
			talk("!!!");
			talk("findcorpseraycast just went over 256!");
			break;
		}
	}
	return 0;
}

datablock PlayerData(EmptyPlayer)
{
	shapeFile = "base/data/shapes/empty.dts";
};

function EmptyPlayer::onUnmount(%db,%obj,%mount,%node) 
{
	%obj.delete();
}

function Player::grabCorpse(%obj, %corpse)
{
	if(%corpse.getObjectMount())
	{
		return;
	}

	%obj.unMountImage(0);
	fixArmReady(%obj);

	if(!isObject(%obj.corpseHolder))
	{
		%obj.corpseHolder = new AIPlayer()
		{
			dataBlock = "EmptyPlayer";
		};
		%obj.mountObject(%obj.corpseHolder,0);
	}

	%corpseHolder = %obj.corpseHolder;
	%corpseHolder.mountObject(%corpse, 0);

	%obj.playThread(2, "ArmReadyBoth");
	%obj.heldCorpse = %corpse;

	%corpse.holder = %obj;
	%corpse.setTransform("0 0 -100 0 0 -1 -1.5709");

	// DNA
	if(strstr(%corpse.fingerPrints, %obj) < 0)
		%corpse.fingerPrints = %corpse.fingerPrints @ "	" @ %obj;

	//make an inventory for the corpse if it doesn't exist already
	if(!isObject(%corpse.corpseInventory))
	{
		%corpse.corpseInventory = %corpse.GetCorpseInventory();
	}

	corpseInventory_Display(%obj.client,%corpse.corpseInventory);

	//loot credits if availible
	%client = %obj.client;
	if(isObject(%client))
	{
		%credits = %corpse.credits;
		%corpse.credits = 0;

		if(%credits > 0)
		{
			%client.credits += %credits;
			%client.chatMessage("\c6You looted\c3" SPC %credits SPC "Credits\c6 from the corpse.");
		}
	}
}


function Player::throwCorpse(%obj)
{
	if (!isObject(%corpse = %obj.heldCorpse))
		return 0;

	%obj.playThread(2, "shiftUp");
	%a = %obj.getMuzzlePoint(3);
	%b = vectorAdd(vectorScale(%obj.getEyeVector(), 5), %a);
	%ray = containerRayCast(%a, %b, $TypeMasks::All ^ $TypeMasks::fxAlwaysBrickObjectType, %obj);
	if(%ray)
		%b = getWords(%ray, 1, 3);
	%velocity = vectorScale(%obj.getEyeVector(), vectorDist(%a, %b));
	%velocity = vectorAdd(%velocity, %obj.getVelocity()); //velocity inheritance
	%pos = vectorScale(vectorAdd(%a, %b), 0.5); //Get middle of raycast

	%obj.mountObject(%corpse, 8);
	%corpse.dismount();
	%corpse.setTransform(%pos);
	%corpse.setVelocity(%velocity);

	corpseInventory_UnDisplay(%obj.client);
	return 1;
}

// =================================================
// 6. MinigameSO
// =================================================
function BBB_Minigame::announce(%so, %text)
{
	// for(%a = 0; %a < ClientGroup.getCount(); %a++)
	// {
		// %client = ClientGroup.getObject(%a);
		// if(%client.inBBB)
			// %client.centerPrint(%text, $BBB::Time::PostRound / 1000);
	// }

	for(%i = 0; %i < %so.numMembers; %i++)
	{
		%client = %so.member[%i];
		%time = ($BBB::Time::PostRound - $BBB::Time::Shock) / 1000;
		%client.centerPrint(%text, $BBB::Time::PostRound / 1000);
		//%client.schedule($BBB::Time::Shock, "centerPrint", %text, %time);
	}
}

function BBB_Minigame::assignRandomOutfit(%so)
{
	// Old version
	// $BBB::Outfit = getRandom(0, 7);

	// for(%i = 0; %i < %so.numMembers; %i++)
	// {
		// %client = %so.member[%i];
		// %obj = %client.player;
		// if(isObject(%obj))
			// %obj.BBB_applyOutfit($BBB::Outfit);
	// }


	// Shit version
	// %num = getRandom(0, %so.nmMembers);
	// %srcClient = %so.member[%num];
	// for(%i = 0; %i < %so.numMembers; %i++)
	// {
		// %client = %so.member[%i];
		// if(%client == %srcClient)
			// continue;
		// %obj = %client.player;

		// if(isObject(%obj) && isObject(%srcClient.player))
		// {
			// %orig = %client.player;
			// %client.player = %srcClient.player
			// %client.applyBodyParts();
			// %client.applyBodyColors();
			// %client.player = %orig;
	// }


	%num = getRandom(0, %so.numMembers - 1);
	%client = %so.member[%num];

	// Organized based on height, not alphabetic this time. Sorry ;((
	// $BBB::Outfit::Node_[0] = %client.hat;
	// $BBB::Outfit::Node_[1] = %client.chest;
	// $BBB::Outfit::Node_[2] = %client.accent;
	// $BBB::Outfit::Node_[3] = %client.pack;
	// $BBB::Outfit::Node_[4] = %client.secondpack;
	// $BBB::Outfit::Node_[5] = %client.larm;
	// $BBB::Outfit::Node_[6] = %client.rarm;
	// $BBB::Outfit::Node_[7] = %client.lhand;
	// $BBB::Outfit::Node_[8] = %client.rhand;
	// $BBB::Outfit::Node_[9] = %client.hip;
	// $BBB::Outfit::Node_[10] = %client.lleg;
	// $BBB::Outfit::Node_[11] = %client.rleg;

	// $BBB::Outfit::Color_[0] = %client.hatColor;
	// $BBB::Outfit::Color_[1] = %client.chestColor;
	// $BBB::Outfit::Color_[2] = %client.accentColor;
	// $BBB::Outfit::Color_[3] = %client.packColor;
	// $BBB::Outfit::Color_[4] = %client.secondpackColor;
	// $BBB::Outfit::Color_[5] = %client.larmColor;
	// $BBB::Outfit::Color_[6] = %client.rarmColor;
	// $BBB::Outfit::Color_[7] = %client.lhandColor;
	// $BBB::Outfit::Color_[8] = %client.rhandColor;
	// $BBB::Outfit::Color_[9] = %client.hipColor;
	// $BBB::Outfit::Color_[10] = %client.llegColor;
	// $BBB::Outfit::Color_[11] = %client.rlegColor;

	// $BBB::Outfit::Texture_[0] = %client.decalName;
	// $BBB::Outfit::Texture_[1] = %client.faceName;

	// Hat
	switch(%client.hat)
	{
		case 1:
			$BBB::Outfit::Node_[0] = "helmet";
		case 2:
			$BBB::Outfit::Node_[0] = "pointyhelmet";
		case 3:
			$BBB::Outfit::Node_[0] = "flarehelmet";
		case 4:
			$BBB::Outfit::Node_[0] = "scouthat";
		case 5:
			$BBB::Outfit::Node_[0] = "bicorn";
		case 6:
			$BBB::Outfit::Node_[0] = "copHat";
		case 7:
			$BBB::Outfit::Node_[0] = "knitHat";
		default:
			$BBB::Outfit::Node_[0] = "";
	}

	// Chest
	switch(%client.chest)
	{
		case 1:
			$BBB::Outfit::Node_[1] = "femchest";
		default:
			$BBB::Outfit::Node_[1] = "chest";
	}

	// Accent
	if(%client.hat == 1 && %client.accent == 1)
		$BBB::Outfit::Node_[2] = "visor";
	else if(%client.hat > 3)
	{
		switch(%client.accent)
		{
			case 1:
				$BBB::Outfit::Node_[2] = "plume";
			case 2:
				$BBB::Outfit::Node_[2] = "triplume";
			case 3:
				$BBB::Outfit::Node_[2] = "septplume";
			default:
				$BBB::Outfit::Node_[2] = "";
		}
	}
	else
		$BBB::Outfit::Node_[2] = "";

	// Pack
	switch(%client.pack)
	{
		case 1:
			$BBB::Outfit::Node_[3] = "armor";
		case 2:
			$BBB::Outfit::Node_[3] = "bucket";
		case 3:
			$BBB::Outfit::Node_[3] = "cape";
		case 4:
			$BBB::Outfit::Node_[3] = "pack";
		case 5:
			$BBB::Outfit::Node_[3] = "quiver";
		case 6:
			$BBB::Outfit::Node_[3] = "tank";
		default:
			$BBB::Outfit::Node_[3] = "";
	}

	// Second Pack
	switch(%client.secondpack)
	{
		case 1:
			$BBB::Outfit::Node_[4] = "epaulets";
		case 2:
			$BBB::Outfit::Node_[4] = "epauletsRankA";
		case 3:
			$BBB::Outfit::Node_[4] = "epauletsRankB";
		case 4:
			$BBB::Outfit::Node_[4] = "epauletsRankC";
		case 5:
			$BBB::Outfit::Node_[4] = "epauletsRankD";
		case 6:
			$BBB::Outfit::Node_[4] = "ShoulderPads";
		default:
			$BBB::Outfit::Node_[4] = "";
	}

	// LArm
	switch(%client.larm)
	{
		case 1:
			$BBB::Outfit::Node_[5] = "LArmSlim";
		default:
			$BBB::Outfit::Node_[5] = "LArm";
	}

	// RArm
	switch(%client.larm)
	{
		case 1:
			$BBB::Outfit::Node_[6] = "RArmSlim";
		default:
			$BBB::Outfit::Node_[6] = "RArm";
	}

	// LHand
	switch(%client.lhand)
	{
		case 1:
			$BBB::Outfit::Node_[7] = "LHook";
		default:
			$BBB::Outfit::Node_[7] = "LHand";
	}

	// RHand
	switch(%client.rhand)
	{
		case 1:
			$BBB::Outfit::Node_[8] = "RHook";
		default:
			$BBB::Outfit::Node_[8] = "RHand";
	}

	// Hip
	switch(%client.hip)
	{
		case 1:
			$BBB::Outfit::Node_[9] = "skirtHip";
		default:
			$BBB::Outfit::Node_[9] = "pants";
	}

	// LLeg
	if(%client.hip == 0) // reg pants
	{
		switch(%client.lleg)
		{
			case 1:
				$BBB::Outfit::Node_[10] = "LPeg";
			default:
				$BBB::Outfit::Node_[10] = "LShoe";
		}
	}
	else // skirt
		$BBB::Outfit::Node_[10] = "SkirtTrimLeft";

	// RLeg
	if(%client.hip == 0)
	{
		switch(%client.rleg)
		{
			case 1:
				$BBB::Outfit::Node_[11] = "RPeg";
			default:
				$BBB::Outfit::Node_[11] = "RShoe";
		}
	}
	else
		$BBB::Outfit::Node_[11] = "SkirtTrimRight";
	$BBB::Outfit::Node_[12] = "headSkin";

	$BBB::Outfit::Color_[0] = %client.hatColor;
	$BBB::Outfit::Color_[1] = %client.chestColor;
	$BBB::Outfit::Color_[2] = %client.accentColor;
	$BBB::Outfit::Color_[3] = %client.packColor;
	$BBB::Outfit::Color_[4] = %client.secondpackColor;
	$BBB::Outfit::Color_[5] = %client.larmColor;
	$BBB::Outfit::Color_[6] = %client.rarmColor;
	$BBB::Outfit::Color_[7] = %client.lhandColor;
	$BBB::Outfit::Color_[8] = %client.rhandColor;
	$BBB::Outfit::Color_[9] = %client.hipColor;
	$BBB::Outfit::Color_[10] = %client.llegColor;
	$BBB::Outfit::Color_[11] = %client.rlegColor;
	$BBB::Outfit::Color_[12] = %client.headColor;

	$BBB::Outfit::Decal = %client.decalName;
	$BBB::Outfit::Face = %client.faceName;

	$BBB::Outfit::Owner = %client;

	for(%i = 0; %i < %so.numMembers; %i++)
	{
		%target = %so.member[%i];
		%obj = %target.player;
		if(isObject(%obj))
			%obj.BBB_applyOutfit();
	}
}

function BBB_Minigame::assignRoles(%so)
{
	// Shuffle Loop
	// =============================================
	%playerCount = 0;
	for(%i = 0; %i < %so.numMembers; %i++)
	{
		%client = %so.member[%i];
		if(isObject(%client.player) && %client.slayed <= 0)
		{
			%so.playingClients[%playerCount] = %client;
			%playerCount++;
		}
	}
	%so.numPlayers = %playerCount;

	messageAll("", "<font:Palatino Linotype:35>\c4B<font:Palatino Linotype:34>\c4EGINNING ROUND \c6" @ $BBB::Round);
	messageAll("", "<font:Palatino Linotype:28>\c4T<font:Palatino Linotype:27>\c4HERE ARE\c6" SPC %so.numPlayers SPC "\c4PLAYERS THIS ROUND...");

	%defaultcolor = %so.roleGroup.defaultChatColor;
	for(%a = 0; %a < %playerCount; %a++)
	{
		%client = %so.playingClients[%a];
		%player = %client.player;
		if(!isObject(%player.roleBBM))
		{
			%player.roleBBM = OverheadBillboardMount.Make();
		}
		%player.mountObject(%player.roleBBM,8);
		for(%b = 0; %b < %playerCount; %b++)
		{
			%targetclient = %so.playingClients[%b];
			%client.inspectInfo[%targetclient] = "\c6";
			%client.namecolor[%targetclient] = %defaultcolor;
			%client.badge[%targetclient] = "";
			%client.role = "";
		}
		%clients = %clients SPC %so.playingClients[%a];	
	}
	%clients = lTrim(%clients);
	%so.nameList.setNames(%clients);
	%so.activeRoleGroup = %so.rolegroup.setRoles(%clients);
	NameList_Update();
}

function BBB_Minigame::CleanUp(%so)
{
	if($BBB::Round::Phase $= "MapVote")
		return;
	$BBB::Round::Active = false;
	$BBB::bTimeLeft = 0;
	$BBB::rTimeLeft = 0;
	$BBB::Round::Phase = "";

	%so.setGlobalPrint("Waiting...");

	for(%i = 0; %i < %so.numMembers; %i++)
	{
		%client = %so.member[%i];
		%role = %client.role;
		if(isObject(%role))
		{
			%role.delete();
		}
		%client.role = "";
	}

	%so.respawnTime = "1";
	%so.weapondamage = false;

	BBB_TimerLoop();
}

function BBB_Minigame::doWinCheck(%so, %scheduled)
{
	%winners = %so.activeRoleGroup.WinCheck($BBB::rTimeLeft <= 0);
	if(%winners !$= "")
	{
		%so.roundEnd(%winners);
	}	
}

function BBB_Minigame::getNumAlive(%so, %role)
{
	%counter = 0;
	for(%i = 0; %i < %so.numMembers; %i++)
	{
		%client = %so.member[%i];
		%player = %client.player;
		if(isObject(%player) && %client.role.data.name $= %role)
			%counter++;
	}

	return %counter;
}

function BBB_Minigame::healAllPlayers(%so)
{
	// for(%a = 0; %a < ClientGroup.getCount(); %a++)
	// {
		// %client = ClientGroup.getObject(%a);
		// if(%client.inBBB)
		// {
			// if(isObject(%client.player))
				// %client.player.setDamageLevel(0);
		// }
	// }
	for(%i = 0; %i < %so.numMembers; %i++)
	{
		if(isObject(%client.player))
			%client.player.setDamageLevel(0);
	}
}

function BBB_Minigame::playGlobalSound(%so, %db)
{
	// for(%a = 0; %a < ClientGroup.getCount(); %a++)
	// {
		// %client = ClientGroup.getObject(%a);
		// if(%client.inBBB)
			// %client.play2D(%db);
	// }
	for(%i = 0; %i < %so.numMembers; %i++)
	{
		%client = %so.member[%i];
		%client.play2D(%db);
	}
}

function BBB_Minigame::roundEnd(%so, %type)
{
	if(!$BBB::Round::Active)
		return;

	BBB_StopGlobalMusic();
	serverPlay2D(BBB_EndRound_Sound);
	%count = getWordCount(%type);
	for(%i = 0; %i < %count; %i++)
	{
		%text = %text SPC getWord(%type,%i).display;
	}
	%text = "<br><br><font:Palatino Linotype:80>" SPC lTrim(%text) SPC "WIN";


	$BBB::Round::Phase = "PostRound";

	//item popping time baby! (wtf?)
	$BBB::ItemPop = true;

	announceDeathLogs();
	clearDeathLogs();
	Oopsies_EndRound();

	%so.schedule($BBB::Time::Shock, announce, %text);
	//%so.schedule(1, playGlobalSound, BBB_EndRound_Sound);
	%so.setGlobalPrint("ROUND OVER");

	$BBB::rTimeLeft = $BBB::Time::PostRound;
	BBB_TimerLoop();

	NameList_Update(true);
}

function BBB_Minigame::roundSetup(%so)
{
	if(isEventPending($BBB::Round::Schedule))
		cancel($BBB::Round::Schedule);

	%so.reset();

	%so.assignRandomOutfit();
	%so.clearRoles();
	%so.weapondamage = false;

	BBB_SpawnItems();

	$BBB::Round::Active = true;
	$BBB::Round::Phase = "PreRound";

	//no more item popping past this point
	$BBB::ItemPop = false;

	%so.spawnAllPlayers(true);

	%so.setGlobalPrint("PREPARING...");
	%so.respawnTime = "-1";

	if(isObject(CorpseGroup))
		CorpseGroup.deleteAll();

	$BBB::rTimeLeft = $BBB::Time::PreRound;

	BBB_TimerLoop();
}

function BBB_Minigame::roundStart(%so)
{
	if(!$BBB::Round::Active)
		return;

	if(isEventPending($BBB::Round::Schedule))
		cancel($BBB::Round::Schedule);

	%so.respawnTime = "-1";
	%so.weapondamage = true;
	%so.spawnAllPlayers();
	%so.healAllPlayers();

	$BBB::Round++;

	%so.assignRoles();

	%so.playGlobalSound(BBB_StartRound_Sound);

	$BBB::Round::Phase = "Round";

	$BBB::bTimeLeft = %so.rolegroup.time;
	$BBB::rTimeLeft = %so.rolegroup.time;

	BBB_LookLoop();
	BBB_TimerLoop();

	%so.playGlobalSound(BBB_Chat_Sound);
}

function BBB_Minigame::setGlobalPrint(%so, %text)
{
	// for(%a = 0; %a < ClientGroup.getCount(); %a++)
	// {
		// %client = ClientGroup.getObject(%a);
		// if(%client.inBBB)
			// %client.print = "<just:left><font:Palatino Linotype:36>\c3" @ getSubStr(%text, 0, 1) @ "<font:Palatino Linotype:34>\c3" @ getSubStr(%text, 1, strLen(%text));
	// }
	$BBB::GlobalPrint = "<just:left><font:Palatino Linotype:36>\c3" @ getSubStr(%text, 0, 1) @ "<font:Palatino Linotype:34>\c3" @ getSubStr(%text, 1, strLen(%text));
	for(%i = 0; %i < %so.numMembers; %i++)
	{
		%client = %so.member[%i];
		%client.print = $BBB::GlobalPrint;
	}
}

function BBB_Minigame::spawnAllPlayers(%so, %override)
{
	// for(%a = 0; %a < ClientGroup.getCount(); %a++)
	// {
		// %client = ClientGroup.getObject(%a);
		// if(%client.inBBB)
		// {
			// if(%override)
				// %client.instantRespawn();
			// else if($BBB::Round::Phase $= "PreRound" && !isObject(%client.player))
				// %client.instantRespawn();
		// }
	// }
	for(%i = 0; %i < %so.numMembers; %i++)
	{
		%client = %so.member[%i];
		%client.credits = 0;
		if(%client.slayed > 0)
		{
			if(%override)
			{
				%client.slayed--;
			}
			
			continue;
		}

		if(!%client.AVBillboardGroup.loaded)
		{
			continue;
		}
		if(%override)
		{
			%client.instantRespawn();
		}
		else if($BBB::Round::Phase $= "PreRound" && !isObject(%client.player))
		{
			%client.instantRespawn();
		}

		%client.player.displayName = %client.fakeName;
		%client.player.setShapeName("", 8564862);
	}
}

// =================================================
// 7. ServerCMD
// =================================================
function BBB_CreditBuy(%client,%item,%price,%stock)
{
	%player = %client.player;
	if(isObject(%item) && isObject(%player))
	{
		%credits = %client.credits;
		

		if(%price $= "")
		{
			%price = 1;
		}

		if(%stock $= "")
		{
			%stock = inf;
		}

		if(%credits >= %price && %stock > %player.bought[%item.getId()])
		{
			%client.credits -= %price;
			%player.bought[%item.getId()]++;

			%success = %player.pickup(new Item(){dataBlock = %item;});
			return %success;
		}
	}


	return false;
}

function serverCmdBuy(%client, %num)
{
	%player = %client.player;
	%role = %client.role.data.name;

	%item = $BBB::Shop_[%role,%num];
	if(!isObject(%item))
	{
		return;
	}

	%success = BBB_CreditBuy(%client,%item,$BBB::WeaponPrice[%role,%item.getName()],$BBB::WeaponStock[%role,%item.getName()]);

	if(%success)
	{
		%client.chatMessage("\c6You bought" SPC %item.uiName SPC ".");
	}
	else
	{
		%client.chatMessage("Purchase failed.");
	}

	%client.play2D(BBB_Chat_Sound);
}

function serverCmdEndRound(%client)
{
	if(!%client.isAdmin)
		return;

	%mini = getMiniGameFromObject(%client);
	if(!%mini.isBBB)
		return;

	if($BBB::Round::Phase !$= "Round")
		return;

	%mini.roundEnd("IWin");
	messageAll('MsgAdminForce', '\c3%1\c6 has forcefully ended the round.', %client.getPlayerName());
}

function serverCmdShop(%client)
{
	if(!%client.inBBB || !isObject(%client.player) || $BBB::Round::Phase !$= "Round")
		return;
	%client.BBB_DisplayShop(%client.role.data.name);
}

function serverCmdSlay(%client, %a1,%a2,%a3,%a4,%a5,%a6,%a7,%a8,%a9,%a10,%a11,%a12,%a13,%a14,%a15)
{
	if(!%client.isAdmin)
		return;

	//look for a name in the arguments
	%c = 0;
	while(!isObject(%target = findClientByName(%search = %search @ %a[%c++])) && %c < 16){}

	%amount = %a[%c + 1];
	if(!isObject(%target))
	{
		%amount = %a2;
		//look for a blid
		if(!isObject(%target = findClientByBL_ID(%a1)))
		{
			%client.chatMessage("Not a valid player name or BLID");
			return;
		}
	}

	%message = '\c3%1\c6%1 has slain \c3%2\c6.';
	if(%amount > 0)
	{
		%message = '\c3%1\c6 has slain \c3%2 \c6for \c2%3 \c4Rounds\c6.';
	}
	else
	{
		%amount = 0;
	}

	messageAll('MsgAdminForce', %message, %client.getPlayerName(), %target.getPlayerName(), %amount);

	%target.slayed = %amount;

	if($BBB::Round::Phase $= "Round")
	{
		%target.slayed += 1;
	}

	%player = %target.player;
	if(isObject(%player))
	{
		%player.kill();
	}
}

function serverCmdVoteMap(%client, %n)
{
	%client.lastvotedmap = "";

	if($BBB::Round::Phase !$= "MapVote")
		return;

	if(strstr($BBB::Vote::Data, "|" @ %client.getBLID() @ "|") > -1)
	{
		messageClient(%client, '', "\c6You already voted!");
		%client.play2D(BBB_Chat_Sound);
		return;
	}

	if(%n < 1 || %n > $BBB::SelectedCount)
	{
		messageClient(%client, '', "\c6Choose a valid map!");
		%client.play2D(BBB_Chat_Sound);
		return;
	}

	%n -= 1;
	$BBB::Vote::Data = $BBB::Vote::Data @ "|" @ %client.getBLID() @ "|";
	$BBB::Map::Votes[%n] += 1;
	
	%filename = getField($BBB::SelectedMaps,%n);
	%displayName = FilePath2MapName(%fileName);
	messageClient(%client, '', "<bitmap:base/client/ui/ci/star>\c6 You " @ "\c4voted for \c6" @ %displayName @ "\c4.");

	%client.play2D(BBB_Chat_Sound);

	%client.lastvotedmap = %displayName;
}

function serverCmdRole(%cl, %t)
{
	if(!%cl.isAdmin)
		return;
	
	if(%t $= "")
		return;
	
	%u = findClientByName(%t);
	if(isObject(%u))
		messageClient(%cl, '', "<font:Palatino Linotype:22><color:494949>" @ %u.name SPC "is a(n)" SPC %u.role.data.name @ ".");
}

function serverCmdLog(%cl, %t, %tk)
{
	if(!%cl.isAdmin)
		return;
	
	if(%t $= "")
		return;
	
	%u = findClientByName(%t);
	if(isObject(%u))
	{
		messageClient(%cl, '', "<font:Palatino Linotype:22><color:494949>" @ %u.name SPC "'s kill log:");
		for(%i = 0; %i < %u.killLogCount; %i++)
		{
			if(!%tk)
				messageClient(%cl, '', "<font:Palatino Linotype:18>" @ getField(%u.killLog[%i], 0));
			else
			{
				if(getField(%u.killLog[%i], 1))
					messageClient(%cl, '', "<font:Palatino Linotype:18>" @ getField(%u.killLog[%i], 0));
			}
		}
	}
}

$RTV::Percent = 0.50;
$RTV::VotingRound = 0;
$RTV::CurrentCooldown = 0;
$RTV::VoteTime = 60000; // 1 minute
$RTV::Voting = false;
$RTV::Cooldown = 60000 * 5; // 5 minutes
function servercmdRTV(%client)
{
	if($RTV::CurrentCooldown > getSimTime())
	{
		%client.chatMessage("\c6Please wait" SPC mfloor(($RTV::CurrentCooldown - getSimTime()) / 1000) SPC "more seconds.");
		return;
	}

	if(!$RTV::Voting)
	{
		$RTV::Voting = true;
		$RTV::VotingRound++;

		//start the vote count down
		$RTV::Finish = schedule($RTV::VoteTime,0,"RTV_Finish");
	}

	if($RTV::VotingRound == $RTV::BLIDToRound[%client.getBLID()])
	{
		%client.chatMessage("\c6You already rocked the vote.");
		return;
	}

	$RTV::Votes++;
	$RTV::BLIDToRound[%client.getBLID()] = $RTV::VotingRound;

	%votesRemaining = mFloor($RTV::Percent * clientGroup.getCount()) - $RTV::Votes;
	if(%votesRemaining <= 0)
	{
		//vote suceeded
		MessageAll ('', "\c3" @ %client.getPlayerName() SPC "\c6has rocked the vote! Vote suceeded, map vote will happen at the end of the round.");
		$BBB::Round = $BBB::Map::Rounds;
		RTV_Reset();
	}
	else
	{
		MessageAll ('', "\c3" @ %client.getPlayerName() SPC "\c6has rocked the vote!\c3" SPC %votesRemaining SPC "\c6more \c3/rtv \c6votes needed.");
	}
}

function RTV_Finish()
{
	MessageAll ('', "\c3RTV failed.");
	RTV_Reset();
}

function RTV_Reset()
{
	$RTV::CurrentCooldown = $RTV::Cooldown + getsimtime();
	$RTV::Voting = false;
	$RTV::Votes = 0;
	cancel($RTV::Finish);
}
