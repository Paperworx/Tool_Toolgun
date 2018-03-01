exec("Add-Ons/Tool_Toolgun/support.cs");
exec("Add-Ons/Tool_Toolgun/datablocks.cs");
exec("Add-Ons/Tool_Toolgun/packages.cs");

if(isFile("Add-Ons/System_ReturnToBlockland/server.cs")) {
  if(!$RTB::RTBR_ServerControl_Hook)
    exec("Add-Ons/System_ReturnToBlockland/RTBR_ServerControl_Hook.cs");

  RTB_registerPref("Replace Wrench on Spawn", "Toolgun", "$Pref::Server::Toolgun::ReplaceWrench", "bool", "Tool_Toolgun", 1, false, false);
  RTB_registerPref("Highlight Duration", "Toolgun", "$Pref::Server::Toolgun::HighlightMS", "int 0 1000", "Tool_Toolgun", 250, false, false);
  RTB_registerPref("Range", "Toolgun", "$Pref::Server::Toolgun::Range", "int 5 500", "Tool_Toolgun", 500, false, false);
  RTB_registerPref("Instant Brick Deletion", "Toolgun", "$Pref::Server::Toolgun::InstantDelete", "bool", "Tool_Toolgun", 0, false, false);
  RTB_registerPref("Allow in Minigames", "Toolgun", "$Pref::Server::Toolgun::AllowInMinigames", "bool", "Tool_Toolgun", 0, false, false);
  RTB_registerPref("Interact Trust Required", "Toolgun", "$Pref::Server::Toolgun::InteractTrustRequired", "list None 0 Build 1 Full 2", "Tool_Toolgun", 1, false, false, "onChangeToolgunInteractTrust");
} else {
  if($Pref::Server::Toolgun::ReplaceWrench $= "")
    $Pref::Server::Toolgun::ReplaceWrench = true;

  if($Pref::Server::Toolgun::HighlightMS $= "")
    $Pref::Server::Toolgun::HighlightMS = 250;

  if($Pref::Server::Toolgun::Range $= "")
    $Pref::Server::Toolgun::Range = 500;

  if($Pref::Server::Toolgun::InstantDelete $= "")
    $Pref::Server::Toolgun::InstantDelete = false;

  if($Pref::Server::Toolgun::AllowInMinigames $= "")
    $Pref::Server::Toolgun::AllowInMinigames = false;

  if($Pref::Server::Toolgun::InteractTrustRequired $= "")
    $Pref::Server::Toolgun::InteractTrustRequired = 1;
}

if(isObject(ToolgunCategories)) {
  warn("Toolgun: Clearing all categories...");

  ToolgunCategories.chainDeleteAll();
  ToolgunCategories.schedule(1, "delete");
}

new ScriptGroup(ToolgunCategories);

function ToolgunCategories::getTotal(%this) {
  for(%i = 0; %i < %this.getCount(); %i++) {
    %c += %this.getObject(%i).getCount();
  }

  return %c;
}

function getToolgunCategory(%category) {
  for(%i = 0; %i < ToolgunCategories.getCount(); %i++) {
    %tc = ToolgunCategories.getObject(%i);

    if(%tc.name $= %category)
      return %tc;
  }

  return false;
}

function getToolgunMode(%mode) {
  for(%i = 0; %i < ToolgunCategories.getCount(); %i++) {
    %tc = ToolgunCategories.getObject(%i);

    for(%o = 0; %o < %tc.getCount(); %o++) {
      %tm = %tc.getObject(%o);

      if(%tm.name $= %mode)
        return %tm;
    }
  }

  return false;
}

function unRegisterToolgunCategory(%name) {
  if(!(%category = getToolgunCategory(%name))) {
    error("Toolgun: Tried to unregister non-existent category '" @ %name @ "'.");
    return;
  }

  warn("Toolgun: Unregistered category '" @ %category.name @ "'.");

  %category.chainDeleteAll();
  %category.schedule(1, "delete");
}

function unRegisterToolgunMode(%name) {
  if(!(%mode = getToolgunMode(%name))) {
    error("Toolgun: Tried to unregister non-existent mode '" @ %name @ "'.");
    return;
  }

  warn("Toolgun: Unregistered mode '" @ %mode.name @ "'.");

  %mode.delete();
}

