
//////////
// item //
//////////
datablock ItemData(SilencedGunItem : GunItem)
{
	//gui stuff
	uiName = "Silenced Gun";

	doColorShift = true;
	colorShiftColor = "0.35 0.35 0.35 1.000";

	 // Dynamic properties defined by the scripts
	image = silencedGunImage;
};

////////////////
//weapon image//
////////////////
datablock ShapeBaseImageData(silencedGunImage : gunImage)
{


   doColorShift = true;
   colorShiftColor = SilencedGunItem.colorShiftColor;//"0.400 0.196 0 1.000";

   TTT_Contraband = true;

   //casing = " ";

   // Images have a state system which controls how the animations
   // are run, which sounds are played, script callbacks, etc. This
   // state system is downloaded to the client so that clients can
   // predict state changes and animate accordingly.  The following
   // system supports basic ready->fire->reload transitions as
   // well as a no-ammo->dryfire idle state.

   // Initial start up state
	stateName[0]                     = "Activate";
	stateTimeoutValue[0]             = 0.15;
	stateTransitionOnTimeout[0]       = "Ready";
	stateSound[0]					= weaponSwitchSound;

	stateName[1]                     = "Ready";
	stateTransitionOnTriggerDown[1]  = "Fire";
	stateAllowImageChange[1]         = true;
	stateSequence[1]	= "Ready";

	stateName[2]                    = "Fire";
	stateTransitionOnTimeout[2]     = "Smoke";
	stateTimeoutValue[2]            = 0.28;
	stateFire[2]                    = true;
	stateAllowImageChange[2]        = false;
	stateSequence[2]                = "Fire";
	stateScript[2]                  = "onFire";
	stateWaitForTimeout[2]			= true;
	stateEmitter[2]					= gunFlashEmitter;
	stateEmitterTime[2]				= 0.05;
	stateEmitterNode[2]				= "muzzleNode";
	stateSound[2]					= "";
	stateEjectShell[2]       = true;

	stateName[3] = "Smoke";
	stateEmitter[3]					= gunSmokeEmitter;
	stateEmitterTime[3]				= 0.05;
	stateEmitterNode[3]				= "muzzleNode";
	stateTimeoutValue[3]            = 0.01;
	stateTransitionOnTimeout[3]     = "Reload";

	stateName[4]			= "Reload";
	stateSequence[4]                = "Reload";
	stateTransitionOnTriggerUp[4]     = "Ready";
	stateSequence[4]	= "Ready";

};

function silencedGunImage::onFire(%this,%obj,%slot)
{
	if(%obj.getDamagePercent() < 1.0)
		%obj.playThread(2, shiftAway);
	Parent::onFire(%this,%obj,%slot);	
}
