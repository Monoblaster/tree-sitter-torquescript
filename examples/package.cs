// =================================================
// System_BBB - By Alphadin, LegoPepper, Nobot
// =================================================
// File Name: package.cs
// Description: Hooks for players, weapons, or other.
// =================================================
// Table of Contents:
//
// 1. Namespaceless
// 2. Armor
// 3. fxDTSBrick
// 4. GameConnection
// 5. MinigameSO
// 6. Player
// 7. ServerCMD
// =================================================

// =================================================
// 1. Namespaceless
// =================================================
function SimObject::onCameraEnterOrbit(%obj, %camera) {}
function SimObject::onCameraLeaveOrbit(%obj, %camera) {}

function Projectile::FuseExplode(%proj)
{
	%db = %proj.getDatablock();
	%vel = %proj.getVelocity();
	%pos = %proj.getPosition();
	%sObj = %proj.sourceObject;
	%sSlot = %proj.sourceSlot;
	%cli = %proj.client;

	%proj.delete();

	if(vectorLen(%vel) == 0)
		%vel = "0 0 0.1";
	
	%p = new Projectile()
	{
		dataBlock = %db;
		initialVelocity = %vel;
		initialPosition = %pos;
		sourceObject = %sObj;
		sourceSlot = %sSlot;
		client = %cli;
		sourceImage = %proj.sourceImage;
		sourceItem = %proj.sourceItem;
	};
	
	MissionCleanup.add(%p);

	%p.explode();
}

