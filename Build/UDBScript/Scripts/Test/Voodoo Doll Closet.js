var UDB = importNamespace('CodeImp.DoomBuilder');
var Map = UDB.General.Map.Map;
var Vector2D = UDB.Geometry.Vector2D;

var basepos = UDB.General.Map.Grid.SnappedToGrid(UDB.General.Editing.Mode.MouseMapPos);

var closetlength = parseInt(ScriptOptions.length);
var closetwidth = 64;

var triggerlines = Map.GetSelectedLinedefs(true);

log(triggerlines);

// Get two new tags, one for the scrolling, one for the blocking sector
var tags = Map.GetMultipleNewTags(2);

var p = new Pen();

// Draw the closet
p.SetAngleDegrees(90);
p.MoveTo(basepos); p.DrawVertex();
p.MoveForward(closetlength); p.DrawVertex(); p.TurnRight();
p.MoveForward(closetwidth); p.DrawVertex(); p.TurnRight();
p.MoveForward(closetlength); p.DrawVertex();

if(!p.FinishDrawing())
	UDB.General.Interface.DisplayStatus(UDB.Windows.StatusType.Warning, "Something went wrong while drawing!");

// Get the new sector and assign a tag
var sector = Map.GetMarkedSectors(true)[0]
sector.Tag = tags[0];
sector.FloorHeight = 0;
sector.CeilHeight = 56;

// Draw the carrying line
p.SetAngleDegrees(90);
p.MoveTo(basepos); p.DrawVertex();
p.MoveForward(32); p.DrawVertex();

if(!p.FinishDrawing())
	UDB.General.Interface.DisplayStatus(UDB.Windows.StatusType.Warning, "Something went wrong while drawing!");

var line = Map.GetMarkedLinedefs(true)[0];
line.Action = 252;
line.Tag = tags[0];

// Draw player blocking sector
p.SetAngleDegrees(0);
p.MoveTo(new Vector2D(basepos.x + 16, basepos.y + 48)); p.DrawVertex();
p.MoveForward(closetwidth - 32); p.DrawVertex();
p.MoveTo(new Vector2D(basepos.x + 16, basepos.y + 52)); p.DrawVertex();

if(!p.FinishDrawing())
	UDB.General.Interface.DisplayStatus(UDB.Windows.StatusType.Warning, "Something went wrong while drawing!");

// Get the new sectors and assign a tag
sector = Map.GetMarkedSectors(true)[0];
sector.Tag = tags[1];
sector.FloorHeight = 0;
sector.CeilHeight = 55;

// Assign actions to release the voodoo doll to the previosly selected lines
triggerlines.forEach(tl => {
		if(	tl.Front.HighTexture.startsWith('SW1') || tl.Front.HighTexture.startsWith('SW2') ||
			tl.Front.MiddleTexture.startsWith('SW1') || tl.Front.MiddleTexture.startsWith('SW2') ||
			tl.Front.LowTexture.startsWith('SW1') || tl.Front.LowTexture.startsWith('SW2')
			)
		{
			tl.Action = 166; // S1 Ceiling Raise to Highest Ceiling
			tl.Tag = tags[1];
		}
		else if(tl.Back != null)
		{
			tl.Action = 40; // W1 Ceiling Raise to Highest Ceiling
			tl.Tag = tags[1];
		}
});

// The actual player always spawns on the last player 1 start that was placed,
// so we move the last player 1 start to the monster closet and create anew player 1
// start at the old position

// Get all player 1 starts
var playerthings = Map.Things.filter(o => { return o.Type == 1; });

// Sort them by their index, so that the first element is that last player 1 start
playerthings.sort((a, b) => (a.Index < b.Index) ? 1 : -1);

// Store old position and angle and move the last player 1 start to the closet
var oldpos = playerthings[0].Position;
var oldangle = playerthings[0].Angle;
playerthings[0].Move(basepos.x + 32, basepos.y + 32, 0);

// Create a new player 1 start and move it to the old position
var t = Map.CreateThing();
t.Type = 1;
t.Move(oldpos.x, oldpos.y, 0);
t.Rotate(oldangle);
t.UpdateConfiguration();