if(isPackage(toolgunPackage))
  deactivatePackage(toolgunPackage);

package toolgunPackage {

  function GameConnection::onDeath(%client, %sourceObject, %sourceClient, %damageType, %damageLoc) {
    if(isObject(%player = %client.player)) {
      if(%player.getMountedImage(0) == toolgunImage.getID()) {
        commandToClient(%client, 'clearBottomPrint');
      }
    }

    parent::onDeath(%client, %sourceObject, %sourceClient, %damageType, %damageLoc);
  }

  function GameConnection::spawnPlayer(%client) {
    parent::spawnPlayer(%client);

    if($Pref::Server::Toolgun::ReplaceWrench) {
      %toolgunID = toolgunItem.getID();

      if(isObject(%toolgunID)) {
        if(isObject(%player = %client.player)) {
          for(%i = 0; %i < %player.getDatablock().maxTools; %i++) {
            if(%player.tool[%i] == wrenchItem.getID()) {
              %player.tool[%i] = %toolgunID;
              messageClient(%client, 'MsgItemPickup', '', %i, %toolgunID);
            }
          }
        }
      }
    }
  }

  function serverCmdLight(%client) {
    if(isObject(%player = %client.player) && %player.isUsingToolgun()) {
      if(ToolgunCategories.getObject(%client.toolgunCategory).getCount() > 1) {
        %player.rotateToolgunMode();
        return;
      }
    }

    parent::serverCmdLight(%client);
  }

  function serverCmdPrevSeat(%client) {
    if(isObject(%player = %client.player) && %player.isUsingToolgun()) {
      if(ToolgunCategories.getCount() > 1) {
        %player.rotateToolgunCategory();
        return;
      }
    }

    parent::serverCmdPrevSeat(%client);
  }

  function serverCmdNextSeat(%client) {
    if(isObject(%player = %client.player) && %player.isUsingToolgun()) {
      if(ToolgunCategories.getTotal() > 1) {
        %player.toggleToolgunSafety();
        return;
      }
    }

    parent::serverCmdNextSeat(%client);
  }

};

activatePackage(toolgunPackage);