package BBB_Namespaceless
{
	//only pop items when we want them to
	function Item::schedulePop(%obj)
	{
		
		if($BBB::ItemPop)
		{
			Parent::schedulePop(%obj);
		}
		else
		{
			%obj.schedule($Game::Item::PopTime,"schedulePop");
		}
	}

	function Player::WeaponAmmoUse(%player)
	{
		%image = %player.getMountedImage(0);
		%player.lastBBBUsedImage = %image;
		%item = %player.tool[%player.currTool];
		 %player.lastBBBUsedItem = %item;
		parent::WeaponAmmoUse(%player);
	}

	function Projectile::onAdd(%projectile)
	{
		%return = Parent::onAdd(%projectile);
		if(isObject(%projectile))
		{
			%player = %projectile.sourceObject;


			if(isObject(%player))
			{
				if(%player.getClassName() $= "Player")
				{
					if(%projectile.sourceImage $= "")
					{
						%image = %player.getMountedImage(0);
						if(!isObject(%image))
						{
							%image = %player.lastBBBUsedImage;
						}
						%projectile.sourceImage = %image;
					}

					if(%projectile.sourceItem $= "")
					{
						%item = %player.tool[%player.currTool];
		
						if(!isObject(%item))
						{
							%item = %player.lastBBBUsedItem;
						}

						%projectile.sourceItem = %item;
					}
				}
			}
		}
		return %r;
	}
};
activatePackage(BBB_Namespaceless);
//overwrite so grenades inherit sourceimage
// =================================================
// 2. Armor
// =================================================
package BBB_Armor
{
	function Armor::Damage(%data, %victim, %hitter, %pos, %damage, %damageType)
	{
		Parent::Damage(%data, %victim, %hitter, %pos, %damage, %damageType);
		%client = %victim.client;
		if(!%client.inBBB)
			return;
		if(isObject(%victim))
		{
			%health = mCeil(100 - %victim.getDamageLevel());
			%healthText = "\c3" @ %health;
			BBB_TimerLoop_ForceUpdate(%client, "bottomPrint", 2, %healthText);

			if(%health > 80)
				%victim.setShapeNameColor("0 1 0");
			else if(%health > 60)
				%victim.setShapeNameColor("0.7 1 0.1");
			else if(%health > 40)
				%victim.setShapeNameColor("1 1 0");
			else if(%health > 20)
				%victim.setShapeNameColor("1 0.5 0");
			else
				%victim.setShapeNameColor("1 0 0");
		}
	}

	function Player::Pickup(%pl, %item)
	{
		if(%item == AE_AmmoItem.getID())
			return 1;
		
		Parent::Pickup(%pl, %item);
	}

	function Armor::onCollision(%this, %obj, %col, %vec, %speed)
	{
		if(isObject(%col))
		{
			%client = %obj.client;

			if(isObject(%client) && isObject(%obj) && isObject(AE_AmmoItem))
			{

				if(%client.getClassname() $= "GameConnection")
				{
					if((%col.getType() & $TypeMasks::ItemObjectType) && %col.getDatablock() != AE_AmmoItem.getID())
					{
						if(%obj.pickup(%col))
							return;
					}
					else
					{
						if(getSimTime() - %obj.lastAmmoPickupTime > 15000)
						{
							%data = %col.getDatablock();
							if(%data == AE_AmmoItem.getID())
							{
								if(%col.canPickup && %obj.getDamagePercent() < 1.0 && minigameCanUse(%obj, %col))
								{
									%col.spawnBrick = "";
									Parent::onCollision(%this, %obj, %col, %vel, %speed);
									
									if(%data == AE_AmmoItem.getID())
										%col.schedule(10, delete);
									%obj.lastAmmoPickupTime = getSimTime();
									return;
								}
							}
						}
						else
							return;
					}
				}	
			}
		}

		return parent::onCollision(%this, %obj, %col, %vec, %speed);
	}
	function Armor::onTrigger(%this, %obj, %trigger, %state)
	{

		%client = %obj.client;

		switch(%trigger)
		{
			// Fire
			case 0:
				if(%state)
				{
					if(!%obj.dropCorpse() && !isObject(%obj.getMountedImage(0)) && !isObject(%obj.meleeHand))
					{
						if(isObject(%corpse = %obj.findCorpseRayCast()))
						{
							// We use this instead of the function because if my specific unnecessary need for 'an' 'a' etc.

							messageClient(%client, '', "\c6[\c4Corpse Data\c6]");
							
							%rolecolor = %corpse.role.data.color;
							if(%rolecolor !$= "")
							{
								%rolecolorml = "<color:"@%rolecolor@">";
							}

							%rolename =  %corpse.role.data.name;
							if(%rolename $= "")
							{
								%rolename = "???";
							}
							messageClient(%client, '', "\c6 > \c3Name\c6: " @ %corpse.name);
							messageClient(%client, '', "\c6 > \c3Role\c6: " @ %rolecolorml @ %rolename);
							messageClient(%client, '', "\c6 > \c3Cause of death\c6: [" @ %corpse.SOD @ "\c6]");
							if(%corpse.deadTime !$= "")
							{
								%time = getSimTime() - %corpse.deadTime;
								%minutes = mFloor(%time / 60000);
								%seconds = mFloor((%time - %minutes * 60000) / 1000);
								if(%seconds < 10)
									%seconds = "0" @ %seconds;
								messageClient(%client, '', "\c6 - \c3Dead for\c6 " @ %minutes @ ":" @ %seconds @ "\c3 " @ (%minutes > 0 ? (%minutes > 1 ? "minutes" : "minute") : (%seconds > 1 || %seconds == 0 ? "seconds" : "second")) @ ".");
							}
							if(%corpse.lastWords !$= "")
								messageClient(%client, '', "\c6 - \c3Last Words: \c6\"" @ %corpse.lastWords @ "\"");
							if(%corpse.unIDed)
							{
								%corpse.displayName = %corpse.name @ "'s corpse";
								%corpse.setShapeName(%corpse.displayName, 8564862);
								//findclientbyname(lego).player.setShapeName("0", 8564862);
								%corpse.unIDed = false;
								if($BBB::Announce::BodyFound)
								{
									chatMessageAll("", "\c6" @ %obj.client.fakeName SPC "\c4found\c6" SPC %corpse.displayName @ "\c4!" SPC "They were" @ (%rolename $= "Innocent" ? " " : " a ") @ %rolecolorml @ %rolename @ "\c4!");
									
									%client = findClientByName(%corpse.name);
									if(isObject(%client))
									{
										//mark them as dead in the player list
										if($BBB::Round::Phase $= "Round")
										{
											%client.state = "X";
											NameList_Update();
										}
									}
								}
							}

						}
					}
				}
				return parent::onTrigger(%this, %obj, %trigger, %state);

			// Jump
			case 2:
				return parent::onTrigger(%this, %obj, %trigger, %state);

			// Crouch
			case 3:
				return parent::onTrigger(%this, %obj, %trigger, %state);

			// Jet
			case 4:
				if(%state) // On Right Click
				{
					// %mounted = %obj.getMountedObject(0);
					// if(isObject(%mounted) && %mounted.corpse)
					// {
						// %mounted.dismount();
						// %mounted.addVelocity(vectorScale(%obj.getEyeVector(),10));
						// %obj.playThread(3,"root");
						// return;
					// }
					// else
					// {
						// %start = %obj.getEyePoint();
						// %targets = ($TypeMasks::FxBrickObjectType | $TypeMasks::PlayerObjectType | $TypeMasks::StaticObjectType | $TypeMasks::TerrainObjectType | $TypeMasks::VehicleObjectType);
						// %vec = %obj.getEyeVector();
						// %end = vectorAdd(%start,vectorScale(%vec,10));
						// %ray = containerRaycast(%start,%end,%targets,%obj);
						// %col = firstWord(%ray);
						// if(!isObject(%col))
							// return parent::onTrigger(%this, %obj, %trigger, %state);
						// if(!%col.corpse)
							// return parent::onTrigger(%this, %obj, %trigger, %state);

						// At this point we know its a corpse.
						// %obj.mountObject(%col,0);
						// %col.setTransform("0 0 0 0 0 -1 -1.5709");
						// %obj.playThread(3,"ArmReadyBoth");
						// return;
					// }

					if (!%obj.throwCorpse()) //No corpse to throw, try grabbing one instead
					{
						if (isObject(%corpse = %obj.findCorpseRayCast()))
						{
							%obj.grabCorpse(%corpse);
						}
					}
				}

				return parent::onTrigger(%this, %obj, %trigger, %state);

			// Anything else.
			default:
				return parent::onTrigger(%this, %obj, %trigger, %state);
		}

		return parent::onTrigger(%this, %obj, %trigger, %state);
	}

	// Playertype armor
	function BBB_Standard_Corpse_Armor::onUnMount(%this, %obj, %mount, %slot)
	{
		Parent::onUnMount(%this, %obj, %mount, %slot);
		if (!isObject(%mount.heldCorpse) || %mount.heldCorpse != %obj)
			return;
		%mount.heldCorpse = "";
		%mount.playThread(2, "root");
	}
};
activatePackage(BBB_Armor);
// =================================================
// 3. fxDTSBrick
// =================================================
package BBB_fxDTSBrick
{
	function fxDTSBrick::onActivate(%this, %obj, %client)
	{
		if(%client.role.data.name $= "Traitor")
			%this.onTraitorActivate(%client);
		else
			%this.onInnocentActivate(%client);
		return parent::onActivate(%this, %obj, %client);
	}

	function fxDTSBrick::onPlayerTouch(%this, %obj)
	{
		%client = %obj.client;
		if(isObject(%client))
		{
			if(%client.role.data.name $= "Detective" || %client.role.data.name $= "Innocent")
				%this.onInnocentTouch(%obj);
			else if(%client.role.data.name $= "Traitor")
				%this.onTraitorTouch(%obj);
		}

		if(%obj.isBody)
			%this.onCorpseTouch(%obj);

		return parent::onPlayerTouch(%this, %obj);
	}
};
activatePackage(BBB_fxDTSBrick);
// =================================================
// 4. GameConnection
// =================================================
//overwrite
// All the corpse code is made by Jack Noir
function GameConnection_onDeath (%client, %sourceObject, %sourceClient, %damageType, %damLoc)
{
	if (%sourceObject.sourceObject.isBot)
	{
		%sourceClientIsBot = 1;
		%sourceClient = %sourceObject.sourceObject;
	}
	%player = %client.Player;
	if (isObject (%player))
	{
		%player.setShapeName ("", 8564862);
		if (isObject (%player.tempBrick))
		{
			%player.tempBrick.delete ();
			%player.tempBrick = 0;
		}
		%player.client = 0;
	}
	else 
	{
		warn ("WARNING: No player object in GameConnection::onDeath() for client \'" @ %client @ "\'");
	}
	if (isObject (%client.Camera) && isObject (%client.Player))
	{
		if (%client.getControlObject () == %client.Camera && %client.Camera.getControlObject () > 0)
		{
			%client.Camera.setControlObject (%client.dummyCamera);
		}
		else 
		{
			%client.Camera.setMode ("Corpse", %client.Player);
			%client.setControlObject (%client.Camera);
			%client.Camera.setControlObject (0);
		}
	}
	%client.Player = 0;
	if ($Damage::Direct[%damageType] != 1)
	{
		if (getSimTime () - %player.lastDirectDamageTime < 100)
		{
			if (%player.lastDirectDamageType !$= "")
			{
				%damageType = %player.lastDirectDamageType;
			}
		}
	}
	if (%damageType == $DamageType::Impact)
	{
		if (isObject (%player.lastPusher))
		{
			if (getSimTime () - %player.lastPushTime <= 1000)
			{
				%sourceClient = %player.lastPusher;
			}
		}
	}
	%message = "%2 killed %1";
	if (%sourceClient == %client || %sourceClient == 0)
	{
		%message = $DeathMessage_Suicide[%damageType];
	}
	else 
	{
		%message = $DeathMessage_Murder[%damageType];
	}
	if ($Damage::Direct[%damageType] == 1 && %player.getWaterCoverage () < 0.05)
	{
		if (%sourceClient && isObject (%sourceClient.Player))
		{
			%playerVelocity = ((VectorLen (VectorSub (%player.preHitVelocity, %sourceClient.Player.getVelocity ())) / 2.64) * 6 * 3600) / 5280;
		}
		else 
		{
			%playerVelocity = ((VectorLen (%player.preHitVelocity) / 2.64) * 6 * 3600) / 5280;
		}
		%playerPos = %player.getPosition ();
		%mask = $TypeMasks::StaticShapeObjectType | $TypeMasks::FxBrickObjectType | $TypeMasks::TerrainObjectType;
		%res0 = containerRayCast (VectorAdd (%playerPos, "0 0 2"), VectorAdd (%playerPos, "0 0  -6.8"), %mask);
		%res1 = containerRayCast (VectorAdd (%playerPos, "0 0 2"), VectorAdd (%playerPos, "0 -1 -6.8"), %mask);
		%res2 = containerRayCast (VectorAdd (%playerPos, "0 0 2"), VectorAdd (%playerPos, "1 1  -6.8"), %mask);
		%res3 = containerRayCast (VectorAdd (%playerPos, "0 0 2"), VectorAdd (%playerPos, "-1 1 -6.8"), %mask);
		if (!isObject (getWord (%res0, 0)) && !isObject (getWord (%res1, 0)) && !isObject (getWord (%res2, 0)) && !isObject (getWord (%res3, 0)))
		{
			%range = round ((VectorLen (VectorSub (%playerPos, %sourceObject.originPoint)) / 2.65) * 6);
			if (isObject (%sourceClient.Player))
			{
				%sourceClient.Player.emote (winStarProjectile, 1);
			}
			if (!%sourceClientIsBot)
			{
				%sourceClient.play2D (rewardSound);
				commandToClient (%sourceClient, 'BottomPrint', "<bitmap:base/client/ui/ci/star>\c3 MID AIR KILL - " @ %client.getPlayerName () @ " " @ round (%playerVelocity) @ "MPH, " @ %range @ "ft!", 3);
			}
			commandToClient (%client, 'BottomPrint', "\c5 MID AIR\'d by " @ %sourceClient.getPlayerName () @ " - " @ round (%playerVelocity) @ "MPH, " @ %range @ "ft!", 3);
		}
	}
	if (isObject (%client.miniGame))
	{
		if (%sourceClient == %client)
		{
			%client.incScore (%client.miniGame.Points_KillSelf);
		}
		else if (%sourceClient == 0)
		{
			%client.incScore (%client.miniGame.Points_Die);
		}
		else 
		{
			if (!%sourceClientIsBot)
			{
				%sourceClient.incScore (%client.miniGame.Points_KillPlayer);
			}
			%client.incScore (%client.miniGame.Points_Die);
		}
	}
	%clientName = %client.getPlayerName ();
	if (isObject (%sourceClient))
	{
		%sourceClientName = %sourceClient.getPlayerName ();
	}
	else if (isObject (%sourceObject.sourceObject) && %sourceObject.sourceObject.getClassName () $= "AIPlayer")
	{
		%sourceClientName = %sourceObject.sourceObject.name;
	}
	else 
	{
		%sourceClientName = "";
	}
	%mg = %client.miniGame;
	if (isObject (%mg))
	{
		%mg.messageAllExcept (%client, 'MsgClientKilled', %message, %client.getPlayerName (), %sourceClientName);
		messageClient (%client, 'MsgYourDeath', %message, %client.getPlayerName (), %sourceClientName, %mg.RespawnTime);
		if (%mg.RespawnTime < 0)
		{
			commandToClient (%client, 'centerPrint', "", 1);
		}
		%mg.checkLastManStanding ();
	}
	else 
	{
		messageAllExcept (%client, -1, 'MsgClientKilled', %message, %client.getPlayerName (), %sourceClientName);
		messageClient (%client, 'MsgYourDeath', %message, %client.getPlayerName (), %sourceClientName, $Game::MinRespawnTime);
	}
}

