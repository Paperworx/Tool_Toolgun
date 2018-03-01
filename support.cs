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

function rgbToHex(%rgb) { // greek2me
	// use % to find remainder
	%r = getWord(%rgb,0);
	%g = getWord(%rgb,1);
	%b = getWord(%rgb,2);
	// in-case the rgb value isn't on the right scale
	%r = ( %r <= 1 ) ? %r * 255 : %r;
	%g = ( %g <= 1 ) ? %g * 255 : %g;
	%b = ( %b <= 1 ) ? %b * 255 : %b;
	// the hexidecimal numbers
	%a = "0123456789ABCDEF";

	%r = getSubStr(%a,(%r-(%r % 16))/16,1) @ getSubStr(%a,(%r % 16),1);
	%g = getSubStr(%a,(%g-(%g % 16))/16,1) @ getSubStr(%a,(%g % 16),1);
	%b = getSubStr(%a,(%b-(%b % 16))/16,1) @ getSubStr(%a,(%b % 16),1);

	return %r @ %g @ %b;
}