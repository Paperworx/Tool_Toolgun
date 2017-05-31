if(!$Toolgun::Initialized) {
  datablock AudioProfile(toolgun_Fire_1) {
    filename = "Add-Ons/Tool_Toolgun/toolgun_Fire_1.wav";
    description = AudioClose3d;
    preload = true;
  };

  datablock AudioProfile(toolgun_Fire_2 : toolgun_Fire_1) {
    filename = "Add-Ons/Tool_Toolgun/toolgun_Fire_2.wav";
  };

  datablock ItemData(toolgunItem : wrenchItem) {
    shapeFile 		= "Add-Ons/Tool_Toolgun/toolgun.dts";
    
    uiName 				= "Toolgun";
    iconName 			= "Add-Ons/Tool_Toolgun/Toolgun";
    
    doColorShift 	= false;
    
    image 			  = toolgunImage;
  };

  datablock ShapeBaseImageData(toolgunImage : wrenchImage) {
    shapeFile 	  = "Add-Ons/Tool_Toolgun/toolgun.dts";
    
    offset			  = "-0.02 0 -0.02";
    eyeOffset		  = "0.5 0.6 -0.7";
    
    doColorShift  = false;
    
    stateTimeoutValue[3] = 0.18;
    
    item				  = toolgunItem;
  };
  
  if(isFile("Add-Ons/System_ReturnToBlockland/server.cs")) {
    if(!$RTB::RTBR_ServerControl_Hook)
      exec("Add-Ons/System_ReturnToBlockland/RTBR_ServerControl_Hook.cs");
    
    RTB_registerPref("Replace wrench on spawn", "Toolgun", "$Pref::Server::Toolgun::ReplaceWrench", "bool", "Tool_Toolgun", 1, false, false);
  } else {
    if($Pref::Server::Toolgun::ReplaceWrench $= "") {
      $Pref::Server::Toolgun::ReplaceWrench = true;
    }
  }
}

$Toolgun::HighlightMS = 250;
$Toolgun::Range = 500; // torque units

function determineNearestColor(%color) {
	for(%i = 0; %i < 64; %i++) {
		%colorID = getColorIDTable(%i);
		%dist = vectorDist(%colorID, %color);
		if(%dist < %best || %best $= "") {
			%best = %dist;
			
			%match = %i;
		}
	}
	
	return %match;
}

function FxDTSBrick::onToolgunInteract(%brick, %client) {
  $InputTarget_["Self"] = %brick;
  $InputTarget_["Player"] = %client.player;
  $InputTarget_["Client"] = %client;

  if($Server::LAN) {
    $InputTarget_["MiniGame"] = getMiniGameFromObject(%client);
  } else {
    if(getMiniGameFromObject(%brick) == getMiniGameFromObject(%client)) {
      $InputTarget_["MiniGame"] = getMiniGameFromObject(%brick);
    } else {
      $InputTarget_["MiniGame"] = 0;
    }
  }
  
  %brick.processInputEvent("onToolgunInteract", %client);
}

if(!$Toolgun::Initialized)
  registerInputEvent("FxDTSBrick", "onToolgunInteract", "Self FxDTSBrick" TAB "Player Player" TAB "Client GameConnection" TAB "MiniGame MiniGame");

function FxDTSBrick::toolgunFinish(%brick, %mode) {
	if(isObject(%brick)) {
    if(%mode != 1) {
      %brick.setColor(%brick.oldColorID);
      %brick.setColorFx(%brick.oldColorFxID);
      
      %brick.oldColorID = "";
      %brick.oldColorFxID = "";
      
      if(%brick.getDatablock().specialBrickType $= "VehicleSpawn") {
        %brick.colorVehicle();
      }
    } else {
      %brick.delete();
    }
	}
}

function Player::isUsingToolgun(%player) {
  if(isObject(%player) && %player.getMountedImage(0) == toolgunImage.getID())
    return true;
  
  return false;
}