function registerToolgunCategory(%name, %color) {
  if(getToolgunCategory(%name)) {
    error("Toolgun: Tried to register '" @ %name @ "' category more than once.");
    return;
  }

  %category = new ScriptGroup() {
    name = %name;
    color = %color;
  };

  ToolgunCategories.add(%category);

  echo("Toolgun: Registered '" @ %category.name @ "' category.");
}

function registerToolgunMode(%name, %category, %color, %trust, %function, %mask) {
  if(getToolgunMode(%name)) {
    error("Toolgun: Tried to register '" @ %name @ "' mode more than once.");
    return;
  }

  if(!(%obj = getToolgunCategory(%category))) {
    error("Toolgun: Tried to register '" @ %name @ "' mode to a non-existent category.");
    return;
  }

  if(%trust < 0 || %trust > 3) {
    error("Toolgun: Tried to register '" @ %name @ "' mode to an invalid trust level.");
    return;
  }

  if(!isFunction(%function)) {
    error("Toolgun: Tried to register '" @ %name @ "' mode to a non-existent function.");
    return;
  }

  %mode = new ScriptObject(ToolgunMode) {
    name = %name;
    category = %category;
    color = %color;
    trust = %trust;
    func = %function;
    mask = %mask;
  };

  %obj.add(%mode);

  echo("Toolgun: Registered '" @ %mode.name @ "' mode.");
}

registerToolgunCategory("Simple", "0 255 255");
registerToolgunCategory("Extra", "50 50 255");

function toolgun_Modify(%slot, %col, %ray, %client) {
  if(%col.getClassName() $= "StaticShape")
    return;

  wrenchImage.onHitObject(%client.player, %slot, %col, "0 0 -1000"); // 5/30/2017 - fixed the god damn wrench sound bug, js christ (supplied -1000 pos for sound location)
}

registerToolgunMode("Modify", "Simple", "0 255 255 255", 1, "toolgun_Modify", $TypeMasks::FxBrickAlwaysObjectType | $TypeMasks::FxBrickObjectType | $TypeMasks::StaticShapeObjectType);

function toolgun_Delete(%slot, %col, %ray, %client) {
  if(%col.getClassName() $= "StaticShape")
    return;

  if(!$Pref::Server::Toolgun::InstantDelete) {
    %col.schedule($Pref::Server::Toolgun::HighlightMS, "delete");
  } else {
    %col.delete();
  }
}

registerToolgunMode("Delete", "Simple", "255 0 0 255", 2, "toolgun_Delete", $TypeMasks::FxBrickAlwaysObjectType | $TypeMasks::FxBrickObjectType | $TypeMasks::StaticShapeObjectType);

function toolgun_Print(%slot, %col, %ray, %client) {
  if(%col.getClassName() $= "StaticShape")
    return;

  if(!%col.getDatablock().hasPrint) {
    %client.centerPrint("This is not a print brick.", 1);
    return;
  }

  printGunImage.onHitObject(%client.player, %slot, %col, "0 0 -1000");
}

registerToolgunMode("Print", "Extra", "255 0 255 255", 2, "toolgun_Print", $TypeMasks::FxBrickAlwaysObjectType | $TypeMasks::FxBrickObjectType | $TypeMasks::StaticShapeObjectType);

function toolgun_Interact(%slot, %col, %ray, %client) {
  if(%col.getClassName() $= "StaticShape")
    return;

  %col.onToolgunInteract(%client);

  %player = %client.player;

  %col.onActivate(%player, %client, getWords(%ray, 1, 3), %player.getEyeVector());
}

registerToolgunMode("Interact", "Extra", "255 255 0 255", $Pref::Server::Toolgun::InteractTrustRequired, "toolgun_Interact", $TypeMasks::FxBrickAlwaysObjectType | $TypeMasks::FxBrickObjectType | $TypeMasks::StaticShapeObjectType);

function onChangeToolgunInteractTrust(%old, %new) {
  if(!(%mode = getToolgunMode("Interact")))
    return;

  %mode.trust = %new;
}

function Player::isUsingToolgun(%player) {
  if(isObject(%player) && %player.getMountedImage(0) == toolgunImage.getID())
    return true;

  return false;
}

