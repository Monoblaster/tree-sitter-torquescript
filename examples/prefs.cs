$Pref::RBloodMod::LimbReddening = false;
$Pref::BHole::FadeDelay = 250; // < i fucked up the defaults for these
$Pref::BHole::OpacityR = 0.1;  // < ^
$Pref::BHole::ColorShiftToBrick = true;
$Pref::BHole::Restrictions = false;
$Pref::AEBase::HUDPos = 3; 
$Pref::AEBase::HUD = 9;
$Pref::AEBase::laserBlind = 0.3;
$Pref::AEBase::flashlightBlind = 0.3;
$Pref::AEBase::playerRecoilMult = 0;
$Blood::MinDecalAmt = 1;
$Blood::MaxDecalAmt = 10;
$Blood::DeathDecalAmt = 20;
trap_healthImage.healthteam = true;
grenade_remoteImage.maxActive = 8;
$pref::aebase::projectilesas = 4;
function Player::WeaponAmmoPrint(%pl, %cl, %idx, %sit)
{
	return;
}

function grenade_dynamiteImage::onFire(%this, %obj, %slot)
{
	%obj.stopAudio(2);
	%obj.playThread(2, shiftDown);
	%obj.weaponAmmoUse();
	serverPlay3D(grenade_throwSound, %obj.getMuzzlePoint(%slot));
	%projs = ProjectileFire(%this.Projectile, %obj.getMuzzlePoint(%slot), %obj.getMuzzleVector(%slot), 0, 1, %slot, %obj, %obj.client);
	for(%i = 0; %i < getFieldCount(%projs); %i++)
	{
		%proj = getField(%projs, %i);
		%proj.cookDeath = %proj.schedule((%proj.getDatablock().lifeTime * 32) - (getSimTime() - %obj.chargeStartTime[%this]), FuseExplode);
	}

	%obj.chargeStartTime[%this] = "";
}

//disable weapon ammo pickups
package aeAmmo
{
	function Armor::onCollision(%this, %obj, %col, %vec, %speed)
	{
		return parent::onCollision(%this, %obj, %col, %vec, %speed);
	}
};

package ThrowingKnifePackage
{
	function Armor::onTrigger(%this, %player, %slot, %val)
	{
		if(%player.getMountedImage(0) $= ThrowingKnifeImage.getID() && %slot $= 4 && %val)
		{
			%projectile = ThrowingKnifeThrowProjectile;
			%vector = %player.getMuzzleVector(0);
			%objectVelocity = %player.getVelocity();
			%vector1 = VectorScale(%vector, %projectile.muzzleVelocity);
			%vector2 = VectorScale(%objectVelocity, %projectile.velInheritFactor);
			%velocity = VectorAdd(%vector1,%vector2);
			%p = new Projectile()
			{
				dataBlock = %projectile;
				initialVelocity = %velocity;
				initialPosition = %player.getMuzzlePoint(0);
				sourceObject = %player;
				sourceSlot = 0;
				client = %player.client;
		};
			%currSlot = %player.realCurrTool;
			%player.tool[%currSlot] = 0;
			%player.weaponCount--;
			messageClient(%player.client,'MsgItemPickup','',%currSlot,0);
			serverCmdUnUseTool(%player.client);
			%player.unmountImage(0);
			
			serverPlay3D(ThrowingKnifeSwingSound,%player.getPosition());
			%player.playthread("3","Activate");
			MissionCleanup.add(%p);
			return %p;
		}
		Parent::onTrigger(%this, %player, %slot, %val);
	}
};

trap_healthImage.TTT_notWeapon = true;
grenade_decoyImage.TTT_notWeapon = true;
grenade_stimImage.TTT_notWeapon = true;
grenade_smokeImage.TTT_notWeapon = true;

grenade_mollyImage.TTT_Contraband = true;
grenade_dynamiteImage.TTT_Contraband = true;
grenade_remoteImage.TTT_Contraband = true;
mine_proxyImage.TTT_Contraband = true;
mine_incendiaryImage.TTT_Contraband = true;
ThrowingKnifeImage.TTT_Contraband = true;

trap_healthItem.doColorShift = false; // OXY!!

function silenceWeaponEquip()
{
	%group = dataBlockGroup.getId();
	%count = %group.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%data = %group.getObject(%i);
		if(%data.stateSound[0] $= "" || striPos(%data.shapefile,"TierTacMelee") == -1)
		{
			continue;
		}
		%name = %data.getName();
		%newName = "Silenced"@%name;
		eval("datablock ShapeBaseImageData("@%newName@" : "@%name@"){silenced=true;stateSound[0]=\"\";};"@
		"function "@%newName@"::onFire(%this,%obj,%slot){return "@%name@"::onFire(%this,%obj,%slot);}"@
		"function "@%newName@"::onActivate(%this,%obj,%slot){return "@%name@"::onActivate(%this,%obj,%slot);}"@
		"function "@%newName@"::onPreFire(%this,%obj,%slot){return "@%name@"::onPreFire(%this,%obj,%slot);}"@
		"function "@%newName@"::onStabFire(%this,%obj,%slot){return "@%name@"::onStabFire(%this,%obj,%slot);}"@
		"function "@%newName@"::onCharge(%this,%obj,%slot){return "@%name@"::onCharge(%this,%obj,%slot);}"@
		"function "@%newName@"::TT_isRaycastCritical(%this,%obj,%slot,%col,%pos,%normal,%hit){return "@%name@"::TT_isRaycastCritical(%this,%obj,%slot,%col,%pos,%normal,%hit);}");
		%data.item.image = %newName;
	}
}
silenceWeaponEquip();