function GameConnection::onDeath(%client, %sourceObject, %sourceClient, %damageType, %damLoc)
{
	//if (%client.miniGame != $DefaultMiniGame)
	//	return Parent::onDeath(%client, %sourceObject, %sourceClient, %damageType, %damLoc);
	if(!%client.inBBB || $BBB::Round::Phase !$= "Round")
	{
		return GameConnection_onDeath (%client, %sourceObject, %sourceClient, %damageType, %damLoc);
	}

	if (%sourceObject.sourceObject.isBot)
	{
		%sourceClientIsBot = 1;
		%sourceClient = %sourceObject.sourceObject;
	}

	%client.play2D(BBB_Death_Sound);

	%player = %client.player;

	if(isObject(%player))
	{
		if (isObject(%player.tempBrick))
		{
			%player.tempBrick.delete();
			%player.tempBrick = 0;
		}

		%player.changeDatablock(BBB_Standard_Corpse_Armor); //Doing this before client is nullified is important for appearance stuff
		%player.playDeathAnimation(); //...still call this because datablock switch fucks with anims

		%player.BBB_ApplyOutfit($BBB::Outfit);
		%player.displayName = "an Unidentified Body";
		%player.setShapeName("Unidentified Body", 8564862);
		%player.setShapeNameDistance(13);
		%player.setShapeNameColor("1 1 0");
		%player.isBody = true;
		%player.client = 0;
		%player.origClient = %client;
		%player.credits = %client.credits;

		// BBB Corpse data
		%player.name = %client.fakeName;
		%player.role = %client.role;
		%player.deadTime = getSimTime();
		%player.fingerPrints = %sourceClient.player;
		if(%player.fingerPrints $= "")
		{
			%player.fingerPrints = %sourceClient.corpse;
		}
		
		%s = "???";

		if(%damageType == $DamageType::Suicide)
			%s = "Suicide";
		else if(%damageType == $DamageType::Fall)
			%s = "Broken leg";
		else if(%item = %sourceObject.sourceItem)
		{
			%s = %item.uiname;	
		}
		else if(%sourceObject.getClassName() $= "GameConnection")
		{
			%sourceplayer = %sourceObject.player;
			%item = %sourceplayer.tool[%sourceplayer.currTool];
			%s = %item.uiname;
		}

		%player.SOD = %s;//getWord(getTaggedString($DeathMessage_Murder[%damageType]), 1);
		if(%player.deadTime - %player.lastMsgTime < 2000)
			%player.lastWords = %player.lastMsg;
		%player.unIDed = true;

		if (!isObject(CorpseGroup))
			new SimSet(CorpseGroup);

		CorpseGroup.add(%player);
	}
	else
		warn("WARNING: No player object in GameConnection::onDeath() for client '" @ %client @ "'");

	if (isObject(%client.camera) || isObject(%player))
	{ // this part of the code isn't accurate
		if (%client.getControlObject() != %client.camera)
		{
			%client.camera.setMode("Corpse", %player);
			%client.camera.setControlObject(0);
			if(%client.getControlObject().getDatablock().getName() !$= "BillboardLoadingCamera")
	  		{
				%client.setControlObject(%client.camera);
			}
		}
	}

	%client.player = 0;

	%client.corpse = %player;
	
	%client.tpschecked = false;

	if ($Damage::Direct[$damageType] && getSimTime() - %player.lastDirectDamageTime < 100 && %player.lastDirectDamageType !$= "")
		%damageType = %player.lastDirectDamageType;

	if (%damageType == $DamageType::Impact && isObject(%player.lastPusher) && getSimTime() - %player.lastPushTime <= 1000)
		%sourceClient = %player.lastPusher;

	%message = '%2 killed %1';

	if (%sourceClient == %client || %sourceClient == 0)
	{
		%message = $DeathMessage_Suicide[%damageType];
		%client.corpse.suicide = true;
	}
	else
		%message = $DeathMessage_Murder[%damageType];

	// removed mid-air kills code here
	// removed mini-game kill points here

	if(%client.fakeName $= %client.getPlayerName())
	{
		%clientName = %client.getPlayerName();
	}
	else
	{
		%clientName = %client.fakeName@"/"@%client.getPlayerName();
	}
	

	if (isObject(%sourceClient))
		if(%client.fakeName $= %client.getPlayerName())
		{
			%sourceClientName = %sourceClient.getPlayerName();
		}
		else
		{
			%sourceClientName = %sourceClient.fakeName@"/"@%sourceClient.getPlayerName();
		}
	else if (isObject(%sourceObject.sourceObject) && %sourceObject.sourceObject.getClassName() $= "AIPlayer")
		%sourceClientName = %sourceObject.sourceObject.name;
	else
		%sourceClientName = "";

	%client.print = "<just:left><font:Palatino Linotype:22><font:Palatino Linotype:45><color:808080>D<font:Palatino Linotype:43><color:808080>EAD";

	%hsv = rgb2hsv(%sourceClient.role.data.color);
	%sourcecolor = hsv2rgb(getWord(%hsv,0)+15,getWord(%hsv,1)*0.75,getWord(%hsv,2));
	%sourcerolename = %sourceClient.role.data.name;
	%client.chatMessage("<font:Palatino Linotype:22><color:494949>You were killed by <color:" @ %sourcecolor @ ">" 
	@ %sourceClientName @ ", a(n)" SPC %sourcerolename @ "<color:494949>.");

	//deathlogs//

	if($BBB::Round::Phase $= "Round")
	{
		%hsv = rgb2hsv(%client.role.data.color);
		%clientcolor = hsv2rgb(getWord(%hsv,0)+15,getWord(%hsv,1)*0.75,getWord(%hsv,2));
		%clientrolename = %client.role.data.name;

		%dlmsg = "<color:" @ %sourcecolor @ ">" @ %sourceClientName  SPC "(" @ %sourcerolename
		@ ") \c6killed<color:" @ %clientcolor @ ">" SPC %clientname SPC "(" @ %clientrolename @ ")\c6 at" SPC getStringFromTime($BBB::bTimeLeft) SPC "("@getStringFromTime($BBB::rTimeLeft)@")";

		if(!isObject(%sourceclient))
			%dlmsg = "<color:" @ %clientcolor @ ">" @ %clientname SPC "(" @  %clientrolename @ ")\c6" SPC "died.";
		
		if(%sourceclient == %client)
			%dlmsg = "<color:" @ %clientcolor @ ">" @ %clientname SPC "(" @  %clientrolename @ ")\c6" SPC "suicided.";

		echo("\c4" SPC %sourceClientName SPC "(" @ %sourcerolename @ ") killed" SPC (%client == %sourceClient ? "themselves" : %clientName @ "(" @ %clientrolename @ ")"));

		$DeathLog[$DeathLogCount] = %dlmsg;
		$DeathLogCount++;
		if(%sourceClient.killLogCount $= "")
			%sourceClient.killLogCount = 0;
		
		if(%clientrolename $= "Detective")
			%cr = "Innocent";
		if(%sourcerolename $= "Detective")
			%scr = "Innocent";

		if(%sourceClient.killLogCount $= "")
			%sourceClient.killLogCount = 0;
		%sourceClient.killLog[%sourceClient.killLogCount] = %dlmsg TAB (%scr $= %cr);
		%sourceClient.killLogCount++;

		if(%client.killLogCount $= "")
			%client.killLogCount = 0;
		%client.killLog[%client.killLogCount] = %dlmsg TAB (%scr $= %cr);
		%client.killLogCount++;

		for(%i = 0; %i < ClientGroup.getCount(); %i++)
		{
			%cc = ClientGroup.getObject(%i);
			if(%cc.isAdmin)
			{
				if(%cc.getDamagePercent >= 1.0 || !%cc.inBBB)
				{
					chatMessage(%cc, '', %dlmsg);
				}
			}
		}
	}

	%sourceClient.role.StartCallback("OnKill",%sourceClient,%client);
	// removed mini-game checks here
	// removed death message print here
	// removed %message and %sourceClientName arguments
	messageClient(%client, 'MsgYourDeath', '', %clientName, '', %client.miniGame.respawnTime);
	//commandToClient(%client, 'CenterPrint', "", 1);
	BBB_Minigame.doWinCheck();
}

