var UDB = importNamespace('CodeImp.DoomBuilder');
var General = UDB.General;
var Map = UDB.General.Map.Map;

var radius = 128;
var num = 10;

function Random(min, max)
{
	return Math.floor(Math.random() * (max - min)) + min;
}

var basepos = General.Editing.Mode.MouseMapPos;

for(var i=0; i < num; i++)
{
	thingnums = [ 79, 80, 81, 24 ];
	
	var t = Map.CreateThing();
	t.Type = thingnums[Random(0, thingnums.length)];
	t.Move(basepos.x + Random(-radius, radius), basepos.y + Random(-radius, radius), 0);
	t.UpdateConfiguration();
}