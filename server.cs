datablock AudioProfile(toolgun_Fire_1) {
	filename = "./toolgun_Fire_1.wav";
	description = AudioClose3d;
	preload = true;
};

datablock AudioProfile(toolgun_Fire_2 : toolgun_Fire_1) {
	filename = "./toolgun_Fire_2.wav";
};

if (isFile("Add-Ons/System_ReturnToBlockland/server.cs")) {
	if (!$RTB::RTBR_ServerControl_Hook)
		exec("Add-Ons/System_ReturnToBlockland/RTBR_ServerControl_Hook.cs");
	
	RTB_registerPref("Replace wrench on spawn", "Toolgun", "$Pref::Server::Toolgun::ReplaceWrench", "bool", "Tool_Toolgun", 1, false, false);
} else {
	if ($Pref::Server::Toolgun::ReplaceWrench $= "")
		$Pref::Server::Toolgun::ReplaceWrench = true;
}

$Toolgun::Modes = 2;
$Toolgun::HighlightMS = 250;

function determineNearestColor(%color) {
	for (%i = 0; %i < 64; %i++) {
		%colorID = getColorIDTable(%i);
		%dist = vectorDist(%colorID, %color);
		if (%dist < %best || %best $= "") {
			%best = %dist;
			
			%match = %i;
		}
	}
	
	return %match;
}

function FxDTSBrick::toolgunFinish(%this, %mode) {
	if (isObject(%this)) {
		switch (%mode) {
			case 1: // modify
				%this.setColor(%this.oldColorID);
				%this.setColorFx(%this.oldColorFxID);
				
				%this.oldColorID = "";
				%this.oldColorFxID = "";
				
				if (%this.getDatablock().specialBrickType $= "VehicleSpawn")
					%this.colorVehicle();
			case 2: // delete
				%this.delete();
			default:
				error("Invalid toolgun mode.");
		}
	}
}

function Player::updateToolgunStatus(%this) {
	if (!isObject(%this))
		return;
	
	%client = %this.client;
	
	switch (%this.toolgunMode) {
		case 1:
			%mode = "\c4Modify";
		case 2:
			%mode = "<color:FF0000>Delete";
	}
	
	%client.bottomPrint("<font:Impact:35>\c7Mode\c6: " @ %mode, 0, true);
}

function serverCmdToolgun(%client) {
	if (!isObject(%player = %client.player))
		return;
	
	if (isObject(%client.minigame) && !%client.isAdmin)
		return;
	
	%player.updateArm("toolgunImage");
	%player.mountImage("toolgunImage", 0);
	
	%player.toolgunMode = 1;
	%player.updateToolgunStatus();
	
	return %player;
}

function serverCmdTG(%client) { serverCmdToolgun(%client); }

function serverCmdDel(%client) { // for people who were used to the del launcher
	if (!isObject(%player = serverCmdToolgun(%client)))
		return;
	
	%player.toolgunMode = 2;
	%player.updateToolgunStatus();
}

datablock ItemData(toolgunItem : wrenchItem) {
	shapeFile 			= "./toolgun.dts";
	
	uiName 				= "Toolgun";
	iconName 			= "./Toolgun";
	
	doColorShift 		= false;
	
	image 				= toolgunImage;
};

datablock ShapeBaseImageData(toolgunImage : wrenchImage) {
	shapeFile 			= "./toolgun.dts";
	
	offset				= "-0.02 -0.015 -0.02";
	eyeOffset			= "0 0 0";
	
	doColorShift		= false;
	
	item				= toolgunItem;
};

function toolgunImage::onFire(%this, %obj, %slot) { // i should redo this part entirely at some point
	%eye = %obj.getEyePoint();
	%vec = vectorAdd(%eye, vectorScale(%obj.getEyeVector(), 500));
	%mask = $TypeMasks::FxBrickAlwaysObjectType | $TypeMasks::FxBrickObjectType;
	%ray = containerRayCast(%eye, %vec, %mask, %obj);
	%col = firstWord(%ray);
	
	%rand = getRandom(0, 1) ? 1 : 2;
	%obj.playAudio(%rand, "toolgun_Fire_" @ %rand);
	
	%obj.playThread(2, "shiftAway");
	
	if (isObject(%col)) {
		%client = %obj.client;
		
		switch (%obj.toolgunMode) { // it's a switch because i was/am planning to add more modes
			case 1: // modify
				%col.oldColorID = %col.getColorID();
				%col.oldColorFxID = %col.getColorFxID();
				
				wrenchImage.onHitObject(%obj, %slot, %col);
				
				%trustReq = 1;
				%color = "0.1 0.45 0.75 1";
			case 2: // delete
				%trustReq = 2;
				%color = "1 0 0 1";
			default:
				error("Invalid toolgun mode.");
		}
		
		%trustLvl = getTrustLevel(%col, %obj);
		
		if ((%trustLvl < %trustReq) && !%client.isAdmin) {
			%group = %col.getGroup();
			%brickgroup = %group.getName();
			%bl_id = %brickgroup.bl_id;
			
			if (%owner = findClientByBL_ID(%bl_id))
				%client.centerPrint(%owner.getPlayerName() @ " does not trust you enough to do that.", 3);
			else
				%client.centerPrint("\c1BL_ID: " @ %bl_id @ "\c0 does not trust you enough to do that.", 3);
			
			return;
		}

		%col.setColor(determineNearestColor(%color));
		%col.setColorFx(3);
		
		%col.schedule($Toolgun::HighlightMS, "toolgunFinish", %obj.toolgunMode);
	}
}

function toolgunImage::onStopFire(%this, %obj, %slot) { wrenchImage::onStopFire(%this, %obj, %slot); }

function toolgunImage::onMount(%this, %obj, %slot) {
	%obj.toolgunMode = 1; // will always default to modify mode
	
	%obj.updateToolgunStatus();
	
	Parent::onMount(%this, %obj, %slot);
}

function toolgunImage::onUnMount(%this, %obj, %slot) {
	%obj.client.bottomPrint("", 0, true);
	
	Parent::onUnMount(%this, %obj, %slot);
}

if (isPackage(toolgunPackage))
	deactivatePackage(toolgunPackage);

package toolgunPackage {
	
	function GameConnection::onDeath(%this) {
		if (isObject(%player = %this.player))
			if (%player.getMountedImage(0) == toolgunImage.getID())
				%this.bottomPrint("", 0, true);
		
		Parent::onDeath(%this);
	}
	
	function GameConnection::spawnPlayer(%this) {
		Parent::spawnPlayer(%this);
		
		if ($Pref::Server::Toolgun::ReplaceWrench) {
			if (isObject(%player = %this.player)) {
				%toolgunID = toolgunItem.getID();
				
				for (%i = 0; %i < %player.getDatablock().maxTools; %i++) {
					if (%player.tool[%i] == wrenchItem.getID()) {
						%player.tool[%i] = %toolgunID;
						messageClient(%this, 'MsgItemPickup', '', %i, %toolgunID);
					}
				}
			}
		}
	}
	
	function serverCmdLight(%client) {
		if (isObject(%player = %client.player)) {
			if (%player.getMountedImage(0) == toolgunImage.getID()) {
				%player.toolgunMode++;
				
				if (%player.toolgunMode > $Toolgun::Modes)
					%player.toolgunMode = 1;
				
				%player.updateToolgunStatus();
				
				%player.playThread(2, "shiftRight");
				%client.play2D(BrickChangeSound);
				
				return;
			}
		}
		
		Parent::serverCmdLight(%client);
	}
	
};

activatePackage(toolgunPackage);