function ServerCmdDropTool (%client, %position)
{
	%player = %client.Player;
	if (!isObject (%player))
	{
		return;
	}

	%image = %player.tool[%position].image;
	if(isObject(%image) && %player.getPendingImage(0) == %player.tool[%position].image.getID())
	{
		%player.unmountImage(0);
	}

	%item = %player.tool[%position];
	if (isObject (%item))
	{
		if (%item.canDrop == 1)
		{
			%zScale = getWord (%player.getScale (), 2);
			%muzzlepoint = VectorAdd (%player.getPosition (), "0 0" SPC 1.5 * %zScale);
			%muzzlevector = %player.getEyeVector ();
			%muzzlepoint = VectorAdd (%muzzlepoint, %muzzlevector);
			%playerRot = rotFromTransform (%player.getTransform ());
			%thrownItem = new Item ("")
			{
				dataBlock = %item;
			};
			%thrownItem.setScale (%player.getScale ());
			MissionCleanup.add (%thrownItem);
			%thrownItem.setTransform (%muzzlepoint @ " " @ %playerRot);
			%thrownItem.setVelocity (VectorScale (%muzzlevector, 20 * %zScale));
			%thrownItem.schedulePop ();
			%thrownItem.miniGame = %client.miniGame;
			%thrownItem.bl_id = %client.getBLID ();
			%thrownItem.setCollisionTimeout (%player);
			if (%item.className $= "Weapon")
			{
				%player.weaponCount -= 1;
			}
			%player.tool[%position] = 0;
			messageClient (%client, 'MsgItemPickup', '', %position, 0);
			if (%player.getMountedImage (%item.image.mountPoint) > 0)
			{
				if (%player.getMountedImage (%item.image.mountPoint).getId () == %item.image.getId ())
				{
					%player.unmountImage (%item.image.mountPoint);
				}
			}
		}
	}
}