function Player::updateToolgunStatus(%player, %animate) { // update toolgun hud
  if(!isObject(%player))
    return;

  if(ToolgunCategories.getCount() <= 0)
    return;

  %client = %player.client;

  %category = ToolgunCategories.getObject(%client.toolgunCategory);
  %mode = %category.getObject(%player.toolgunMode);

  %categoryColor = rgbToHex(%category.color);
  %modeColor = rgbToHex(%mode.color);

  %info = "<font:Arial Bold:26>\c6Toolgun<br><font:Arial:18>";
  %info = %info @ "\c6Mode: <color:" @ %modeColor @ ">" @ %mode.name @ " \c6" @ (%category.getCount() > 1 ? "[Light]" : "") @ "<br>";
  if(ToolgunCategories.getCount() > 1)
    %info = %info @ "\c6Category: <color:" @ %categoryColor @ ">" @ %category.name @ " \c6[Prev Seat]<br>";
  if(ToolgunCategories.getTotal() > 1)
    %info = %info @ "\c6Safety: " @ (%player.toolgunSafety ? "\c2ON" : "\c0OFF") @ " \c6[Next Seat]<br>";

  commandToClient(%client, 'bottomPrint', %info, 0, true);

  if(%animate) {
    %player.playThread(2, "shiftRight");
    %client.play2D(isObject("Beep_Key_Sound") ? "Beep_Key_Sound" : "BrickChangeSound");
  }
}

function Player::setToolgunCategory(%player, %category) {
  if(!isObject(%player))
    return;

  for(%i = 0; %i < ToolgunCategories.getCount(); %i++) {
    %tc = ToolgunCategories.getObject(%i);

    if(%tc.name $= %category) {
      %found = true;
      break;
    }
  }

  if(!%found) {
    error("Toolgun: Tried to set player to non-existent category '" @ %category @ "'.");
    return;
  }

  %client.toolgunCategory = %i;

  %player.updateToolgunStatus(false);
}

function Player::setToolgunMode(%player, %mode) {
  if(!isObject(%player))
    return;

  for(%i = 0; %i < ToolgunCategories.getCount(); %i++) {
    %tc = ToolgunCategories.getObject(%i);

    for(%o = 0; %o < %tc.getCount(); %o++) {
      %tm = %tc.getObject(%o);

      if(%tm.name $= %mode) {
        %found = true;
        break;
      }
    }

    if(%found) {
      break;
    }
  }

  if(!%found) {
    error("Toolgun: Tried to set player to non-existent mode '" @ %mode @ "'.");
    return;
  }

  %player.client.toolgunCategory = %i;
  %player.toolgunMode = %o;

  %player.updateToolgunStatus(false);
}

function Player::rotateToolgunCategory(%player) {
  if(!isObject(%player))
    return;

  %client = %player.client;

  if(ToolgunCategories.getObject(%client.toolgunCategory).getCount() <= 0)
    return;

  %player.toolgunMode = 0;
  %client.toolgunCategory++;

  if(%client.toolgunCategory >= ToolgunCategories.getCount())
    %client.toolgunCategory = 0;

  %player.updateToolgunStatus(true);
}

function Player::rotateToolgunMode(%player) {
  if(!isObject(%player))
    return;

  %player.toolgunMode++;

  if(%player.toolgunMode >= ToolgunCategories.getObject(%client.toolgunCategory).getCount())
    %player.toolgunMode = 0;

  %player.updateToolgunStatus(true);
}

