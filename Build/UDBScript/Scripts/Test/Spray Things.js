var UDB = importNamespace('CodeImp.DoomBuilder');
var General = UDB.General;
var Map = UDB.General.Map.Map;

var minradius = parseFloat(ScriptOptions.minradius);
var maxradius = parseFloat(ScriptOptions.maxradius);
var amount = parseInt(ScriptOptions.amount);
var thingtype = parseInt(ScriptOptions.type);
var square = (ScriptOptions.square == 1)
var basepos = General.Editing.Mode.MouseMapPos;

function Random(min, max)
{
	return Math.floor(Math.random() * (max - min)) + min;
}

for(var i=0; i < amount; i++)
{
	var t = Map.CreateThing();
	t.Type = thingtype;
	
	if(square)
		t.Move(basepos.x + Random(-maxradius, maxradius), basepos.y + Random(-maxradius, maxradius), 0);
	else
	{
		var angle = Random(0, 359) * Math.PI / 180;
		var distance = Random(minradius, maxradius);
		t.Move(basepos.x + Math.cos(angle)*distance, basepos.y + Math.sin(angle)*distance, 0);
	}
	
	t.UpdateConfiguration();
}