package BBB_GameConnection
{
	function GameConnection::unmountAllHats(%cl) {
		for(%i = 0; %i < 4; %i++)
			%cl.dismountHat(%i);
		
		if(isObject(%pl = %cl.player))
			cancel(%pl.hvisloop);
}

	function GameConnection::applyBodyParts(%client)
	{
		if(!%client.inBBB || !$BBB::Round::Active)
			return parent::applyBodyParts(%client);
		else
		{
			if(isObject(%player = %client.player))// && isObject(%image = %player.getMountedImage(2)) && %image.hatName !$= "")
			{
				if(isObject(%bot = %player.hatBot))
				{
					for(%i = 0; %i < 4; %i++)
					{
						if(isObject(%image = %bot.getMountedImage(%i)))
						{
							if ($HatMod::Nodes::showHat[%image.hatName] == false)
							{
								for(%u=0; $hat[%u] !$= ""; %u++)
									%player.hideNode($hat[%u]);
								
								for(%u=0; $accent[%u] !$= ""; %u++)
									%player.hideNode($accent[%u]);
							}
							
							if ((%list = $HatMod::Nodes::hiddenNodes[%image.hatName]) !$= "")
							{
								for (%u=0; getWord(%list,%u) !$= ""; %u++)
								{
									%player.hideNode(getWord(%list,%u));
								}
							}
						}
					}
				}
			}
		}
	}

	function GameConnection::applyBodyColors(%client)
	{
		if(!%client.inBBB || !$BBB::Round::Active)
			return parent::applyBodyColors(%client);
	}

	function GameConnection::onClientEnterGame(%client)
	{
		parent::onClientEnterGame(%client);
		if(%client.isSuperAdmin)
			%client.icon = "\c2[<color:FF7744>S-ADMIN\c2]";
		else if(%client.isAdmin)
			%client.icon = "\c2[<color:FF7744>ADMIN\c2]";
	}

	function GameConnection::spawnPlayer(%this)
	{
		// %mini = getMiniGameFromObject(%this);
		// if(%mini.isBBB)
		// {
			// if($BBB::Round::Phase $= "Round" || $BBB::Round::Phase $= "PostRound")
				// return;
		// }
		if(isObject(%this.corpse))
			%this.corpse.delete();
		Parent::spawnPlayer(%this);
		if($BBB::Round::Active)
			%this.player.BBB_applyOutfit($BBB::Outfit);
	}

	// function GameConnection::startTalking(%this)
	// {
		// %obj = %this.player;
		// if(%obj.corpse)
			// return;
		// Parent::startTalking(%this);
	// }
};
activatePackage(BBB_GameConnection);

