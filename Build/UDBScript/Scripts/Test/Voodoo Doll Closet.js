var Map = UDB.General.Map.Map;
var Vector2D = UDB.Geometry.Vector2D;

// Make sure the has a correct minimum length
if(ScriptOptions.length < 96)
	throw 'Voodoo doll closet has to be at least 96 map units long!';

// Get the mouse position in the map, snapped to the grid
var basepos = UDB.General.Map.Grid.SnappedToGrid(UDB.General.Editing.Mode.MouseMapPos);

// Closet width is static
var closetwidth = 64;

// Get the currently selected lines. Those will get actions to release the voodoo doll
var triggerlines = Map.GetSelectedLinedefs(true);

// The number of tags we need depend on the selected options
var numnewtags = 1;
var newtagindex = 0;

// We need an additional tag if the player is blocked at the beginning
if(ScriptOptions.inactive)
	numnewtags++;

// We need an additional tag if the closet should be looping
if(ScriptOptions.looping)
	numnewtags++;

// Get thew new tags
var tags = Map.GetMultipleNewTags(numnewtags);

log(numnewtags);
log(tags);

for(var i=0; i < tags.length; i++)
	log("new tag: " + tags[i]);

// Create a pen for drawing geometry
var p = new Pen();

// Draw the closet
p.SetAngleDegrees(90 * ScriptOptions.direction);
p.MoveTo(basepos); p.DrawVertex();
p.MoveForward(ScriptOptions.length); p.DrawVertex(); p.TurnRight();
p.MoveForward(closetwidth); p.DrawVertex(); p.TurnRight();
p.MoveForward(ScriptOptions.length); p.DrawVertex();

if(!p.FinishDrawing())
	throw "Something went wrong while drawing!";


// Get the new sector and assign a tag
var sector = Map.GetMarkedSectors(true)[0]
sector.Tag = tags[newtagindex];
sector.FloorHeight = 0;
sector.CeilHeight = 56;

// Draw the carrying line
p.SetAngleDegrees(90 * ScriptOptions.direction);
p.MoveTo(basepos); p.DrawVertex();
p.MoveForward(32); p.DrawVertex();

if(!p.FinishDrawing())
	throw 'Something went wrong while drawing!';

// Assign the action and tag to the line
var line = Map.GetMarkedLinedefs(true)[0];
line.Action = 252;
line.Tag = tags[newtagindex];

// Increment the new tag index, so that the next new tag will be used for the next step
newtagindex++;

// Create the player blocking geometry if necessary
if(ScriptOptions.inactive)
{
	// Draw the blocking sector
	p.SetAngleDegrees(90 * ScriptOptions.direction);
	p.MoveTo(basepos);
	p.MoveForward(64); p.TurnRight(); p.MoveForward(16); p.DrawVertex();
	p.TurnRight(); p.MoveForward(8); p.DrawVertex();
	p.TurnLeft(); p.MoveForward(closetwidth - 32); p.DrawVertex();
	
	if(!p.FinishDrawing())
		throw "Something went wrong while drawing!";

	// Get the new sectors and assign a tag
	sector = Map.GetMarkedSectors(true)[0];
	sector.Tag = tags[newtagindex];
	sector.FloorHeight = 0;
	sector.CeilHeight = 55;

	// Assign actions to release the voodoo doll to the previosly selected lines. If the line has a texture
	// starting with SW1 or SW2 a switch action will be applied. Otherwise a walk-over action is applied (but only if
	// it's a 2-sided line
	triggerlines.forEach(tl => {
			if(	tl.Front.HighTexture.startsWith('SW1') || tl.Front.HighTexture.startsWith('SW2') ||
				tl.Front.MiddleTexture.startsWith('SW1') || tl.Front.MiddleTexture.startsWith('SW2') ||
				tl.Front.LowTexture.startsWith('SW1') || tl.Front.LowTexture.startsWith('SW2')
				)
			{
				tl.Action = 166; // S1 Ceiling Raise to Highest Ceiling
				tl.Tag = tags[newtagindex];
			}
			else if(tl.Back != null)
			{
				tl.Action = 40; // W1 Ceiling Raise to Highest Ceiling
				tl.Tag = tags[newtagindex];
			}
	});

	// Increment the new tag index, so that the next new tag will be used for the next step
	newtagindex++;
}

// Create the looping teleporter geometry if necessary
if(ScriptOptions.looping)
{
	// Create the teleport destination line
	p.SetAngleDegrees(90 * ScriptOptions.direction);
	p.MoveTo(basepos);
	p.MoveForward(32); p.TurnRight(); p.MoveForward(8); p.DrawVertex();
	p.MoveForward(closetwidth - 16); p.DrawVertex();

	if(!p.FinishDrawing())
		throw 'Something went wrong while drawing!';
	
	// The destination line only needs a tag and no action
	line = Map.GetMarkedLinedefs(true);
	line[0].Tag = tags[newtagindex];
	
	// Create the teleport line
	p.SetAngleDegrees(90 * ScriptOptions.direction);
	p.MoveTo(basepos);
	p.MoveForward(ScriptOptions.length - 32); p.TurnRight(); p.MoveForward(8); p.DrawVertex();
	p.MoveForward(closetwidth - 16); p.DrawVertex();
	
	if(!p.FinishDrawing())
		throw 'Something went wrong while drawing!';
	
	// The teleport line needs a tag and an action
	line = Map.GetMarkedLinedefs(true);
	line[0].Action = 263;
	line[0].Tag = tags[newtagindex];
}

// The actual player always spawns on the last player 1 start that was placed,
// so we move the last player 1 start to the monster closet and create a new player 1
// start at the old position

p.SetAngleDegrees(90 * ScriptOptions.direction);
p.MoveTo(basepos);
p.MoveForward(32); p.TurnRight(); p.MoveForward(32);

// Get all player 1 starts
var playerthings = Map.Things.filter(o => { return o.Type == 1; });

//  If there are already player things in the map we need to store the old position and move the last one
if(playerthings.length > 0)
{
	// Sort them by their index, so that the first element is that last player 1 start
	playerthings.sort((a, b) => (a.Index < b.Index) ? 1 : -1);

	// Store old position and angle and move the last player 1 start to the closet
	var oldpos = playerthings[0].Position;
	var oldangle = playerthings[0].Angle;
	
	playerthings[0].Move(p.curpos.x, p.curpos.y, 0);

	// Create a new player 1 start and move it to the old position	
	var t = Map.CreateThing();
	t.Type = 1;
	t.Move(oldpos.x, oldpos.y, 0);
	t.Rotate(oldangle);
	t.UpdateConfiguration();
}
else
{
	var t = Map.CreateThing();
	t.Type = 1;
	t.Move(p.curpos.x, p.curpos.y, 0);
	t.UpdateConfiguration();
}
