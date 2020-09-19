var General = UDB.General;
var Map = UDB.General.Map.Map;

var basepos = General.Editing.Mode.MouseMapPos;

function Random(min, max)
{
	return Math.floor(Math.random() * (max - min)) + min;
}

for(var i=0; i < ScriptOptions.amount; i++)
{
	var t = Map.CreateThing();
	t.Type = ScriptOptions.type;
	
	if(ScriptOptions.square)
	{
		t.Move(basepos.x + Random(-ScriptOptions.maxradius, ScriptOptions.maxradius), basepos.y + Random(-ScriptOptions.maxradius, ScriptOptions.maxradius), 0);
	}
	else
	{
		var angle = Random(0, 359) * Math.PI / 180;
		var distance = Random(ScriptOptions.minradius, ScriptOptions.maxradius);
		t.Move(basepos.x + Math.cos(angle)*distance, basepos.y + Math.sin(angle)*distance, 0);
	}
	
	t.UpdateConfiguration();
}