// =================================================
// 5. MinigameSO
// =================================================
package BBB_MinigameSO
{
	function MinigameSO::addMember(%this, %client)
	{
		parent::addMember(%this, %client);

		if(!%this.isBBB)
			return;

		%client.inBBB = true;

		if(!%client.inBBB || $BBB::Round::Phase !$= "Round")
			%client.print = $BBB::GlobalPrint;
		else
			%client.print = "<just:left><font:Palatino Linotype:22><font:Palatino Linotype:45>\c3S<font:Palatino Linotype:43>\c3PECTATING";

		if(!$BBB::Round::Active || $BBB::Round::Active $= "" )
		{
			if(%this.numMembers > 1)
				BBB_Minigame.roundSetup();
			else
				BBB_Minigame.cleanUp();
		}
	}

	function MinigameSO::checkLastManStanding(%this)
	{
		if(%this.isBBB)
			return;
		parent::checkLastManStanding(%this, %client);
	}

    function MinigameSO::removeMember(%this, %client)
    {
        parent::removeMember(%this, %client);

        if(!%this.isBBB)
            return;

        if($BBB::Round::Active)
        {
            %playerCount = BBB_Minigame.numMembers;

            if(%this.numMembers < 2)
                BBB_Minigame.cleanUp();
            else
            {
                if($BBB::Round::Phase $= "Round")
                    BBB_Minigame.doWinCheck();
            }
        }
    }

	// Credit jes00 - https://forum.blockland.us/index.php?topic=243057.0#post_ClearItemsOnReset
	function MiniGameSO::reset(%mini, %client)
	{
		parent::reset(%mini, %client);
		if(!%mini.isBBB)
			return;

		%count = MissionCleanup.getCount() - 1;

		if(%count >= 0)
		{
			for(%i = %count; %i >= 0; %i--)
			{
				%obj = MissionCleanup.getObject(%i);

				if(%obj.getClassName() $= "Item" && %obj.miniGame == %mini)
				{
					%obj.delete();
				}
			}
		}
	}
};
activatePackage(BBB_MinigameSO);