deactivatePackage("WeaponDropCharge");
function L4BIceAxeImage::onFire(%this, %obj, %slot)
{
	if(%obj.getDamagePercent() >= 1.0)
		return;

	%obj.playThread(2, shiftTo);

	if(getRandom(0,1))
	{
		%this.TT_raycastExplosionBrickSound = L4BMacheteHitSoundA;
	}
	else
	{
		%this.TT_raycastExplosionBrickSound = L4BMacheteHitSoundB;
	}

	Parent::onFire(%this, %obj, %slot);
}

function L4BIceAxeImage::onActivate(%this, %obj, %slot)
{
	%obj.playthread(2, plant);
}

function L4BIceAxeImage::onPreFire(%this, %obj, %slot)
{
	%obj.playthread(2, shiftAway);
}

function L4BIceAxeImage::TT_isRaycastCritical(%this,%obj,%slot,%col,%pos,%normal,%hit)
{
	if(!isObject(%col))
		return 0;
	
	return TT_isMeleeRaycastCrit(%this,%obj,%slot,%col,%pos,%normal,%hit) || (ae_calculateDamagePosition(%col, %pos) $= "head");
}

$DataInstance::FilePath = "Config/Server/DataInstance/TTT"; // to prevent ttt from nuking deathrace saves

function mineCanTrigger(%src, %col)
{
	if(!isObject(%src)) return 1;
	if(!isObject(%col)) return 0;

	if(isObject(%src.client))
		%src = %src.client;

	if(%col.getType() & $TypeMasks::VehicleObjectType)
	{
		if(isObject(%con = %col.getControllingObject()))
			%col = %con;
		else return minigameCanDamage(%src, %col) == 1;
	}

	if(isObject(%src.winCondition) && %src.winCondition.isMiskill(%col.client.winCondition))
		return 0;

	if((%mini = getMinigameFromObject(%src)) == getMinigameFromObject(%col) && %src != %col.client && %src != %col)
	{
		if(%mini.isSlayerMinigame)
		{
			%srcTeam = (isObject(%src) ? %src.getTeam() : 0);
			if(%col.IsA("GameConnection"))
				%colTeam = %col.getTeam();
			else
				%colTeam = (isObject(%col.client) ? %col.client.getTeam() : 0);

			if(%srcTeam == 0 || %colTeam == 0 || %srcTeam != %colTeam && !%srcTeam.isAlliedTeam(%colTeam))
				return minigameCanDamage(%src, %col) == 1;
		}
		else return minigameCanDamage(%src, %col) == 1;
	}
	
	return 0;
}

function BBB_ClosestPointOnLine(%p1,%p2,%point)
{
	%t = vectorDot(vectorSub(%p1,%p2),vectorSub(%p2,%point))/vectorDot(vectorSub(%p1,%p2),vectorSub(%p1,%p2));
	%g = vectorSub(%p2,vectorScale(vectorSub(%p1,%p2),%t));

	%d = vectorSub(%p1,%p2);
	%f = vectorSub(%g,%p2);
	%z = 0;
	for(%i = 0; %i < 3; %i++)
	{
		%a = getWord(%d,%i);
		%b = getWord(%f,%i);
		if(%a == 0 || %b == 0)
		{
			continue;
		}
		%z = %b / %a;
		break;
	}

	if(%z > 1)
	{
		return %p1;
	}
	else if(%z < 0)
	{
		return %p2;
	}
	else
	{
		return %g;
	}
}

function AESuppressArea(%pos, %dir, %shape, %img)
{
	%super = %img.whizzSupersonic;

	if(%shape.previousPoint $= "")
	{
		%shape.previousPoint = %shape.originPoint;
	}

	if(%shape.previousPoint $= %pos)
	{
		return;
	}

	if(%super == 0)
		%sfx = AESubsonicWhizz @ getRandom(1, 4) @ Sound;
	else if(%super == 1)
		%sfx = AESupersonicCrack @ getRandom(1, 4) @ Sound;
	else if(%super == 2)
		%sfx = AESupersonicBigCrack @ getRandom(1, 4) @ Sound;
	
	%angle = 1 - (%img.whizzAngle / 90);
	%chance = mClampF(%img.whizzChance / 100, 0, 1);
	%through = %img.whizzThrough;

	%sourcePlayer = %shape.sourceObject;
	%sourceClient = %sourcePlayer.client;

	for(%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%cc = ClientGroup.getObject(%i);

		%obj = %cc.getControlObject();

		if(!isObject(%obj) || %shape.suppressed[%obj])
			continue;
		
		%eye = %obj.getEyePoint();

		%linePoint = BBB_ClosestPointOnLine(%shape.previousPoint,%pos,%eye);
		%dist = vectorDist(%linePoint, %eye);
		%dot = vectorDot(%dir, vectorNormalize(vectorSub(%shape.previousPoint, %eye)));
		if(%dist < 6 && %dot <= %angle && getRandom() <= %chance)
		{
			if(%through || !isObject(containerRayCast(%linepoint, %eye, $TypeMasks::fxBrickObjectType | $TypeMasks::StaticObjectType)))
			{
				%shape.suppressed[%obj] = true;
				%cc.play3D(%sfx, %linePoint);
				if(isObject(%sourcePlayer))
				{
					Oopsies_DoVisibleEvent(%sourcePlayer,$ValidState::Criminal);
				}

				%p = new Projectile()
				{
					dataBlock = R_ShotgunRecoilProjectile;
					initialPosition = %eye;
				};

				%p.setScale("0.1 0.1 0.1");

				MissionCleanup.add(%p);

				%p.explode();
			}
		}
	}
	%shape.previousPoint = %pos;
}