function Player::toggleToolgunSafety(%player) {
  if(!isObject(%player))
    return;

  %player.toolgunSafety = !%player.toolgunSafety;

  %player.updateToolgunStatus(true);
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

registerInputEvent("FxDTSBrick", "onToolgunInteract", "Self FxDTSBrick" TAB "Player Player" TAB "Client GameConnection" TAB "MiniGame MiniGame");

function FxDTSBrick::toolgunFinish(%brick) {
  if(isObject(%brick)) {
    if(%brick.oldColorID !$= "")
      %brick.setColor(%brick.oldColorID);

    if(%brick.oldColorFxID !$= "")
      %brick.setColorFx(%brick.oldColorFxID);

    %brick.oldColorID = "";
    %brick.oldColorFxID = "";

    if(%brick.getDatablock().specialBrickCategory $= "VehicleSpawn") {
      %brick.colorVehicle();
    }
  }
}

function serverCmdToolgun(%client) {
  if(!isObject(%player = %client.player))
    return;

  if(isObject(%client.minigame) && !%client.isAdmin && !$Pref::Server::Toolgun::AllowInMinigames) {
    messageClient(%client, '', "Toolgun commands have been disabled while in a minigame.");
    return;
  }

  %player.updateArm("toolgunImage");
  %player.mountImage("toolgunImage", 0);

  return %player; // blah
}

function serverCmdTG(%client) {
  serverCmdToolgun(%client);
}

function serverCmdMod(%client) {
  if(!getToolgunMode("Modify"))
    return;

  %client.player.setToolgunMode("Modify");

  serverCmdToolgun(%client);
}

function serverCmdDel(%client) { // for people who were/are accustomed to the del launcher
  if(!getToolgunMode("Delete"))
    return;

  if(!isObject(%player = serverCmdToolgun(%client)))
    return;

  %player.setToolgunMode("Delete");
  %player.updateToolgunStatus();
}

function serverCmdPrt(%client) {
  if(!getToolgunMode("Print"))
    return;

  if(!isObject(%player = serverCmdToolgun(%client)))
    return;

  %player.setToolgunMode("Print");
  %player.updateToolgunStatus();
}

function serverCmdInt(%client) {
  if(!getToolgunMode("Interact"))
    return;

  if(!isObject(%player = serverCmdToolgun(%client)))
    return;

  %player.setToolgunMode("Interact");
  %player.updateToolgunStatus();
}

function toolgunImage::onFire(%image, %player, %slot) {
  %client = %player.client;

  %mode = ToolgunCategories.getObject(%client.toolgunCategory).getObject(%player.toolgunMode);

  %eye = %player.getEyePoint();
  %vec = vectorAdd(%eye, vectorScale(%player.getEyeVector(), $Pref::Server::Toolgun::Range));
  %mask = %mode.mask;
  %ray = containerRayCast(%eye, %vec, %mask, %player);
  %col = firstWord(%ray);

  %rand = getRandom(1, 2);
  %player.stopAudio(%rand);
  %player.playAudio(%rand, "toolgun_Fire_" @ %rand);

  %player.playThread(2, "shiftAway");

  if(isObject(%col)) {
    if(!isEventPending(%col.toolgunSchedule) && %mode.color !$= "") {
      %col.oldColorID = %col.getColorID();
      %col.oldColorFxID = %col.getColorFxID();
    }

    %adminOverride = %client.isAdmin;

    %trustLvl = getTrustLevel(%col, %player);

    if((%trustLvl < %mode.trust) && !%adminOverride) {
      %client.sendTrustFailureMessage(%col.getGroup());
      return;
    }

    call(%mode.func, %slot, %col, %ray, %client);

    for(%i = 0; %i < %col.numEvents; %i++) {
      if(%col.eventOutput[%i] $= "setColor" || %col.eventOutput[%i] $= "setColorFx") {
        %dont = true;
        break;
      }
    }

    if(isObject(%col)) {
      if(!%dont && %mode.color !$= "" && $Pref::Server::Toolgun::HighlightMS > 0) {
        %color = (getWord(%mode.color, 0) / 255) SPC (getWord(%mode.color, 1) / 255) SPC (getWord(%mode.color, 2) / 255) SPC (getWord(%mode.color, 3) / 255);
        %col.setColor(determineNearestColor(%color));
        %col.setColorFx(3);
      }

      if(!isEventPending(%col.toolgunSchedule)) {
        if($Pref::Server::Toolgun::HighlightMS > 0) {
          %col.toolgunSchedule = %col.schedule($Pref::Server::Toolgun::HighlightMS, "toolgunFinish");
        } else {
          %col.toolgunFinish();
        }
      }
    }
  }
}

function toolgunImage::onStopFire(%image, %player, %slot) { wrenchImage::onStopFire(%image, %player, %slot); }

function toolgunImage::onMount(%image, %player, %slot) {
  %client = %player.client;

  if(%client.toolgunCategory $= "")
    %client.toolgunCategory = 0;

  if(%player.toolgunSafety $= "")
    %player.toolgunSafety = true;

  if(%player.toolgunSafety)
    %player.toolgunMode = 0; // will always default to modify mode with safety

  %player.updateToolgunStatus();

  parent::onMount(%image, %player, %slot);
}

function toolgunImage::onUnMount(%image, %player, %slot) {
  commandToClient(%player.client, 'clearBottomPrint');

  parent::onUnMount(%image, %player, %slot);
}