// =================================================
// 6. Player
// =================================================
package BBB_Player
{
	function Player::setHats(%pl)
	{
		if(!isObject(%cc = $BBB::Outfit::Owner))
			return;
		if(isObject(%cl = %pl.client) && isObject(%pl))
		{
			if(!%pl.isCorpse)
				%cl.unMountAllHats();

			for(%i=0;%i<4;%i++){
				%img = %cc.superhat[%i];
				if(isHat(%img))
					%pl.mountHat(%img,%i);
			}

			if(!%pl.isCorpse)
			{
				%pl.ghostingHatBot = true;
				%pl.hatVisLoop();
			}
			
			%cl.applyBodyParts();
			%cl.applyBodyColors();
		}

		return;
	}
	// Some corpse code taken from MARBLE MAN's corpse mod.
	// function Player::activateStuff(%this)
	// {
		// Parent::activateStuff(%this);

		// %client = %this.client;

		// if(isObject(%this.getMountedObject(1)))
			// return Parent::activateStuff(%this);

		// %mounted = %this.getMountedObject(0);
		// if(isObject(%mounted) && %mounted.corpse)
		// {
		  // %mounted.dismount();
		  // %mounted.addVelocity(vectorScale(%this.getEyeVector(),10));
		  // %this.playThread(3,"root");
		// }
		// else
		// {
			// %start = %this.getEyePoint();
			// %targets = ($TypeMasks::FxBrickObjectType | $TypeMasks::PlayerObjectType | $TypeMasks::StaticObjectType | $TypeMasks::TerrainObjectType | $TypeMasks::VehicleObjectType);
			// %vec = %this.getEyeVector();
			// %end = vectorAdd(%start,vectorScale(%vec,10));
			// %ray = containerRaycast(%start,%end,%targets,%this);
			// %col = firstWord(%ray);
			// if(!isObject(%col))
				// return Parent::activateStuff(%this);
			// if(!%col.corpse)
				// return Parent::activateStuff(%this);
			// At this point we know its a corpse.
			// Double click to pick up, 1000ms window.
			// if(getSimTime() - %col.lastClicked[%this] < 250)
			// {
				// cancel(%client.corpseClickSched);
				// %this.mountObject(%col,0);
				// %col.setTransform("0 0 0 0 0 -1 -1.5709");
				// %this.playThread(3,"ArmReadyBoth");

			// }
			// else // Otherwise, let's print the corpse data.
			// {
				// cancel(%client.corpseClickSched);
				// %client.corpseClickSched = schedule(250, %client, "BBB_PrintCorpseData", %client, %col);
				// %time = getSimTime() - %col.deadTime;
				// %minutes = mFloor(%time / 60000);
				// %seconds = mFloor((%time - %minutes * 60000) / 1000);
				// if(%seconds < 10)
					// %seconds = "0" @ %seconds;
				// messageClient(%client, '', "\c6[\c4Corpse Data\c6]");
				// messageClient(%client, '', "\c6 - \c3Cause of death\c6: [" @ %col.SOD @ "\c6]");
				// messageClient(%client, '', "\c6 - \c3Dead for\c6: " @ %minutes @ ":" @ %seconds);
			// }
			// %col.lastClicked[%this] = getSimTime();
		// }
	// }

	function Player::mountImage(%this, %image, %slot, %loaded, %skintag)
	{
		parent::mountImage(%this, %image, %slot, %loaded, %skintag);
		if (%slot == 0 && isObject(%this.heldCorpse))
			%this.throwCorpse();
	}

	function Player::removeBody(%this)
	{
		if(%this.isBody)
			return;

		if (isObject(%this.origClient))
			%this.origClient.corpse = "";
		if (isObject(%this.holder))
		{
			%this.holder.heldCorpse = "";
			%this.holder.playThread(2, "root");
		}
		return Parent::removeBody(%this);
	}
		// parent::RemoveBody(%this);
	// }


};
activatePackage(BBB_Player);


