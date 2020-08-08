var UDB = importNamespace('CodeImp.DoomBuilder');

// This script draws a square on the front or back (depending on mouse position) of a highlighted linedef.
// The size of the square is based on the linedef's length

// Get the highlight range setting from BuilderModes
var highlightrange = UDB.General.Settings.ReadPluginSetting("BuilderModes", "highlightrange", 20);

// Shortcut to the mouse position in map coordinates
var mousemappos = UDB.General.Editing.Mode.MouseMapPos;

// Get the linedef that's closest to the mouse position and that is in highlight range
var linedef = UDB.General.Map.Map.NearestLinedefRange(mousemappos, highlightrange / UDB.General.Map.Renderer2D.Scale);

if(linedef !== null) // Got a valid linedef?
{
	var length = linedef.Length;
	
	// Determine if the mouse cursor is on the front or the back of the linedef
	var front = linedef.Line.GetSideOfLine(mousemappos) <= 0;
	
	// Create the pen to do the drawing
	var p = new Pen();
	
	// We always want to draw in clockwise order, so we need to set different
	// start positions and angles depending if we draw for the front ot back
	// of the line
	if(front)
	{
		p.MoveTo(linedef.End.Position);
		p.SetAngle(linedef.Angle + Math.PI);
	}
	else
	{
		p.MoveTo(linedef.Start.Position);
		p.SetAngle(linedef.Angle);
	}
	
	// Place the vertices
	p.DrawVertex();

	p.MoveForward(length);
	p.DrawVertex();

	p.TurnRight();
	p.MoveForward(length);
	p.DrawVertex();

	p.TurnRight();
	p.MoveForward(length);
	p.DrawVertex();

	// Actually do the drawing. Show a warning if something went wrong
	if(!p.FinishDrawing())
		UDB.General.Interface.DisplayStatus(UDB.Windows.StatusType.Warning, "Something went wrong while drawing!");
}
else
{
	// Show a warning in the status bar
	UDB.General.Interface.DisplayStatus(UDB.Windows.StatusType.Warning, 'You have to highlight a linedef!');
}