if(!isObject(toolgun_Fire_1)) {
  datablock AudioProfile(toolgun_Fire_1) {
    filename = "Add-Ons/Tool_Toolgun/toolgun_Fire_1.wav";
    description = AudioClose3d;
    preload = true;
  };
}

if(!isObject(toolgun_Fire_2)) {
  datablock AudioProfile(toolgun_Fire_2 : toolgun_Fire_1) {
    filename = "Add-Ons/Tool_Toolgun/toolgun_Fire_2.wav";
  };
}

if(!isObject(toolgunItem)) {
  datablock ItemData(toolgunItem : wrenchItem) {
    shapeFile     = "Add-Ons/Tool_Toolgun/toolgun.dts";

    uiName        = "Toolgun";
    iconName      = "Add-Ons/Tool_Toolgun/Toolgun";

    doColorShift  = false;

    image         = toolgunImage;
  };
}

if(!isObject(toolgunImage)) {
  datablock ShapeBaseImageData(toolgunImage : wrenchImage) {
    shapeFile     = "Add-Ons/Tool_Toolgun/toolgun.dts";

    offset        = "-0.02 0 -0.02";
    eyeOffset     = "0.5 0.6 -0.7";

    doColorShift  = false;

    stateTimeoutValue[3] = 0.18;

    item          = toolgunItem;
  };
}