// =================================================
// 7. ServerCMD
// =================================================
package BBB_ServerCMD
{
	function ServerLoadSaveFile_End()
	{
		Parent::ServerLoadSaveFile_End();
		if(BBB_Minigame.numMembers > 1)
			BBB_Minigame.roundSetup();
		else
			BBB_Minigame.cleanUp();
	}

	function serverCmdMessageSent(%client, %msg)
	{
		if(!$BBB::Round::Active)
			return parent::serverCmdMessageSent(%client, %msg);

		if(%msg $= "")
		{
			return;
		}

		%msg = trim(StripMLControlChars(%msg));

		%msg = strreplace(%msg, "https://", "http://");

		if(strpos(%msg, "http://") == 0)
		{
			%msg = getSubStr(%msg, 7, strlen(%msg) - 7);
			%link = firstWord(%msg);
			%rest = restWords(%msg);

			%msg = trim("<a:" @ %link @ ">" @ %link @ "</a>" SPC %rest);
		}

		%mg = BBB_Minigame;
		// Regular Chat
		if(isObject(%client.player) && isObject(getMiniGameFromObject(%client)))
		{
			%defaultcolor = %client.minigame.roleGroup.defaultChatColor;
			%group = ClientGroup.getId();
			%count = %group.getCount();
			for(%i = 0; %i < %count; %i++)
			{
				%targetClient = %group.getObject(%i);
				if($BBB::Round::Phase !$= "Round")
				{
					%icon = %client.Icon;
					%color = %defaultcolor;
					%name = %client.getPlayerName();

					if(%client.fakename !$= "" && %client.fakename !$= %client.getPlayerName())
					{
						%name = %client.getPlayerName() SPC "("@%client.fakename@")";
					}
				}
				else
				{
					%color = %client.nameColor[%targetClient];
					%name = %client.fakename;

					if(%name $= "")
					{
						%name = %client.getPlayerName();
					}
				}
				

				chatMessageClient(%targetClient, %client,'','' ,'%6%5%2\c6: %4', %client.clanPrefix, %name, 
				%client.clanSuffix, %msg, "<color:"@%color@">", %icon, %a7, %a8, %a9, %a10);
				%targetClient.play2D(BBB_Chat_Sound);
			}
			%client.player.lastMsg = %msg;
			%client.player.lastMsgTime = getSimTime();
		}
		else // Dead Chat
		{
			%name = %client.getPlayerName();
			if(%client.fakeName !$= "" && %client.fakename !$= %client.getPlayerName())
			{
				%name = %name SPC "("@%client.fakeName@")";
			}

			%type = "\c6[" @ (%client.hasSpawnedOnce == 1 ? "DEAD" : "LOADING") @ "]";
			if($BBB::Round::Phase !$= "Round")
			{
				chatMessageAll (%client, '%5%6\c4%2<color:DDDDDD>: %4', %client.clanPrefix, %name, 
				%client.clanSuffix, %msg, %type, %client.icon, %a7, %a8, %a9, %a10);
				//messageAll("", %send);
				%mg.playGlobalSound(BBB_Chat_Sound);
			}
			else
			{
				for(%i = 0; %i < ClientGroup.getCount(); %i++)
				{
					%tarClient = ClientGroup.getObject(%i);
					%player = %tarClient.player;
					if(!isObject(%player))
					{
						chatMessageClient (%tarClient, %client,'','' ,'%5%6\c4%2<color:DDDDDD>: %4', %client.clanPrefix, 
						%name, %client.clanSuffix, %msg, %type, %client.icon, %a7, %a8, %a9, %a10);
						//messageClient(%tarClient, "", %send);
						%tarClient.play2D(BBB_Chat_Sound);
					}
				}
			}
		}

		echo(%client.getPlayerName() @ ": " @ %msg);
	}

	function serverCmdTeamMessageSent(%client, %msg)
	{
		%minigame = getMiniGameFromObject(%client);
		if(!%client.inBBB)
			return parent::serverCmdTeamMessageSent(%client, %msg);

		if($BBB::Round::Phase !$= "Round")
			return parent::serverCmdMessageSent(%client, %msg);

		%obj = %client.player;

		if(!isObject(%obj))
		{
			serverCmdMessageSent(%client, %msg);
			return;
		}

		if(!%client.role.data.rolechat)
		{
			serverCmdMessageSent(%client, %msg);
			return;
		}

		if(%msg $= "")
		{
			return;
		}

		%msg = StripMLControlChars(%msg);

		%msg = strreplace(%msg, "https://", "http://");

		if(strpos(%msg, "http://") == 0)
		{
			%msg = getSubStr(%msg, 7, strlen(%msg) - 7);
			%link = firstWord(%msg);
			%rest = restWords(%msg);

			%msg = trim("<a:" @ %link @ ">" @ %link @ "</a>" SPC %rest);
		}

		%color = %client.role.data.color;
		%hsv = rgb2hsv(%color);
		%s = "<color:"@hsv2rgb(getWord(%hsv,0),getWord(%hsv,1)* 0.2,getWord(%hsv,2))@">";
		%t = "<color:"@%color@">";
		%teamname = %client.role.data.name;
		%team = %minigame.activeRoleGroup.WithRole(%teamname);
		%count = getWordCount(%team);
		%type = "\c7[" @ %t @ %teamname @ "\c7] ";
		for(%i = 0; %i < %count; %i++)
		{
			%teamclient = getWord(%team,%i);
			chatMessageClient (%teamclient, %client,'','' , '%5\c4%2%7: %4', %client.clanPrefix, %client.fakeName, 
			%client.clanSuffix, %msg, %type, %client.icon, %s, %a8, %a9, %a10);
			%teamclient.play2D(BBB_Chat_Sound);
		}
	}

	function serverCmdShiftBrick(%this, %x, %y, %z)
	{
		if(%this.inBBB && isObject(%this.player) && $Sim::Time - %this.player.lastBBBmsg > 1)
		{
			%target = %this.player.BBB_TargetAPlayer();
			%this.player.lastBBBmsg = $Sim::Time;
			if(%z == -1) // numpad 1
				serverCmdMessageSent(%this, "Yes.");
			if(%x == -1) // numpad 2
				serverCmdMessageSent(%this, "No.");
			if(%z == 1) // numpad 3
				serverCmdMessageSent(%this, "Help!");
			if(%y == 1) // numpad 4
				serverCmdMessageSent(%this, "I'm with" SPC %target @ ".");
			if(%z == -3) // numpad 5
				serverCmdMessageSent(%this, "I see" SPC %target @ ".");
			if(%y == -1) // numpad 6
				serverCmdMessageSent(%this, %target SPC "acts suspicious.");
			if(%x == 1) // numpad 8
				serverCmdMessageSent(%this, %target SPC "is innocent.");
			return;
		}
		parent::serverCmdShiftBrick(%this, %x, %y, %z);
	}
	function serverCmdStartTalking(%client)
	{
		if($BBB::Round::Phase !$= "Round")
			Parent::serverCmdStartTalking(%client);
	}

	function serverCmdRotateBrick(%this, %dir)
	{
		if(%this.inBBB && isObject(%this.player) && $Sim::Time - %this.player.lastBBBmsg > 1)
		{
			%target = %this.player.BBB_TargetAPlayer();
			%this.player.lastBBBmsg = $Sim::Time;
			if(%dir == -1) // numpad 7
				serverCmdMessageSent(%this, %target SPC "is a Traitor!");
			if(%dir == 1) // numpad 9
				serverCmdMessageSent(%this, "Anyone still alive?");
			return;
		}
		parent::serverCmdRotateBrick(%this, %dir);
	}

	// For some reason this doesn't work properly??
	// function serverCmdUnUseTool(%client)
	// {
		// parent::serverCmdUnUseTool(%client);

		// if(isObject(%obj = %client.player))
			// %obj.unMountImage(0);

	// }
};
activatePackage(BBB_ServerCMD);

function ProjectileData::Damage(%this, %obj, %col, %fade, %pos, %normal)
{
	if (%this.directDamage <= 0)
	{
		return;
	}
	%damageType = $DamageType::Direct;
	if (%this.DirectDamageType)
	{
		%damageType = %this.DirectDamageType;
	}
	%scale = getWord(%obj.getScale(), 2);
	%directDamage = %this.directDamage * %scale;
	if (%col.getType() & $TypeMasks::PlayerObjectType)
	{
		%col.Damage(%obj, %pos, %directDamage, %damageType);
	}
	else
	{
		%col.Damage(%obj, %pos, %directDamage, %damageType);
	}
}