function Player::updateToolgunStatus(%player) {
	if(!isObject(%player))
		return;
	
	%client = %player.client;

  switch(%player.toolgunMode) {
    case 0:
      %mode = "\c4Modify";
    case 1:
      %mode = "<color:FF0000>Delete";
    case 2:
      %mode = "\c5Print";
    case 3:
      %mode = "\c3Interact";
  }
	
  %title = "<font:Arial Bold:26>\c6Toolgun<br>";
  %one = "\c6Mode: " @ %mode @ " \c6[Light]<br>";
  %two = "\c6Type: " @ (%client.toolgunType ? "\c1Extended" : "\c2Simple") @ " \c6[Prev Seat]<br>";
  %three = "\c6Safety: " @ (%player.toolgunSafety ? "\c2ON" : "<color:FF0000>OFF") @ " \c6[Next Seat]<br>";
  
	%client.bottomPrint(%title @ "<font:Arial:18>" @ %one @ %two @ %three, 0, true);
}

function Player::changeToolgunStatus(%player, %change) {
  if(!isObject(%player))
    return;
  
  %client = %player.client;
  
  switch$(%change) {
    case "mode":
      %player.toolgunMode++;
    case "type":
      %client.toolgunType = !%client.toolgunType;
    case "safety":
      %player.toolgunSafety = !%player.toolgunSafety;
  }
  
  if(!%client.toolgunType) {
    if(%player.toolgunMode > 1) {
      %player.toolgunMode = 0;
    }
  } else {
    if(%player.toolgunMode > 3) {
      %player.toolgunMode = 0;
    }
  }
  
  %player.playThread(2, "shiftRight");
  
  if(isObject("Beep_Key_Sound"))
    %client.play2D("Beep_Key_Sound");
  else
    %client.play2D("BrickChangeSound");
  
  %player.updateToolgunStatus();
}

function serverCmdToolgun(%client) {
	if(!isObject(%player = %client.player))
		return;
	
	if(isObject(%client.minigame) && !%client.isAdmin)
		return;
	
	%player.updateArm("toolgunImage");
	%player.mountImage("toolgunImage", 0);
	
	%player.toolgunMode = 1;
	%player.updateToolgunStatus();
	
	return %player;
}

function serverCmdTG(%client) { serverCmdToolgun(%client); }

function serverCmdMod(%client) {
	if(!isObject(%player = serverCmdToolgun(%client))) // blah
		return;
	
	%player.toolgunMode = 0;
	%player.updateToolgunStatus();
}

function serverCmdDel(%client) { // also for people who were accustomed to the del launcher
	if(!isObject(%player = serverCmdToolgun(%client)))
		return;
	
	%player.toolgunMode = 1;
	%player.updateToolgunStatus();
}

function serverCmdPrt(%client) {
	if(!isObject(%player = serverCmdToolgun(%client)))
		return;
	
  %client.toolgunType = 1;
	%player.toolgunMode = 2;
	%player.updateToolgunStatus();
}

function serverCmdInt(%client) {
	if(!isObject(%player = serverCmdToolgun(%client)))
		return;
	
  %client.toolgunType = 1;
	%player.toolgunMode = 3;
	%player.updateToolgunStatus();
}

