UDB.General.Map.Map.Sectors.forEach(s => {
	var passes = [ s.FloorTexture == "CEIL1_2", s.CeilTexture == "CEIL1_2" ]

	for(var i=0; i < 2; i++) {
		if(passes[i]) {
			var height;
			
			if(i == 0)
				height = 8;
			else
				height = s.CeilHeight - s.FloorHeight - 8;
			
			var t = UDB.General.Map.Map.CreateThing();
			UDB.General.Settings.ApplyDefaultThingSettings(t);
			t.Type = 9830;
			t.Args[0] = 255;
			t.Args[1] = 255;
			t.Args[2] = 255;
			t.Args[3] = 64;
			t.move(s.BBox.X + s.BBox.Width / 2, s.BBox.Y + s.BBox.Height / 2, height);
			t.UpdateConfiguration();
		}
	}
});