function toolgunImage::onFire(%image, %obj, %slot) {
	%eye = %obj.getEyePoint();
	%vec = vectorAdd(%eye, vectorScale(%obj.getEyeVector(), $Toolgun::Range));
	%mask = $TypeMasks::FxBrickAlwaysObjectType | $TypeMasks::FxBrickObjectType | $TypeMasks::StaticShapeObjectType;
	%ray = containerRayCast(%eye, %vec, %mask, %obj);
	%col = firstWord(%ray);
	
	%rand = getRandom(1, 2);
	%obj.stopAudio(%rand);
	%obj.playAudio(%rand, "toolgun_Fire_" @ %rand);
	
	%obj.playThread(2, "shiftAway");
  
	if(isObject(%col)) {
    if(%col.getClassName() $= "StaticShape")
      return;
    
		%client = %obj.client;
    
    if(!isEventPending(%col.toolgunSchedule)) {
      %col.oldColorID = %col.getColorID();
      %col.oldColorFxID = %col.getColorFxID();
    }
    
    %adminOverride = %client.isAdmin;
		
		switch(%obj.toolgunMode) {
			case 0: // modify
				%trustReq = 1;
				%color = "0.1 0.45 0.75 1";
			case 1: // delete
				%trustReq = 2;
				%color = "1 0 0 1";
      case 2: // print
        if(!%col.getDatablock().hasPrint) {
          %client.centerPrint("This is not a print brick.", 1);
          return;
        }
        
        %adminOverride = false;
        
        %trustReq = 2;
        %color = "1 0 1 1";
      case 3: // interact
        for(%i = 0; %i < %col.numEvents; %i++) {
          if(%col.eventOutput[%i] $= "setColor" || %col.eventOutput[%i] $= "setColorFx") {
            %dont = true;
          }
        }
        
        if(!%dont) {
          %trustReq = 1;
          %color = "1 1 0 1";
        }
		}
		
		%trustLvl = getTrustLevel(%col, %obj);
		
		if((%trustLvl < %trustReq) && !%adminOverride) {
      %client.sendTrustFailureMessage(%col.getGroup());
			return;
		}
    
    switch(%obj.toolgunMode) {
			case 0:
				wrenchImage.onHitObject(%obj, %slot, %col, "0 0 -1000"); // 05/30/2017 - fixed the god damn wrench sound bug, js christ (supplied -1000 pos for sound location)
      case 2:
        printGunImage.onHitObject(%obj, %slot, %col, "0 0 -1000");
      case 3:
        %col.onToolgunInteract(%client);
        %col.onActivate(%obj, %client, getWords(%ray, 1, 3), %obj.getEyeVector());
    }
    
    if(%color !$= "") {
      %col.setColor(determineNearestColor(%color));
      %col.setColorFx(3);
      
      if(!isEventPending(%col.toolgunSchedule)) {
        %col.toolgunSchedule = %col.schedule($Toolgun::HighlightMS, "toolgunFinish", %obj.toolgunMode);
      }
    }
	}
}

function toolgunImage::onStopFire(%image, %obj, %slot) { wrenchImage::onStopFire(%image, %obj, %slot); }

function toolgunImage::onMount(%image, %obj, %slot) {
  %client = %obj.client;
  
  if(%client.toolgunType $= "")
    %client.toolgunType = 0;
  
  if(%obj.toolgunSafety $= "")
    %obj.toolgunSafety = true;
  
  if(%obj.toolgunSafety)
    %obj.toolgunMode = 0; // will always default to modify mode with safety
	
	%obj.updateToolgunStatus();
	
	parent::onMount(%image, %obj, %slot);
}

function toolgunImage::onUnMount(%image, %obj, %slot) {
	%obj.client.bottomPrint("", 0, true);
	
	parent::onUnMount(%image, %obj, %slot);
}

if(isPackage(toolgunPackage))
	deactivatePackage(toolgunPackage);

package toolgunPackage {
	
	function GameConnection::onDeath(%client) {
		if(isObject(%player = %client.player)) {
			if(%player.getMountedImage(0) == toolgunImage.getID()) {
				%client.bottomPrint("", 0, true);
      }
    }
		
		parent::onDeath(%client);
	}
	
	function GameConnection::spawnPlayer(%client) {
		parent::spawnPlayer(%client);
		
		if($Pref::Server::Toolgun::ReplaceWrench) {
			if(isObject(%player = %client.player)) {
				%toolgunID = toolgunItem.getID();
				
				for(%i = 0; %i < %player.getDatablock().maxTools; %i++) {
					if(%player.tool[%i] == wrenchItem.getID()) {
						%player.tool[%i] = %toolgunID;
						messageClient(%client, 'MsgItemPickup', '', %i, %toolgunID);
					}
				}
			}
		}
	}
	
	function serverCmdLight(%client) {
    if(isObject(%player = %client.player) && %player.isUsingToolgun()) {
      %player.changeToolgunStatus("mode");
      return;
    }
		
		parent::serverCmdLight(%client);
	}
  
  function serverCmdPrevSeat(%client) {
		if(isObject(%player = %client.player) && %player.isUsingToolgun()) {
      %player.changeToolgunStatus("type");
      return;
		}
    
    parent::serverCmdPrevSeat(%client);
  }
  
  function serverCmdNextSeat(%client) {
		if(isObject(%player = %client.player) && %player.isUsingToolgun()) {
      %player.changeToolgunStatus("safety");
      return;
		}
    
    parent::serverCmdNextSeat(%client);
  }
	
};

activatePackage(toolgunPackage);

$Toolgun::Initialized = true;