
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.GZBuilder.Data;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Plugins;
using CodeImp.DoomBuilder.Properties;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.VisualModes;

#endregion

namespace CodeImp.DoomBuilder.Windows
{
	public partial class MainForm : DelayedForm, IMainForm
	{
		#region ================== Constants
		
		// Recent files
		private const int MAX_RECENT_FILES_PIXELS = 250;
		
		// Status bar
		internal const int WARNING_FLASH_COUNT = 10;
		internal const int WARNING_FLASH_INTERVAL = 100;
		internal const int WARNING_RESET_DELAY = 5000;
		internal const int INFO_RESET_DELAY = 5000;
		internal const int ACTION_FLASH_COUNT = 3;
		internal const int ACTION_FLASH_INTERVAL = 50;
		internal const int ACTION_RESET_DELAY = 5000;
		
		internal readonly Image[,] STATUS_IMAGES = new Image[,]
		{
			// Normal versions
			{
			  Resources.Status0, Resources.Status1,
			  Resources.Status2, Resources.Warning
			},
			
			// Flashing versions
			{
			  Resources.Status10, Resources.Status11,
			  Resources.Status12, Resources.WarningOff
			}
		};
		
		#endregion 

		#region ================== Delegates

		//private delegate void CallUpdateStatusIcon();
		//private delegate void CallImageDataLoaded(ImageData img);
		private delegate void CallBlink(); //mxd

		#endregion

		#region ================== mxd. Events

		public event EventHandler OnEditFormValuesChanged; //mxd

		#endregion

		#region ================== Variables

		// Position/size
		private bool displayresized = true;
		private bool windowactive;
		
		// Mouse in display
		private bool mouseinside;
		
		// Input
		private bool shift, ctrl, alt;
		private MouseButtons mousebuttons;
		private MouseInput mouseinput;
		private bool mouseexclusive;
		private int mouseexclusivebreaklevel;
		
		// Last info on panels
		private object lastinfoobject;
		
		// Recent files
		private ToolStripMenuItem[] recentitems;
		
		// View modes
		private ToolStripButton[] viewmodesbuttons;
		private ToolStripMenuItem[] viewmodesitems;

		//mxd. Geometry merge modes
		private ToolStripButton[] geomergemodesbuttons;
		private ToolStripMenuItem[] geomergemodesitems;
		
		// Edit modes
		private List<ToolStripItem> editmodeitems;
		
		// Toolbar
		private List<PluginToolbarButton> pluginbuttons;
		private EventHandler buttonvisiblechangedhandler;
		private bool preventupdateseperators;
		private bool updatingfilters;
		private bool toolbarContextMenuShiftPressed; //mxd
		
		// Statusbar
		private StatusInfo status;
		private int statusflashcount;
		private bool statusflashicon;
		
		// Properties
		private IntPtr windowptr;
		
		// Processing
		private int processingcount;
		private long lastupdatetime;

		// Updating
		private int lockupdatecount;
		private bool mapchanged; //mxd

		//mxd. Hints
		private Docker hintsDocker;
		private HintsPanel hintsPanel;

		//mxd
		private System.Timers.Timer blinkTimer; 
		private bool editformopen;

		//mxd. Misc drawing
		private Graphics graphics;
		
		#endregion

		#region ================== Properties

		public bool ShiftState { get { return shift; } }
		public bool CtrlState { get { return ctrl; } }
		public bool AltState { get { return alt; } }
		new public MouseButtons MouseButtons { get { return mousebuttons; } }
		public bool MouseInDisplay { get { return mouseinside; } }
		public RenderTargetControl Display { get { return display; } }
		public bool SnapToGrid { get { return buttonsnaptogrid.Checked; } }
		public bool AutoMerge { get { return buttonautomerge.Checked; } }
		public bool MouseExclusive { get { return mouseexclusive; } }
		new public IntPtr Handle { get { return windowptr; } }
		public bool IsInfoPanelExpanded { get { return (panelinfo.Height == heightpanel1.Height); } }
		public string ActiveDockerTabName { get { return dockerspanel.IsCollpased ? "None" : dockerspanel.SelectedTabName; } }
		public bool IsActiveWindow { get { return windowactive; } }
		public StatusInfo Status { get { return status; } }
		public static Size ScaledIconSize = new Size(16, 16); //mxd
		public static SizeF DPIScaler = new SizeF(1.0f, 1.0f); //mxd
		
		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		internal MainForm()
		{
			// Fetch pointer
			windowptr = base.Handle;
			
			//mxd. Graphics
			graphics = Graphics.FromHwndInternal(windowptr);
			
			//mxd. Set DPI-aware icon size
			DPIScaler = new SizeF(graphics.DpiX / 96, graphics.DpiY / 96);

			if(DPIScaler.Width != 1.0f || DPIScaler.Height != 1.0f)
			{
				ScaledIconSize.Width = (int)Math.Round(ScaledIconSize.Width * DPIScaler.Width);
				ScaledIconSize.Height = (int)Math.Round(ScaledIconSize.Height * DPIScaler.Height);
			}
			
			// Setup controls
			InitializeComponent();
			InitializeComponent2();

			//mxd. Resize status labels
			if(DPIScaler.Width != 1.0f)
			{
				gridlabel.Width = (int)Math.Round(gridlabel.Width * DPIScaler.Width);
				zoomlabel.Width = (int)Math.Round(zoomlabel.Width * DPIScaler.Width);
				xposlabel.Width = (int)Math.Round(xposlabel.Width * DPIScaler.Width);
				yposlabel.Width = (int)Math.Round(yposlabel.Width * DPIScaler.Width);
				warnsLabel.Width = (int)Math.Round(warnsLabel.Width * DPIScaler.Width);
			}

			pluginbuttons = new List<PluginToolbarButton>();
			editmodeitems = new List<ToolStripItem>();
			labelcollapsedinfo.Text = "";
			display.Dock = DockStyle.Fill;
			
			// Make array for view modes
			viewmodesbuttons = new ToolStripButton[Renderer2D.NUM_VIEW_MODES];
			viewmodesbuttons[(int)ViewMode.Normal] = buttonviewnormal;
			viewmodesbuttons[(int)ViewMode.Brightness] = buttonviewbrightness;
			viewmodesbuttons[(int)ViewMode.FloorTextures] = buttonviewfloors;
			viewmodesbuttons[(int)ViewMode.CeilingTextures] = buttonviewceilings;
			viewmodesitems = new ToolStripMenuItem[Renderer2D.NUM_VIEW_MODES];
			viewmodesitems[(int)ViewMode.Normal] = itemviewnormal;
			viewmodesitems[(int)ViewMode.Brightness] = itemviewbrightness;
			viewmodesitems[(int)ViewMode.FloorTextures] = itemviewfloors;
			viewmodesitems[(int)ViewMode.CeilingTextures] = itemviewceilings;

			//mxd. Make arrays for geometry merge modes
			int numgeomodes = Enum.GetValues(typeof(MergeGeometryMode)).Length;
			geomergemodesbuttons = new ToolStripButton[numgeomodes];
			geomergemodesbuttons[(int)MergeGeometryMode.CLASSIC] = buttonmergegeoclassic;
			geomergemodesbuttons[(int)MergeGeometryMode.MERGE] = buttonmergegeo;
			geomergemodesbuttons[(int)MergeGeometryMode.REPLACE] = buttonplacegeo;
			geomergemodesitems = new ToolStripMenuItem[numgeomodes];
			geomergemodesitems[(int)MergeGeometryMode.CLASSIC] = itemmergegeoclassic;
			geomergemodesitems[(int)MergeGeometryMode.MERGE] = itemmergegeo;
			geomergemodesitems[(int)MergeGeometryMode.REPLACE] = itemreplacegeo;
			
			// Visual Studio IDE doesn't let me set these in the designer :(
			buttonzoom.Font = menufile.Font;
			buttonzoom.DropDownDirection = ToolStripDropDownDirection.AboveLeft;
			buttongrid.Font = menufile.Font;
			buttongrid.DropDownDirection = ToolStripDropDownDirection.AboveLeft;

			// Event handlers
			buttonvisiblechangedhandler = ToolbarButtonVisibleChanged;
			//mxd
			display.OnKeyReleased += display_OnKeyReleased;
			toolbarContextMenu.KeyDown += toolbarContextMenu_KeyDown;
			toolbarContextMenu.KeyUp += toolbarContextMenu_KeyUp;
			linedefcolorpresets.DropDown.MouseLeave += linedefcolorpresets_MouseLeave;
			this.MouseCaptureChanged += MainForm_MouseCaptureChanged;
			
			// Apply shortcut keys
			ApplyShortcutKeys();
			
			// Make recent items list
			CreateRecentFiles();
			
			// Show splash
			ShowSplashDisplay();

			//mxd
			blinkTimer = new System.Timers.Timer {Interval = 500};
			blinkTimer.Elapsed += blinkTimer_Elapsed;

			//mxd. Debug Console
#if DEBUG
			modename.Visible = false;
#else
			console.Visible = false;
#endif

			//mxd. Hints
			hintsPanel = new HintsPanel();
			hintsDocker = new Docker("hints", "Help", hintsPanel);

			KeyPreview = true;
			PreviewKeyDown += new PreviewKeyDownEventHandler(MainForm_PreviewKeyDown);
		}
		
		#endregion
		
		#region ================== General

		// Editing mode changed!
		internal void EditModeChanged()
		{
			// Check appropriate button on interface
			// And show the mode name
			if(General.Editing.Mode != null)
			{
				General.MainWindow.CheckEditModeButton(General.Editing.Mode.EditModeButtonName);
				General.MainWindow.DisplayModeName(General.Editing.Mode.Attributes.DisplayName);
			}
			else
			{
				General.MainWindow.CheckEditModeButton("");
				General.MainWindow.DisplayModeName("");
			}

			// View mode only matters in classic editing modes
			bool isclassicmode = (General.Editing.Mode is ClassicMode);
			for(int i = 0; i < Renderer2D.NUM_VIEW_MODES; i++)
			{
				viewmodesitems[i].Enabled = isclassicmode;
				viewmodesbuttons[i].Enabled = isclassicmode;
			}

			//mxd. Merge geometry mode only matters in classic editing modes
			for(int i = 0; i < geomergemodesbuttons.Length; i++)
			{
				geomergemodesbuttons[i].Enabled = isclassicmode;
				geomergemodesitems[i].Enabled = isclassicmode;
			}

			UpdateEditMenu();
			UpdatePrefabsMenu();
		}

		// This makes a beep sound
		public void MessageBeep(MessageBeepType type)
		{
			General.MessageBeep(type);
		}

		// This sets up the interface
		internal void SetupInterface()
		{
			// Setup docker
			if(General.Settings.DockersPosition != 2 && General.Map != null)
			{
				LockUpdate();
				dockerspanel.Visible = true;
				dockersspace.Visible = true;

				// We can't place the docker easily when collapsed
				dockerspanel.Expand();

				// Setup docker width
				if(General.Settings.DockersWidth < dockerspanel.GetCollapsedWidth())
					General.Settings.DockersWidth = dockerspanel.GetCollapsedWidth();

				// Determine fixed space required
				if(General.Settings.CollapseDockers)
					dockersspace.Width = dockerspanel.GetCollapsedWidth();
				else
					dockersspace.Width = General.Settings.DockersWidth;

				// Setup docker
				int targetindex = this.Controls.IndexOf(display) + 1; //mxd
				if(General.Settings.DockersPosition == 0)
				{
					modestoolbar.Dock = DockStyle.Right; //mxd
					dockersspace.Dock = DockStyle.Left;
					AdjustDockersSpace(targetindex); //mxd
					dockerspanel.Setup(false);
					dockerspanel.Location = dockersspace.Location;
					dockerspanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
				}
				else
				{
					modestoolbar.Dock = DockStyle.Left; //mxd
					dockersspace.Dock = DockStyle.Right;
					AdjustDockersSpace(targetindex); //mxd
					dockerspanel.Setup(true);
					dockerspanel.Location = new Point(dockersspace.Right - General.Settings.DockersWidth, dockersspace.Top);
					dockerspanel.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
				}

				dockerspanel.Width = General.Settings.DockersWidth;
				dockerspanel.Height = dockersspace.Height;
				dockerspanel.BringToFront();

				if(General.Settings.CollapseDockers) dockerspanel.Collapse();

				UnlockUpdate();
			}
			else
			{
				dockerspanel.Visible = false;
				dockersspace.Visible = false;
				modestoolbar.Dock = DockStyle.Left; //mxd
			}
		}

		//mxd. dockersspace display index gets messed up while re-docking. This fixes it...
		private void AdjustDockersSpace(int targetindex)
		{
			while(this.Controls.IndexOf(dockersspace) != targetindex)
			{
				this.Controls.SetChildIndex(dockersspace, targetindex);
			}
		}
		
		// This updates all menus for the current status
		internal void UpdateInterface()
		{
			//mxd. Update title
			UpdateTitle();

			// Update the status bar
			UpdateStatusbar();
			
			// Update menus and toolbar icons
			UpdateFileMenu();
			UpdateEditMenu();
			UpdateViewMenu();
			UpdateModeMenu();
			UpdatePrefabsMenu();
			UpdateToolsMenu();
			UpdateToolbar();
			UpdateSkills();
			UpdateHelpMenu();
		}

		//mxd
		private void UpdateTitle()
		{
			string programname = this.Text = Application.ProductName + " R" + General.ThisAssembly.GetName().Version.Revision;
			if (Environment.Is64BitProcess)
				programname += " (64-bit)";
			else programname += " (32-bit)";

			// Map opened?
			if (General.Map != null)
			{
				// Get nice name
				string maptitle = (!string.IsNullOrEmpty(General.Map.Data.MapInfo.Title) ? ": " + General.Map.Data.MapInfo.Title : "");
				
				// Show map name and filename in caption
				this.Text = (mapchanged ? "\u25CF " : "") + General.Map.FileTitle + " (" + General.Map.Options.CurrentName + maptitle + ") - " + programname;
			}
			else
			{
				// Show normal caption
				this.Text = programname;
			}
		}
		
		// Generic event that invokes the tagged action
		public void InvokeTaggedAction(object sender, EventArgs e)
		{
			this.Update();
			
			if(sender is ToolStripItem)
				General.Actions.InvokeAction(((ToolStripItem)sender).Tag.ToString());
			else if(sender is Control)
				General.Actions.InvokeAction(((Control)sender).Tag.ToString());
			else
				General.Fail("InvokeTaggedAction used on an unexpected control.");
			
			this.Update();
		}
		
		#endregion
		
		#region ================== Window
		
		// This locks the window for updating
		internal void LockUpdate()
		{
			lockupdatecount++;
			if(lockupdatecount == 1) General.LockWindowUpdate(this.Handle);
		}

		// This unlocks for updating
		internal void UnlockUpdate()
		{
			lockupdatecount--;
			if(lockupdatecount == 0) General.LockWindowUpdate(IntPtr.Zero);
			if(lockupdatecount < 0) lockupdatecount = 0;
		}

		// This unlocks for updating
		/*internal void ForceUnlockUpdate()
		{
			if(lockupdatecount > 0) General.LockWindowUpdate(IntPtr.Zero);
			lockupdatecount = 0;
		}*/

		//mxd
		internal void UpdateMapChangedStatus()
		{
			if(General.Map == null || General.Map.IsChanged == mapchanged) return;
			mapchanged = General.Map.IsChanged;
			UpdateTitle();
		}
		
		// This sets the focus on the display for correct key input
		public bool FocusDisplay()
		{
			return display.Focus();
		}

		// Window is first shown
		private void MainForm_Shown(object sender, EventArgs e)
		{
			// Perform auto map loading action when the window is not delayed
			if(!General.DelayMainWindow) PerformAutoMapLoading();
		}

		// Auto map loading that must be done when the window is first shown after loading
		// but also before the window is shown when the -delaywindow parameter is given
		internal void PerformAutoMapLoading()
		{
			// Check if the command line arguments tell us to load something
			if(General.AutoLoadFile != null)
			{
				bool showdialog = false;
				MapOptions options = new MapOptions();
				
				// Any of the options already given?
				if(General.AutoLoadMap != null)
				{
					Configuration mapsettings;
					
					// Try to find existing options in the settings file
					string dbsfile = General.AutoLoadFile.Substring(0, General.AutoLoadFile.Length - 4) + ".dbs";
					if(File.Exists(dbsfile))
						try { mapsettings = new Configuration(dbsfile, true); }
						catch(Exception) { mapsettings = new Configuration(true); }
					else
						mapsettings = new Configuration(true);

					//mxd. Get proper configuration file
					bool longtexturenamessupported = false;
					string configfile = General.AutoLoadConfig;
					if(string.IsNullOrEmpty(configfile)) configfile = mapsettings.ReadSetting("gameconfig", "");
					if(configfile.Trim().Length == 0)
					{
						showdialog = true;
					}
					else
					{
						// Get if long texture names are supported from the game configuration
						ConfigurationInfo configinfo = General.GetConfigurationInfo(configfile);
						longtexturenamessupported = configinfo.Configuration.ReadSetting("longtexturenames", false);
					}

					// Set map name and other options
					options = new MapOptions(mapsettings, General.AutoLoadMap, longtexturenamessupported);

					// Set resource data locations
					options.CopyResources(General.AutoLoadResources);

					// Set strict patches
					options.StrictPatches = General.AutoLoadStrictPatches;
					
					// Set configuration file (constructor already does this, but we want this info from the cmd args if possible)
					options.ConfigFile = configfile;
				}
				else
				{
					// No options given
					showdialog = true;
				}

				// Show open map dialog?
				if(showdialog)
				{
					// Show open dialog
					General.OpenMapFile(General.AutoLoadFile, null);
				}
				else
				{
					// Open with options
					General.OpenMapFileWithOptions(General.AutoLoadFile, options);
				}
			}
		}

		// Window is loaded
		private void MainForm_Load(object sender, EventArgs e)
		{
			//mxd. Enable drag and drop
			this.AllowDrop = true;
			this.DragEnter += OnDragEnter;
			this.DragDrop += OnDragDrop;

			// Info panel state?
			bool expandedpanel = General.Settings.ReadSetting("windows." + configname + ".expandedinfopanel", true);
			if(expandedpanel != IsInfoPanelExpanded) ToggleInfoPanel();
		}

		// Window receives focus
		private void MainForm_Activated(object sender, EventArgs e)
		{
			windowactive = true;

			//UpdateInterface();
			ResumeExclusiveMouseInput();
			ReleaseAllKeys();
			FocusDisplay();
		}
		
		// Window loses focus
		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			windowactive = false;
			
			BreakExclusiveMouseInput();
			ReleaseAllKeys();
		}

		//mxd. Looks like in some cases StartMouseExclusive is called before app aquires the mouse
		// which results in setting Cursor.Clip not taking effect.
		private void MainForm_MouseCaptureChanged(object sender, EventArgs e)
		{
			if(mouseexclusive && windowactive && mouseinside && Cursor.Clip != display.RectangleToScreen(display.ClientRectangle))
				Cursor.Clip = display.RectangleToScreen(display.ClientRectangle);
		}

		// Window is being closed
		protected override void OnFormClosing(FormClosingEventArgs e) 
		{
			base.OnFormClosing(e);
			if(e.CloseReason == CloseReason.ApplicationExitCall) return;

			// Close the map
			if(General.CloseMap()) 
			{
				General.WriteLogLine("Closing main interface window...");

				// Stop timers
				statusflasher.Stop();
				statusresetter.Stop();
				blinkTimer.Stop(); //mxd

				// Stop exclusive mode, if any is active
				StopExclusiveMouseInput();
				StopProcessing();

				// Unbind methods
				General.Actions.UnbindMethods(this);

				// Determine window state to save
				General.Settings.WriteSetting("windows." + configname + ".expandedinfopanel", IsInfoPanelExpanded);

				// Save recent files
				SaveRecentFiles();

				// Terminate the program
				General.Terminate(true);
			} 
			else 
			{
				// Cancel the close
				e.Cancel = true;
			}
		}

		//mxd
		private void OnDragEnter(object sender, DragEventArgs e) 
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
			} 
			else 
			{
				e.Effect = DragDropEffects.None;
			}
		}

		//mxd
		private void OnDragDrop(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop)) 
			{
				string[] filepaths = (string[])e.Data.GetData(DataFormats.FileDrop);
				if(filepaths.Length != 1) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Cannot open multiple files at once!");
					return;
				}

				if(!File.Exists(filepaths[0])) 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Cannot open \"" + filepaths[0] + "\": file does not exist!");
					return;
				}

				string ext = Path.GetExtension(filepaths[0]);
				if(string.IsNullOrEmpty(ext) || ext.ToLower() != ".wad") 
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Cannot open \"" + filepaths[0] + "\": only WAD files can be loaded this way!");
					return;
				}

				// If we call General.OpenMapFile here, it will lock the source window in the waiting state untill OpenMapOptionsForm is closed.
				Timer t = new Timer { Tag = filepaths[0], Interval = 10 };
				t.Tick += OnDragDropTimerTick;
				t.Start();
			}
		}

		private void OnDragDropTimerTick(object sender, EventArgs e)
		{
			Timer t = sender as Timer;
			if(t != null)
			{
				t.Stop();
				string targetwad = t.Tag.ToString();
				this.Update(); // Update main window
				General.OpenMapFile(targetwad, null);
				UpdateGZDoomPanel();
			}
		}

		#endregion
		
		#region ================== Statusbar
		
		// This updates the status bar
		private void UpdateStatusbar()
		{
			// Map open?
			if(General.Map != null)
			{
				// Enable items
				xposlabel.Enabled = true;
				yposlabel.Enabled = true;
				poscommalabel.Enabled = true;
				zoomlabel.Enabled = true;
				buttonzoom.Enabled = true;
				gridlabel.Enabled = true;
				itemgrid05.Visible = General.Map.UDMF; //mxd
				itemgrid025.Visible = General.Map.UDMF; //mxd
				itemgrid0125.Visible = General.Map.UDMF; //mxd
				buttongrid.Enabled = true;
				configlabel.Text = General.Map.Config.Name;
				
				//mxd. Raise grid size to 1 if it was lower and the map isn't in UDMF
				if(!General.Map.UDMF && General.Map.Grid.GridSizeF < GridSetup.MINIMUM_GRID_SIZE)
					General.Map.Grid.SetGridSize(GridSetup.MINIMUM_GRID_SIZE);
			}
			else
			{
				// Disable items
				xposlabel.Text = "--";
				yposlabel.Text = "--";
				xposlabel.Enabled = false;
				yposlabel.Enabled = false;
				poscommalabel.Enabled = false;
				zoomlabel.Enabled = false;
				buttonzoom.Enabled = false;
				gridlabel.Enabled = false;
				buttongrid.Enabled = false;
				configlabel.Text = "";
			}
			
			UpdateStatusIcon();
		}
		
		// This flashes the status icon
		private void statusflasher_Tick(object sender, EventArgs e)
		{
			statusflashicon = !statusflashicon;
			UpdateStatusIcon();
			statusflashcount--;
			if(statusflashcount == 0) statusflasher.Stop();
		}
		
		// This resets the status to ready
		private void statusresetter_Tick(object sender, EventArgs e)
		{
			DisplayReady();
		}
		
		// This changes status text
		public void DisplayStatus(StatusType type, string message) { DisplayStatus(new StatusInfo(type, message)); }
		public void DisplayStatus(StatusInfo newstatus)
		{
			// Stop timers
			if(!newstatus.displayed)
			{
				statusresetter.Stop();
				statusflasher.Stop();
				statusflashicon = false;
			}
			
			// Determine what to do specifically for this status type
			switch(newstatus.type)
			{
				// Shows information without flashing the icon.
				case StatusType.Ready: //mxd
				case StatusType.Selection: //mxd
				case StatusType.Info:
					if(!newstatus.displayed)
					{
						statusresetter.Interval = INFO_RESET_DELAY;
						statusresetter.Start();
					}
					break;
					
				// Shows action information and flashes up the status icon once.	
				case StatusType.Action:
					if(!newstatus.displayed)
					{
						statusflashicon = true;
						statusflasher.Interval = ACTION_FLASH_INTERVAL;
						statusflashcount = ACTION_FLASH_COUNT;
						statusflasher.Start();
						statusresetter.Interval = ACTION_RESET_DELAY;
						statusresetter.Start();
					}
					break;
					
				// Shows a warning, makes a warning sound and flashes a warning icon.
				case StatusType.Warning:
					if(!newstatus.displayed)
					{
						MessageBeep(MessageBeepType.Warning);
						statusflasher.Interval = WARNING_FLASH_INTERVAL;
						statusflashcount = WARNING_FLASH_COUNT;
						statusflasher.Start();
						statusresetter.Interval = WARNING_RESET_DELAY;
						statusresetter.Start();
					}
					break;
			}
			
			// Update status description
			status = newstatus;
			status.displayed = true;
			statuslabel.Text = status.ToString(); //mxd. message -> ToString()
			
			// Update icon as well
			UpdateStatusIcon();
			
			// Refresh
			statusbar.Invalidate();
			//this.Update(); // ano - this is unneeded afaict and slow
		}
		
		// This changes status text to Ready
		public void DisplayReady()
		{
			DisplayStatus(StatusType.Ready, null);
		}
		
		// This updates the status icon
		private void UpdateStatusIcon()
		{
			int statusicon = 0;
			int statusflashindex = statusflashicon ? 1 : 0;
			
			// Loading icon?
			if((General.Map != null) && (General.Map.Data != null) && General.Map.Data.IsLoading)
				statusicon = 1;
			
			// Status type
			switch(status.type)
			{
				case StatusType.Ready:
				case StatusType.Info:
				case StatusType.Action:
				case StatusType.Selection: //mxd
					statuslabel.Image = STATUS_IMAGES[statusflashindex, statusicon];
					break;
				
				case StatusType.Busy:
					statuslabel.Image = STATUS_IMAGES[statusflashindex, 2];
					break;
					
				case StatusType.Warning:
					statuslabel.Image = STATUS_IMAGES[statusflashindex, 3];
					break;
			}
		}
		
		// This changes coordinates display
		public void UpdateCoordinates(Vector2D coords){ UpdateCoordinates(coords, false); } //mxd
		public void UpdateCoordinates(Vector2D coords, bool snaptogrid)
		{
			//mxd
			if(snaptogrid) coords = General.Map.Grid.SnappedToGrid(coords);
			
			// X position
			xposlabel.Text = (double.IsNaN(coords.x) ? "--" : coords.x.ToString("####0"));

			// Y position
			yposlabel.Text = (double.IsNaN(coords.y) ? "--" : coords.y.ToString("####0"));
		}

		// This changes zoom display
		internal void UpdateZoom(float scale)
		{
			// Update scale label
			zoomlabel.Text = (float.IsNaN(scale) ? "--" : (scale * 100).ToString("##0") + "%");
		}

		// Zoom to a specified level
		private void itemzoomto_Click(object sender, EventArgs e)
		{
			// In classic mode?
			if(General.Map != null && General.Editing.Mode is ClassicMode)
			{
				// Requested from menu?
				ToolStripMenuItem item = sender as ToolStripMenuItem;
				if(item != null)
				{
					// Get integral zoom level
					int zoom = int.Parse(item.Tag.ToString(), CultureInfo.InvariantCulture);

					// Zoom now
					((ClassicMode)General.Editing.Mode).SetZoom(zoom / 100f);
				}
			}
		}

		// Zoom to fit in screen
		private void itemzoomfittoscreen_Click(object sender, EventArgs e)
		{
			// In classic mode?
			if(General.Map != null && General.Editing.Mode is ClassicMode)
				((ClassicMode)General.Editing.Mode).CenterInScreen();
		}

		// This changes grid display
		internal void UpdateGrid(double gridsize)
		{
			// Update grid label
			gridlabel.Text = (gridsize == 0 ? "--" : gridsize + " mp");
		}

		// Set grid to a specified size
		private void itemgridsize_Click(object sender, EventArgs e)
		{
			if(General.Map == null) return;

			// In classic mode?
			if(General.Editing.Mode is ClassicMode)
			{
				// Requested from menu?
				ToolStripMenuItem item = sender as ToolStripMenuItem;
				if(item != null)
				{
					//mxd. Get decimal zoom level
					float size = float.Parse(item.Tag.ToString(), CultureInfo.InvariantCulture);

					//mxd. Disable automatic grid resizing
					DisableDynamicGridResize();

					// Change grid size
					General.Map.Grid.SetGridSize(size);
					
					// Redraw display
					RedrawDisplay();
				}
			}
		}

		// Show grid setup
		private void itemgridcustom_Click(object sender, EventArgs e)
		{
			if(General.Map != null) GridSetup.ShowGridSetup();
		}
		
		#endregion

		#region ================== Display

		// This shows the splash screen on display
		internal void ShowSplashDisplay()
		{
			// Change display to show splash logo
			display.SetSplashLogoDisplay();
			display.Cursor = Cursors.Default;
			this.Update();
		}
		
		// This clears the display
		internal void ClearDisplay()
		{
			// Clear the display
			display.SetManualRendering();
			this.Update();
		}

		// This sets the display cursor
		public void SetCursor(Cursor cursor)
		{
			// Only when a map is open
			if(General.Map != null) display.Cursor = cursor;
		}

		// This redraws the display on the next paint event
		public void RedrawDisplay()
		{
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditRedrawDisplayBegin();
				General.Editing.Mode.OnRedrawDisplay();
				General.Plugins.OnEditRedrawDisplayEnd();
				statistics.UpdateStatistics(); //mxd
			}
			else
			{
				display.Invalidate();
			}
		}

		// This event is called when a repaint is needed
		private void display_Paint(object sender, PaintEventArgs e)
		{
			if(General.Map != null)
			{
				if(General.Editing.Mode != null)
				{
					if(!displayresized) General.Editing.Mode.OnPresentDisplay();
				}
				else
				{
					if(General.Colors != null)
						e.Graphics.Clear(Color.FromArgb(General.Colors.Background.ToInt()));
					else
						e.Graphics.Clear(SystemColors.ControlDarkDark);
				}
			}
		}
		
		// Redraw requested
		private void redrawtimer_Tick(object sender, EventArgs e)
		{
			// Disable timer (only redraw once)
			redrawtimer.Enabled = false;

			// Don't do anything when minimized (mxd)
			if(this.WindowState == FormWindowState.Minimized) return;

			// Resume control layouts
			//if(displayresized) General.LockWindowUpdate(IntPtr.Zero);

			// Map opened?
			if(General.Map != null)
			{
				// Display was resized?
				if(displayresized)
				{
					//mxd. Aspect ratio may've been changed
					General.Map.CRenderer3D.CreateProjection();
				}

				// This is a dirty trick to give the display a new mousemove event with correct arguments
				if(mouseinside)
				{
					Point mousepos = Cursor.Position;
					Cursor.Position = new Point(mousepos.X + 1, mousepos.Y + 1);
					Cursor.Position = mousepos;
				}
				
				// Redraw now
				RedrawDisplay();
			}

			// Display resize is done
			displayresized = false;
		}

		// Display size changes
		private void display_Resize(object sender, EventArgs e)
		{
			// Resizing
			//if(!displayresized) General.LockWindowUpdate(display.Handle);
			displayresized = true;

			//mxd. Separators may need updating
			UpdateSeparators();
			
			// Request redraw
			if(!redrawtimer.Enabled) redrawtimer.Enabled = true;
		}
		
		// This requests a delayed redraw
		public void DelayedRedraw()
		{
			// Request redraw
			if(!redrawtimer.Enabled) redrawtimer.Enabled = true;
		}
		
		// Mouse click
		private void display_MouseClick(object sender, MouseEventArgs e)
		{
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseClick(e);
				General.Editing.Mode.OnMouseClick(e);
			}
		}

		// Mouse doubleclick
		private void display_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseDoubleClick(e);
				General.Editing.Mode.OnMouseDoubleClick(e);
			}
		}

		// Mouse down
		private void display_MouseDown(object sender, MouseEventArgs e)
		{
			int key = 0;
			
			LoseFocus(this, EventArgs.Empty);
			
			int mod = 0;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Apply button
			mousebuttons |= e.Button;
			
			// Create key
			switch(e.Button)
			{
				case MouseButtons.Left: key = (int)Keys.LButton; break;
				case MouseButtons.Middle: key = (int)Keys.MButton; break;
				case MouseButtons.Right: key = (int)Keys.RButton; break;
				case MouseButtons.XButton1: key = (int)Keys.XButton1; break;
				case MouseButtons.XButton2: key = (int)Keys.XButton2; break;
			}
			
			// Invoke any actions associated with this key
			General.Actions.KeyPressed(key | mod);
			
			// Invoke on editing mode
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseDown(e);
				General.Editing.Mode.OnMouseDown(e);
			}
		}

		// Mouse enters
		private void display_MouseEnter(object sender, EventArgs e)
		{
			mouseinside = true;
			//mxd. Skip when in mouseexclusive (e.g. Visual) mode to avoid mouse disappearing when moving it
			// on top of inactive editor window while Visual mode is active
			if((General.Map != null) && (mouseinput == null) && (General.Editing.Mode != null) && !mouseexclusive)
			{
				General.Plugins.OnEditMouseEnter(e);
				General.Editing.Mode.OnMouseEnter(e);
				if(Application.OpenForms.Count == 1 || editformopen) display.Focus(); //mxd
			}
		}

		// Mouse leaves
		private void display_MouseLeave(object sender, EventArgs e)
		{
			mouseinside = false;
			if((General.Map != null) && (mouseinput == null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseLeave(e);
				General.Editing.Mode.OnMouseLeave(e);
			}
		}

		// Mouse moves
		private void display_MouseMove(object sender, MouseEventArgs e)
		{
			if((General.Map != null) && (mouseinput == null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseMove(e);
				General.Editing.Mode.OnMouseMove(e);
			}
		}

		// Mouse up
		private void display_MouseUp(object sender, MouseEventArgs e)
		{
			int key = 0;
			
			int mod = 0;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Apply button
			mousebuttons &= ~e.Button;
			
			// Create key
			switch(e.Button)
			{
				case MouseButtons.Left: key = (int)Keys.LButton; break;
				case MouseButtons.Middle: key = (int)Keys.MButton; break;
				case MouseButtons.Right: key = (int)Keys.RButton; break;
				case MouseButtons.XButton1: key = (int)Keys.XButton1; break;
				case MouseButtons.XButton2: key = (int)Keys.XButton2; break;
			}
			
			// Invoke any actions associated with this key
			General.Actions.KeyReleased(key | mod);

			// Invoke on editing mode
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				General.Plugins.OnEditMouseUp(e);
				General.Editing.Mode.OnMouseUp(e);
			}
		}
		
		#endregion

		#region ================== Input
		
		// This is a tool to lock the mouse in exclusive mode
		private void StartMouseExclusive()
		{
			// Not already locked?
			if(mouseinput == null)
			{
				// Start special input device
				mouseinput = new MouseInput(this);

				// Lock and hide the mouse in window
				Cursor.Position = display.PointToScreen(new Point(display.ClientSize.Width / 2, display.ClientSize.Height / 2)); //mxd
				Cursor.Clip = display.RectangleToScreen(display.ClientRectangle);
				Cursor.Hide();
			}
		}

		// This is a tool to unlock the mouse
		private void StopMouseExclusive()
		{
			// Locked?
			if(mouseinput != null)
			{
				// Stop special input device
				mouseinput.Dispose();
				mouseinput = null;

				// Release and show the mouse
				Cursor.Clip = Rectangle.Empty;
				Cursor.Position = display.PointToScreen(new Point(display.ClientSize.Width / 2, display.ClientSize.Height / 2));
				Cursor.Show();
			}
		}
		
		// This requests exclusive mouse input
		public void StartExclusiveMouseInput()
		{
			// Only when not already in exclusive mode
			if(!mouseexclusive)
			{
				General.WriteLogLine("Starting exclusive mouse input mode...");
				
				// Start special input device
				StartMouseExclusive();
				mouseexclusive = true;
				mouseexclusivebreaklevel = 0;
			}
		}
		
		// This stops exclusive mouse input
		public void StopExclusiveMouseInput()
		{
			// Only when in exclusive mode
			if(mouseexclusive)
			{
				General.WriteLogLine("Stopping exclusive mouse input mode...");

				// Stop special input device
				StopMouseExclusive();
				mouseexclusive = false;
				mouseexclusivebreaklevel = 0;
			}
		}

		// This temporarely breaks exclusive mode and counts the break level
		public void BreakExclusiveMouseInput()
		{
			// Only when in exclusive mode
			if(mouseexclusive)
			{
				// Stop special input device
				StopMouseExclusive();
				
				// Count the break level
				mouseexclusivebreaklevel++;
			}
		}

		// This resumes exclusive mode from a break when all breaks have been called to resume
		public void ResumeExclusiveMouseInput()
		{
			// Only when in exclusive mode
			if(mouseexclusive && (mouseexclusivebreaklevel > 0))
			{
				// Decrease break level
				mouseexclusivebreaklevel--;

				// All break levels resumed? Then lock the mouse again.
				if(mouseexclusivebreaklevel == 0)
					StartMouseExclusive();
			}
		}

		// This releases all keys
		internal void ReleaseAllKeys()
		{
			General.Actions.ReleaseAllKeys();
			mousebuttons = MouseButtons.None;
			shift = false;
			ctrl = false;
			alt = false;
		}
		
		// When the mouse wheel is changed
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			int mod = 0;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Scrollwheel up?
			if(e.Delta > 0)
			{
				// Invoke actions for scrollwheel
				//for(int i = 0; i < e.Delta; i += 120)
				General.Actions.KeyPressed((int)SpecialKeys.MScrollUp | mod);
				General.Actions.KeyReleased((int)SpecialKeys.MScrollUp | mod);
			}
			// Scrollwheel down?
			else if(e.Delta < 0)
			{
				// Invoke actions for scrollwheel
				//for(int i = 0; i > e.Delta; i -= 120)
				General.Actions.KeyPressed((int)SpecialKeys.MScrollDown | mod);
				General.Actions.KeyReleased((int)SpecialKeys.MScrollDown | mod);
			}
			
			// Let the base know
			base.OnMouseWheel(e);
		}

		// [ZZ]
		private void OnMouseHWheel(int delta)
		{
			int mod = 0;
			if (alt) mod |= (int)Keys.Alt;
			if (shift) mod |= (int)Keys.Shift;
			if (ctrl) mod |= (int)Keys.Control;

			// Scrollwheel left?
			if (delta < 0)
			{
				General.Actions.KeyPressed((int)SpecialKeys.MScrollLeft | mod);
				General.Actions.KeyReleased((int)SpecialKeys.MScrollLeft | mod);
			}
			else if (delta > 0)
			{
				General.Actions.KeyPressed((int)SpecialKeys.MScrollRight | mod);
				General.Actions.KeyReleased((int)SpecialKeys.MScrollRight | mod);
			}

			// base? what base?
		}
		
		private void MainForm_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.F10)
				e.IsInputKey = true;
		}

		// When a key is pressed
		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			int mod = 0;
			
			// Keep key modifiers
			alt = e.Alt;
			shift = e.Shift;
			ctrl = e.Control;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Don't process any keys when they are meant for other input controls
			if((ActiveControl == null) || (ActiveControl == display))
			{
				// Invoke any actions associated with this key
				General.Actions.UpdateModifiers(mod);
				e.Handled = General.Actions.KeyPressed((int)e.KeyData);
				
				// Invoke on editing mode
				if((General.Map != null) && (General.Editing.Mode != null))
				{
					General.Plugins.OnEditKeyDown(e);
					General.Editing.Mode.OnKeyDown(e);
				}

				// Handled
				if(e.Handled)
					e.SuppressKeyPress = true;
			}
			
			// F1 pressed?
			if((e.KeyCode == Keys.F1) && (e.Modifiers == Keys.None))
			{
				// No action bound to F1?
				Actions.Action[] f1actions = General.Actions.GetActionsByKey((int)e.KeyData);
				if(f1actions.Length == 0)
				{
					// If we don't have any map open, show the Main Window help
					// otherwise, give the help request to the editing mode so it
					// can open the appropriate help file.
					if((General.Map == null) || (General.Editing.Mode == null))
					{
						General.ShowHelp("introduction.html");
					}
					else
					{
						General.Editing.Mode.OnHelp();
					}
				}
			}

			if (e.KeyCode == Keys.F10)
			{
				Actions.Action[] f10actions = General.Actions.GetActionsByKey((int)e.KeyData);
				if (f10actions.Length > 0)
				{
					e.SuppressKeyPress = true;
					e.Handled = true;
				}
			}
		}

		// When a key is released
		private void MainForm_KeyUp(object sender, KeyEventArgs e)
		{
			int mod = 0;
			
			// Keep key modifiers
			alt = e.Alt;
			shift = e.Shift;
			ctrl = e.Control;
			if(alt) mod |= (int)Keys.Alt;
			if(shift) mod |= (int)Keys.Shift;
			if(ctrl) mod |= (int)Keys.Control;
			
			// Don't process any keys when they are meant for other input controls
			if((ActiveControl == null) || (ActiveControl == display))
			{
				// Invoke any actions associated with this key
				General.Actions.UpdateModifiers(mod);
				e.Handled = General.Actions.KeyReleased((int)e.KeyData);
				
				// Invoke on editing mode
				if((General.Map != null) && (General.Editing.Mode != null))
				{
					General.Plugins.OnEditKeyUp(e);
					General.Editing.Mode.OnKeyUp(e);
				}
				
				// Handled
				if(e.Handled)
					e.SuppressKeyPress = true;
			}

			if (e.KeyCode == Keys.F10)
			{
				Actions.Action[] f10actions = General.Actions.GetActionsByKey((int)e.KeyData);
				if (f10actions.Length > 0)
				{
					e.SuppressKeyPress = true;
					e.Handled = true;
				}
			}
		}

		//mxd. Sometimes it's handeled by RenderTargetControl, not by MainForm leading to keys being "stuck"
		private void display_OnKeyReleased(object sender, KeyEventArgs e)
		{
			MainForm_KeyUp(sender, e);
		}
		
		// These prevent focus changes by way of TAB or Arrow keys
		protected override bool IsInputChar(char charCode) { return false; }
		protected override bool IsInputKey(Keys keyData) { return false; }
		protected override bool ProcessKeyPreview(ref Message m) { return false; }
		protected override bool ProcessDialogKey(Keys keyData) { return false; }
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { return false; }
		
		// This fixes some odd input behaviour
		private void display_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if((ActiveControl == null) || (ActiveControl == display))
			{
				LoseFocus(this, EventArgs.Empty);
				KeyEventArgs ea = new KeyEventArgs(e.KeyData);
				MainForm_KeyDown(sender, ea);
			}
		}
		
		#endregion

		#region ================== Toolbar
		
		// This updates the skills list
		private void UpdateSkills()
		{
			// Clear list
			buttontest.DropDownItems.Clear();
			
			// Map loaded?
			if(General.Map != null)
			{
				// Make the new items list
				List<ToolStripItem> items = new List<ToolStripItem>(General.Map.Config.Skills.Count * 2 + General.Map.ConfigSettings.TestEngines.Count + 2);
				
				// Positive skills are with monsters
				foreach(SkillInfo si in General.Map.Config.Skills)
				{
					ToolStripMenuItem menuitem = new ToolStripMenuItem(si.ToString());
					menuitem.Image = Resources.Monster2;
					menuitem.Click += TestSkill_Click;
					menuitem.Tag = si.Index;
					menuitem.Checked = (General.Settings.TestMonsters && (General.Map.ConfigSettings.TestSkill == si.Index));
					items.Add(menuitem);
				}

				// Add seperator
				items.Add(new ToolStripSeparator { Padding = new Padding(0, 3, 0, 3) });

				// Negative skills are without monsters
				foreach(SkillInfo si in General.Map.Config.Skills)
				{
					ToolStripMenuItem menuitem = new ToolStripMenuItem(si.ToString());
					menuitem.Image = Resources.Monster3;
					menuitem.Click += TestSkill_Click;
					menuitem.Tag = -si.Index;
					menuitem.Checked = (!General.Settings.TestMonsters && (General.Map.ConfigSettings.TestSkill == si.Index));
					items.Add(menuitem);
				}

				//mxd. Add seperator
				items.Add(new ToolStripSeparator { Padding = new Padding(0, 3, 0, 3) });

				//mxd. Add test engines
				for(int i = 0; i < General.Map.ConfigSettings.TestEngines.Count; i++)
				{
					if(General.Map.ConfigSettings.TestEngines[i].TestProgramName == EngineInfo.DEFAULT_ENGINE_NAME) continue;
					ToolStripMenuItem menuitem = new ToolStripMenuItem(General.Map.ConfigSettings.TestEngines[i].TestProgramName);
					menuitem.Image = General.Map.ConfigSettings.TestEngines[i].TestProgramIcon;
					menuitem.Click += TestEngine_Click;
					menuitem.Tag = i;
					menuitem.Checked = (i == General.Map.ConfigSettings.CurrentEngineIndex);
					items.Add(menuitem);
				}
				
				// Add to list
				buttontest.DropDownItems.AddRange(items.ToArray());
			}
		}

		//mxd
		internal void DisableDynamicGridResize()
		{
			if(General.Settings.DynamicGridSize)
			{
				General.Settings.DynamicGridSize = false;
				itemdynamicgridsize.Checked = false;
				buttontoggledynamicgrid.Checked = false;
			}
		}

		//mxd
		private void TestEngine_Click(object sender, EventArgs e)
		{
			General.Map.ConfigSettings.CurrentEngineIndex = (int)(((ToolStripMenuItem)sender).Tag);
			General.Map.ConfigSettings.Changed = true;
			General.Map.Launcher.TestAtSkill(General.Map.ConfigSettings.TestSkill);
			UpdateSkills();
		}
		
		// Event handler for testing at a specific skill
		private void TestSkill_Click(object sender, EventArgs e)
		{
			int skill = (int)((sender as ToolStripMenuItem).Tag);
			General.Settings.TestMonsters = (skill > 0);
			General.Map.ConfigSettings.TestSkill = Math.Abs(skill);
			General.Map.Launcher.TestAtSkill(Math.Abs(skill));
			UpdateSkills();
		}
		
		// This loses focus
		private void LoseFocus(object sender, EventArgs e)
		{
			// Lose focus!
			try { display.Focus(); } catch(Exception) { }
			this.ActiveControl = null;
		}

		//mxd. Things filter selected
		private void thingfilters_DropDownItemClicked(object sender, EventArgs e)
		{
			// Only possible when a map is open
			if((General.Map != null) && !updatingfilters)
			{
				updatingfilters = true;
				ToolStripMenuItem clickeditem = sender as ToolStripMenuItem;

				// Keep already selected items selected
				if(!clickeditem.Checked)
				{
					clickeditem.Checked = true;
					updatingfilters = false;
					return;
				}

				// Change filter
				ThingsFilter f = clickeditem.Tag as ThingsFilter;
				General.Map.ChangeThingFilter(f);

				// Deselect other items...
				foreach(var item in thingfilters.DropDown.Items)
				{
					if(item != clickeditem) ((ToolStripMenuItem)item).Checked = false;
				}

				// Update button text
				thingfilters.Text = f.Name;

				updatingfilters = false;
			}
			
			// Lose focus
			LoseFocus(sender, e);
		}
		
		//mxd. This updates the things filter on the toolbar
		internal void UpdateThingsFilters()
		{
			// Only possible to list filters when a map is open
			if(General.Map != null)
			{
				ThingsFilter oldfilter = null;

				// Anything selected?
				foreach(var item in thingfilters.DropDown.Items)
				{
					if(((ToolStripMenuItem)item).Checked)
					{
						oldfilter = ((ToolStripMenuItem)item).Tag as ThingsFilter;
						break;
					}
				}
				
				updatingfilters = true;

				// Clear the list
				thingfilters.DropDown.Items.Clear();

				// Add null filter
				if(General.Map.ThingsFilter is NullThingsFilter)
					thingfilters.DropDown.Items.Add(CreateThingsFilterMenuItem(General.Map.ThingsFilter));
				else
					thingfilters.DropDown.Items.Add(CreateThingsFilterMenuItem(new NullThingsFilter()));

				// Add all filters, select current one
				foreach(ThingsFilter f in General.Map.ConfigSettings.ThingsFilters)
					thingfilters.DropDown.Items.Add(CreateThingsFilterMenuItem(f));

				updatingfilters = false;
				
				// No filter selected?
				ToolStripMenuItem selecteditem = null;
				foreach(var i in thingfilters.DropDown.Items)
				{
					ToolStripMenuItem item = i as ToolStripMenuItem;
					if(item.Checked)
					{
						selecteditem = item;
						break;
					}
				}

				if(selecteditem == null)
				{
					ToolStripMenuItem first = thingfilters.DropDown.Items[0] as ToolStripMenuItem;
					first.Checked = true;
				}
				// Another filter got selected?
				else if(selecteditem.Tag != oldfilter)
				{
					selecteditem.Checked = true;
				}

				// Update button text
				if(selecteditem != null)
					thingfilters.Text = ((ThingsFilter)selecteditem.Tag).Name;
			}
			else
			{
				// Clear the list
				thingfilters.DropDown.Items.Clear();
				thingfilters.Text = "(show all)";
			}
		}

		// This selects the things filter based on the filter set on the map manager
		internal void ReflectThingsFilter()
		{
			if(!updatingfilters)
			{
				updatingfilters = true;
				
				// Select current filter
				bool selecteditemfound = false;
				foreach(var i in thingfilters.DropDown.Items)
				{
					ToolStripMenuItem item = i as ToolStripMenuItem;
					ThingsFilter f = item.Tag as ThingsFilter;

					if(f == General.Map.ThingsFilter)
					{
						item.Checked = true;
						thingfilters.Text = f.Name;
						selecteditemfound = true;
					}
					else
					{
						item.Checked = false;
					}
				}

				// Not in the list?
				if(!selecteditemfound)
				{
					// Select nothing
					thingfilters.Text = "(show all)"; //mxd
				}

				updatingfilters = false;
			}
		}

		//mxd
		private ToolStripMenuItem CreateThingsFilterMenuItem(ThingsFilter f)
		{
			// Make decorated name
			string name = f.Name;
			if(f.Invert) name = "!" + name;
			switch(f.DisplayMode)
			{
				case ThingsFilterDisplayMode.CLASSIC_MODES_ONLY: name += " [2D]"; break;
				case ThingsFilterDisplayMode.VISUAL_MODES_ONLY: name += " [3D]"; break;
			}

			// Create and select the item
			ToolStripMenuItem item = new ToolStripMenuItem(name) { CheckOnClick = true, Tag = f };
			item.CheckedChanged += thingfilters_DropDownItemClicked;
			item.Checked = (f == General.Map.ThingsFilter);
			
			// Update icon
			if(!(f is NullThingsFilter) && !f.IsValid())
			{
				item.Image = Resources.Warning;
				//item.ImageScaling = ToolStripItemImageScaling.None;
			}

			return item;
		}

		//mxd. Linedef color preset (de)selected
		private void linedefcolorpresets_ItemClicked(object sender, EventArgs e)
		{
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			((LinedefColorPreset)item.Tag).Enabled = item.Checked;

			List<string> enablednames = new List<string>();
			foreach(LinedefColorPreset p in General.Map.ConfigSettings.LinedefColorPresets)
			{
				if(p.Enabled) enablednames.Add(p.Name);
			}

			// Update button text
			UpdateColorPresetsButtonText(linedefcolorpresets, enablednames);
			
			General.Map.Map.UpdateCustomLinedefColors();
			General.Map.ConfigSettings.Changed = true;

			// Update display
			if(General.Editing.Mode is ClassicMode) General.Interface.RedrawDisplay();
		}

		//mxd. Handle Shift key...
		private void linedefcolorpresets_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			linedefcolorpresets.DropDown.AutoClose = (ModifierKeys != Keys.Shift);
		}

		//mxd. Handles the mouse leaving linedefcolorpresets.DropDown and clicking on linedefcolorpresets button
		private void linedefcolorpresets_MouseLeave(object sender, EventArgs e)
		{
			linedefcolorpresets.DropDown.AutoClose = true;
		}

		//mxd. This updates linedef color presets selector on the toolbar
		internal void UpdateLinedefColorPresets()
		{
			// Refill the list
			List<string> enablednames = new List<string>();
			linedefcolorpresets.DropDown.Items.Clear();

			if(General.Map != null)
			{
				foreach(LinedefColorPreset p in General.Map.ConfigSettings.LinedefColorPresets)
				{
					// Create menu item
					ToolStripMenuItem item = new ToolStripMenuItem(p.Name)
					{
						CheckOnClick = true,
						Tag = p,
						//ImageScaling = ToolStripItemImageScaling.None,
						Checked = p.Enabled,
						ToolTipText = "Hold Shift to toggle several items at once"
					};

					// Create icon
					if(p.IsValid())
					{
						Bitmap icon = new Bitmap(16, 16);
						using(Graphics g = Graphics.FromImage(icon))
						{
							g.FillRectangle(new SolidBrush(p.Color.ToColor()), 2, 3, 12, 10);
							g.DrawRectangle(Pens.Black, 2, 3, 11, 9);
						}

						item.Image = icon;
					}
					// Or use the warning icon
					else
					{
						item.Image = Resources.Warning;
					}

					item.CheckedChanged += linedefcolorpresets_ItemClicked;
					linedefcolorpresets.DropDown.Items.Add(item);
					if(p.Enabled) enablednames.Add(p.Name);
				}
			}

			// Update button text
			UpdateColorPresetsButtonText(linedefcolorpresets, enablednames);
		}

		//mxd
		private static void UpdateColorPresetsButtonText(ToolStripItem button, List<string> names)
		{
			if(names.Count == 0)
			{
				button.Text = "No active presets";
			}
			else
			{
				string text = string.Join(", ", names.ToArray());
				if(TextRenderer.MeasureText(text, button.Font).Width > button.Width)
					button.Text = names.Count + (names.Count.ToString(CultureInfo.InvariantCulture).EndsWith("1") ? " preset" : " presets") + " active";
				else
					button.Text = text;
			}
		}

		//mxd
		public void BeginToolbarUpdate()
		{
			toolbar.SuspendLayout();
			modestoolbar.SuspendLayout();
			modecontrolstoolbar.SuspendLayout();
		}

		//mxd
		public void EndToolbarUpdate()
		{
			toolbar.ResumeLayout(true);
			modestoolbar.ResumeLayout(true);
			modecontrolstoolbar.ResumeLayout(true);
		}

		// This adds a button to the toolbar
		public void AddButton(ToolStripItem button) { AddButton(button, ToolbarSection.Custom, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly())); }
		public void AddButton(ToolStripItem button, ToolbarSection section) { AddButton(button, section, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly())); }
		private void AddButton(ToolStripItem button, ToolbarSection section, Plugin plugin)
		{
			// Fix tags to full action names
			ToolStripItemCollection items = new ToolStripItemCollection(toolbar, new ToolStripItem[0]);
			items.Add(button);
			RenameTagsToFullActions(items, plugin);

			// Add to the list so we can update it as needed
			PluginToolbarButton buttoninfo = new PluginToolbarButton();
			buttoninfo.button = button;
			buttoninfo.section = section;
			pluginbuttons.Add(buttoninfo);
			
			// Bind visible changed event
			if(!(button is ToolStripSeparator)) button.VisibleChanged += buttonvisiblechangedhandler;
			
			// Insert the button in the right section
			switch(section)
			{
				case ToolbarSection.File: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorfile), button); break;
				case ToolbarSection.Script: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorscript), button); break;
				case ToolbarSection.UndoRedo: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorundo), button); break;
				case ToolbarSection.CopyPaste: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorcopypaste), button); break;
				case ToolbarSection.Prefabs: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorprefabs), button); break;
				case ToolbarSection.Things: toolbar.Items.Insert(toolbar.Items.IndexOf(buttonviewnormal), button); break;
				case ToolbarSection.Views: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorviews), button); break;
				case ToolbarSection.Geometry: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatorgeometry), button); break;
				case ToolbarSection.Helpers: toolbar.Items.Insert(toolbar.Items.IndexOf(separatorgzmodes), button); break; //mxd
				case ToolbarSection.Testing: toolbar.Items.Insert(toolbar.Items.IndexOf(seperatortesting), button); break;
				case ToolbarSection.Modes: modestoolbar.Items.Add(button); break; //mxd
				case ToolbarSection.Custom: modecontrolstoolbar.Items.Add(button); modecontrolstoolbar.Visible = true; break; //mxd
			}
			
			UpdateToolbar();
		}

		//mxd
		public void AddModesButton(ToolStripItem button, string group) 
		{
			// Set proper styling
			button.Padding = new Padding(0, 1, 0, 1);
			button.Margin = new Padding();
			
			// Fix tags to full action names
			ToolStripItemCollection items = new ToolStripItemCollection(toolbar, new ToolStripItem[0]);
			items.Add(button);
			RenameTagsToFullActions(items, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly()));

			// Add to the list so we can update it as needed
			PluginToolbarButton buttoninfo = new PluginToolbarButton();
			buttoninfo.button = button;
			buttoninfo.section = ToolbarSection.Modes;
			pluginbuttons.Add(buttoninfo);

			button.VisibleChanged += buttonvisiblechangedhandler;

			//find the separator we need
			for(int i = 0; i < modestoolbar.Items.Count; i++) 
			{
				if(modestoolbar.Items[i] is ToolStripSeparator && modestoolbar.Items[i].Text == group) 
				{
					modestoolbar.Items.Insert(i + 1, button);
					break;
				}
			}

			UpdateToolbar();
		}

		// Removes a button
		public void RemoveButton(ToolStripItem button)
		{
			// Find in the list and remove it
			PluginToolbarButton buttoninfo = new PluginToolbarButton();
			for(int i = 0; i < pluginbuttons.Count; i++)
			{
				if(pluginbuttons[i].button == button)
				{
					buttoninfo = pluginbuttons[i];
					pluginbuttons.RemoveAt(i);
					break;
				}
			}

			if(buttoninfo.button != null)
			{
				// Unbind visible changed event
				if(!(button is ToolStripSeparator)) button.VisibleChanged -= buttonvisiblechangedhandler;

				//mxd. Remove button from toolbars
				switch(buttoninfo.section) 
				{
					case ToolbarSection.Modes:
						modestoolbar.Items.Remove(button);
						break;
					case ToolbarSection.Custom:
						modecontrolstoolbar.Items.Remove(button);
						modecontrolstoolbar.Visible = (modecontrolstoolbar.Items.Count > 0);
						break;
					default:
						toolbar.Items.Remove(button);
						break;
				}
				
				UpdateSeparators();
			}
		}

		// This handle visibility changes in the toolbar buttons
		private void ToolbarButtonVisibleChanged(object sender, EventArgs e)
		{
			if(!preventupdateseperators)
			{
				// Update the seeprators
				UpdateSeparators();
			}
		}

		// This hides redundant separators
		internal void UpdateSeparators()
		{
			UpdateToolStripSeparators(toolbar.Items, false);
			UpdateToolStripSeparators(menumode.DropDownItems, true);

			//mxd
			UpdateToolStripSeparators(modestoolbar.Items, true);
			UpdateToolStripSeparators(modecontrolstoolbar.Items, true);
		}
		
		// This hides redundant separators
		private static void UpdateToolStripSeparators(ToolStripItemCollection items, bool defaultvisible)
		{
			ToolStripItem pvi = null;
			foreach(ToolStripItem i in items) 
			{
				bool separatorvisible = false;

				// This is a seperator?
				if(i is ToolStripSeparator) 
				{
					// Make visible when previous item was not a seperator
					separatorvisible = !(pvi is ToolStripSeparator) && (pvi != null);
					i.Visible = separatorvisible;
				}

				// Keep as previous visible item
				if(i.Visible || separatorvisible || (defaultvisible && !(i is ToolStripSeparator))) pvi = i;
			}

			// Hide last item if it is a seperator
			if(pvi is ToolStripSeparator) pvi.Visible = false;
		}
		
		// This enables or disables all editing mode items and toolbar buttons
		private void UpdateToolbar()
		{
			preventupdateseperators = true;
			
			// Show/hide items based on preferences
			bool maploaded = (General.Map != null); //mxd
			buttonnewmap.Visible = General.Settings.ToolbarFile;
			buttonopenmap.Visible = General.Settings.ToolbarFile;
			buttonsavemap.Visible = General.Settings.ToolbarFile;
			buttonscripteditor.Visible = General.Settings.ToolbarScript && maploaded && General.Map.Config.HasScriptLumps(); // Only show script editor if there a script lumps defined
			buttonundo.Visible = General.Settings.ToolbarUndo && maploaded;
			buttonredo.Visible = General.Settings.ToolbarUndo && maploaded;
			buttoncut.Visible = General.Settings.ToolbarCopy && maploaded;
			buttoncopy.Visible = General.Settings.ToolbarCopy && maploaded;
			buttonpaste.Visible = General.Settings.ToolbarCopy && maploaded;
			buttoninsertprefabfile.Visible = General.Settings.ToolbarPrefabs && maploaded;
			buttoninsertpreviousprefab.Visible = General.Settings.ToolbarPrefabs && maploaded;
			buttonthingsfilter.Visible = General.Settings.ToolbarFilter && maploaded;
			thingfilters.Visible = General.Settings.ToolbarFilter && maploaded;
			separatorlinecolors.Visible = General.Settings.ToolbarFilter && maploaded; //mxd
			buttonlinededfcolors.Visible = General.Settings.ToolbarFilter && maploaded; //mxd
			linedefcolorpresets.Visible = General.Settings.ToolbarFilter && maploaded; //mxd
			separatorfilters.Visible = General.Settings.ToolbarViewModes && maploaded; //mxd
			buttonfullbrightness.Visible = General.Settings.ToolbarViewModes && maploaded; //mxd
			buttonfullbrightness.Checked = Renderer.FullBrightness; //mxd
			buttontogglegrid.Visible = General.Settings.ToolbarViewModes && maploaded; //mxd
			buttontogglegrid.Checked = General.Settings.RenderGrid; //mxd
			buttontogglecomments.Visible = General.Settings.ToolbarViewModes && maploaded && General.Map.UDMF; //mxd
			buttontogglecomments.Checked = General.Settings.RenderComments; //mxd
			buttontogglefixedthingsscale.Visible = General.Settings.ToolbarViewModes && maploaded; //mxd
			buttontogglefixedthingsscale.Checked = General.Settings.FixedThingsScale; //mxd
			separatorfullbrightness.Visible = General.Settings.ToolbarViewModes && maploaded; //mxd
			buttonviewbrightness.Visible = General.Settings.ToolbarViewModes && maploaded;
			buttonviewceilings.Visible = General.Settings.ToolbarViewModes && maploaded;
			buttonviewfloors.Visible = General.Settings.ToolbarViewModes && maploaded;
			buttonviewnormal.Visible = General.Settings.ToolbarViewModes && maploaded;
			separatorgeomergemodes.Visible = General.Settings.ToolbarGeometry && maploaded; //mxd
			buttonmergegeoclassic.Visible = General.Settings.ToolbarGeometry && maploaded; //mxd
			buttonmergegeo.Visible = General.Settings.ToolbarGeometry && maploaded; //mxd
			buttonplacegeo.Visible = General.Settings.ToolbarGeometry && maploaded; //mxd
			buttonsnaptogrid.Visible = General.Settings.ToolbarGeometry && maploaded;
			buttontoggledynamicgrid.Visible = General.Settings.ToolbarGeometry && maploaded; //mxd
			buttontoggledynamicgrid.Checked = General.Settings.DynamicGridSize; //mxd
			buttonautomerge.Visible = General.Settings.ToolbarGeometry && maploaded;
			buttonsplitjoinedsectors.Visible = General.Settings.ToolbarGeometry && maploaded; //mxd
			buttonsplitjoinedsectors.Checked = General.Settings.SplitJoinedSectors; //mxd
			buttonautoclearsidetextures.Visible = General.Settings.ToolbarGeometry && maploaded; //mxd
			buttontest.Visible = General.Settings.ToolbarTesting && maploaded;

			//mxd
			modelrendermode.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			dynamiclightmode.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			buttontogglefog.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			buttontogglesky.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			buttontoggleeventlines.Visible = General.Settings.GZToolbarGZDoom && maploaded;
			buttontogglevisualvertices.Visible = General.Settings.GZToolbarGZDoom && maploaded && General.Map.UDMF;
			separatorgzmodes.Visible = General.Settings.GZToolbarGZDoom && maploaded;

			//mxd. Show/hide additional panels
			modestoolbar.Visible = maploaded;
			panelinfo.Visible = maploaded;
			modecontrolstoolbar.Visible = (maploaded && modecontrolstoolbar.Items.Count > 0);
			
			//mxd. modestoolbar index in Controls gets messed up when it's invisible. This fixes it.
			//TODO: find out why this happens in the first place
			if(modestoolbar.Visible) 
			{
				int toolbarpos = this.Controls.IndexOf(toolbar);
				if(this.Controls.IndexOf(modestoolbar) > toolbarpos) 
				{
					this.Controls.SetChildIndex(modestoolbar, toolbarpos);
				}
			}

			// Update plugin buttons
			foreach(PluginToolbarButton p in pluginbuttons)
			{
				switch(p.section)
				{
					case ToolbarSection.File: p.button.Visible = General.Settings.ToolbarFile; break;
					case ToolbarSection.Script: p.button.Visible = General.Settings.ToolbarScript; break;
					case ToolbarSection.UndoRedo: p.button.Visible = General.Settings.ToolbarUndo; break;
					case ToolbarSection.CopyPaste: p.button.Visible = General.Settings.ToolbarCopy; break;
					case ToolbarSection.Prefabs: p.button.Visible = General.Settings.ToolbarPrefabs; break;
					case ToolbarSection.Things: p.button.Visible = General.Settings.ToolbarFilter; break;
					case ToolbarSection.Views: p.button.Visible = General.Settings.ToolbarViewModes; break;
					case ToolbarSection.Geometry: p.button.Visible = General.Settings.ToolbarGeometry; break;
					case ToolbarSection.Testing: p.button.Visible = General.Settings.ToolbarTesting; break;
				}
			}

			preventupdateseperators = false;

			UpdateSeparators();
		}

		// This checks one of the edit mode items (and unchecks all others)
		internal void CheckEditModeButton(string modeclassname)
		{
			// Go for all items
			//foreach(ToolStripItem item in editmodeitems)
			int itemCount = editmodeitems.Count;
			for(int i = 0; i < itemCount; i++)
			{
				ToolStripItem item = editmodeitems[i];
				// Check what type it is
				if(item is ToolStripMenuItem)
				{
					// Check if mode type matches with given name
					(item as ToolStripMenuItem).Checked = ((item.Tag as EditModeInfo).Type.Name == modeclassname);
				}
				else if(item is ToolStripButton)
				{
					// Check if mode type matches with given name
					(item as ToolStripButton).Checked = ((item.Tag as EditModeInfo).Type.Name == modeclassname);
				}
			}
		}
		
		// This removes the config-specific editing mode buttons
		internal void RemoveEditModeButtons()
		{
			// Go for all items
			//foreach(ToolStripItem item in editmodeitems)
			int itemCount = editmodeitems.Count;
			for (int i = 0; i < itemCount; i++)
			{
				ToolStripItem item = editmodeitems[i];
				// Remove it and restart
				menumode.DropDownItems.Remove(item);
				item.Dispose();
			}
			
			// Done
			modestoolbar.Items.Clear(); //mxd
			editmodeitems.Clear();
			UpdateSeparators();
		}
		
		// This adds an editing mode seperator on the toolbar and menu
		internal void AddEditModeSeperator(string group)
		{
			// Create a button
			ToolStripSeparator item = new ToolStripSeparator();
			item.Text = group; //mxd
			item.Margin = new Padding(0, 3, 0, 3); //mxd
			modestoolbar.Items.Add(item); //mxd
			editmodeitems.Add(item);
			
			// Create menu item
			int index = menumode.DropDownItems.Count;
			item = new ToolStripSeparator();
			item.Text = group; //mxd
			item.Margin = new Padding(0, 3, 0, 3);
			menumode.DropDownItems.Insert(index, item);
			editmodeitems.Add(item);
			
			UpdateSeparators();
		}
		
		// This adds an editing mode button to the toolbar and edit menu
		internal void AddEditModeButton(EditModeInfo modeinfo)
		{
			string controlname = modeinfo.ButtonDesc.Replace("&", "&&");
			
			// Create a button
			ToolStripItem item = new ToolStripButton(modeinfo.ButtonDesc, modeinfo.ButtonImage, EditModeButtonHandler);
			item.DisplayStyle = ToolStripItemDisplayStyle.Image;
			item.Padding = new Padding(0, 2, 0, 2);
			item.Margin = new Padding();
			item.Tag = modeinfo;
			modestoolbar.Items.Add(item); //mxd
			editmodeitems.Add(item);
			
			// Create menu item
			int index = menumode.DropDownItems.Count;
			item = new ToolStripMenuItem(controlname, modeinfo.ButtonImage, EditModeButtonHandler);
			item.Tag = modeinfo;
			menumode.DropDownItems.Insert(index, item);
			editmodeitems.Add(item);
			item.Visible = true;
			
			ApplyShortcutKeys(menumode.DropDownItems);
			UpdateSeparators();
		}

		// This handles edit mode button clicks
		private void EditModeButtonHandler(object sender, EventArgs e)
		{
			this.Update();
			EditModeInfo modeinfo = (EditModeInfo)((sender as ToolStripItem).Tag);
			General.Actions.InvokeAction(modeinfo.SwitchAction.GetFullActionName(modeinfo.Plugin.Assembly));
			this.Update();
		}

		//mxd
		public void UpdateGZDoomPanel() 
		{
			if(General.Map != null && General.Settings.GZToolbarGZDoom) 
			{
				foreach(ToolStripMenuItem item in modelrendermode.DropDownItems)
				{
					item.Checked = ((ModelRenderMode)item.Tag == General.Settings.GZDrawModelsMode);
					if(item.Checked) modelrendermode.Image = item.Image;
				}

				foreach(ToolStripMenuItem item in dynamiclightmode.DropDownItems)
				{
					item.Checked = ((LightRenderMode)item.Tag == General.Settings.GZDrawLightsMode);
					if(item.Checked) dynamiclightmode.Image = item.Image;
				}
				
				buttontogglefog.Checked = General.Settings.GZDrawFog;
				buttontogglesky.Checked = General.Settings.GZDrawSky;
				buttontoggleeventlines.Checked = General.Settings.GZShowEventLines;
				buttontogglevisualvertices.Visible = General.Map.UDMF;
				buttontogglevisualvertices.Checked = General.Settings.GZShowVisualVertices;
			} 
		}

		#endregion

		#region ================== Toolbar context menu (mxd)

		private void toolbarContextMenu_Opening(object sender, CancelEventArgs e)
		{
			if(General.Map == null)
			{
				e.Cancel = true;
				return;
			}

			toggleFile.Image = General.Settings.ToolbarFile ? Resources.Check : null;
			toggleScript.Image = General.Settings.ToolbarScript ? Resources.Check : null;
			toggleUndo.Image = General.Settings.ToolbarUndo ? Resources.Check : null;
			toggleCopy.Image = General.Settings.ToolbarCopy ? Resources.Check : null;
			togglePrefabs.Image = General.Settings.ToolbarPrefabs ? Resources.Check : null;
			toggleFilter.Image = General.Settings.ToolbarFilter ? Resources.Check : null;
			toggleViewModes.Image = General.Settings.ToolbarViewModes ? Resources.Check : null;
			toggleGeometry.Image = General.Settings.ToolbarGeometry ? Resources.Check : null;
			toggleTesting.Image = General.Settings.ToolbarTesting ? Resources.Check : null;
			toggleRendering.Image = General.Settings.GZToolbarGZDoom ? Resources.Check : null;
		}

		private void toolbarContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e) 
		{
			e.Cancel = (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked && toolbarContextMenuShiftPressed);
		}

		private void toolbarContextMenu_KeyDown(object sender, KeyEventArgs e) 
		{
			toolbarContextMenuShiftPressed = (e.KeyCode == Keys.ShiftKey);
		}

		private void toolbarContextMenu_KeyUp(object sender, KeyEventArgs e) 
		{
			toolbarContextMenuShiftPressed = (e.KeyCode != Keys.ShiftKey);
		}

		private void toggleFile_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarFile = !General.Settings.ToolbarFile;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleFile.Image = General.Settings.ToolbarFile ? Resources.Check : null;
		}

		private void toggleScript_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarScript = !General.Settings.ToolbarScript;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleScript.Image = General.Settings.ToolbarScript ? Resources.Check : null;
		}

		private void toggleUndo_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarUndo = !General.Settings.ToolbarUndo;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleUndo.Image = General.Settings.ToolbarUndo ? Resources.Check : null;
		}

		private void toggleCopy_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarCopy = !General.Settings.ToolbarCopy;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleCopy.Image = General.Settings.ToolbarCopy ? Resources.Check : null;
		}

		private void togglePrefabs_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarPrefabs = !General.Settings.ToolbarPrefabs;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				togglePrefabs.Image = General.Settings.ToolbarPrefabs ? Resources.Check : null;
		}

		private void toggleFilter_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarFilter = !General.Settings.ToolbarFilter;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleFilter.Image = General.Settings.ToolbarFilter ? Resources.Check : null;
		}

		private void toggleViewModes_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarViewModes = !General.Settings.ToolbarViewModes;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleViewModes.Image = General.Settings.ToolbarViewModes ? Resources.Check : null;
		}

		private void toggleGeometry_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarGeometry = !General.Settings.ToolbarGeometry;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleGeometry.Image = General.Settings.ToolbarGeometry ? Resources.Check : null;
		}

		private void toggleTesting_Click(object sender, EventArgs e) 
		{
			General.Settings.ToolbarTesting = !General.Settings.ToolbarTesting;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleTesting.Image = General.Settings.ToolbarTesting ? Resources.Check : null;
		}

		private void toggleRendering_Click(object sender, EventArgs e) 
		{
			General.Settings.GZToolbarGZDoom = !General.Settings.GZToolbarGZDoom;
			UpdateToolbar();

			if(toolbarContextMenuShiftPressed) 
				toggleRendering.Image = General.Settings.GZToolbarGZDoom ? Resources.Check : null;
		}

		#endregion

		#region ================== Menus

		// This adds a menu to the menus bar
		public void AddMenu(ToolStripItem menu) { AddMenu(menu, MenuSection.Top, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly())); }
		public void AddMenu(ToolStripItem menu, MenuSection section) { AddMenu(menu, section, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly())); }
		private void AddMenu(ToolStripItem menu, MenuSection section, Plugin plugin)
		{
			// Fix tags to full action names
			ToolStripItemCollection items = new ToolStripItemCollection(this.menumain, new ToolStripItem[0]);
			items.Add(menu);
			RenameTagsToFullActions(items, plugin);
			
			// Insert the menu in the right location
			switch(section)
			{
				case MenuSection.FileNewOpenClose: menufile.DropDownItems.Insert(menufile.DropDownItems.IndexOf(seperatorfileopen), menu); break;
				case MenuSection.FileSave: menufile.DropDownItems.Insert(menufile.DropDownItems.IndexOf(seperatorfilesave), menu); break;
				case MenuSection.FileImport: itemimport.DropDownItems.Add(menu); break; //mxd
				case MenuSection.FileExport: itemexport.DropDownItems.Add(menu); break; //mxd
				case MenuSection.FileRecent: menufile.DropDownItems.Insert(menufile.DropDownItems.IndexOf(seperatorfilerecent), menu); break;
				case MenuSection.FileExit: menufile.DropDownItems.Insert(menufile.DropDownItems.IndexOf(itemexit), menu); break;
				case MenuSection.EditUndoRedo: menuedit.DropDownItems.Insert(menuedit.DropDownItems.IndexOf(seperatoreditundo), menu); break;
				case MenuSection.EditCopyPaste: menuedit.DropDownItems.Insert(menuedit.DropDownItems.IndexOf(seperatoreditcopypaste), menu); break;
				case MenuSection.EditGeometry: menuedit.DropDownItems.Insert(menuedit.DropDownItems.IndexOf(seperatoreditgeometry), menu); break;
				case MenuSection.EditGrid: menuedit.DropDownItems.Insert(menuedit.DropDownItems.IndexOf(seperatoreditgrid), menu); break;
				case MenuSection.EditMapOptions: menuedit.DropDownItems.Add(menu); break;
				case MenuSection.ViewHelpers: menuview.DropDownItems.Insert(menuview.DropDownItems.IndexOf(separatorhelpers), menu); break; //mxd
				case MenuSection.ViewRendering: menuview.DropDownItems.Insert(menuview.DropDownItems.IndexOf(separatorrendering), menu); break; //mxd
				case MenuSection.ViewThings: menuview.DropDownItems.Insert(menuview.DropDownItems.IndexOf(seperatorviewthings), menu); break;
				case MenuSection.ViewViews: menuview.DropDownItems.Insert(menuview.DropDownItems.IndexOf(seperatorviewviews), menu); break;
				case MenuSection.ViewZoom: menuview.DropDownItems.Insert(menuview.DropDownItems.IndexOf(seperatorviewzoom), menu); break;
				case MenuSection.ViewScriptEdit: menuview.DropDownItems.Add(menu); break;
				case MenuSection.PrefabsInsert: menuprefabs.DropDownItems.Insert(menuprefabs.DropDownItems.IndexOf(seperatorprefabsinsert), menu); break;
				case MenuSection.PrefabsCreate: menuprefabs.DropDownItems.Add(menu); break;
				case MenuSection.ToolsResources: menutools.DropDownItems.Insert(menutools.DropDownItems.IndexOf(seperatortoolsresources), menu); break;
				case MenuSection.ToolsConfiguration: menutools.DropDownItems.Insert(menutools.DropDownItems.IndexOf(seperatortoolsconfig), menu); break;
				case MenuSection.ToolsTesting: menutools.DropDownItems.Add(menu); break;
				case MenuSection.HelpManual: menuhelp.DropDownItems.Insert(menuhelp.DropDownItems.IndexOf(seperatorhelpmanual), menu); break;
				case MenuSection.HelpAbout: menuhelp.DropDownItems.Add(menu); break;
				case MenuSection.Top: menumain.Items.Insert(menumain.Items.IndexOf(menutools), menu); break;
			}
			
			ApplyShortcutKeys(items);
		}

		//mxd
		public void AddModesMenu(ToolStripItem menu, string group) 
		{
			// Fix tags to full action names
			ToolStripItemCollection items = new ToolStripItemCollection(this.menumain, new ToolStripItem[0]);
			items.Add(menu);
			RenameTagsToFullActions(items, General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly()));
			
			//find the separator we need
			for(int i = 0; i < menumode.DropDownItems.Count; i++) 
			{
				if(menumode.DropDownItems[i] is ToolStripSeparator && menumode.DropDownItems[i].Text == group) 
				{
					menumode.DropDownItems.Insert(i + 1, menu);
					break;
				}
			}

			ApplyShortcutKeys(items);
		}
		
		// Removes a menu
		public void RemoveMenu(ToolStripItem menu)
		{
			// We actually have no idea in which menu this item is,
			// so try removing from all menus and the top strip
			menufile.DropDownItems.Remove(menu);
			menuedit.DropDownItems.Remove(menu);
			menumode.DropDownItems.Remove(menu); //mxd
			menuview.DropDownItems.Remove(menu);
			menuprefabs.DropDownItems.Remove(menu);
			menutools.DropDownItems.Remove(menu);
			menuhelp.DropDownItems.Remove(menu);
			menumain.Items.Remove(menu);
		}
		
		// Public method to apply shortcut keys
		internal void ApplyShortcutKeys()
		{
			// Apply shortcut keys to menus
			ApplyShortcutKeys(menumain.Items);
		}
		
		// This sets the shortcut keys on menu items
		private static void ApplyShortcutKeys(ToolStripItemCollection items)
		{
			// Go for all controls to find menu items
			foreach(ToolStripItem item in items)
			{
				// This is a menu item?
				if(item is ToolStripMenuItem)
				{
					// Get the item in proper type
					ToolStripMenuItem menuitem = (item as ToolStripMenuItem);

					// Tag set for this item?
					if(menuitem.Tag is string)
					{
						// Action with this name available?
						string actionname = menuitem.Tag.ToString();
						if(General.Actions.Exists(actionname))
						{
							// Put the action shortcut key on the menu item
							menuitem.ShortcutKeyDisplayString = Actions.Action.GetShortcutKeyDesc(General.Actions[actionname].ShortcutKey);
						}
					}
					// Edit mode info set for this item?
					else if(menuitem.Tag is EditModeInfo)
					{
						// Action with this name available?
						EditModeInfo modeinfo = (EditModeInfo)menuitem.Tag;
						string actionname = modeinfo.SwitchAction.GetFullActionName(modeinfo.Plugin.Assembly);
						if(General.Actions.Exists(actionname))
						{
							// Put the action shortcut key on the menu item
							menuitem.ShortcutKeyDisplayString = Actions.Action.GetShortcutKeyDesc(General.Actions[actionname].ShortcutKey);
						}
					}

					// Recursively apply shortcut keys to child menu items as well
					ApplyShortcutKeys(menuitem.DropDownItems);
				}
			}
		}

		// This fixes short action names to fully qualified
		// action names on menu item tags
		private static void RenameTagsToFullActions(ToolStripItemCollection items, Plugin plugin)
		{
			// Go for all controls to find menu items
			foreach(ToolStripItem item in items)
			{
				// Tag set for this item?
				if(item.Tag is string)
				{
					// Check if the tag does not already begin with the assembly name
					if(!((string)item.Tag).StartsWith(plugin.Name + "_", StringComparison.OrdinalIgnoreCase))
					{
						// Change the tag to a fully qualified action name
						item.Tag = plugin.Name.ToLowerInvariant() + "_" + (string)item.Tag;
					}
				}

				// This is a menu item?
				if(item is ToolStripMenuItem)
				{
					// Get the item in proper type
					ToolStripMenuItem menuitem = (item as ToolStripMenuItem);
					
					// Recursively perform operation on child menu items
					RenameTagsToFullActions(menuitem.DropDownItems, plugin);
				}
			}
		}
		
		#endregion

		#region ================== File Menu

		// This sets up the file menu
		private void UpdateFileMenu()
		{
			//mxd. Show/hide items
			bool show = (General.Map != null); //mxd
			itemclosemap.Visible = show;
			itemsavemap.Visible = show;
			itemsavemapas.Visible = show;
			itemsavemapinto.Visible = show;
			itemopenmapincurwad.Visible = show; //mxd
			itemimport.Visible = show; //mxd
			itemexport.Visible = show; //mxd
			seperatorfileopen.Visible = show; //mxd
			seperatorfilesave.Visible = show; //mxd

			// Toolbar icons
			buttonsavemap.Enabled = show;
		}

		// This sets the recent files from configuration
		private void CreateRecentFiles()
		{
			bool anyitems = false;

			// Where to insert
			int insertindex = menufile.DropDownItems.IndexOf(itemnorecent);
			
			// Create all items
			recentitems = new ToolStripMenuItem[General.Settings.MaxRecentFiles];
			for(int i = 0; i < General.Settings.MaxRecentFiles; i++)
			{
				// Create item
				recentitems[i] = new ToolStripMenuItem("");
				recentitems[i].Tag = "";
				recentitems[i].Click += recentitem_Click;
				menufile.DropDownItems.Insert(insertindex + i, recentitems[i]);

				// Get configuration setting
				string filename = General.Settings.ReadSetting("recentfiles.file" + i, "");
				if(!string.IsNullOrEmpty(filename) && File.Exists(filename))
				{
					// Set up item
					int number = i + 1;
					recentitems[i].Text = "&" + number + "  " + GetDisplayFilename(filename);
					recentitems[i].Tag = filename;
					recentitems[i].Visible = true;
					anyitems = true;
				}
				else
				{
					// Hide item
					recentitems[i].Visible = false;
				}
			}

			// Hide the no recent item when there are items
			itemnorecent.Visible = !anyitems;
		}
		
		// This saves the recent files list
		private void SaveRecentFiles()
		{
			// Go for all items
			for(int i = 0; i < recentitems.Length; i++)
			{
				// Recent file set?
				if(!string.IsNullOrEmpty(recentitems[i].Text))
				{
					// Save to configuration
					General.Settings.WriteSetting("recentfiles.file" + i, recentitems[i].Tag.ToString());
				}
			}
		}
		
		// This adds a recent file to the list
		internal void AddRecentFile(string filename)
		{
			//mxd. Recreate recent files list
			if(recentitems.Length != General.Settings.MaxRecentFiles)
			{
				UpdateRecentItems();
			}

			int movedownto = General.Settings.MaxRecentFiles - 1;
			
			// Check if this file is already in the list
			for(int i = 0; i < General.Settings.MaxRecentFiles; i++)
			{
				// File same as this item?
				if(string.Compare(filename, recentitems[i].Tag.ToString(), true) == 0)
				{
					// Move down to here so that this item will disappear
					movedownto = i;
					break;
				}
			}
			
			// Go for all items, except the last one, backwards
			for(int i = movedownto - 1; i >= 0; i--)
			{
				// Move recent file down the list
				int number = i + 2;
				recentitems[i + 1].Text = "&" + number + "  " + GetDisplayFilename(recentitems[i].Tag.ToString());
				recentitems[i + 1].Tag = recentitems[i].Tag.ToString();
				recentitems[i + 1].Visible = !string.IsNullOrEmpty(recentitems[i].Tag.ToString());
			}

			// Add new file at the top
			recentitems[0].Text = "&1  " + GetDisplayFilename(filename);
			recentitems[0].Tag = filename;
			recentitems[0].Visible = true;

			// Hide the no recent item
			itemnorecent.Visible = false;
		}

		//mxd
		private void UpdateRecentItems()
		{
			foreach(ToolStripMenuItem item in recentitems)
				menufile.DropDownItems.Remove(item);

			SaveRecentFiles();
			CreateRecentFiles();
		}

		// This returns the trimmed file/path string
		private string GetDisplayFilename(string filename)
		{
			// String doesnt fit?
			if(MeasureString(filename, this.Font).Width > MAX_RECENT_FILES_PIXELS)
			{
				// Start chopping off characters
				for(int i = filename.Length - 6; i >= 0; i--)
				{
					// Does it fit now?
					string newname = filename.Substring(0, 3) + "..." + filename.Substring(filename.Length - i, i);
					if(MeasureString(newname, this.Font).Width <= MAX_RECENT_FILES_PIXELS) return newname;
				}

				// Cant find anything that fits (most unlikely!)
				return "wtf?!";
			}
			else
			{
				// The whole string fits
				return filename;
			}
		}
		
		// Exit clicked
		private void itemexit_Click(object sender, EventArgs e) { this.Close(); }

		// Recent item clicked
		private void recentitem_Click(object sender, EventArgs e)
		{
			// Get the item that was clicked
			ToolStripItem item = (sender as ToolStripItem);

			// Open this file
			General.OpenMapFile(item.Tag.ToString(), null);
		}

		//mxd
		private void menufile_DropDownOpening(object sender, EventArgs e)
		{
			UpdateRecentItems();
		}
		
		#endregion

		#region ================== Edit Menu

		// This sets up the edit menu
		private void UpdateEditMenu()
		{
			// No edit menu when no map open
			menuedit.Visible = (General.Map != null);
			
			// Enable/disable items
			itemundo.Enabled = (General.Map != null) && (General.Map.UndoRedo.NextUndo != null);
			itemredo.Enabled = (General.Map != null) && (General.Map.UndoRedo.NextRedo != null);
			itemcut.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			itemcopy.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			itempaste.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			itempastespecial.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			itemsplitjoinedsectors.Checked = General.Settings.SplitJoinedSectors; //mxd
			itemautoclearsidetextures.Checked = General.Settings.AutoClearSidedefTextures; //mxd
			itemdynamicgridsize.Enabled = (General.Map != null); //mxd
			itemdynamicgridsize.Checked = General.Settings.DynamicGridSize; //mxd

			// Determine undo description
			if(itemundo.Enabled)
				itemundo.Text = "Undo " + General.Map.UndoRedo.NextUndo.Description;
			else
				itemundo.Text = "Undo";

			// Determine redo description
			if(itemredo.Enabled)
				itemredo.Text = "Redo " + General.Map.UndoRedo.NextRedo.Description;
			else
				itemredo.Text = "Redo";
			
			// Toolbar icons
			buttonundo.Enabled = itemundo.Enabled;
			buttonredo.Enabled = itemredo.Enabled;
			buttonundo.ToolTipText = itemundo.Text;
			buttonredo.ToolTipText = itemredo.Text;
			buttonautoclearsidetextures.Checked = itemautoclearsidetextures.Checked; //mxd
			buttoncut.Enabled = itemcut.Enabled;
			buttoncopy.Enabled = itemcopy.Enabled;
			buttonpaste.Enabled = itempaste.Enabled;

			//mxd. Geometry merge mode items
			if(General.Map != null)
			{
				for(int i = 0; i < geomergemodesbuttons.Length; i++)
				{
					// Check the correct item
					geomergemodesbuttons[i].Checked = (i == (int)General.Settings.MergeGeometryMode);
					geomergemodesitems[i].Checked = (i == (int)General.Settings.MergeGeometryMode);
				}
			}
		}

		//mxd
		private void menuedit_DropDownOpening(object sender, EventArgs e) 
		{
			if(General.Map == null) 
			{
				selectGroup.Enabled = false;
				clearGroup.Enabled = false;
				addToGroup.Enabled = false;
				return;
			}

			//get data
			ToolStripItem item;
			GroupInfo[] infos = new GroupInfo[10];
			for(int i = 0; i < infos.Length; i++) infos[i] = General.Map.Map.GetGroupInfo(i);

			//update "Add to group" menu
			addToGroup.Enabled = true;
			addToGroup.DropDownItems.Clear();
			foreach(GroupInfo gi in infos) 
			{
				item = addToGroup.DropDownItems.Add(gi.ToString());
				item.Tag = "builder_assigngroup" + gi.Index;
				item.Click += InvokeTaggedAction;
			}

			//update "Select group" menu
			selectGroup.DropDownItems.Clear();
			foreach(GroupInfo gi in infos) 
			{
				if(gi.Empty) continue;
				item = selectGroup.DropDownItems.Add(gi.ToString());
				item.Tag = "builder_selectgroup" + gi.Index;
				item.Click += InvokeTaggedAction;
			}

			//update "Clear group" menu
			clearGroup.DropDownItems.Clear();
			foreach(GroupInfo gi in infos) 
			{
				if(gi.Empty) continue;
				item = clearGroup.DropDownItems.Add(gi.ToString());
				item.Tag = "builder_cleargroup" + gi.Index;
				item.Click += InvokeTaggedAction;
			}

			selectGroup.Enabled = selectGroup.DropDownItems.Count > 0;
			clearGroup.Enabled = clearGroup.DropDownItems.Count > 0;
		}

		//mxd. Action to toggle comments rendering
		[BeginAction("togglecomments")]
		internal void ToggleComments()
		{
			buttontogglecomments.Checked = !buttontogglecomments.Checked;
			itemtogglecomments.Checked = buttontogglecomments.Checked;
			General.Settings.RenderComments = buttontogglecomments.Checked;
			DisplayStatus(StatusType.Action, "Comment icons are " + (buttontogglecomments.Checked ? "SHOWN" : "HIDDEN"));

			// Redraw display to show changes
			RedrawDisplay();
		}

		//mxd. Action to toggle fixed things scale
		[BeginAction("togglefixedthingsscale")]
		internal void ToggleFixedThingsScale()
		{
			buttontogglefixedthingsscale.Checked = !buttontogglefixedthingsscale.Checked;
			itemtogglefixedthingsscale.Checked = buttontogglefixedthingsscale.Checked;
			General.Settings.FixedThingsScale = buttontogglefixedthingsscale.Checked;
			DisplayStatus(StatusType.Action, "Fixed things scale is " + (buttontogglefixedthingsscale.Checked ? "ENABLED" : "DISABLED"));

			// Redraw display to show changes
			RedrawDisplay();
		}

		// Action to toggle snap to grid
		[BeginAction("togglesnap")]
		internal void ToggleSnapToGrid()
		{
			buttonsnaptogrid.Checked = !buttonsnaptogrid.Checked;
			itemsnaptogrid.Checked = buttonsnaptogrid.Checked;
			DisplayStatus(StatusType.Action, "Snap to grid is " + (buttonsnaptogrid.Checked ? "ENABLED" : "DISABLED"));
		}

		// Action to toggle auto merge
		[BeginAction("toggleautomerge")]
		internal void ToggleAutoMerge()
		{
			buttonautomerge.Checked = !buttonautomerge.Checked;
			itemautomerge.Checked = buttonautomerge.Checked;
			DisplayStatus(StatusType.Action, "Snap to geometry is " + (buttonautomerge.Checked ? "ENABLED" : "DISABLED"));
		}

		//mxd
		[BeginAction("togglejoinedsectorssplitting")]
		internal void ToggleJoinedSectorsSplitting()
		{
			buttonsplitjoinedsectors.Checked = !buttonsplitjoinedsectors.Checked;
			itemsplitjoinedsectors.Checked = buttonsplitjoinedsectors.Checked;
			General.Settings.SplitJoinedSectors = buttonsplitjoinedsectors.Checked;
			DisplayStatus(StatusType.Action, "Joined sectors splitting is " + (General.Settings.SplitJoinedSectors ? "ENABLED" : "DISABLED"));
		}

		//mxd
		[BeginAction("togglebrightness")]
		internal void ToggleBrightness() 
		{
			Renderer.FullBrightness = !Renderer.FullBrightness;
			buttonfullbrightness.Checked = Renderer.FullBrightness;
			itemfullbrightness.Checked = Renderer.FullBrightness;
			General.Interface.DisplayStatus(StatusType.Action, "Full Brightness is now " + (Renderer.FullBrightness ? "ON" : "OFF"));

			// Redraw display to show changes
			General.Interface.RedrawDisplay();
		}

		//mxd
		[BeginAction("togglegrid")]
		protected void ToggleGrid()
		{
			General.Settings.RenderGrid = !General.Settings.RenderGrid;
			itemtogglegrid.Checked = General.Settings.RenderGrid;
			buttontogglegrid.Checked = General.Settings.RenderGrid;
			General.Interface.DisplayStatus(StatusType.Action, "Grid rendering is " + (General.Settings.RenderGrid ? "ENABLED" : "DISABLED"));

			// Redraw display to show changes
			General.Map.CRenderer2D.GridVisibilityChanged();
			General.Interface.RedrawDisplay();
		}
		
		[BeginAction("aligngridtolinedef")]
		protected void AlignGridToLinedef()
		{
			if (General.Map.Map.SelectedLinedefsCount != 1)
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Exactly one linedef must be selected");
				General.Interface.MessageBeep(MessageBeepType.Warning);
				return;
			}
			Linedef line = General.Map.Map.SelectedLinedefs.First.Value;
			Vertex vertex = line.Start;
			General.Map.Grid.SetGridRotation(line.Angle);
			General.Map.Grid.SetGridOrigin(vertex.Position.x, vertex.Position.y);
			General.Map.CRenderer2D.GridVisibilityChanged();
			General.Interface.RedrawDisplay();
		}

		[BeginAction("setgridorigintovertex")]
		protected void SetGridOriginToVertex()
		{
			if (General.Map.Map.SelectedVerticessCount != 1)
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Exactly one vertex must be selected");
				General.Interface.MessageBeep(MessageBeepType.Warning);
				return;
			}
			Vertex vertex = General.Map.Map.SelectedVertices.First.Value;
			General.Map.Grid.SetGridOrigin(vertex.Position.x, vertex.Position.y);
			General.Map.CRenderer2D.GridVisibilityChanged();
			General.Interface.RedrawDisplay();
		}

		[BeginAction("resetgrid")]
		protected void ResetGrid()
		{
			General.Map.Grid.SetGridRotation(0.0f);
			General.Map.Grid.SetGridOrigin(0, 0);
			General.Map.CRenderer2D.GridVisibilityChanged();
			General.Interface.RedrawDisplay();
		}

		//mxd
		[BeginAction("toggledynamicgrid")]
		protected void ToggleDynamicGrid()
		{
			General.Settings.DynamicGridSize = !General.Settings.DynamicGridSize;
			itemdynamicgridsize.Checked = General.Settings.DynamicGridSize;
			buttontoggledynamicgrid.Checked = General.Settings.DynamicGridSize;
			General.Interface.DisplayStatus(StatusType.Action, "Dynamic grid size is " + (General.Settings.DynamicGridSize ? "ENABLED" : "DISABLED"));

			// Redraw display to show changes
			if(General.Editing.Mode is ClassicMode) ((ClassicMode)General.Editing.Mode).MatchGridSizeToDisplayScale();
			General.Interface.RedrawDisplay();
		}

		//mxd
		[BeginAction("toggleautoclearsidetextures")]
		internal void ToggleAutoClearSideTextures() 
		{
			buttonautoclearsidetextures.Checked = !buttonautoclearsidetextures.Checked;
			itemautoclearsidetextures.Checked = buttonautoclearsidetextures.Checked;
			General.Settings.AutoClearSidedefTextures = buttonautoclearsidetextures.Checked;
			DisplayStatus(StatusType.Action, "Auto removal of unused sidedef textures is " + (buttonautoclearsidetextures.Checked ? "ENABLED" : "DISABLED"));
		}

		//mxd
		[BeginAction("viewusedtags")]
		internal void ViewUsedTags() 
		{
			TagStatisticsForm f = new TagStatisticsForm();
			f.ShowDialog(this);
		}

		//mxd
		[BeginAction("viewthingtypes")]
		internal void ViewThingTypes()
		{
			ThingStatisticsForm f = new ThingStatisticsForm();
			f.ShowDialog(this);
		}

		//mxd
		[BeginAction("geomergeclassic")]
		private void GeoMergeClassic()
		{
			General.Settings.MergeGeometryMode = MergeGeometryMode.CLASSIC;
			UpdateToolbar();
			UpdateEditMenu();
			DisplayStatus(StatusType.Action, "\"Merge Dragged Vertices Only\" mode selected");
		}

		//mxd
		[BeginAction("geomerge")]
		private void GeoMerge()
		{
			General.Settings.MergeGeometryMode = MergeGeometryMode.MERGE;
			UpdateToolbar();
			UpdateEditMenu();
			DisplayStatus(StatusType.Action, "\"Merge Dragged Geometry\" mode selected");
		}

		//mxd
		[BeginAction("georeplace")]
		private void GeoReplace()
		{
			General.Settings.MergeGeometryMode = MergeGeometryMode.REPLACE;
			UpdateToolbar();
			UpdateEditMenu();
			DisplayStatus(StatusType.Action, "\"Replace with Dragged Geometry\" mode selected");
		}
		
		#endregion

		#region ================== View Menu

		// This sets up the View menu
		private void UpdateViewMenu()
		{
			menuview.Visible = (General.Map != null); //mxd
			
			// Menu items
			itemfullbrightness.Checked = Renderer.FullBrightness; //mxd
			itemtogglegrid.Checked = General.Settings.RenderGrid; //mxd
			itemtoggleinfo.Checked = IsInfoPanelExpanded;
			itemtogglecomments.Visible = (General.Map != null && General.Map.UDMF); //mxd
			itemtogglecomments.Checked = General.Settings.RenderComments; //mxd
			itemtogglefixedthingsscale.Visible = (General.Map != null); //mxd
			itemtogglefixedthingsscale.Checked = General.Settings.FixedThingsScale; //mxd
			itemtogglefog.Checked = General.Settings.GZDrawFog;
			itemtogglesky.Checked = General.Settings.GZDrawSky;
			itemtoggleeventlines.Checked = General.Settings.GZShowEventLines;
			itemtogglevisualverts.Visible = (General.Map != null && General.Map.UDMF);
			itemtogglevisualverts.Checked = General.Settings.GZShowVisualVertices;

			// Update Model Rendering Mode items...
			foreach(ToolStripMenuItem item in itemmodelmodes.DropDownItems)
			{
				item.Checked = ((ModelRenderMode)item.Tag == General.Settings.GZDrawModelsMode);
				if(item.Checked) itemmodelmodes.Image = item.Image;
			}

			// Update Dynamic Light Mode items...
			foreach(ToolStripMenuItem item in itemdynlightmodes.DropDownItems)
			{
				item.Checked = ((LightRenderMode)item.Tag == General.Settings.GZDrawLightsMode);
				if(item.Checked) itemdynlightmodes.Image = item.Image;
			}
			
			// View mode items
			if(General.Map != null)
			{
				for(int i = 0; i < Renderer2D.NUM_VIEW_MODES; i++)
				{
					// Check the correct item
					viewmodesbuttons[i].Checked = (i == (int)General.Map.CRenderer2D.ViewMode);
					viewmodesitems[i].Checked = (i == (int)General.Map.CRenderer2D.ViewMode);
				}
			}
		}

		//mxd
		[BeginAction("gztoggleenhancedrendering")]
		public void ToggleEnhancedRendering()
		{
			General.Settings.EnhancedRenderingEffects = !General.Settings.EnhancedRenderingEffects;

			General.Settings.GZDrawFog = General.Settings.EnhancedRenderingEffects;
			General.Settings.GZDrawSky = General.Settings.EnhancedRenderingEffects;
			General.Settings.GZDrawLightsMode = (General.Settings.EnhancedRenderingEffects ? LightRenderMode.ALL : LightRenderMode.NONE);
			General.Settings.GZDrawModelsMode = (General.Settings.EnhancedRenderingEffects ? ModelRenderMode.ALL : ModelRenderMode.NONE);

			UpdateGZDoomPanel();
			UpdateViewMenu();
			DisplayStatus(StatusType.Info, "Enhanced rendering effects are " + (General.Settings.EnhancedRenderingEffects ? "ENABLED" : "DISABLED"));
		}

		//mxd
		[BeginAction("gztogglefog")]
		internal void ToggleFog()
		{
			General.Settings.GZDrawFog = !General.Settings.GZDrawFog;

			itemtogglefog.Checked = General.Settings.GZDrawFog;
			buttontogglefog.Checked = General.Settings.GZDrawFog;

			General.MainWindow.DisplayStatus(StatusType.Action, "Fog rendering is " + (General.Settings.GZDrawFog ? "ENABLED" : "DISABLED"));
			General.MainWindow.RedrawDisplay();
			General.MainWindow.UpdateGZDoomPanel();
		}

		//mxd
		[BeginAction("gztogglesky")]
		internal void ToggleSky()
		{
			General.Settings.GZDrawSky = !General.Settings.GZDrawSky;

			itemtogglesky.Checked = General.Settings.GZDrawSky;
			buttontogglesky.Checked = General.Settings.GZDrawSky;

			General.MainWindow.DisplayStatus(StatusType.Action, "Sky rendering is " + (General.Settings.GZDrawSky ? "ENABLED" : "DISABLED"));
			General.MainWindow.RedrawDisplay();
			General.MainWindow.UpdateGZDoomPanel();
		}

		[BeginAction("gztoggleeventlines")]
		internal void ToggleEventLines()
		{
			General.Settings.GZShowEventLines = !General.Settings.GZShowEventLines;

			itemtoggleeventlines.Checked = General.Settings.GZShowEventLines;
			buttontoggleeventlines.Checked = General.Settings.GZShowEventLines;

			General.MainWindow.DisplayStatus(StatusType.Action, "Event lines are " + (General.Settings.GZShowEventLines ? "ENABLED" : "DISABLED"));
			General.MainWindow.RedrawDisplay();
			General.MainWindow.UpdateGZDoomPanel();
		}

		[BeginAction("gztogglevisualvertices")]
		internal void ToggleVisualVertices()
		{
			General.Settings.GZShowVisualVertices = !General.Settings.GZShowVisualVertices;

			itemtogglevisualverts.Checked = General.Settings.GZShowVisualVertices;
			buttontogglevisualvertices.Checked = General.Settings.GZShowVisualVertices;

			General.MainWindow.DisplayStatus(StatusType.Action, "Visual vertices are " + (General.Settings.GZShowVisualVertices ? "ENABLED" : "DISABLED"));
			General.MainWindow.RedrawDisplay();
			General.MainWindow.UpdateGZDoomPanel();
		}

		#endregion

		#region ================== Mode Menu

		// This sets up the modes menu
		private void UpdateModeMenu()
		{
			menumode.Visible = (General.Map != null);
		}
		
		#endregion

		#region ================== Help Menu
		
		// This sets up the help menu
		private void UpdateHelpMenu()
		{
			itemhelpeditmode.Visible = (General.Map != null); //mxd
			itemhelpeditmode.Enabled = (General.Map != null && General.Editing.Mode != null);
		}

		//mxd. Check updates clicked
		private void itemhelpcheckupdates_Click(object sender, EventArgs e)
		{
			UpdateChecker.PerformCheck(true);
		}

		//mxd. Github issues clicked
		private void itemhelpissues_Click(object sender, EventArgs e)
		{
			General.OpenWebsite("https://github.com/jewalky/GZDoom-Builder-Bugfix/issues");
		}
		
		// About clicked
		private void itemhelpabout_Click(object sender, EventArgs e)
		{
			// Show about dialog
			AboutForm aboutform = new AboutForm();
			aboutform.ShowDialog(this);
		}

		// Reference Manual clicked
		private void itemhelprefmanual_Click(object sender, EventArgs e)
		{
			General.ShowHelp("introduction.html");
		}

		// About this Editing Mode
		private void itemhelpeditmode_Click(object sender, EventArgs e)
		{
			if((General.Map != null) && (General.Editing.Mode != null))
				General.Editing.Mode.OnHelp();
		}

		//mxd
		private void itemShortcutReference_Click(object sender, EventArgs e) 
		{
			const string columnLabels = "<tr><td width=\"240px;\"><strong>Action</strong></td><td width=\"120px;\"><div align=\"center\"><strong>Shortcut</strong></div></td><td width=\"120px;\"><div align=\"center\"><strong>Modifiers</strong></div></td><td><strong>Description</strong></td></tr>";
			const string categoryPadding = "<tr><td colspan=\"4\"></td></tr>";
			const string categoryStart = "<tr><td colspan=\"4\" bgcolor=\"#333333\"><strong style=\"color:#FFFFFF\">";
			const string categoryEnd = "</strong><div style=\"text-align:right; float:right\"><a style=\"color:#FFFFFF\" href=\"#top\">[to top]</a></div></td></tr>";
			const string fileName = "GZDB Actions Reference.html";

			Actions.Action[] actions = General.Actions.GetAllActions();
			Dictionary<string, List<Actions.Action>> sortedActions = new Dictionary<string, List<Actions.Action>>(StringComparer.Ordinal);

			foreach(Actions.Action action in actions) 
			{
				if(!sortedActions.ContainsKey(action.Category))
					sortedActions.Add(action.Category, new List<Actions.Action>());
				sortedActions[action.Category].Add(action);
			}

			System.Text.StringBuilder html = new System.Text.StringBuilder();

			//head
			html.AppendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">" + Environment.NewLine +
								"<html xmlns=\"http://www.w3.org/1999/xhtml\">" + Environment.NewLine +
								"<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" /><title>GZDoom Builder Actions Reference</title></head>" + Environment.NewLine +
								"<body bgcolor=\"#666666\">" + Environment.NewLine +
									"<div style=\"padding-left:60px; padding-right:60px; padding-top:20px; padding-bottom:20px;\">" + Environment.NewLine);

			//table header
			html.AppendLine("<table bgcolor=\"#FFFFFF\" width=\"100%\" border=\"0\" cellspacing=\"6\" cellpadding=\"6\" style=\"font-family: 'Trebuchet MS',georgia,Verdana,Sans-serif;\">" + Environment.NewLine +
							"<tr><td colspan=\"4\" bgcolor=\"#333333\"><span style=\"font-size: 24px\"><a name=\"top\" id=\"top\"></a><strong style=\"color:#FFFFFF\">GZDoom Builder Actions Reference</strong></span></td></tr>");

			//categories navigator
			List<string> catnames = new List<string>(sortedActions.Count);
			int counter = 0;
			int numActions = 0;
			foreach(KeyValuePair<string, List<Actions.Action>> category in sortedActions) 
			{
				catnames.Add("<a href=\"#cat" + (counter++) + "\">" + General.Actions.Categories[category.Key] + "</a>");
				numActions += category.Value.Count;
			}

			html.AppendLine("<tr><td colspan=\"4\"><strong>Total number of actions:</strong> " + numActions + "<br/><strong>Jump to:</strong> ");
			html.AppendLine(string.Join(" | ", catnames.ToArray()));
			html.AppendLine("</td></tr>" + Environment.NewLine);

			//add descriptions
			counter = 0;
			foreach(KeyValuePair<string, List<Actions.Action>> category in sortedActions) 
			{
				//add category title
				html.AppendLine(categoryPadding);
				html.AppendLine(categoryStart + "<a name=\"cat" + counter + "\" id=\"cat" + counter + "\"></a>" + General.Actions.Categories[category.Key] + categoryEnd);
				html.AppendLine(columnLabels);
				counter++;

				Dictionary<string, Actions.Action> actionsByTitle = new Dictionary<string, Actions.Action>(StringComparer.Ordinal);
				List<string> actionTitles = new List<string>();

				foreach(Actions.Action action in category.Value) 
				{
					actionsByTitle.Add(action.Title, action);
					actionTitles.Add(action.Title);
				}

				actionTitles.Sort();

				foreach(string title in actionTitles) 
				{
					Actions.Action a = actionsByTitle[title];
					List<string> modifiers = new List<string>();

					html.AppendLine("<tr>");
					html.AppendLine("<td>" + title + "</td>");
					html.AppendLine("<td><div align=\"center\">" + Actions.Action.GetShortcutKeyDesc(a.ShortcutKey) + "</div></td>");

					if(a.DisregardControl) modifiers.Add("Ctrl");
					if(a.DisregardAlt) modifiers.Add("Alt");
					if(a.DisregardShift) modifiers.Add("Shift");

					html.AppendLine("<td><div align=\"center\">" + string.Join(", ", modifiers.ToArray()) + "</div></td>");
					html.AppendLine("<td>" + a.Description + "</td>");
					html.AppendLine("</tr>");
				}
			}

			//add bottom
			html.AppendLine("</table></div></body></html>");

			//write
			string path;
			try 
			{
				path = Path.Combine(General.AppPath, fileName);
				using(StreamWriter writer = File.CreateText(path)) 
				{
					writer.Write(html.ToString());
				}
			} 
			catch(Exception) 
			{
				//Configurtions path SHOULD be accessible and not read-only, right?
				path = Path.Combine(General.SettingsPath, fileName);
				using(StreamWriter writer = File.CreateText(path)) 
				{
					writer.Write(html.ToString());
				}
			}

			//open file
			DisplayStatus(StatusType.Info, "Shortcut reference saved to \"" + path + "\"");
			Process.Start(path);
		}

		//mxd
		private void itemopenconfigfolder_Click(object sender, EventArgs e)
		{
			if(Directory.Exists(General.SettingsPath)) Process.Start(General.SettingsPath);
			else General.ShowErrorMessage("Huh? Where did Settings folder go?.." + Environment.NewLine
				+ "I swear it was here: \"" + General.SettingsPath + "\"!", MessageBoxButtons.OK); // I don't think this will ever happen
		}
		
		#endregion

		#region ================== Prefabs Menu

		// This sets up the prefabs menu
		private void UpdatePrefabsMenu()
		{
			menuprefabs.Visible = (General.Map != null); //mxd
			
			// Enable/disable items
			itemcreateprefab.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			iteminsertprefabfile.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Editing.Mode.Attributes.AllowCopyPaste;
			iteminsertpreviousprefab.Enabled = (General.Map != null) && (General.Editing.Mode != null) && General.Map.CopyPaste.IsPreviousPrefabAvailable && General.Editing.Mode.Attributes.AllowCopyPaste;
			
			// Toolbar icons
			buttoninsertprefabfile.Enabled = iteminsertprefabfile.Enabled;
			buttoninsertpreviousprefab.Enabled = iteminsertpreviousprefab.Enabled;
		}
		
		#endregion
		
		#region ================== Tools Menu

		// This sets up the tools menu
		private void UpdateToolsMenu()
		{
			//mxd. Enable/disable items
			bool enabled = (General.Map != null);
			itemreloadresources.Visible = enabled;
			seperatortoolsconfig.Visible = enabled;
			itemsavescreenshot.Visible = enabled;
			itemsaveeditareascreenshot.Visible = enabled;
			separatortoolsscreenshots.Visible = enabled;
			itemtestmap.Visible = enabled;

			bool supported = (enabled && !string.IsNullOrEmpty(General.Map.Config.DecorateGames));
			itemReloadGldefs.Visible = supported;
			itemReloadModedef.Visible = supported;
		}
		
		// Errors and Warnings
		[BeginAction("showerrors")]
		internal void ShowErrors()
		{
			ErrorsForm errform = new ErrorsForm();
			errform.ShowDialog(this);
			errform.Dispose();
			//mxd
			SetWarningsCount(General.ErrorLogger.ErrorsCount, false);
		}
		
		// Game Configuration action
		[BeginAction("configuration")]
		internal void ShowConfiguration()
		{
			// Show configuration dialog
			ShowConfigurationPage(-1);
		}

		// This shows the configuration on a specific page
		internal void ShowConfigurationPage(int pageindex)
		{
			// Show configuration dialog
			ConfigForm cfgform = new ConfigForm();
			if(pageindex > -1) cfgform.ShowTab(pageindex);
			if(cfgform.ShowDialog(this) == DialogResult.OK)
			{
				// Update stuff
				SetupInterface();
				UpdateInterface();
				General.Editing.UpdateCurrentEditModes();
				General.Plugins.ProgramReconfigure();
				
				// Reload resources if a map is open
				if((General.Map != null) && cfgform.ReloadResources) General.Actions.InvokeAction("builder_reloadresources");
				
				// Redraw display
				RedrawDisplay();
			}

			// Done
			cfgform.Dispose();
		}

		// Preferences action
		[BeginAction("preferences")]
		internal void ShowPreferences()
		{
			// Show preferences dialog
			PreferencesForm prefform = new PreferencesForm();
			if(prefform.ShowDialog(this) == DialogResult.OK)
			{
				// Update stuff
				SetupInterface();
				UpdateInterface();
				ApplyShortcutKeys();
				General.Colors.CreateCorrectionTable();
				General.Plugins.ProgramReconfigure();
				
				// Map opened?
				if(General.Map != null)
				{
					// Reload resources!
					if(General.Map.ScriptEditor != null) General.Map.ScriptEditor.Editor.RefreshSettings();
					General.Map.Graphics.SetupSettings();
					General.Map.UpdateConfiguration();
					if(prefform.ReloadResources) General.Actions.InvokeAction("builder_reloadresources");
				}
				
				// Redraw display
				RedrawDisplay();
			}

			// Done
			prefform.Dispose();
		}

		//mxd
		internal void SaveScreenshot(bool activeControlOnly) 
		{
			//pick a valid folder
			string folder = General.Settings.ScreenshotsPath;
			if(!Directory.Exists(folder)) 
			{
				if(folder != General.DefaultScreenshotsPath
					&& General.ShowErrorMessage("Screenshots save path \"" + folder
					+ "\" does not exist!\nPress OK to save to the default folder (\"" 
					+ General.DefaultScreenshotsPath
					+ "\").\nPress Cancel to abort.", MessageBoxButtons.OKCancel) == DialogResult.Cancel) return;


				folder = General.DefaultScreenshotsPath;
				if(!Directory.Exists(folder)) Directory.CreateDirectory(folder);
			}

			// Create name and bounds
			string name;
			Rectangle bounds;
			bool displayextrainfo = false;
			string mapname = (General.Map != null ? Path.GetFileNameWithoutExtension(General.Map.FileTitle) : General.ThisAssembly.GetName().Name);

			if(activeControlOnly)
			{
				if(Form.ActiveForm != null && Form.ActiveForm != this)
				{
					name = mapname + " (" + Form.ActiveForm.Text + ") at ";
					bounds = (Form.ActiveForm.WindowState == FormWindowState.Maximized ? 
						Screen.GetWorkingArea(Form.ActiveForm) : 
						Form.ActiveForm.Bounds);
				}
				else
				{
					name = mapname + " (edit area) at ";
					bounds = this.display.Bounds;
					bounds.Offset(this.PointToScreen(new Point()));
					displayextrainfo = true;
				}
			} 
			else
			{
				name = mapname + " at ";
				bounds = (this.WindowState == FormWindowState.Maximized ? Screen.GetWorkingArea(this) : this.Bounds);
			}

			Point cursorLocation = Point.Empty;
			//dont want to render the cursor in VisualMode
			if(General.Editing.Mode == null || !(General.Editing.Mode is VisualMode))
				cursorLocation = Cursor.Position - new Size(bounds.Location);

			//create path
			string date = DateTime.Now.ToString("yyyy.MM.dd HH-mm-ss.fff");
			string revision = (General.DebugBuild ? "DEVBUILD" : "R" + General.ThisAssembly.GetName().Version.MinorRevision);
			string path = Path.Combine(folder, name + date + " [" + revision + "].jpg");

			//save image
			using(Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height)) 
			{
				using(Graphics g = Graphics.FromImage(bitmap)) 
				{
					g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);

					//draw the cursor
					if(!cursorLocation.IsEmpty) g.DrawImage(Resources.Cursor, cursorLocation);

					//gather some info
					string info;
					if(displayextrainfo && General.Editing.Mode != null) 
					{
						info = General.Map.FileTitle + " | " + General.Map.Options.CurrentName + " | ";

						//get map coordinates
						if(General.Editing.Mode is ClassicMode) 
						{
							Vector2D pos = ((ClassicMode) General.Editing.Mode).MouseMapPos;

							//mouse inside the view?
							if(pos.IsFinite()) 
							{
								info += "X:" + Math.Round(pos.x) + " Y:" + Math.Round(pos.y);
							} 
							else 
							{
								info += "X:" + Math.Round(General.Map.Renderer2D.TranslateX) + " Y:" + Math.Round(General.Map.Renderer2D.TranslateY);
							}
						} 
						else 
						{ //should be visual mode
							info += "X:" + Math.Round(General.Map.VisualCamera.Position.x) + " Y:" + Math.Round(General.Map.VisualCamera.Position.y) + " Z:" + Math.Round(General.Map.VisualCamera.Position.z);
						}

						//add the revision number
						info += " | " + revision;
					} 
					else 
					{
						//just use the revision number
						info = revision;
					}

					//draw info
					Font font = new Font("Tahoma", 10);
					SizeF rect = g.MeasureString(info, font);
					float px = bounds.Width - rect.Width - 4;
					float py = 4;

					g.FillRectangle(Brushes.Black, px, py, rect.Width, rect.Height + 3);
					using(SolidBrush brush = new SolidBrush(Color.White))
					{
						g.DrawString(info, font, brush, px + 2, py + 2);
					}
				}

				try 
				{
					ImageCodecInfo jpegCodec = null;
					ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
					foreach(ImageCodecInfo codec in codecs) 
					{
						if(codec.FormatID == ImageFormat.Jpeg.Guid) 
						{
							jpegCodec = codec;
							break;
						}
					}

					EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, 90L);
					EncoderParameters encoderParams = new EncoderParameters(1);
					encoderParams.Param[0] = qualityParam;

					bitmap.Save(path, jpegCodec, encoderParams);
					DisplayStatus(StatusType.Info, "Screenshot saved to \"" + path + "\"");
				} 
				catch(ExternalException e) 
				{
					DisplayStatus(StatusType.Warning, "Failed to save screenshot...");
					General.ErrorLogger.Add(ErrorType.Error, "Failed to save screenshot: " + e.Message);
				}
			}
		}
		
		#endregion

		#region ================== Models and Lights mode (mxd)

		private void ChangeModelRenderingMode(object sender, EventArgs e)
		{
			General.Settings.GZDrawModelsMode = (ModelRenderMode)((ToolStripMenuItem)sender).Tag;

			switch(General.Settings.GZDrawModelsMode) 
			{
				case ModelRenderMode.NONE:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: NONE");
					break;

				case ModelRenderMode.SELECTION:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: SELECTION ONLY");
					break;

				case ModelRenderMode.ACTIVE_THINGS_FILTER:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: ACTIVE THINGS FILTER ONLY");
					break;

				case ModelRenderMode.ALL:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: ALL");
					break;
			}

			UpdateViewMenu();
			UpdateGZDoomPanel();
			RedrawDisplay();
		}

		private void ChangeLightRenderingMode(object sender, EventArgs e) 
		{
			General.Settings.GZDrawLightsMode = (LightRenderMode)((ToolStripMenuItem)sender).Tag;

			switch(General.Settings.GZDrawLightsMode) 
			{
				case LightRenderMode.NONE:
					General.MainWindow.DisplayStatus(StatusType.Action, "Dynamic lights rendering mode: NONE");
					break;

				case LightRenderMode.ALL:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: ALL");
					break;

				case LightRenderMode.ALL_ANIMATED:
					General.MainWindow.DisplayStatus(StatusType.Action, "Models rendering mode: ANIMATED");
					break;
			}

			UpdateViewMenu();
			UpdateGZDoomPanel();
			RedrawDisplay();
		}


		#endregion

		#region ================== Info Panels

		// This toggles the panel expanded / collapsed
		[BeginAction("toggleinfopanel")]
		internal void ToggleInfoPanel()
		{
			if(IsInfoPanelExpanded)
			{
				panelinfo.Height = buttontoggleinfo.Height + buttontoggleinfo.Top;
				buttontoggleinfo.Image = Resources.InfoPanelExpand; //mxd
				if(linedefinfo.Visible) linedefinfo.Hide();
				if(vertexinfo.Visible) vertexinfo.Hide();
				if(sectorinfo.Visible) sectorinfo.Hide();
				if(thinginfo.Visible) thinginfo.Hide();
				modename.Visible = false;
#if DEBUG
				console.Visible = false; //mxd
#endif
				statistics.Visible = false; //mxd
				labelcollapsedinfo.Visible = true;
				itemtoggleinfo.Checked = false;
			}
			else
			{
				panelinfo.Height = heightpanel1.Height;
				buttontoggleinfo.Image = Resources.InfoPanelCollapse; //mxd
				labelcollapsedinfo.Visible = false;
				itemtoggleinfo.Checked = true;
				if(lastinfoobject is Vertex) ShowVertexInfo((Vertex)lastinfoobject);
				else if(lastinfoobject is Linedef) ShowLinedefInfo((Linedef)lastinfoobject);
				else if(lastinfoobject is Sector) ShowSectorInfo((Sector)lastinfoobject);
				else if(lastinfoobject is Thing) ShowThingInfo((Thing)lastinfoobject);
				else HideInfo();
			}

			dockerspanel.Height = dockersspace.Height; //mxd
			FocusDisplay();
		}

		// Mouse released on info panel toggle button
		private void buttontoggleinfo_MouseUp(object sender, MouseEventArgs e)
		{
			dockerspanel.Height = dockersspace.Height; //mxd
			FocusDisplay();
		}
		
		// This displays the current mode name
		internal void DisplayModeName(string name)
		{
			if(lastinfoobject == null) 
			{
				labelcollapsedinfo.Text = name;
				labelcollapsedinfo.Refresh();
			}
			modename.Text = name;
			modename.Refresh();
		}
		
		// This hides all info panels
		public void HideInfo()
		{
			// Hide them all
			// [ZZ]
			panelinfo.SuspendLayout();
			bool showModeName = ((General.Map != null) && IsInfoPanelExpanded); //mxd
			lastinfoobject = null;
			if(linedefinfo.Visible) linedefinfo.Hide();
			if(vertexinfo.Visible) vertexinfo.Hide();
			if(sectorinfo.Visible) sectorinfo.Hide();
			if(thinginfo.Visible) thinginfo.Hide();
			labelcollapsedinfo.Text = modename.Text;
			labelcollapsedinfo.Refresh();
#if DEBUG
			console.Visible = true;
#else
			modename.Visible = showModeName;
#endif
			modename.Refresh();
			statistics.Visible = showModeName; //mxd

			//mxd. Let the plugins know
			General.Plugins.OnHighlightLost();
			// [ZZ]
			panelinfo.ResumeLayout();
		}
		
		// This refreshes info
		public void RefreshInfo()
		{
			if(lastinfoobject is Vertex) ShowVertexInfo((Vertex)lastinfoobject);
			else if(lastinfoobject is Linedef) ShowLinedefInfo((Linedef)lastinfoobject);
			else if(lastinfoobject is Sector) ShowSectorInfo((Sector)lastinfoobject);
			else if(lastinfoobject is Thing) ShowThingInfo((Thing)lastinfoobject);

			//mxd. Let the plugins know
			// [ZZ]
			panelinfo.SuspendLayout();
			General.Plugins.OnHighlightRefreshed(lastinfoobject);
			panelinfo.ResumeLayout();
		}

		//mxd
		public void ShowHints(string hintsText) 
		{
			if(!string.IsNullOrEmpty(hintsText)) 
			{
				hintsPanel.SetHints(hintsText);
			} 
			else 
			{
				ClearHints();
			}
		}

		//mxd
		public void ClearHints() 
		{
			hintsPanel.ClearHints();
		}

		//mxd
		internal void AddHintsDocker() 
		{
			if(!dockerspanel.Contains(hintsDocker)) dockerspanel.Add(hintsDocker, false);
		}

		//mxd
		internal void RemoveHintsDocker() 
		{
			dockerspanel.Remove(hintsDocker);
		}

		//mxd. Show linedef info
		public void ShowLinedefInfo(Linedef l) 
		{
			ShowLinedefInfo(l, null);
		}
		
		//mxd. Show linedef info and highlight given sidedef
		public void ShowLinedefInfo(Linedef l, Sidedef highlightside)
		{
			if(l.IsDisposed)
			{
				HideInfo();
				return;
			}

			// [ZZ]
			panelinfo.SuspendLayout();
			lastinfoobject = l;
			modename.Visible = false;
#if DEBUG
			console.Visible = console.AlwaysOnTop; //mxd
#endif
			statistics.Visible = false; //mxd
			if(vertexinfo.Visible) vertexinfo.Hide();
			if(sectorinfo.Visible) sectorinfo.Hide();
			if(thinginfo.Visible) thinginfo.Hide();
			if(IsInfoPanelExpanded) linedefinfo.ShowInfo(l, highlightside);

			// Show info on collapsed label
			if(General.Map.Config.LinedefActions.ContainsKey(l.Action)) 
			{
				LinedefActionInfo act = General.Map.Config.LinedefActions[l.Action];
				labelcollapsedinfo.Text = act.ToString();
			} 
			else if(l.Action == 0)
			{
				labelcollapsedinfo.Text = l.Action + " - None";
			}
			else
			{
				labelcollapsedinfo.Text = l.Action + " - Unknown";
			}
			labelcollapsedinfo.Refresh();

			//mxd. let the plugins know
			General.Plugins.OnHighlightLinedef(l);
			// [ZZ]
			panelinfo.ResumeLayout();
		}

		// Show vertex info
		public void ShowVertexInfo(Vertex v) 
		{
			if(v.IsDisposed) 
			{
				HideInfo();
				return;
			}
			
			// [ZZ]
			panelinfo.SuspendLayout();
			lastinfoobject = v;
			modename.Visible = false;
#if DEBUG
			console.Visible = console.AlwaysOnTop; //mxd
#endif
			statistics.Visible = false; //mxd
			if(linedefinfo.Visible) linedefinfo.Hide();
			if(sectorinfo.Visible) sectorinfo.Hide();
			if(thinginfo.Visible) thinginfo.Hide();
			if(IsInfoPanelExpanded) vertexinfo.ShowInfo(v);

			// Show info on collapsed label
			labelcollapsedinfo.Text = v.Position.x.ToString("0.##") + ", " + v.Position.y.ToString("0.##");
			labelcollapsedinfo.Refresh();

			//mxd. let the plugins know
			General.Plugins.OnHighlightVertex(v);
			// [ZZ]
			panelinfo.ResumeLayout();
		}

		//mxd. Show sector info
		public void ShowSectorInfo(Sector s) 
		{
			ShowSectorInfo(s, false, false);
		}

		// Show sector info
		public void ShowSectorInfo(Sector s, bool highlightceiling, bool highlightfloor) 
		{
			if(s.IsDisposed) 
			{
				HideInfo();
				return;
			}

			// [ZZ]
			panelinfo.SuspendLayout();
			lastinfoobject = s;
			modename.Visible = false;
#if DEBUG
			console.Visible = console.AlwaysOnTop; //mxd
#endif
			statistics.Visible = false; //mxd
			if(linedefinfo.Visible) linedefinfo.Hide();
			if(vertexinfo.Visible) vertexinfo.Hide();
			if(thinginfo.Visible) thinginfo.Hide();
			if(IsInfoPanelExpanded) sectorinfo.ShowInfo(s, highlightceiling, highlightfloor); //mxd

			// Show info on collapsed label
			if(General.Map.Config.SectorEffects.ContainsKey(s.Effect))
				labelcollapsedinfo.Text = General.Map.Config.SectorEffects[s.Effect].ToString();
			else if(s.Effect == 0)
				labelcollapsedinfo.Text = s.Effect + " - Normal";
			else
				labelcollapsedinfo.Text = s.Effect + " - Unknown";

			labelcollapsedinfo.Refresh();

			//mxd. let the plugins know
			General.Plugins.OnHighlightSector(s);
			// [ZZ]
			panelinfo.ResumeLayout();
		}

		// Show thing info
		public void ShowThingInfo(Thing t)
		{
			if(t.IsDisposed)
			{
				HideInfo();
				return;
			}

			// [ZZ]
			panelinfo.SuspendLayout();
			lastinfoobject = t;
			modename.Visible = false;
#if DEBUG
			console.Visible = console.AlwaysOnTop; //mxd
#endif
			statistics.Visible = false; //mxd
			if(linedefinfo.Visible) linedefinfo.Hide();
			if(vertexinfo.Visible) vertexinfo.Hide();
			if(sectorinfo.Visible) sectorinfo.Hide();
			if(IsInfoPanelExpanded) thinginfo.ShowInfo(t);

			// Show info on collapsed label
			ThingTypeInfo ti = General.Map.Data.GetThingInfo(t.Type);
			labelcollapsedinfo.Text = t.Type + " - " + ti.Title;
			labelcollapsedinfo.Refresh();

			//mxd. let the plugins know
			General.Plugins.OnHighlightThing(t);
			// [ZZ]
			panelinfo.ResumeLayout();
		}

		#endregion

		#region ================== Dialogs

		// This browses for a texture
		// Returns the new texture name or the same texture name when cancelled
		public string BrowseTexture(IWin32Window owner, string initialvalue)
		{
			return TextureBrowserForm.Browse(owner, initialvalue, false);//mxd
		}

		// This browses for a flat
		// Returns the new flat name or the same flat name when cancelled
		public string BrowseFlat(IWin32Window owner, string initialvalue)
		{
			return TextureBrowserForm.Browse(owner, initialvalue, true); //mxd. was FlatBrowserForm
		}
		
		// This browses the lindef types
		// Returns the new action or the same action when cancelled
		public int BrowseLinedefActions(IWin32Window owner, int initialvalue)
		{
			return ActionBrowserForm.BrowseAction(owner, initialvalue, false);
		}
		
		//mxd. This browses the lindef types
		// Returns the new action or the same action when cancelled
		public int BrowseLinedefActions(IWin32Window owner, int initialvalue, bool addanyaction)
		{
			return ActionBrowserForm.BrowseAction(owner, initialvalue, addanyaction);
		}

		// This browses sector effects
		// Returns the new effect or the same effect when cancelled
		public int BrowseSectorEffect(IWin32Window owner, int initialvalue)
		{
			return EffectBrowserForm.BrowseEffect(owner, initialvalue, false);
		}

		//mxd. This browses sector effects
		// Returns the new effect or the same effect when cancelled
		public int BrowseSectorEffect(IWin32Window owner, int initialvalue, bool addanyeffect)
		{
			return EffectBrowserForm.BrowseEffect(owner, initialvalue, addanyeffect);
		}

		// This browses thing types
		// Returns the new thing type or the same thing type when cancelled
		public int BrowseThingType(IWin32Window owner, int initialvalue)
		{
			return ThingBrowserForm.BrowseThing(owner, initialvalue);
		}

		//mxd
		public DialogResult ShowEditVertices(ICollection<Vertex> vertices) 
		{
			return ShowEditVertices(vertices, true);
		}

		//mxd. This shows the dialog to edit vertices
		public DialogResult ShowEditVertices(ICollection<Vertex> vertices, bool allowPositionChange)
		{
			// Show sector edit dialog
			VertexEditForm f = new VertexEditForm();
			DisableProcessing(); //mxd
			f.Setup(vertices, allowPositionChange);
			EnableProcessing(); //mxd
			f.OnValuesChanged += EditForm_OnValuesChanged;
			editformopen = true; //mxd
			DialogResult result = f.ShowDialog(this);
			editformopen = false; //mxd
			f.Dispose();

			return result;
		}
		
		// This shows the dialog to edit lines
		public DialogResult ShowEditLinedefs(ICollection<Linedef> lines)
		{
			return ShowEditLinedefs(lines, false, false);
		}
		
		// This shows the dialog to edit lines
		public DialogResult ShowEditLinedefs(ICollection<Linedef> lines, bool selectfront, bool selectback)
		{
			DialogResult result;
			
			// Show line edit dialog
			if(General.Map.UDMF) //mxd
			{
				LinedefEditFormUDMF f = new LinedefEditFormUDMF(selectfront, selectback);
				DisableProcessing(); //mxd
				f.Setup(lines, selectfront, selectback);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}
			else
			{
				LinedefEditForm f = new LinedefEditForm();
				DisableProcessing(); //mxd
				f.Setup(lines);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}

			return result;
		}

		// This shows the dialog to edit sectors
		public DialogResult ShowEditSectors(ICollection<Sector> sectors)
		{
			DialogResult result;

			// Show sector edit dialog
			if(General.Map.UDMF) //mxd
			{ 
				SectorEditFormUDMF f = new SectorEditFormUDMF();
				DisableProcessing(); //mxd
				f.Setup(sectors);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}
			else
			{
				SectorEditForm f = new SectorEditForm();
				DisableProcessing(); //mxd
				f.Setup(sectors);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}

			return result;
		}

		// This shows the dialog to edit things
		public DialogResult ShowEditThings(ICollection<Thing> things) 
		{
			DialogResult result;

			// Show thing edit dialog
			if(General.Map.UDMF) 
			{
				ThingEditFormUDMF f = new ThingEditFormUDMF();
				DisableProcessing(); //mxd
				f.Setup(things);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			} 
			else 
			{
				ThingEditForm f = new ThingEditForm();
				DisableProcessing(); //mxd
				f.Setup(things);
				EnableProcessing(); //mxd
				f.OnValuesChanged += EditForm_OnValuesChanged;
				editformopen = true; //mxd
				result = f.ShowDialog(this);
				editformopen = false; //mxd
				f.Dispose();
			}

			return result;
		}

		//mxd
		private void EditForm_OnValuesChanged(object sender, EventArgs e) 
		{
			if(OnEditFormValuesChanged != null) 
			{
				OnEditFormValuesChanged(sender, e);
			} 
			else 
			{
				//If current mode doesn't handle this event, let's at least update the map and redraw display.
				General.Map.Map.Update();
				RedrawDisplay();
			}
		}

		#endregion

		#region ================== Threadsafe updates

		object syncobject = new object();
		List<System.Action> queuedActions = new List<System.Action>();

		internal void ProcessQueuedUIActions()
		{
			List<System.Action> queue;
			lock (syncobject)
			{
				queue = queuedActions;
				queuedActions = new List<System.Action>();
			}

			foreach (System.Action action in queue)
			{
				action();
			}
		}

		public void RunOnUIThread(System.Action action)
		{
			if (!InvokeRequired)
			{
				action();
			}
			else
			{
				bool notify;
				lock (syncobject)
				{
					notify = queuedActions.Count == 0;
					queuedActions.Add(action);
				}

				if (notify)
					General.InvokeUIActions(this);
			}
		}

		public void UpdateStatus()
		{
			RunOnUIThread(() =>
			{
				DisplayStatus(status);
			});
		}

		public void ImageDataLoaded(string imagename)
		{
			RunOnUIThread(() =>
			{
				if ((General.Map != null) && (General.Map.Data != null))
				{
					ImageData img = General.Map.Data.GetFlatImage(imagename);
					ImageDataLoaded(img);
				}
			});
		}

		public void SpriteDataLoaded(string spritename)
		{
			RunOnUIThread(() =>
			{
				if ((General.Map != null) && (General.Map.Data != null))
				{
					ImageData img = General.Map.Data.GetSpriteImage(spritename);
					if (img != null && img.UsedInMap && !img.IsDisposed)
					{
						DelayedRedraw();
					}
				}
			});
		}

		#endregion

		#region ================== Message Pump

		// This handles messages
		protected override void WndProc(ref Message m)
		{
			// Notify message?
			switch(m.Msg)
			{
				case General.WM_UIACTION:
					ProcessQueuedUIActions();
					break;

				case General.WM_SYSCOMMAND:
					// We don't want to open a menu when ALT is pressed
					if(m.WParam.ToInt32() != General.SC_KEYMENU)
					{
						base.WndProc(ref m);
					}
					break;

				case General.WM_MOUSEHWHEEL:
					int delta = unchecked((short)(m.WParam.ToInt64() >> 16));
					OnMouseHWheel(delta);
					m.Result = new IntPtr(delta);
					break;
					
				default:
					// Let the base handle the message
					base.WndProc(ref m);
					break;
			}
		}

		//mxd. Warnings panel
		private delegate void SetWarningsCountCallback(int count, bool blink);
		internal void SetWarningsCount(int count, bool blink) 
		{
			RunOnUIThread(() =>
			{
				// Update icon, start annoying blinking if necessary
				if (count > 0)
				{
					if (blink && !blinkTimer.Enabled) blinkTimer.Start();
					warnsLabel.Image = Resources.Warning;
				}
				else
				{
					blinkTimer.Stop();
					warnsLabel.Image = Resources.WarningOff;
					warnsLabel.BackColor = SystemColors.Control;
				}

				// Update errors count
				warnsLabel.Text = count.ToString();
			});
		}

		//mxd. Bliks warnings indicator
		private void Blink() 
		{
			warnsLabel.BackColor = (warnsLabel.BackColor == Color.Red ? SystemColors.Control : Color.Red);
		}

		//mxd
		private void warnsLabel_Click(object sender, EventArgs e) 
		{
			ShowErrors();
		}

		//mxd
		private void blinkTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) 
		{
			if(!blinkTimer.Enabled) return;
			try 
			{
				RunOnUIThread(() =>
				{
					Blink();
				});
			} catch(ObjectDisposedException) { } //la-la-la. We don't care.
		}
		
		#endregion
		
		#region ================== Processing
		
		// This is called from the background thread when images are loaded
		// but only when first loaded or when dimensions were changed
		internal void ImageDataLoaded(ImageData img)
		{
			// Image is used in the map?
			if ((img != null) && img.UsedInMap && !img.IsDisposed)
			{
				// Go for all setors
				bool updated = false;
				long imgshorthash = General.Map.Data.GetShortLongFlatName(img.LongName); //mxd. Part of long name support shennanigans

				foreach(Sector s in General.Map.Map.Sectors)
				{
					// Update floor buffer if needed
					if(s.LongFloorTexture == img.LongName || s.LongFloorTexture == imgshorthash)
					{
						s.UpdateFloorSurface();
						updated = true;
					}
					
					// Update ceiling buffer if needed
					if(s.LongCeilTexture == img.LongName || s.LongCeilTexture == imgshorthash)
					{
						s.UpdateCeilingSurface();
						updated = true;
					}
				}
				
				// If we made updates, redraw the screen
				if(updated) DelayedRedraw();
			}
		}

		public void EnableProcessing()
		{
			// Increase count
			processingcount++;

			// If not already enabled, enable processing now
			if(!processor.Enabled)
			{
				processor.Enabled = true;
				lastupdatetime = Clock.CurrentTime;
			}
		}

		public void DisableProcessing()
		{
			// Increase count
			processingcount--;
			if(processingcount < 0) processingcount = 0;
			
			// Turn off
			if(processor.Enabled && (processingcount == 0))
				processor.Enabled = false;
		}

		internal void StopProcessing()
		{
			// Turn off
			processingcount = 0;
			processor.Enabled = false;
		}

		//mxd
		internal void ResetClock()
		{
			Clock.Reset();
			lastupdatetime = 0;
			
			// Let the mode know...
			if(General.Editing.Mode != null)
				General.Editing.Mode.OnClockReset();
		}
		
		// Processor event
		private void processor_Tick(object sender, EventArgs e)
		{
			long curtime = Clock.CurrentTime;
			long deltatime = curtime - lastupdatetime;
			lastupdatetime = curtime;
			
			if((General.Map != null) && (General.Editing.Mode != null))
			{
				// In exclusive mouse mode?
				if(mouseinput != null)
				{
					Vector2D deltamouse = mouseinput.Process();
					General.Plugins.OnEditMouseInput(deltamouse);
					General.Editing.Mode.OnMouseInput(deltamouse);
				}

				// Process signal
				General.Editing.Mode.OnProcess(deltatime);
			}
		}

		#endregion

		#region ================== Dockers
		
		// This adds a docker
		public void AddDocker(Docker d)
		{
			if(dockerspanel.Contains(d)) return; //mxd
			
			// Make sure the full name is set with the plugin name as prefix
			Plugin plugin = General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly());
			d.MakeFullName(plugin.Name.ToLowerInvariant());

			dockerspanel.Add(d, false);
		}

		//mxd. This also adds a docker
		public void AddDocker(Docker d, bool notify)
		{
			if(dockerspanel.Contains(d)) return; //mxd

			// Make sure the full name is set with the plugin name as prefix
			Plugin plugin = General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly());
			d.MakeFullName(plugin.Name.ToLowerInvariant());
			
			dockerspanel.Add(d, notify);
		}
		
		// This removes a docker
		public bool RemoveDocker(Docker d)
		{
			if(!dockerspanel.Contains(d)) return true; //mxd. Already removed/never added
			
			// Make sure the full name is set with the plugin name as prefix
			//Plugin plugin = General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly());
			//d.MakeFullName(plugin.Name.ToLowerInvariant());
			
			// We must release all keys because the focus may be stolen when
			// this was the selected docker (the previous docker is automatically selected)
			ReleaseAllKeys();
			
			return dockerspanel.Remove(d);
		}
		
		// This selects a docker
		public bool SelectDocker(Docker d)
		{
			if(!dockerspanel.Contains(d)) return false; //mxd
			
			// Make sure the full name is set with the plugin name as prefix
			Plugin plugin = General.Plugins.FindPluginByAssembly(Assembly.GetCallingAssembly());
			d.MakeFullName(plugin.Name.ToLowerInvariant());
			
			// We must release all keys because the focus will be stolen
			ReleaseAllKeys();
			
			return dockerspanel.SelectDocker(d);
		}
		
		// This selects the previous selected docker
		public void SelectPreviousDocker()
		{
			// We must release all keys because the focus will be stolen
			ReleaseAllKeys();
			
			dockerspanel.SelectPrevious();
		}
		
		// Mouse enters dockers window
		private void dockerspanel_MouseContainerEnter(object sender, EventArgs e)
		{
			if(General.Settings.CollapseDockers)
				dockerscollapser.Start();
			
			dockerspanel.Expand();
		}
		
		// Automatic collapsing
		private void dockerscollapser_Tick(object sender, EventArgs e)
		{
			if(General.Settings.CollapseDockers)
			{
				if(!dockerspanel.IsFocused)
				{
					Point p = this.PointToClient(Cursor.Position);
					Rectangle r = new Rectangle(dockerspanel.Location, dockerspanel.Size);
					if(!r.IntersectsWith(new Rectangle(p, Size.Empty)))
					{
						dockerspanel.Collapse();
						dockerscollapser.Stop();
					}
				}
			}
			else
			{
				dockerscollapser.Stop();
			}
		}
		
		// User resizes the docker
		private void dockerspanel_UserResize(object sender, EventArgs e)
		{
			General.Settings.DockersWidth = dockerspanel.Width;

			if(!General.Settings.CollapseDockers)
			{
				dockersspace.Width = dockerspanel.Width;
				dockerspanel.Left = dockersspace.Left;
			}
		}
		
		#endregion

		#region ================== Updater (mxd)

		private delegate void UpdateAvailableCallback(int remoterev, string changelog);
		internal void UpdateAvailable(int remoterev, string changelog)
		{
			RunOnUIThread(() => {
				// Show the window
				UpdateForm form = new UpdateForm(remoterev, changelog);
				form.FormClosing += delegate
				{
					// Update ignored revision number
					General.Settings.IgnoredRemoteRevision = (form.IgnoreThisUpdate ? remoterev : 0);
				};
				form.Show(this);
			});
		}

		#endregion

		#region ================== Graphics (mxd)

		public SizeF MeasureString(string text, Font font)
		{
			SizeF length;

			// Be thread safe
			lock(graphics)
			{
				length = graphics.MeasureString(text, font);
			}

			return length;
		}

		public SizeF MeasureString(string text, Font font, int width, StringFormat format)
		{
			SizeF length;

			// Be thread safe
			lock (graphics)
			{

				length = graphics.MeasureString(text, font, width, format);
			}

			return length;
		}

		#endregion

		RenderTargetControl display = new RenderTargetControl();
		Panel panelinfo = new Panel();
		Panel heightpanel1 = new Panel();
		Panel dockersspace = new Panel();
		MenuStrip menumain = new MenuStrip();
		ToolStrip toolbar = new ToolStrip();
		ToolStrip modestoolbar = new ToolStrip();
		ToolStrip modecontrolstoolbar = new ToolStrip();
		StatusStrip statusbar = new StatusStrip();
		ToolStripButton buttonsnaptogrid = new ToolStripButton();
		ToolStripButton buttonautomerge = new ToolStripButton();
		ToolStripButton buttonviewnormal = new ToolStripButton();
		ToolStripButton buttonviewbrightness = new ToolStripButton();
		ToolStripButton buttonviewfloors = new ToolStripButton();
		ToolStripButton buttonviewceilings = new ToolStripButton();
		ToolStripButton buttonmergegeoclassic = new ToolStripButton();
		ToolStripButton buttonmergegeo = new ToolStripButton();
		ToolStripButton buttonplacegeo = new ToolStripButton();
		ToolStripButton buttontoggledynamicgrid = new ToolStripButton();
		ToolStripButton buttonnewmap = new ToolStripButton();
		ToolStripButton buttonopenmap = new ToolStripButton();
		ToolStripButton buttonsavemap = new ToolStripButton();
		ToolStripButton buttonscripteditor = new ToolStripButton();
		ToolStripButton buttonundo = new ToolStripButton();
		ToolStripButton buttonredo = new ToolStripButton();
		ToolStripButton buttoncut = new ToolStripButton();
		ToolStripButton buttoncopy = new ToolStripButton();
		ToolStripButton buttonpaste = new ToolStripButton();
		ToolStripButton buttoninsertprefabfile = new ToolStripButton();
		ToolStripButton buttoninsertpreviousprefab = new ToolStripButton();
		ToolStripButton buttonthingsfilter = new ToolStripButton();
		ToolStripButton buttonfullbrightness = new ToolStripButton();
		ToolStripButton buttonlinededfcolors = new ToolStripButton();
		ToolStripButton buttontogglegrid = new ToolStripButton();
		ToolStripButton buttontogglecomments = new ToolStripButton();
		ToolStripButton buttontogglefixedthingsscale = new ToolStripButton();
		ToolStripButton buttonsplitjoinedsectors = new ToolStripButton();
		ToolStripButton buttontogglefog = new ToolStripButton();
		ToolStripButton buttontogglesky = new ToolStripButton();
		ToolStripButton buttontoggleeventlines = new ToolStripButton();
		ToolStripButton buttontogglevisualvertices = new ToolStripButton();
		ToolStripButton buttonautoclearsidetextures = new ToolStripButton();
		ToolStripSplitButton buttontest = new ToolStripSplitButton();
		ToolStripSplitButton dynamiclightmode = new ToolStripSplitButton();
		ToolStripSplitButton modelrendermode = new ToolStripSplitButton();
		ToolStripDropDownButton buttonzoom = new ToolStripDropDownButton();
		ToolStripDropDownButton buttongrid = new ToolStripDropDownButton();
		ToolStripDropDownButton linedefcolorpresets = new ToolStripDropDownButton();
		ToolStripDropDownButton thingfilters = new ToolStripDropDownButton();
		ToolStripSeparator seperatorfile = new ToolStripSeparator();
		ToolStripSeparator seperatorscript = new ToolStripSeparator();
		ToolStripSeparator seperatorprefabs = new ToolStripSeparator();
		ToolStripSeparator seperatorundo = new ToolStripSeparator();
		ToolStripSeparator seperatorcopypaste = new ToolStripSeparator();
		ToolStripSeparator seperatorfileopen = new ToolStripSeparator();
		ToolStripSeparator seperatorfilerecent = new ToolStripSeparator();
		ToolStripSeparator seperatoreditgrid = new ToolStripSeparator();
		ToolStripSeparator seperatoreditcopypaste = new ToolStripSeparator();
		ToolStripSeparator seperatorgeometry = new ToolStripSeparator();
		ToolStripSeparator seperatorviews = new ToolStripSeparator();
		ToolStripSeparator separatorgzmodes = new ToolStripSeparator();
		ToolStripSeparator seperatorfilesave = new ToolStripSeparator();
		ToolStripSeparator seperatortesting = new ToolStripSeparator();
		ToolStripSeparator seperatoreditgeometry = new ToolStripSeparator();
		ToolStripSeparator separatorlinecolors = new ToolStripSeparator();
		ToolStripSeparator separatorfilters = new ToolStripSeparator();
		ToolStripSeparator separatorhelpers = new ToolStripSeparator();
		ToolStripSeparator separatorfullbrightness = new ToolStripSeparator();
		ToolStripSeparator separatorrendering = new ToolStripSeparator();
		ToolStripSeparator seperatoreditundo = new ToolStripSeparator();
		ToolStripSeparator separatorgeomergemodes = new ToolStripSeparator();
		ToolStripSeparator seperatortoolsresources = new ToolStripSeparator();
		ToolStripSeparator seperatorviewthings = new ToolStripSeparator();
		ToolStripSeparator seperatorviewzoom = new ToolStripSeparator();
		ToolStripSeparator separatorgeomerge = new ToolStripSeparator();
		ToolStripSeparator seperatortoolsconfig = new ToolStripSeparator();
		ToolStripSeparator seperatorprefabsinsert = new ToolStripSeparator();
		ToolStripSeparator seperatorviewviews = new ToolStripSeparator();
		ToolStripSeparator seperatorhelpmanual = new ToolStripSeparator();
		ToolStripSeparator separatorDrawModes = new ToolStripSeparator();
		ToolStripSeparator toolStripSeparator5 = new ToolStripSeparator();
		ToolStripSeparator separatortoolsscreenshots = new ToolStripSeparator();
		ToolStripSeparator separatorTransformModes = new ToolStripSeparator();
		ToolStripSeparator toolStripSeparator1 = new ToolStripSeparator();
		ToolStripSeparator toolStripSeparator9 = new ToolStripSeparator();
		ToolStripSeparator toolStripSeparator12 = new ToolStripSeparator();
		ToolStripSeparator toolStripMenuItem4 = new ToolStripSeparator();
		ToolStripSeparator toolStripSeparator2 = new ToolStripSeparator();
		ToolStripSeparator toolStripSeparator3 = new ToolStripSeparator();
		ToolStripSeparator separatorio = new ToolStripSeparator();
		ToolStripMenuItem menufile = new ToolStripMenuItem();
		ToolStripMenuItem menuhelp = new ToolStripMenuItem();
		ToolStripMenuItem menutools = new ToolStripMenuItem();
		ToolStripMenuItem menuedit = new ToolStripMenuItem();
		ToolStripMenuItem menuview = new ToolStripMenuItem();
		ToolStripMenuItem menuprefabs = new ToolStripMenuItem();
		ToolStripMenuItem menuzoom = new ToolStripMenuItem();
		ToolStripMenuItem menumode = new ToolStripMenuItem();
		ToolStripMenuItem itemviewnormal = new ToolStripMenuItem();
		ToolStripMenuItem itemviewbrightness = new ToolStripMenuItem();
		ToolStripMenuItem itemviewfloors = new ToolStripMenuItem();
		ToolStripMenuItem itemviewceilings = new ToolStripMenuItem();
		ToolStripMenuItem itemmergegeoclassic = new ToolStripMenuItem();
		ToolStripMenuItem itemmergegeo = new ToolStripMenuItem();
		ToolStripMenuItem itemreplacegeo = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid05 = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid025 = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid0125 = new ToolStripMenuItem();
		ToolStripMenuItem itemdynamicgridsize = new ToolStripMenuItem();
		ToolStripMenuItem itemundo = new ToolStripMenuItem();
		ToolStripMenuItem itemredo = new ToolStripMenuItem();
		ToolStripMenuItem itemsnaptogrid = new ToolStripMenuItem();
		ToolStripMenuItem itemautomerge = new ToolStripMenuItem();
		ToolStripMenuItem toggleFile = new ToolStripMenuItem();
		ToolStripMenuItem toggleScript = new ToolStripMenuItem();
		ToolStripMenuItem toggleUndo = new ToolStripMenuItem();
		ToolStripMenuItem toggleCopy = new ToolStripMenuItem();
		ToolStripMenuItem togglePrefabs = new ToolStripMenuItem();
		ToolStripMenuItem toggleFilter = new ToolStripMenuItem();
		ToolStripMenuItem toggleViewModes = new ToolStripMenuItem();
		ToolStripMenuItem toggleGeometry = new ToolStripMenuItem();
		ToolStripMenuItem toggleTesting = new ToolStripMenuItem();
		ToolStripMenuItem toggleRendering = new ToolStripMenuItem();
		ToolStripMenuItem addToGroup = new ToolStripMenuItem();
		ToolStripMenuItem selectGroup = new ToolStripMenuItem();
		ToolStripMenuItem clearGroup = new ToolStripMenuItem();
		ToolStripMenuItem itemopenmap = new ToolStripMenuItem();
		ToolStripMenuItem itemsavemap = new ToolStripMenuItem();
		ToolStripMenuItem itemsavemapas = new ToolStripMenuItem();
		ToolStripMenuItem itemsavemapinto = new ToolStripMenuItem();
		ToolStripMenuItem itemexit = new ToolStripMenuItem();
		ToolStripMenuItem itemclosemap = new ToolStripMenuItem();
		ToolStripMenuItem itemhelpissues = new ToolStripMenuItem();
		ToolStripMenuItem itemhelpabout = new ToolStripMenuItem();
		ToolStripMenuItem itemhelpcheckupdates = new ToolStripMenuItem();
		ToolStripMenuItem itemnorecent = new ToolStripMenuItem();
		ToolStripMenuItem itemzoomfittoscreen = new ToolStripMenuItem();
		ToolStripMenuItem itemzoom100 = new ToolStripMenuItem();
		ToolStripMenuItem itemzoom200 = new ToolStripMenuItem();
		ToolStripMenuItem itemzoom50 = new ToolStripMenuItem();
		ToolStripMenuItem itemzoom25 = new ToolStripMenuItem();
		ToolStripMenuItem itemzoom10 = new ToolStripMenuItem();
		ToolStripMenuItem itemzoom5 = new ToolStripMenuItem();
		ToolStripMenuItem configurationToolStripMenuItem = new ToolStripMenuItem();
		ToolStripMenuItem preferencesToolStripMenuItem = new ToolStripMenuItem();
		ToolStripMenuItem itemmapoptions = new ToolStripMenuItem();
		ToolStripMenuItem itemreloadresources = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid1024 = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid256 = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid128 = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid64 = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid32 = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid16 = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid4 = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid8 = new ToolStripMenuItem();
		ToolStripMenuItem itemgridcustom = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid512 = new ToolStripMenuItem();
		ToolStripMenuItem itemsplitjoinedsectors = new ToolStripMenuItem();
		ToolStripMenuItem itemgridinc = new ToolStripMenuItem();
		ToolStripMenuItem itemgriddec = new ToolStripMenuItem();
		ToolStripMenuItem itemgridsetup = new ToolStripMenuItem();
		ToolStripMenuItem itemcut = new ToolStripMenuItem();
		ToolStripMenuItem itemcopy = new ToolStripMenuItem();
		ToolStripMenuItem itempaste = new ToolStripMenuItem();
		ToolStripMenuItem itemnewmap = new ToolStripMenuItem();
		ToolStripMenuItem itemthingsfilter = new ToolStripMenuItem();
		ToolStripMenuItem itemscripteditor = new ToolStripMenuItem();
		ToolStripMenuItem itemtestmap = new ToolStripMenuItem();
		ToolStripMenuItem itemcreateprefab = new ToolStripMenuItem();
		ToolStripMenuItem iteminsertprefabfile = new ToolStripMenuItem();
		ToolStripMenuItem iteminsertpreviousprefab = new ToolStripMenuItem();
		ToolStripMenuItem itemshowerrors = new ToolStripMenuItem();
		ToolStripMenuItem item2zoom5 = new ToolStripMenuItem();
		ToolStripMenuItem item2zoom10 = new ToolStripMenuItem();
		ToolStripMenuItem itemfittoscreen = new ToolStripMenuItem();
		ToolStripMenuItem item2zoom200 = new ToolStripMenuItem();
		ToolStripMenuItem item2zoom100 = new ToolStripMenuItem();
		ToolStripMenuItem item2zoom50 = new ToolStripMenuItem();
		ToolStripMenuItem item2zoom25 = new ToolStripMenuItem();
		ToolStripMenuItem itemhelprefmanual = new ToolStripMenuItem();
		ToolStripMenuItem itemhelpeditmode = new ToolStripMenuItem();
		ToolStripMenuItem itemtoggleinfo = new ToolStripMenuItem();
		ToolStripMenuItem itempastespecial = new ToolStripMenuItem();
		ToolStripMenuItem itemReloadModedef = new ToolStripMenuItem();
		ToolStripMenuItem itemReloadGldefs = new ToolStripMenuItem();
		ToolStripMenuItem itemviewusedtags = new ToolStripMenuItem();
		ToolStripMenuItem itemsavescreenshot = new ToolStripMenuItem();
		ToolStripMenuItem itemsaveeditareascreenshot = new ToolStripMenuItem();
		ToolStripMenuItem itemShortcutReference = new ToolStripMenuItem();
		ToolStripMenuItem itemopenconfigfolder = new ToolStripMenuItem();
		ToolStripMenuItem itemopenmapincurwad = new ToolStripMenuItem();
		ToolStripMenuItem itemgrid1 = new ToolStripMenuItem();
		ToolStripMenuItem itemzoom400 = new ToolStripMenuItem();
		ToolStripMenuItem itemautoclearsidetextures = new ToolStripMenuItem();
		ToolStripMenuItem itemgotocoords = new ToolStripMenuItem();
		ToolStripMenuItem itemdosnaptogrid = new ToolStripMenuItem();
		ToolStripMenuItem itemfullbrightness = new ToolStripMenuItem();
		ToolStripMenuItem itemdynlightmodes = new ToolStripMenuItem();
		ToolStripMenuItem itemnodynlights = new ToolStripMenuItem();
		ToolStripMenuItem itemdynlights = new ToolStripMenuItem();
		ToolStripMenuItem itemdynlightsanim = new ToolStripMenuItem();
		ToolStripMenuItem itemmodelmodes = new ToolStripMenuItem();
		ToolStripMenuItem itemnomdl = new ToolStripMenuItem();
		ToolStripMenuItem itemselmdl = new ToolStripMenuItem();
		ToolStripMenuItem itemfiltermdl = new ToolStripMenuItem();
		ToolStripMenuItem itemallmdl = new ToolStripMenuItem();
		ToolStripMenuItem itemtogglefog = new ToolStripMenuItem();
		ToolStripMenuItem itemtogglesky = new ToolStripMenuItem();
		ToolStripMenuItem itemtoggleeventlines = new ToolStripMenuItem();
		ToolStripMenuItem itemtogglevisualverts = new ToolStripMenuItem();
		ToolStripMenuItem itemimport = new ToolStripMenuItem();
		ToolStripMenuItem itemexport = new ToolStripMenuItem();
		ToolStripMenuItem itemviewthingtypes = new ToolStripMenuItem();
		ToolStripMenuItem sightsdontshow = new ToolStripMenuItem();
		ToolStripMenuItem lightsshow = new ToolStripMenuItem();
		ToolStripMenuItem lightsshowanimated = new ToolStripMenuItem();
		ToolStripMenuItem modelsdontshow = new ToolStripMenuItem();
		ToolStripMenuItem modelsshowselection = new ToolStripMenuItem();
		ToolStripMenuItem modelsshowfiltered = new ToolStripMenuItem();
		ToolStripMenuItem modelsshowall = new ToolStripMenuItem();
		ToolStripMenuItem item2zoom400 = new ToolStripMenuItem();
		ToolStripMenuItem item2zoom800 = new ToolStripMenuItem();
		ToolStripMenuItem itemzoom800 = new ToolStripMenuItem();
		ToolStripMenuItem itemlinedefcolors = new ToolStripMenuItem();
		ToolStripMenuItem itemtogglegrid = new ToolStripMenuItem();
		ToolStripMenuItem itemaligngridtolinedef = new ToolStripMenuItem();
		ToolStripMenuItem itemsetgridorigintovertex = new ToolStripMenuItem();
		ToolStripMenuItem itemresetgrid = new ToolStripMenuItem();
		ToolStripMenuItem itemtogglecomments = new ToolStripMenuItem();
		ToolStripMenuItem itemtogglefixedthingsscale = new ToolStripMenuItem();
		DockersControl dockerspanel = new DockersControl();
		LinedefInfoPanel linedefinfo = new LinedefInfoPanel();
		VertexInfoPanel vertexinfo = new VertexInfoPanel();
		SectorInfoPanel sectorinfo = new SectorInfoPanel();
		ThingInfoPanel thinginfo = new ThingInfoPanel();
		ToolStripStatusLabel gridlabel = new ToolStripStatusLabel();
		ToolStripStatusLabel zoomlabel = new ToolStripStatusLabel();
		ToolStripStatusLabel xposlabel = new ToolStripStatusLabel();
		ToolStripStatusLabel yposlabel = new ToolStripStatusLabel();
		ToolStripStatusLabel warnsLabel = new ToolStripStatusLabel();
		ToolStripStatusLabel poscommalabel = new ToolStripStatusLabel();
		ToolStripStatusLabel configlabel = new ToolStripStatusLabel();
		ToolStripStatusLabel statuslabel = new ToolStripStatusLabel();
		Label labelcollapsedinfo = new Label();
		Label modename = new Label();
		ContextMenuStrip toolbarContextMenu;
		Timer statusflasher;
		Timer statusresetter;
		Timer redrawtimer;
		Timer processor;
		Timer dockerscollapser;
		StatisticsControl statistics = new StatisticsControl();
		Button buttontoggleinfo = new Button();
		DebugConsole console = new DebugConsole();

		void InitializeComponent2()
		{
			if (components == null)
				components = new Container();

			ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));

			toolbarContextMenu = new ContextMenuStrip(components);
			redrawtimer = new Timer(components);
			processor = new Timer(components);
			statusflasher = new Timer(components);
			statusresetter = new Timer(components);
			dockerscollapser = new Timer(components);

			menumain.SuspendLayout();
			toolbar.SuspendLayout();
			toolbarContextMenu.SuspendLayout();
			statusbar.SuspendLayout();
			panelinfo.SuspendLayout();
			SuspendLayout();

			Name = "MainForm";
			Text = Application.ProductName + " R" + General.ThisAssembly.GetName().Version.Revision;
			AutoScaleDimensions = new SizeF(96F, 96F);
			AutoScaleMode = AutoScaleMode.Dpi;
			ClientSize = new Size(1012, 693);
			Icon = ((Icon)(resources.GetObject("$Icon")));
			StartPosition = FormStartPosition.Manual;
			Deactivate += new EventHandler(MainForm_Deactivate);
			Load += new EventHandler(MainForm_Load);
			Shown += new EventHandler(MainForm_Shown);
			Activated += new EventHandler(MainForm_Activated);
			KeyUp += new KeyEventHandler(MainForm_KeyUp);
			KeyDown += new KeyEventHandler(MainForm_KeyDown);
			MainMenuStrip = menumain;

			Controls.Add(dockerspanel);
			Controls.Add(display);
			Controls.Add(dockersspace);
			Controls.Add(modestoolbar);
			Controls.Add(toolbar);
			Controls.Add(modecontrolstoolbar);
			Controls.Add(panelinfo);
			Controls.Add(statusbar);
			Controls.Add(menumain);

			panelinfo.Controls.Add(statistics);
			panelinfo.Controls.Add(heightpanel1);
			panelinfo.Controls.Add(labelcollapsedinfo);
			panelinfo.Controls.Add(modename);
			panelinfo.Controls.Add(buttontoggleinfo);
			panelinfo.Controls.Add(console);
			panelinfo.Controls.Add(vertexinfo);
			panelinfo.Controls.Add(linedefinfo);
			panelinfo.Controls.Add(thinginfo);
			panelinfo.Controls.Add(sectorinfo);
			panelinfo.Dock = DockStyle.Bottom;
			panelinfo.Location = new Point(0, 564);
			panelinfo.Name = "panelinfo";
			panelinfo.Size = new Size(1012, 106);
			panelinfo.TabIndex = 4;

			menumain.Dock = DockStyle.Top;
			menumain.Location = new Point(0, 0);
			menumain.Name = "menumain";
			menumain.Size = new Size(328, 24);
			menumain.ImageScalingSize = MainForm.ScaledIconSize;
			menumain.TabIndex = 0;
			menumain.Items.AddRange(new ToolStripItem[] {
				menufile,
				menuedit,
				menuview,
				menumode,
				menuprefabs,
				menutools,
				menuhelp
			});

			menufile.Name = "menufile";
			menufile.Size = new Size(37, 20);
			menufile.Text = "&File";
			menufile.DropDownOpening += menufile_DropDownOpening;
			menufile.DropDownItems.AddRange(new ToolStripItem[] {
				itemnewmap,
				itemopenmap,
				itemopenmapincurwad,
				itemclosemap,
				seperatorfileopen,
				itemsavemap,
				itemsavemapas,
				itemsavemapinto,
				seperatorfilesave,
				itemimport,
				itemexport,
				separatorio,
				itemnorecent,
				seperatorfilerecent,
				itemexit
			});

			menuedit.Name = "menuedit";
			menuedit.Size = new Size(39, 20);
			menuedit.Text = "&Edit";
			menuedit.DropDownOpening += new EventHandler(menuedit_DropDownOpening);
			menuedit.DropDownItems.AddRange(new ToolStripItem[] {
				itemundo,
				itemredo,
				seperatoreditundo,
				itemcut,
				itemcopy,
				itempaste,
				itempastespecial,
				seperatoreditcopypaste,
				itemmergegeoclassic,
				itemmergegeo,
				itemreplacegeo,
				separatorgeomerge,
				itemsnaptogrid,
				itemdynamicgridsize,
				itemautomerge,
				itemsplitjoinedsectors,
				itemautoclearsidetextures,
				seperatoreditgeometry,
				itemgridinc,
				itemgriddec,
				itemdosnaptogrid,
				itemaligngridtolinedef,
				itemsetgridorigintovertex,
				itemresetgrid,
				itemgridsetup,
				toolStripSeparator5,
				addToGroup,
				selectGroup,
				clearGroup,
				seperatoreditgrid,
				itemmapoptions,
				itemviewusedtags,
				itemviewthingtypes
			});

			menuview.Name = "menuview";
			menuview.Size = new Size(44, 20);
			menuview.Text = "&View";
			menuview.DropDownItems.AddRange(new ToolStripItem[] {
				itemthingsfilter,
				itemlinedefcolors,
				seperatorviewthings,
				itemviewnormal,
				itemviewbrightness,
				itemviewfloors,
				itemviewceilings,
				seperatorviewviews,
				itemfullbrightness,
				itemtogglegrid,
				itemtogglecomments,
				itemtogglefixedthingsscale,
				separatorrendering,
				itemdynlightmodes,
				itemmodelmodes,
				itemtogglefog,
				itemtogglesky,
				itemtoggleeventlines,
				itemtogglevisualverts,
				separatorhelpers,
				menuzoom,
				itemgotocoords,
				itemfittoscreen,
				itemtoggleinfo,
				seperatorviewzoom,
				itemscripteditor
			});

			menuzoom.Image = global::CodeImp.DoomBuilder.Properties.Resources.Zoom;
			menuzoom.Name = "menuzoom";
			menuzoom.Size = new Size(215, 22);
			menuzoom.Text = "&Zoom";
			menuzoom.DropDownItems.AddRange(new ToolStripItem[] {
				item2zoom800,
				item2zoom400,
				item2zoom200,
				item2zoom100,
				item2zoom50,
				item2zoom25,
				item2zoom10,
				item2zoom5
			});

			menumode.Name = "menumode";
			menumode.Size = new Size(50, 20);
			menumode.Text = "&Mode";
			menumode.DropDownItems.AddRange(new ToolStripItem[] {
				separatorDrawModes,
				separatorTransformModes
			});

			menuprefabs.Name = "menuprefabs";
			menuprefabs.Size = new Size(58, 20);
			menuprefabs.Text = "&Prefabs";
			menuprefabs.DropDownItems.AddRange(new ToolStripItem[] {
				iteminsertprefabfile,
				iteminsertpreviousprefab,
				seperatorprefabsinsert,
				itemcreateprefab
			});

			menutools.Name = "menutools";
			menutools.Size = new Size(48, 20);
			menutools.Text = "&Tools";
			menutools.DropDownItems.AddRange(new ToolStripItem[] {
				itemreloadresources,
				itemReloadModedef,
				itemReloadGldefs,
				itemshowerrors,
				seperatortoolsresources,
				configurationToolStripMenuItem,
				preferencesToolStripMenuItem,
				seperatortoolsconfig,
				itemsavescreenshot,
				itemsaveeditareascreenshot,
				separatortoolsscreenshots,
				itemtestmap
			});

			menuhelp.Name = "menuhelp";
			menuhelp.Size = new Size(44, 20);
			menuhelp.Text = "&Help";
			menuhelp.DropDownItems.AddRange(new ToolStripItem[] {
				itemhelprefmanual,
				itemShortcutReference,
				itemopenconfigfolder,
				itemhelpeditmode,
				itemhelpissues,
				itemhelpcheckupdates,
				seperatorhelpmanual,
				itemhelpabout
			});

			itemdynlightmodes.Image = global::CodeImp.DoomBuilder.Properties.Resources.Light;
			itemdynlightmodes.Name = "itemdynlightmodes";
			itemdynlightmodes.Size = new Size(273, 22);
			itemdynlightmodes.Text = "Dynamic light rendering mode";
			itemdynlightmodes.DropDownItems.AddRange(new ToolStripItem[] {
				itemnodynlights,
				itemdynlights,
				itemdynlightsanim
			});

			itemmodelmodes.Image = global::CodeImp.DoomBuilder.Properties.Resources.Model;
			itemmodelmodes.Name = "itemmodelmodes";
			itemmodelmodes.Size = new Size(273, 22);
			itemmodelmodes.Text = "Model rendering mode";
			itemmodelmodes.DropDownItems.AddRange(new ToolStripItem[] {
				itemnomdl,
				itemselmdl,
				itemfiltermdl,
				itemallmdl
			});

			toolbarContextMenu.Name = "toolbarContextMenu";
			toolbarContextMenu.Size = new Size(227, 224);
			toolbarContextMenu.ImageScalingSize = MainForm.ScaledIconSize;
			toolbarContextMenu.Opening += new System.ComponentModel.CancelEventHandler(toolbarContextMenu_Opening);
			toolbarContextMenu.Closing += new ToolStripDropDownClosingEventHandler(toolbarContextMenu_Closing);
			toolbarContextMenu.Items.AddRange(new ToolStripItem[] {
				toggleFile,
				toggleScript,
				toggleUndo,
				toggleCopy,
				togglePrefabs,
				toggleFilter,
				toggleViewModes,
				toggleGeometry,
				toggleTesting,
				toggleRendering
			});

			buttongrid.AutoToolTip = false;
			buttongrid.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttongrid.Image = global::CodeImp.DoomBuilder.Properties.Resources.Grid2_arrowup;
			buttongrid.ImageScaling = ToolStripItemImageScaling.None;
			buttongrid.ImageTransparentColor = Color.Transparent;
			buttongrid.Name = "buttongrid";
			buttongrid.ShowDropDownArrow = false;
			buttongrid.Size = new Size(29, 21);
			buttongrid.Text = "Grid";
			buttongrid.DropDownItems.AddRange(new ToolStripItem[] {
				itemgrid1024,
				itemgrid512,
				itemgrid256,
				itemgrid128,
				itemgrid64,
				itemgrid32,
				itemgrid16,
				itemgrid8,
				itemgrid4,
				itemgrid1,
				itemgrid05,
				itemgrid025,
				itemgrid0125,
				toolStripMenuItem4,
				itemgridcustom
			});

			buttonzoom.AutoToolTip = false;
			buttonzoom.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonzoom.Image = global::CodeImp.DoomBuilder.Properties.Resources.Zoom_arrowup;
			buttonzoom.ImageScaling = ToolStripItemImageScaling.None;
			buttonzoom.ImageTransparentColor = Color.Transparent;
			buttonzoom.Name = "buttonzoom";
			buttonzoom.ShowDropDownArrow = false;
			buttonzoom.Size = new Size(29, 21);
			buttonzoom.Text = "Zoom";
			buttonzoom.DropDownItems.AddRange(new ToolStripItem[] {
				itemzoom800,
				itemzoom400,
				itemzoom200,
				itemzoom100,
				itemzoom50,
				itemzoom25,
				itemzoom10,
				itemzoom5,
				toolStripSeparator2,
				itemzoomfittoscreen
			});

			dynamiclightmode.DisplayStyle = ToolStripItemDisplayStyle.Image;
			dynamiclightmode.Image = global::CodeImp.DoomBuilder.Properties.Resources.Light;
			dynamiclightmode.ImageTransparentColor = Color.Magenta;
			dynamiclightmode.Name = "dynamiclightmode";
			dynamiclightmode.Size = new Size(32, 20);
			dynamiclightmode.Tag = "builder_gztogglelights";
			dynamiclightmode.Text = "Dynamic light mode";
			dynamiclightmode.ButtonClick += new EventHandler(InvokeTaggedAction);
			dynamiclightmode.DropDownItems.AddRange(new ToolStripItem[] {
				sightsdontshow,
				lightsshow,
				lightsshowanimated
			});

			modelrendermode.DisplayStyle = ToolStripItemDisplayStyle.Image;
			modelrendermode.Image = global::CodeImp.DoomBuilder.Properties.Resources.Model;
			modelrendermode.ImageTransparentColor = Color.Magenta;
			modelrendermode.Name = "modelrendermode";
			modelrendermode.Size = new Size(32, 20);
			modelrendermode.Tag = "builder_gztogglemodels";
			modelrendermode.Text = "Model rendering mode";
			modelrendermode.ButtonClick += new EventHandler(InvokeTaggedAction);
			modelrendermode.DropDownItems.AddRange(new ToolStripItem[] {
				modelsdontshow,
				modelsshowselection,
				modelsshowfiltered,
				modelsshowall
			});

			toolbar.AutoSize = false;
			toolbar.ImageScalingSize = MainForm.ScaledIconSize;
			toolbar.ContextMenuStrip = toolbarContextMenu;
			toolbar.GripStyle = ToolStripGripStyle.Hidden;
			toolbar.Items.AddRange(new ToolStripItem[] {
				buttonnewmap,
				buttonopenmap,
				buttonsavemap,
				seperatorfile,
				buttonscripteditor,
				seperatorscript,
				buttonundo,
				buttonredo,
				seperatorundo,
				buttoncut,
				buttoncopy,
				buttonpaste,
				seperatorcopypaste,
				buttoninsertprefabfile,
				buttoninsertpreviousprefab,
				seperatorprefabs,
				buttonthingsfilter,
				thingfilters,
				separatorlinecolors,
				buttonlinededfcolors,
				linedefcolorpresets,
				separatorfilters,
				buttonfullbrightness,
				buttontogglegrid,
				buttontogglecomments,
				buttontogglefixedthingsscale,
				separatorfullbrightness,
				buttonviewnormal,
				buttonviewbrightness,
				buttonviewfloors,
				buttonviewceilings,
				separatorgeomergemodes,
				buttonmergegeoclassic,
				buttonmergegeo,
				buttonplacegeo,
				seperatorviews,
				buttonsnaptogrid,
				buttontoggledynamicgrid,
				buttonautomerge,
				buttonsplitjoinedsectors,
				buttonautoclearsidetextures,
				seperatorgeometry,
				dynamiclightmode,
				modelrendermode,
				buttontogglefog,
				buttontogglesky,
				buttontoggleeventlines,
				buttontogglevisualvertices,
				separatorgzmodes,
				buttontest,
				seperatortesting
			});
			toolbar.Location = new Point(0, 24);
			toolbar.Name = "toolbar";
			toolbar.Size = new Size(1012, 25);
			toolbar.TabIndex = 1;

			statusbar.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
			statusbar.Location = new Point(0, 670);
			statusbar.Name = "statusbar";
			statusbar.ShowItemToolTips = true;
			statusbar.Size = new Size(1012, 23);
			statusbar.ImageScalingSize = MainForm.ScaledIconSize;
			statusbar.TabIndex = 2;
			statusbar.Items.AddRange(new ToolStripItem[] {
				statuslabel,
				configlabel,
				toolStripSeparator12,
				gridlabel,
				buttongrid,
				toolStripSeparator1,
				zoomlabel,
				buttonzoom,
				toolStripSeparator3,
				xposlabel,
				poscommalabel,
				yposlabel,
				toolStripSeparator9,
				warnsLabel
			});


			// 
			// seperatorfileopen
			// 
			seperatorfileopen.Margin = new Padding(0, 3, 0, 3);
			seperatorfileopen.Name = "seperatorfileopen";
			seperatorfileopen.Size = new Size(222, 6);
			// 
			// seperatorfilerecent
			// 
			seperatorfilerecent.Margin = new Padding(0, 3, 0, 3);
			seperatorfilerecent.Name = "seperatorfilerecent";
			seperatorfilerecent.Size = new Size(222, 6);
			// 
			// seperatoreditgrid
			// 
			seperatoreditgrid.Margin = new Padding(0, 3, 0, 3);
			seperatoreditgrid.Name = "seperatoreditgrid";
			seperatoreditgrid.Size = new Size(216, 6);
			// 
			// seperatoreditcopypaste
			// 
			seperatoreditcopypaste.Margin = new Padding(0, 3, 0, 3);
			seperatoreditcopypaste.Name = "seperatoreditcopypaste";
			seperatoreditcopypaste.Size = new Size(216, 6);
			// 
			// seperatorfile
			// 
			seperatorfile.Margin = new Padding(6, 0, 6, 0);
			seperatorfile.Name = "seperatorfile";
			seperatorfile.Size = new Size(6, 25);
			// 
			// seperatorscript
			// 
			seperatorscript.Margin = new Padding(6, 0, 6, 0);
			seperatorscript.Name = "seperatorscript";
			seperatorscript.Size = new Size(6, 25);
			// 
			// seperatorprefabs
			// 
			seperatorprefabs.Margin = new Padding(6, 0, 6, 0);
			seperatorprefabs.Name = "seperatorprefabs";
			seperatorprefabs.Size = new Size(6, 25);
			// 
			// seperatorundo
			// 
			seperatorundo.Margin = new Padding(6, 0, 6, 0);
			seperatorundo.Name = "seperatorundo";
			seperatorundo.Size = new Size(6, 25);
			// 
			// seperatorcopypaste
			// 
			seperatorcopypaste.Margin = new Padding(6, 0, 6, 0);
			seperatorcopypaste.Name = "seperatorcopypaste";
			seperatorcopypaste.Size = new Size(6, 25);
			// 
			// poscommalabel
			// 
			poscommalabel.Name = "poscommalabel";
			poscommalabel.Size = new Size(11, 18);
			poscommalabel.Tag = "builder_centeroncoordinates";
			poscommalabel.Text = ",";
			poscommalabel.ToolTipText = "Current X, Y coordinates on map.\r\nClick to set specific coordinates.";
			poscommalabel.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemnewmap
			// 
			itemnewmap.Image = global::CodeImp.DoomBuilder.Properties.Resources.File;
			itemnewmap.Name = "itemnewmap";
			itemnewmap.ShortcutKeyDisplayString = "";
			itemnewmap.Size = new Size(225, 22);
			itemnewmap.Tag = "builder_newmap";
			itemnewmap.Text = "&New Map";
			itemnewmap.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemopenmap
			// 
			itemopenmap.Image = global::CodeImp.DoomBuilder.Properties.Resources.OpenMap;
			itemopenmap.Name = "itemopenmap";
			itemopenmap.Size = new Size(225, 22);
			itemopenmap.Tag = "builder_openmap";
			itemopenmap.Text = "&Open Map...";
			itemopenmap.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemopenmapincurwad
			// 
			itemopenmapincurwad.Name = "itemopenmapincurwad";
			itemopenmapincurwad.Size = new Size(225, 22);
			itemopenmapincurwad.Tag = "builder_openmapincurrentwad";
			itemopenmapincurwad.Text = "Open Map in Current &WAD...";
			itemopenmapincurwad.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemclosemap
			// 
			itemclosemap.Name = "itemclosemap";
			itemclosemap.Size = new Size(225, 22);
			itemclosemap.Tag = "builder_closemap";
			itemclosemap.Text = "&Close Map";
			itemclosemap.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemsavemap
			// 
			itemsavemap.Image = global::CodeImp.DoomBuilder.Properties.Resources.SaveMap;
			itemsavemap.Name = "itemsavemap";
			itemsavemap.Size = new Size(225, 22);
			itemsavemap.Tag = "builder_savemap";
			itemsavemap.Text = "&Save Map";
			itemsavemap.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemsavemapas
			// 
			itemsavemapas.Name = "itemsavemapas";
			itemsavemapas.Size = new Size(225, 22);
			itemsavemapas.Tag = "builder_savemapas";
			itemsavemapas.Text = "Save Map &As...";
			itemsavemapas.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemsavemapinto
			// 
			itemsavemapinto.Name = "itemsavemapinto";
			itemsavemapinto.Size = new Size(225, 22);
			itemsavemapinto.Tag = "builder_savemapinto";
			itemsavemapinto.Text = "Save Map &Into...";
			itemsavemapinto.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatorfilesave
			// 
			seperatorfilesave.Margin = new Padding(0, 3, 0, 3);
			seperatorfilesave.Name = "seperatorfilesave";
			seperatorfilesave.Size = new Size(222, 6);
			// 
			// itemimport
			// 
			itemimport.Name = "itemimport";
			itemimport.Size = new Size(225, 22);
			itemimport.Text = "Import";
			// 
			// itemexport
			// 
			itemexport.Name = "itemexport";
			itemexport.Size = new Size(225, 22);
			itemexport.Text = "Export";
			// 
			// separatorio
			// 
			separatorio.Name = "separatorio";
			separatorio.Size = new Size(222, 6);
			// 
			// itemnorecent
			// 
			itemnorecent.Enabled = false;
			itemnorecent.Name = "itemnorecent";
			itemnorecent.Size = new Size(225, 22);
			itemnorecent.Text = "No recently opened files";
			// 
			// itemexit
			// 
			itemexit.Name = "itemexit";
			itemexit.Size = new Size(225, 22);
			itemexit.Text = "E&xit";
			itemexit.Click += new EventHandler(itemexit_Click);
			// 
			// menuedit
			// 
			// 
			// itemundo
			// 
			itemundo.Image = global::CodeImp.DoomBuilder.Properties.Resources.Undo;
			itemundo.Name = "itemundo";
			itemundo.Size = new Size(219, 22);
			itemundo.Tag = "builder_undo";
			itemundo.Text = "&Undo";
			itemundo.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemredo
			// 
			itemredo.Image = global::CodeImp.DoomBuilder.Properties.Resources.Redo;
			itemredo.Name = "itemredo";
			itemredo.Size = new Size(219, 22);
			itemredo.Tag = "builder_redo";
			itemredo.Text = "&Redo";
			itemredo.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatoreditundo
			// 
			seperatoreditundo.Margin = new Padding(0, 3, 0, 3);
			seperatoreditundo.Name = "seperatoreditundo";
			seperatoreditundo.Size = new Size(216, 6);
			// 
			// itemcut
			// 
			itemcut.Image = global::CodeImp.DoomBuilder.Properties.Resources.Cut;
			itemcut.Name = "itemcut";
			itemcut.Size = new Size(219, 22);
			itemcut.Tag = "builder_cutselection";
			itemcut.Text = "Cu&t";
			itemcut.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemcopy
			// 
			itemcopy.Image = global::CodeImp.DoomBuilder.Properties.Resources.Copy;
			itemcopy.Name = "itemcopy";
			itemcopy.Size = new Size(219, 22);
			itemcopy.Tag = "builder_copyselection";
			itemcopy.Text = "&Copy";
			itemcopy.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itempaste
			// 
			itempaste.Image = global::CodeImp.DoomBuilder.Properties.Resources.Paste;
			itempaste.Name = "itempaste";
			itempaste.Size = new Size(219, 22);
			itempaste.Tag = "builder_pasteselection";
			itempaste.Text = "&Paste";
			itempaste.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itempastespecial
			// 
			itempastespecial.Image = global::CodeImp.DoomBuilder.Properties.Resources.PasteSpecial;
			itempastespecial.Name = "itempastespecial";
			itempastespecial.Size = new Size(219, 22);
			itempastespecial.Tag = "builder_pasteselectionspecial";
			itempastespecial.Text = "Paste Special...";
			itempastespecial.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemsnaptogrid
			// 
			itemsnaptogrid.Checked = true;
			itemsnaptogrid.CheckState = CheckState.Checked;
			itemsnaptogrid.Image = global::CodeImp.DoomBuilder.Properties.Resources.Grid4;
			itemsnaptogrid.Name = "itemsnaptogrid";
			itemsnaptogrid.Size = new Size(219, 22);
			itemsnaptogrid.Tag = "builder_togglesnap";
			itemsnaptogrid.Text = "&Snap to Grid";
			itemsnaptogrid.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemautomerge
			// 
			itemautomerge.Checked = true;
			itemautomerge.CheckState = CheckState.Checked;
			itemautomerge.Image = global::CodeImp.DoomBuilder.Properties.Resources.mergegeometry2;
			itemautomerge.Name = "itemautomerge";
			itemautomerge.Size = new Size(219, 22);
			itemautomerge.Tag = "builder_toggleautomerge";
			itemautomerge.Text = "Snap to &Geometry";
			itemautomerge.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemsplitjoinedsectors
			// 
			itemsplitjoinedsectors.Checked = true;
			itemsplitjoinedsectors.CheckState = CheckState.Checked;
			itemsplitjoinedsectors.Image = global::CodeImp.DoomBuilder.Properties.Resources.SplitSectors;
			itemsplitjoinedsectors.Name = "itemsplitjoinedsectors";
			itemsplitjoinedsectors.Size = new Size(219, 22);
			itemsplitjoinedsectors.Tag = "builder_togglejoinedsectorssplitting";
			itemsplitjoinedsectors.Text = "Split &Joined Sectors when Drawing Lines";
			itemsplitjoinedsectors.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemautoclearsidetextures
			// 
			itemautoclearsidetextures.Checked = true;
			itemautoclearsidetextures.CheckState = CheckState.Checked;
			itemautoclearsidetextures.Image = global::CodeImp.DoomBuilder.Properties.Resources.ClearTextures;
			itemautoclearsidetextures.Name = "itemautoclearsidetextures";
			itemautoclearsidetextures.Size = new Size(219, 22);
			itemautoclearsidetextures.Tag = "builder_toggleautoclearsidetextures";
			itemautoclearsidetextures.Text = "&Auto Clear Sidedef Textures";
			itemautoclearsidetextures.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatoreditgeometry
			// 
			seperatoreditgeometry.Margin = new Padding(0, 3, 0, 3);
			seperatoreditgeometry.Name = "seperatoreditgeometry";
			seperatoreditgeometry.Size = new Size(216, 6);
			// 
			// itemgridinc
			// 
			itemgridinc.Image = global::CodeImp.DoomBuilder.Properties.Resources.GridIncrease;
			itemgridinc.Name = "itemgridinc";
			itemgridinc.Size = new Size(219, 22);
			itemgridinc.Tag = "builder_griddec";
			itemgridinc.Text = "&Increase Grid Size";
			itemgridinc.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemgriddec
			// 
			itemgriddec.Image = global::CodeImp.DoomBuilder.Properties.Resources.GridDecrease;
			itemgriddec.Name = "itemgriddec";
			itemgriddec.Size = new Size(219, 22);
			itemgriddec.Tag = "builder_gridinc";
			itemgriddec.Text = "&Decrease Grid Size";
			itemgriddec.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemdosnaptogrid
			// 
			itemdosnaptogrid.Image = global::CodeImp.DoomBuilder.Properties.Resources.SnapVerts;
			itemdosnaptogrid.Name = "itemdosnaptogrid";
			itemdosnaptogrid.Size = new Size(219, 22);
			itemdosnaptogrid.Tag = "builder_snapvertstogrid";
			itemdosnaptogrid.Text = "Snap Selection to Grid";
			itemdosnaptogrid.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemgridsetup
			// 
			itemgridsetup.Image = global::CodeImp.DoomBuilder.Properties.Resources.Grid2;
			itemgridsetup.Name = "itemgridsetup";
			itemgridsetup.Size = new Size(219, 22);
			itemgridsetup.Tag = "builder_gridsetup";
			itemgridsetup.Text = "&Grid and Backdrop Setup...";
			itemgridsetup.Click += new EventHandler(InvokeTaggedAction);
			// 
			// addToGroup
			// 
			addToGroup.Image = global::CodeImp.DoomBuilder.Properties.Resources.GroupAdd;
			addToGroup.Name = "addToGroup";
			addToGroup.Size = new Size(219, 22);
			addToGroup.Text = "Add Selection to Group";
			// 
			// selectGroup
			// 
			selectGroup.Image = global::CodeImp.DoomBuilder.Properties.Resources.Group;
			selectGroup.Name = "selectGroup";
			selectGroup.Size = new Size(219, 22);
			selectGroup.Text = "Select Group";
			// 
			// clearGroup
			// 
			clearGroup.Image = global::CodeImp.DoomBuilder.Properties.Resources.GroupRemove;
			clearGroup.Name = "clearGroup";
			clearGroup.Size = new Size(219, 22);
			clearGroup.Text = "Clear Group";
			// 
			// itemmapoptions
			// 
			itemmapoptions.Image = global::CodeImp.DoomBuilder.Properties.Resources.Properties;
			itemmapoptions.Name = "itemmapoptions";
			itemmapoptions.Size = new Size(219, 22);
			itemmapoptions.Tag = "builder_mapoptions";
			itemmapoptions.Text = "Map &Options...";
			itemmapoptions.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemviewusedtags
			// 
			itemviewusedtags.Image = global::CodeImp.DoomBuilder.Properties.Resources.TagStatistics;
			itemviewusedtags.Name = "itemviewusedtags";
			itemviewusedtags.Size = new Size(219, 22);
			itemviewusedtags.Tag = "builder_viewusedtags";
			itemviewusedtags.Text = "View Used Tags...";
			itemviewusedtags.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemviewthingtypes
			// 
			itemviewthingtypes.Image = global::CodeImp.DoomBuilder.Properties.Resources.ThingStatistics;
			itemviewthingtypes.Name = "itemviewthingtypes";
			itemviewthingtypes.Size = new Size(219, 22);
			itemviewthingtypes.Tag = "builder_viewthingtypes";
			itemviewthingtypes.Text = "View Thing Types...";
			itemviewthingtypes.Click += new EventHandler(InvokeTaggedAction);

			// 
			// itemthingsfilter
			// 
			itemthingsfilter.Image = global::CodeImp.DoomBuilder.Properties.Resources.Filter;
			itemthingsfilter.Name = "itemthingsfilter";
			itemthingsfilter.Size = new Size(215, 22);
			itemthingsfilter.Tag = "builder_thingsfilterssetup";
			itemthingsfilter.Text = "Configure &Things Filters...";
			itemthingsfilter.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemlinedefcolors
			// 
			itemlinedefcolors.Image = global::CodeImp.DoomBuilder.Properties.Resources.LinedefColorPresets;
			itemlinedefcolors.Name = "itemlinedefcolors";
			itemlinedefcolors.Size = new Size(215, 22);
			itemlinedefcolors.Tag = "builder_linedefcolorssetup";
			itemlinedefcolors.Text = "Configure &Linedef Colors...";
			itemlinedefcolors.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatorviewthings
			// 
			seperatorviewthings.Margin = new Padding(0, 3, 0, 3);
			seperatorviewthings.Name = "seperatorviewthings";
			seperatorviewthings.Size = new Size(212, 6);
			// 
			// itemviewnormal
			// 
			itemviewnormal.Image = global::CodeImp.DoomBuilder.Properties.Resources.ViewNormal;
			itemviewnormal.Name = "itemviewnormal";
			itemviewnormal.Size = new Size(215, 22);
			itemviewnormal.Tag = "builder_viewmodenormal";
			itemviewnormal.Text = "&Wireframe";
			itemviewnormal.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemviewbrightness
			// 
			itemviewbrightness.Image = global::CodeImp.DoomBuilder.Properties.Resources.ViewBrightness;
			itemviewbrightness.Name = "itemviewbrightness";
			itemviewbrightness.Size = new Size(215, 22);
			itemviewbrightness.Tag = "builder_viewmodebrightness";
			itemviewbrightness.Text = "&Brightness Levels";
			itemviewbrightness.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemviewfloors
			// 
			itemviewfloors.Image = global::CodeImp.DoomBuilder.Properties.Resources.ViewTextureFloor;
			itemviewfloors.Name = "itemviewfloors";
			itemviewfloors.Size = new Size(215, 22);
			itemviewfloors.Tag = "builder_viewmodefloors";
			itemviewfloors.Text = "&Floor Textures";
			itemviewfloors.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemviewceilings
			// 
			itemviewceilings.Image = global::CodeImp.DoomBuilder.Properties.Resources.ViewTextureCeiling;
			itemviewceilings.Name = "itemviewceilings";
			itemviewceilings.Size = new Size(215, 22);
			itemviewceilings.Tag = "builder_viewmodeceilings";
			itemviewceilings.Text = "&Ceiling Textures";
			itemviewceilings.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatorviewviews
			// 
			seperatorviewviews.Name = "seperatorviewviews";
			seperatorviewviews.Size = new Size(212, 6);
			// 
			// itemmergegeoclassic
			// 
			itemmergegeoclassic.Image = global::CodeImp.DoomBuilder.Properties.Resources.MergeGeoClassic;
			itemmergegeoclassic.Name = "itemmergegeoclassic";
			itemmergegeoclassic.Size = new Size(215, 22);
			itemmergegeoclassic.Tag = "builder_geomergeclassic";
			itemmergegeoclassic.Text = "Merge Dragged Vertices Only";
			itemmergegeoclassic.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemmergegeo
			// 
			itemmergegeo.Image = global::CodeImp.DoomBuilder.Properties.Resources.MergeGeo;
			itemmergegeo.Name = "itemmergegeo";
			itemmergegeo.Size = new Size(215, 22);
			itemmergegeo.Tag = "builder_geomerge";
			itemmergegeo.Text = "Merge Dragged Geometry";
			itemmergegeo.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemreplacegeo
			// 
			itemreplacegeo.Image = global::CodeImp.DoomBuilder.Properties.Resources.MergeGeoRemoveLines;
			itemreplacegeo.Name = "itemreplacegeo";
			itemreplacegeo.Size = new Size(215, 22);
			itemreplacegeo.Tag = "builder_georeplace";
			itemreplacegeo.Text = "Replace with Dragged Geometry";
			itemreplacegeo.Click += new EventHandler(InvokeTaggedAction);
			// 
			// separatorgeomerge
			// 
			separatorgeomerge.Name = "separatorgeomerge";
			separatorgeomerge.Size = new Size(212, 6);
			// 
			// itemfullbrightness
			// 
			itemfullbrightness.Checked = true;
			itemfullbrightness.CheckOnClick = true;
			itemfullbrightness.CheckState = CheckState.Checked;
			itemfullbrightness.Image = global::CodeImp.DoomBuilder.Properties.Resources.Brightness;
			itemfullbrightness.Name = "itemfullbrightness";
			itemfullbrightness.Size = new Size(215, 22);
			itemfullbrightness.Tag = "builder_togglebrightness";
			itemfullbrightness.Text = "Full Brightness";
			itemfullbrightness.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemtogglegrid
			// 
			itemtogglegrid.Checked = true;
			itemtogglegrid.CheckOnClick = true;
			itemtogglegrid.CheckState = CheckState.Checked;
			itemtogglegrid.Image = global::CodeImp.DoomBuilder.Properties.Resources.Grid2;
			itemtogglegrid.Name = "itemtogglegrid";
			itemtogglegrid.Size = new Size(215, 22);
			itemtogglegrid.Tag = "builder_togglegrid";
			itemtogglegrid.Text = "&Render Grid";
			itemtogglegrid.Click += new EventHandler(InvokeTaggedAction);

			// 
			// itemaligngridtolinedef
			//
			itemaligngridtolinedef.Name = "itemaligngridtolinedef";
			itemaligngridtolinedef.Size = new Size(215, 22);
			itemaligngridtolinedef.Tag = "builder_aligngridtolinedef";
			itemaligngridtolinedef.Text = "Align Grid To Selected Linedef";
			itemaligngridtolinedef.Click += new EventHandler(InvokeTaggedAction);

			// 
			// itemsetgridorigintovertex
			//
			itemsetgridorigintovertex.Name = "itemsetgridorigintovertex";
			itemsetgridorigintovertex.Size = new Size(215, 22);
			itemsetgridorigintovertex.Tag = "builder_setgridorigintovertex";
			itemsetgridorigintovertex.Text = "Set Grid Origin To Selected Vertex";
			itemsetgridorigintovertex.Click += new EventHandler(InvokeTaggedAction);

			// 
			// itemresetgrid
			//
			itemresetgrid.Name = "itemresetgrid";
			itemresetgrid.Size = new Size(215, 22);
			itemresetgrid.Tag = "builder_resetgrid";
			itemresetgrid.Text = "Reset Grid Transform";
			itemresetgrid.Click += new EventHandler(InvokeTaggedAction);

			// 
			// item2zoom800
			// 
			item2zoom800.Name = "item2zoom800";
			item2zoom800.Size = new Size(102, 22);
			item2zoom800.Tag = "800";
			item2zoom800.Text = "800%";
			item2zoom800.Click += new EventHandler(itemzoomto_Click);
			// 
			// item2zoom400
			// 
			item2zoom400.Name = "item2zoom400";
			item2zoom400.Size = new Size(102, 22);
			item2zoom400.Tag = "400";
			item2zoom400.Text = "400%";
			item2zoom400.Click += new EventHandler(itemzoomto_Click);
			// 
			// item2zoom200
			// 
			item2zoom200.Name = "item2zoom200";
			item2zoom200.Size = new Size(102, 22);
			item2zoom200.Tag = "200";
			item2zoom200.Text = "200%";
			item2zoom200.Click += new EventHandler(itemzoomto_Click);
			// 
			// item2zoom100
			// 
			item2zoom100.Name = "item2zoom100";
			item2zoom100.Size = new Size(102, 22);
			item2zoom100.Tag = "100";
			item2zoom100.Text = "100%";
			item2zoom100.Click += new EventHandler(itemzoomto_Click);
			// 
			// item2zoom50
			// 
			item2zoom50.Name = "item2zoom50";
			item2zoom50.Size = new Size(102, 22);
			item2zoom50.Tag = "50";
			item2zoom50.Text = "50%";
			item2zoom50.Click += new EventHandler(itemzoomto_Click);
			// 
			// item2zoom25
			// 
			item2zoom25.Name = "item2zoom25";
			item2zoom25.Size = new Size(102, 22);
			item2zoom25.Tag = "25";
			item2zoom25.Text = "25%";
			item2zoom25.Click += new EventHandler(itemzoomto_Click);
			// 
			// item2zoom10
			// 
			item2zoom10.Name = "item2zoom10";
			item2zoom10.Size = new Size(102, 22);
			item2zoom10.Tag = "10";
			item2zoom10.Text = "10%";
			item2zoom10.Click += new EventHandler(itemzoomto_Click);
			// 
			// item2zoom5
			// 
			item2zoom5.Name = "item2zoom5";
			item2zoom5.Size = new Size(102, 22);
			item2zoom5.Tag = "5";
			item2zoom5.Text = "5%";
			item2zoom5.Click += new EventHandler(itemzoomto_Click);
			// 
			// itemgotocoords
			// 
			itemgotocoords.Name = "itemgotocoords";
			itemgotocoords.Size = new Size(215, 22);
			itemgotocoords.Tag = "builder_centeroncoordinates";
			itemgotocoords.Text = "Go To Coordinates...";
			itemgotocoords.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemfittoscreen
			// 
			itemfittoscreen.Name = "itemfittoscreen";
			itemfittoscreen.Size = new Size(215, 22);
			itemfittoscreen.Tag = "builder_centerinscreen";
			itemfittoscreen.Text = "Fit to Screen";
			itemfittoscreen.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemtoggleinfo
			// 
			itemtoggleinfo.Name = "itemtoggleinfo";
			itemtoggleinfo.Size = new Size(215, 22);
			itemtoggleinfo.Tag = "builder_toggleinfopanel";
			itemtoggleinfo.Text = "&Expanded Info Panel";
			itemtoggleinfo.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatorviewzoom
			// 
			seperatorviewzoom.Margin = new Padding(0, 3, 0, 3);
			seperatorviewzoom.Name = "seperatorviewzoom";
			seperatorviewzoom.Size = new Size(212, 6);
			// 
			// itemscripteditor
			// 
			itemscripteditor.Image = global::CodeImp.DoomBuilder.Properties.Resources.Script2;
			itemscripteditor.Name = "itemscripteditor";
			itemscripteditor.Size = new Size(215, 22);
			itemscripteditor.Tag = "builder_openscripteditor";
			itemscripteditor.Text = "&Script Editor...";
			itemscripteditor.Click += new EventHandler(InvokeTaggedAction);
			// 
			// separatorDrawModes
			// 
			separatorDrawModes.Name = "separatorDrawModes";
			separatorDrawModes.Size = new Size(57, 6);
			// 
			// separatorTransformModes
			// 
			separatorTransformModes.Name = "separatorTransformModes";
			separatorTransformModes.Size = new Size(57, 6);

			// 
			// iteminsertprefabfile
			// 
			iteminsertprefabfile.Image = global::CodeImp.DoomBuilder.Properties.Resources.Prefab;
			iteminsertprefabfile.Name = "iteminsertprefabfile";
			iteminsertprefabfile.Size = new Size(199, 22);
			iteminsertprefabfile.Tag = "builder_insertprefabfile";
			iteminsertprefabfile.Text = "&Insert Prefab from File...";
			iteminsertprefabfile.Click += new EventHandler(InvokeTaggedAction);
			// 
			// iteminsertpreviousprefab
			// 
			iteminsertpreviousprefab.Image = global::CodeImp.DoomBuilder.Properties.Resources.Prefab2;
			iteminsertpreviousprefab.Name = "iteminsertpreviousprefab";
			iteminsertpreviousprefab.Size = new Size(199, 22);
			iteminsertpreviousprefab.Tag = "builder_insertpreviousprefab";
			iteminsertpreviousprefab.Text = "Insert &Previous Prefab";
			iteminsertpreviousprefab.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatorprefabsinsert
			// 
			seperatorprefabsinsert.Margin = new Padding(0, 3, 0, 3);
			seperatorprefabsinsert.Name = "seperatorprefabsinsert";
			seperatorprefabsinsert.Size = new Size(196, 6);
			// 
			// itemcreateprefab
			// 
			itemcreateprefab.Name = "itemcreateprefab";
			itemcreateprefab.Size = new Size(199, 22);
			itemcreateprefab.Tag = "builder_createprefab";
			itemcreateprefab.Text = "&Create From Selection...";
			itemcreateprefab.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemreloadresources
			// 
			itemreloadresources.Image = global::CodeImp.DoomBuilder.Properties.Resources.Reload;
			itemreloadresources.Name = "itemreloadresources";
			itemreloadresources.Size = new Size(246, 22);
			itemreloadresources.Tag = "builder_reloadresources";
			itemreloadresources.Text = "&Reload Resources";
			itemreloadresources.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemReloadModedef
			// 
			itemReloadModedef.Image = global::CodeImp.DoomBuilder.Properties.Resources.Reload;
			itemReloadModedef.Name = "itemReloadModedef";
			itemReloadModedef.Size = new Size(246, 22);
			itemReloadModedef.Tag = "builder_gzreloadmodeldef";
			itemReloadModedef.Text = "Reload MODELDEF/VOXELDEF";
			itemReloadModedef.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemReloadGldefs
			// 
			itemReloadGldefs.Image = global::CodeImp.DoomBuilder.Properties.Resources.Reload;
			itemReloadGldefs.Name = "itemReloadGldefs";
			itemReloadGldefs.Size = new Size(246, 22);
			itemReloadGldefs.Tag = "builder_gzreloadgldefs";
			itemReloadGldefs.Text = "Reload GLDEFS";
			itemReloadGldefs.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemshowerrors
			// 
			itemshowerrors.Image = global::CodeImp.DoomBuilder.Properties.Resources.Warning;
			itemshowerrors.Name = "itemshowerrors";
			itemshowerrors.Size = new Size(246, 22);
			itemshowerrors.Tag = "builder_showerrors";
			itemshowerrors.Text = "&Errors and Warnings...";
			itemshowerrors.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatortoolsresources
			// 
			seperatortoolsresources.Margin = new Padding(0, 3, 0, 3);
			seperatortoolsresources.Name = "seperatortoolsresources";
			seperatortoolsresources.Size = new Size(243, 6);
			// 
			// configurationToolStripMenuItem
			// 
			configurationToolStripMenuItem.Image = global::CodeImp.DoomBuilder.Properties.Resources.Configuration;
			configurationToolStripMenuItem.Name = "configurationToolStripMenuItem";
			configurationToolStripMenuItem.Size = new Size(246, 22);
			configurationToolStripMenuItem.Tag = "builder_configuration";
			configurationToolStripMenuItem.Text = "&Game Configurations...";
			configurationToolStripMenuItem.Click += new EventHandler(InvokeTaggedAction);
			// 
			// preferencesToolStripMenuItem
			// 
			preferencesToolStripMenuItem.Image = global::CodeImp.DoomBuilder.Properties.Resources.Preferences;
			preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
			preferencesToolStripMenuItem.Size = new Size(246, 22);
			preferencesToolStripMenuItem.Tag = "builder_preferences";
			preferencesToolStripMenuItem.Text = "Preferences...";
			preferencesToolStripMenuItem.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatortoolsconfig
			// 
			seperatortoolsconfig.Margin = new Padding(0, 3, 0, 3);
			seperatortoolsconfig.Name = "seperatortoolsconfig";
			seperatortoolsconfig.Size = new Size(243, 6);
			// 
			// itemsavescreenshot
			// 
			itemsavescreenshot.Image = global::CodeImp.DoomBuilder.Properties.Resources.Screenshot;
			itemsavescreenshot.Name = "itemsavescreenshot";
			itemsavescreenshot.Size = new Size(246, 22);
			itemsavescreenshot.Tag = "builder_savescreenshot";
			itemsavescreenshot.Text = "Save Screenshot";
			itemsavescreenshot.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemsaveeditareascreenshot
			// 
			itemsaveeditareascreenshot.Image = global::CodeImp.DoomBuilder.Properties.Resources.ScreenshotActiveWindow;
			itemsaveeditareascreenshot.Name = "itemsaveeditareascreenshot";
			itemsaveeditareascreenshot.Size = new Size(246, 22);
			itemsaveeditareascreenshot.Tag = "builder_saveeditareascreenshot";
			itemsaveeditareascreenshot.Text = "Save Screenshot (active window)";
			itemsaveeditareascreenshot.Click += new EventHandler(InvokeTaggedAction);
			// 
			// separatortoolsscreenshots
			// 
			separatortoolsscreenshots.Name = "separatortoolsscreenshots";
			separatortoolsscreenshots.Size = new Size(243, 6);
			// 
			// itemtestmap
			// 
			itemtestmap.Image = global::CodeImp.DoomBuilder.Properties.Resources.Test;
			itemtestmap.Name = "itemtestmap";
			itemtestmap.Size = new Size(246, 22);
			itemtestmap.Tag = "builder_testmap";
			itemtestmap.Text = "&Test Map";
			itemtestmap.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemhelprefmanual
			// 
			itemhelprefmanual.Image = global::CodeImp.DoomBuilder.Properties.Resources.Help;
			itemhelprefmanual.Name = "itemhelprefmanual";
			itemhelprefmanual.Size = new Size(232, 22);
			itemhelprefmanual.Text = "Reference &Manual";
			itemhelprefmanual.Click += new EventHandler(itemhelprefmanual_Click);
			// 
			// itemShortcutReference
			// 
			itemShortcutReference.Image = global::CodeImp.DoomBuilder.Properties.Resources.Keyboard;
			itemShortcutReference.Name = "itemShortcutReference";
			itemShortcutReference.Size = new Size(232, 22);
			itemShortcutReference.Tag = "";
			itemShortcutReference.Text = "Keyboard Shortcuts Reference";
			itemShortcutReference.Click += new EventHandler(itemShortcutReference_Click);
			// 
			// itemopenconfigfolder
			//
			itemopenconfigfolder.Image = global::CodeImp.DoomBuilder.Properties.Resources.FolderExplore;
			itemopenconfigfolder.Name = "itemopenconfigfolder";
			itemopenconfigfolder.Size = new Size(232, 22);
			itemopenconfigfolder.Tag = "";
			itemopenconfigfolder.Text = "Program Configuration Folder";
			itemopenconfigfolder.Click += new EventHandler(itemopenconfigfolder_Click);
			// 
			// itemhelpeditmode
			// 
			itemhelpeditmode.Image = global::CodeImp.DoomBuilder.Properties.Resources.Question;
			itemhelpeditmode.Name = "itemhelpeditmode";
			itemhelpeditmode.Size = new Size(232, 22);
			itemhelpeditmode.Text = "About this &Editing Mode";
			itemhelpeditmode.Click += new EventHandler(itemhelpeditmode_Click);
			// 
			// itemhelpcheckupdates
			// 
			itemhelpcheckupdates.Image = global::CodeImp.DoomBuilder.Properties.Resources.Update;
			itemhelpcheckupdates.Name = "itemhelpcheckupdates";
			itemhelpcheckupdates.Size = new Size(232, 22);
			itemhelpcheckupdates.Text = "&Check for updates...";
			itemhelpcheckupdates.Click += new EventHandler(itemhelpcheckupdates_Click);
			// 
			// seperatorhelpmanual
			// 
			seperatorhelpmanual.Margin = new Padding(0, 3, 0, 3);
			seperatorhelpmanual.Name = "seperatorhelpmanual";
			seperatorhelpmanual.Size = new Size(229, 6);
			// 
			// itemhelpissues
			// 
			itemhelpissues.Image = global::CodeImp.DoomBuilder.Properties.Resources.Github;
			itemhelpissues.Name = "itemhelpissues";
			itemhelpissues.Size = new Size(232, 22);
			itemhelpissues.Text = "&GitHub issues tracker";
			itemhelpissues.Click += new EventHandler(itemhelpissues_Click);
			// 
			// itemhelpabout
			// 
			itemhelpabout.Image = global::CodeImp.DoomBuilder.Properties.Resources.About;
			itemhelpabout.Name = "itemhelpabout";
			itemhelpabout.Size = new Size(232, 22);
			itemhelpabout.Text = "&About Ultimate Doom Builder...";
			itemhelpabout.Click += new EventHandler(itemhelpabout_Click);

			// 
			// toggleFile
			// 
			toggleFile.Name = "toggleFile";
			toggleFile.Size = new Size(226, 22);
			toggleFile.Text = "New / Open / Save";
			toggleFile.Click += new EventHandler(toggleFile_Click);
			// 
			// toggleScript
			// 
			toggleScript.Name = "toggleScript";
			toggleScript.Size = new Size(226, 22);
			toggleScript.Text = "Script Editor";
			toggleScript.Click += new EventHandler(toggleScript_Click);
			// 
			// toggleUndo
			// 
			toggleUndo.Name = "toggleUndo";
			toggleUndo.Size = new Size(226, 22);
			toggleUndo.Text = "Undo / Redo";
			toggleUndo.Click += new EventHandler(toggleUndo_Click);
			// 
			// toggleCopy
			// 
			toggleCopy.Name = "toggleCopy";
			toggleCopy.Size = new Size(226, 22);
			toggleCopy.Text = "Cut / Copy / Paste";
			toggleCopy.Click += new EventHandler(toggleCopy_Click);
			// 
			// togglePrefabs
			// 
			togglePrefabs.Name = "togglePrefabs";
			togglePrefabs.Size = new Size(226, 22);
			togglePrefabs.Text = "Prefabs";
			togglePrefabs.Click += new EventHandler(togglePrefabs_Click);
			// 
			// toggleFilter
			// 
			toggleFilter.Name = "toggleFilter";
			toggleFilter.Size = new Size(226, 22);
			toggleFilter.Text = "Things Filter / Linedef Colors";
			toggleFilter.Click += new EventHandler(toggleFilter_Click);
			// 
			// toggleViewModes
			// 
			toggleViewModes.Name = "toggleViewModes";
			toggleViewModes.Size = new Size(226, 22);
			toggleViewModes.Text = "View Modes";
			toggleViewModes.Click += new EventHandler(toggleViewModes_Click);
			// 
			// toggleGeometry
			// 
			toggleGeometry.Name = "toggleGeometry";
			toggleGeometry.Size = new Size(226, 22);
			toggleGeometry.Text = "Snap / Merge";
			toggleGeometry.Click += new EventHandler(toggleGeometry_Click);
			// 
			// toggleTesting
			// 
			toggleTesting.Name = "toggleTesting";
			toggleTesting.Size = new Size(226, 22);
			toggleTesting.Text = "Testing";
			toggleTesting.Click += new EventHandler(toggleTesting_Click);
			// 
			// toggleRendering
			// 
			toggleRendering.Name = "toggleRendering";
			toggleRendering.Size = new Size(226, 22);
			toggleRendering.Text = "Rendering";
			toggleRendering.Click += new EventHandler(toggleRendering_Click);
			// 
			// buttonnewmap
			// 
			buttonnewmap.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonnewmap.Image = global::CodeImp.DoomBuilder.Properties.Resources.NewMap;
			buttonnewmap.ImageTransparentColor = Color.Magenta;
			buttonnewmap.Margin = new Padding(6, 1, 0, 2);
			buttonnewmap.Name = "buttonnewmap";
			buttonnewmap.Size = new Size(23, 22);
			buttonnewmap.Tag = "builder_newmap";
			buttonnewmap.Text = "New Map";
			buttonnewmap.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonopenmap
			// 
			buttonopenmap.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonopenmap.Image = global::CodeImp.DoomBuilder.Properties.Resources.OpenMap;
			buttonopenmap.ImageTransparentColor = Color.Magenta;
			buttonopenmap.Name = "buttonopenmap";
			buttonopenmap.Size = new Size(23, 22);
			buttonopenmap.Tag = "builder_openmap";
			buttonopenmap.Text = "Open Map";
			buttonopenmap.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonsavemap
			// 
			buttonsavemap.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonsavemap.Image = global::CodeImp.DoomBuilder.Properties.Resources.SaveMap;
			buttonsavemap.ImageTransparentColor = Color.Magenta;
			buttonsavemap.Name = "buttonsavemap";
			buttonsavemap.Size = new Size(23, 22);
			buttonsavemap.Tag = "builder_savemap";
			buttonsavemap.Text = "Save Map";
			buttonsavemap.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonscripteditor
			// 
			buttonscripteditor.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonscripteditor.Image = global::CodeImp.DoomBuilder.Properties.Resources.Script2;
			buttonscripteditor.ImageTransparentColor = Color.Magenta;
			buttonscripteditor.Name = "buttonscripteditor";
			buttonscripteditor.Size = new Size(23, 22);
			buttonscripteditor.Tag = "builder_openscripteditor";
			buttonscripteditor.Text = "Open Script Editor";
			buttonscripteditor.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonundo
			// 
			buttonundo.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonundo.Image = global::CodeImp.DoomBuilder.Properties.Resources.Undo;
			buttonundo.ImageTransparentColor = Color.Magenta;
			buttonundo.Name = "buttonundo";
			buttonundo.Size = new Size(23, 22);
			buttonundo.Tag = "builder_undo";
			buttonundo.Text = "Undo";
			buttonundo.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonredo
			// 
			buttonredo.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonredo.Image = global::CodeImp.DoomBuilder.Properties.Resources.Redo;
			buttonredo.ImageTransparentColor = Color.Magenta;
			buttonredo.Name = "buttonredo";
			buttonredo.Size = new Size(23, 22);
			buttonredo.Tag = "builder_redo";
			buttonredo.Text = "Redo";
			buttonredo.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttoncut
			// 
			buttoncut.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttoncut.Image = global::CodeImp.DoomBuilder.Properties.Resources.Cut;
			buttoncut.ImageTransparentColor = Color.Magenta;
			buttoncut.Name = "buttoncut";
			buttoncut.Size = new Size(23, 22);
			buttoncut.Tag = "builder_cutselection";
			buttoncut.Text = "Cut Selection";
			buttoncut.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttoncopy
			// 
			buttoncopy.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttoncopy.Image = global::CodeImp.DoomBuilder.Properties.Resources.Copy;
			buttoncopy.ImageTransparentColor = Color.Magenta;
			buttoncopy.Name = "buttoncopy";
			buttoncopy.Size = new Size(23, 22);
			buttoncopy.Tag = "builder_copyselection";
			buttoncopy.Text = "Copy Selection";
			buttoncopy.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonpaste
			// 
			buttonpaste.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonpaste.Image = global::CodeImp.DoomBuilder.Properties.Resources.Paste;
			buttonpaste.ImageTransparentColor = Color.Magenta;
			buttonpaste.Name = "buttonpaste";
			buttonpaste.Size = new Size(23, 22);
			buttonpaste.Tag = "builder_pasteselection";
			buttonpaste.Text = "Paste Selection";
			buttonpaste.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttoninsertprefabfile
			// 
			buttoninsertprefabfile.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttoninsertprefabfile.Image = global::CodeImp.DoomBuilder.Properties.Resources.Prefab;
			buttoninsertprefabfile.ImageTransparentColor = Color.Magenta;
			buttoninsertprefabfile.Name = "buttoninsertprefabfile";
			buttoninsertprefabfile.Size = new Size(23, 22);
			buttoninsertprefabfile.Tag = "builder_insertprefabfile";
			buttoninsertprefabfile.Text = "Insert Prefab from File";
			buttoninsertprefabfile.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttoninsertpreviousprefab
			// 
			buttoninsertpreviousprefab.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttoninsertpreviousprefab.Image = global::CodeImp.DoomBuilder.Properties.Resources.Prefab2;
			buttoninsertpreviousprefab.ImageTransparentColor = Color.Magenta;
			buttoninsertpreviousprefab.Name = "buttoninsertpreviousprefab";
			buttoninsertpreviousprefab.Size = new Size(23, 22);
			buttoninsertpreviousprefab.Tag = "builder_insertpreviousprefab";
			buttoninsertpreviousprefab.Text = "Insert Previous Prefab";
			buttoninsertpreviousprefab.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonthingsfilter
			// 
			buttonthingsfilter.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonthingsfilter.Image = global::CodeImp.DoomBuilder.Properties.Resources.Filter;
			buttonthingsfilter.ImageTransparentColor = Color.Magenta;
			buttonthingsfilter.Name = "buttonthingsfilter";
			buttonthingsfilter.Size = new Size(23, 22);
			buttonthingsfilter.Tag = "builder_thingsfilterssetup";
			buttonthingsfilter.Text = "Configure Things Filters";
			buttonthingsfilter.Click += new EventHandler(InvokeTaggedAction);
			// 
			// thingfilters
			// 
			thingfilters.AutoSize = false;
			thingfilters.AutoToolTip = false;
			thingfilters.DisplayStyle = ToolStripItemDisplayStyle.Text;
			thingfilters.Image = ((Image)(resources.GetObject("thingfilters.Image")));
			thingfilters.ImageTransparentColor = Color.Magenta;
			thingfilters.Margin = new Padding(1, 1, 0, 2);
			thingfilters.Name = "thingfilters";
			thingfilters.Size = new Size(120, 22);
			thingfilters.Text = "(show all)";
			thingfilters.TextAlign = ContentAlignment.MiddleLeft;
			thingfilters.DropDownClosed += new EventHandler(LoseFocus);
			// 
			// separatorlinecolors
			// 
			separatorlinecolors.Margin = new Padding(6, 0, 6, 0);
			separatorlinecolors.Name = "separatorlinecolors";
			separatorlinecolors.Size = new Size(6, 25);
			// 
			// buttonlinededfcolors
			// 
			buttonlinededfcolors.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonlinededfcolors.Image = global::CodeImp.DoomBuilder.Properties.Resources.LinedefColorPresets;
			buttonlinededfcolors.ImageTransparentColor = Color.Magenta;
			buttonlinededfcolors.Name = "buttonlinededfcolors";
			buttonlinededfcolors.Size = new Size(23, 22);
			buttonlinededfcolors.Tag = "builder_linedefcolorssetup";
			buttonlinededfcolors.Text = "Configure Linedef Colors";
			buttonlinededfcolors.Click += new EventHandler(InvokeTaggedAction);
			// 
			// linedefcolorpresets
			// 
			linedefcolorpresets.AutoSize = false;
			linedefcolorpresets.AutoToolTip = false;
			linedefcolorpresets.DisplayStyle = ToolStripItemDisplayStyle.Text;
			linedefcolorpresets.ImageTransparentColor = Color.Magenta;
			linedefcolorpresets.Margin = new Padding(1, 1, 0, 2);
			linedefcolorpresets.Name = "linedefcolorpresets";
			linedefcolorpresets.Size = new Size(120, 22);
			linedefcolorpresets.Text = "No presets";
			linedefcolorpresets.TextAlign = ContentAlignment.MiddleLeft;
			linedefcolorpresets.DropDownItemClicked += new ToolStripItemClickedEventHandler(linedefcolorpresets_DropDownItemClicked);
			linedefcolorpresets.DropDownClosed += new EventHandler(LoseFocus);
			linedefcolorpresets.Click += new EventHandler(linedefcolorpresets_MouseLeave);
			// 
			// separatorfilters
			// 
			separatorfilters.Margin = new Padding(6, 0, 6, 0);
			separatorfilters.Name = "separatorfilters";
			separatorfilters.Size = new Size(6, 25);
			// 
			// separatorrendering
			// 
			separatorrendering.Name = "separatorrendering";
			separatorrendering.Size = new Size(270, 6);
			// 
			// itemnodynlights
			// 
			itemnodynlights.CheckOnClick = true;
			itemnodynlights.Image = global::CodeImp.DoomBuilder.Properties.Resources.LightDisabled;
			itemnodynlights.Name = "itemnodynlights";
			itemnodynlights.Size = new Size(237, 22);
			itemnodynlights.Tag = 0;
			itemnodynlights.Text = "Don\'t show dynamic lights";
			itemnodynlights.Click += new EventHandler(ChangeLightRenderingMode);
			// 
			// itemdynlights
			// 
			itemdynlights.CheckOnClick = true;
			itemdynlights.Image = global::CodeImp.DoomBuilder.Properties.Resources.Light;
			itemdynlights.Name = "itemdynlights";
			itemdynlights.Size = new Size(237, 22);
			itemdynlights.Tag = 1;
			itemdynlights.Text = "Show dynamic lights";
			itemdynlights.Click += new EventHandler(ChangeLightRenderingMode);
			// 
			// itemdynlightsanim
			// 
			itemdynlightsanim.CheckOnClick = true;
			itemdynlightsanim.Image = global::CodeImp.DoomBuilder.Properties.Resources.Light_animate;
			itemdynlightsanim.Name = "itemdynlightsanim";
			itemdynlightsanim.Size = new Size(237, 22);
			itemdynlightsanim.Tag = 2;
			itemdynlightsanim.Text = "Show animated dynamic lights";
			itemdynlightsanim.Click += new EventHandler(ChangeLightRenderingMode);
			// 
			// itemnomdl
			// 
			itemnomdl.CheckOnClick = true;
			itemnomdl.Image = global::CodeImp.DoomBuilder.Properties.Resources.ModelDisabled;
			itemnomdl.Name = "itemnomdl";
			itemnomdl.Size = new Size(298, 22);
			itemnomdl.Tag = 0;
			itemnomdl.Text = "Don\'t show models";
			itemnomdl.Click += new EventHandler(ChangeModelRenderingMode);
			// 
			// itemselmdl
			// 
			itemselmdl.CheckOnClick = true;
			itemselmdl.Image = global::CodeImp.DoomBuilder.Properties.Resources.Model_selected;
			itemselmdl.Name = "itemselmdl";
			itemselmdl.Size = new Size(298, 22);
			itemselmdl.Tag = 1;
			itemselmdl.Text = "Show models for selected Things only";
			itemselmdl.Click += new EventHandler(ChangeModelRenderingMode);
			// 
			// itemfiltermdl
			// 
			itemfiltermdl.CheckOnClick = true;
			itemfiltermdl.Image = global::CodeImp.DoomBuilder.Properties.Resources.ModelFiltered;
			itemfiltermdl.Name = "itemfiltermdl";
			itemfiltermdl.Size = new Size(298, 22);
			itemfiltermdl.Tag = 2;
			itemfiltermdl.Text = "Show models for current Things Filter only";
			itemfiltermdl.Click += new EventHandler(ChangeModelRenderingMode);
			// 
			// itemallmdl
			// 
			itemallmdl.CheckOnClick = true;
			itemallmdl.Image = global::CodeImp.DoomBuilder.Properties.Resources.Model;
			itemallmdl.Name = "itemallmdl";
			itemallmdl.Size = new Size(298, 22);
			itemallmdl.Tag = 3;
			itemallmdl.Text = "Always show models";
			itemallmdl.Click += new EventHandler(ChangeModelRenderingMode);
			// 
			// itemfog
			// 
			itemtogglefog.CheckOnClick = true;
			itemtogglefog.Image = global::CodeImp.DoomBuilder.Properties.Resources.fog;
			itemtogglefog.Name = "itemtogglefog";
			itemtogglefog.Size = new Size(273, 22);
			itemtogglefog.Tag = "builder_gztogglefog";
			itemtogglefog.Text = "Render fog (Visual mode)";
			itemtogglefog.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemsky
			// 
			itemtogglesky.CheckOnClick = true;
			itemtogglesky.Image = global::CodeImp.DoomBuilder.Properties.Resources.Sky;
			itemtogglesky.Name = "itemtogglesky";
			itemtogglesky.Size = new Size(273, 22);
			itemtogglesky.Tag = "builder_gztogglesky";
			itemtogglesky.Text = "Render sky (Visual mode)";
			itemtogglesky.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemeventlines
			// 
			itemtoggleeventlines.CheckOnClick = true;
			itemtoggleeventlines.Image = global::CodeImp.DoomBuilder.Properties.Resources.InfoLine;
			itemtoggleeventlines.Name = "itemtoggleeventlines";
			itemtoggleeventlines.Size = new Size(273, 22);
			itemtoggleeventlines.Tag = "builder_gztoggleeventlines";
			itemtoggleeventlines.Text = "Show Event Lines";
			itemtoggleeventlines.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemvisualverts
			// 
			itemtogglevisualverts.CheckOnClick = true;
			itemtogglevisualverts.Image = global::CodeImp.DoomBuilder.Properties.Resources.VisualVertices;
			itemtogglevisualverts.Name = "itemtogglevisualverts";
			itemtogglevisualverts.Size = new Size(273, 22);
			itemtogglevisualverts.Tag = "builder_gztogglevisualvertices";
			itemtogglevisualverts.Text = "Show Editable Vertices (Visual mode)";
			itemtogglevisualverts.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonfullbrightness
			// 
			buttonfullbrightness.Checked = true;
			buttonfullbrightness.CheckOnClick = true;
			buttonfullbrightness.CheckState = CheckState.Checked;
			buttonfullbrightness.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonfullbrightness.Image = global::CodeImp.DoomBuilder.Properties.Resources.Brightness;
			buttonfullbrightness.ImageTransparentColor = Color.Magenta;
			buttonfullbrightness.Name = "buttonfullbrightness";
			buttonfullbrightness.Size = new Size(23, 22);
			buttonfullbrightness.Tag = "builder_togglebrightness";
			buttonfullbrightness.Text = "Full Brightness";
			buttonfullbrightness.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttontogglegrid
			// 
			buttontogglegrid.Checked = true;
			buttontogglegrid.CheckOnClick = true;
			buttontogglegrid.CheckState = CheckState.Checked;
			buttontogglegrid.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttontogglegrid.Image = global::CodeImp.DoomBuilder.Properties.Resources.Grid2;
			buttontogglegrid.ImageTransparentColor = Color.Magenta;
			buttontogglegrid.Name = "buttontogglegrid";
			buttontogglegrid.Size = new Size(23, 22);
			buttontogglegrid.Tag = "builder_togglegrid";
			buttontogglegrid.Text = "Render Grid";
			buttontogglegrid.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttontoggledynamicgrid
			// 
			buttontoggledynamicgrid.Checked = true;
			buttontoggledynamicgrid.CheckOnClick = true;
			buttontoggledynamicgrid.CheckState = CheckState.Checked;
			buttontoggledynamicgrid.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttontoggledynamicgrid.Image = global::CodeImp.DoomBuilder.Properties.Resources.GridDynamic;
			buttontoggledynamicgrid.ImageTransparentColor = Color.Magenta;
			buttontoggledynamicgrid.Name = "buttontoggledynamicgrid";
			buttontoggledynamicgrid.Size = new Size(23, 22);
			buttontoggledynamicgrid.Tag = "builder_toggledynamicgrid";
			buttontoggledynamicgrid.Text = "Dynamic Grid Size";
			buttontoggledynamicgrid.Click += new EventHandler(InvokeTaggedAction);
			// 
			// separatorfullbrightness
			// 
			separatorfullbrightness.Margin = new Padding(6, 0, 6, 0);
			separatorfullbrightness.Name = "separatorfullbrightness";
			separatorfullbrightness.Size = new Size(6, 25);
			// 
			// buttonviewnormal
			// 
			buttonviewnormal.CheckOnClick = true;
			buttonviewnormal.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonviewnormal.Image = global::CodeImp.DoomBuilder.Properties.Resources.ViewNormal;
			buttonviewnormal.ImageTransparentColor = Color.Magenta;
			buttonviewnormal.Name = "buttonviewnormal";
			buttonviewnormal.Size = new Size(23, 22);
			buttonviewnormal.Tag = "builder_viewmodenormal";
			buttonviewnormal.Text = "View Wireframe";
			buttonviewnormal.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonviewbrightness
			// 
			buttonviewbrightness.CheckOnClick = true;
			buttonviewbrightness.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonviewbrightness.Image = global::CodeImp.DoomBuilder.Properties.Resources.ViewBrightness;
			buttonviewbrightness.ImageTransparentColor = Color.Magenta;
			buttonviewbrightness.Name = "buttonviewbrightness";
			buttonviewbrightness.Size = new Size(23, 22);
			buttonviewbrightness.Tag = "builder_viewmodebrightness";
			buttonviewbrightness.Text = "View Brightness Levels";
			buttonviewbrightness.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonviewfloors
			// 
			buttonviewfloors.CheckOnClick = true;
			buttonviewfloors.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonviewfloors.Image = global::CodeImp.DoomBuilder.Properties.Resources.ViewTextureFloor;
			buttonviewfloors.ImageTransparentColor = Color.Magenta;
			buttonviewfloors.Name = "buttonviewfloors";
			buttonviewfloors.Size = new Size(23, 22);
			buttonviewfloors.Tag = "builder_viewmodefloors";
			buttonviewfloors.Text = "View Floor Textures";
			buttonviewfloors.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonviewceilings
			// 
			buttonviewceilings.CheckOnClick = true;
			buttonviewceilings.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonviewceilings.Image = global::CodeImp.DoomBuilder.Properties.Resources.ViewTextureCeiling;
			buttonviewceilings.ImageTransparentColor = Color.Magenta;
			buttonviewceilings.Name = "buttonviewceilings";
			buttonviewceilings.Size = new Size(23, 22);
			buttonviewceilings.Tag = "builder_viewmodeceilings";
			buttonviewceilings.Text = "View Ceiling Textures";
			buttonviewceilings.Click += new EventHandler(InvokeTaggedAction);
			// 
			// separatorgeomergemodes
			// 
			separatorgeomergemodes.Margin = new Padding(6, 0, 6, 0);
			separatorgeomergemodes.Name = "separatorgeomergemodes";
			separatorgeomergemodes.Size = new Size(6, 25);
			// 
			// buttonmergegeoclassic
			// 
			buttonmergegeoclassic.CheckOnClick = true;
			buttonmergegeoclassic.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonmergegeoclassic.Image = global::CodeImp.DoomBuilder.Properties.Resources.MergeGeoClassic;
			buttonmergegeoclassic.ImageTransparentColor = Color.Magenta;
			buttonmergegeoclassic.Name = "buttonmergegeoclassic";
			buttonmergegeoclassic.Size = new Size(23, 22);
			buttonmergegeoclassic.Tag = "builder_geomergeclassic";
			buttonmergegeoclassic.Text = "Merge Dragged Vertices Only";
			buttonmergegeoclassic.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonmergegeoclassic
			// 
			buttonmergegeo.CheckOnClick = true;
			buttonmergegeo.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonmergegeo.Image = global::CodeImp.DoomBuilder.Properties.Resources.MergeGeo;
			buttonmergegeo.ImageTransparentColor = Color.Magenta;
			buttonmergegeo.Name = "buttonmergegeo";
			buttonmergegeo.Size = new Size(23, 22);
			buttonmergegeo.Tag = "builder_geomerge";
			buttonmergegeo.Text = "Merge Dragged Geometry";
			buttonmergegeo.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonmergegeoclassic
			// 
			buttonplacegeo.CheckOnClick = true;
			buttonplacegeo.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonplacegeo.Image = global::CodeImp.DoomBuilder.Properties.Resources.MergeGeoRemoveLines;
			buttonplacegeo.ImageTransparentColor = Color.Magenta;
			buttonplacegeo.Name = "buttonmergegeoclassic";
			buttonplacegeo.Size = new Size(23, 22);
			buttonplacegeo.Tag = "builder_georeplace";
			buttonplacegeo.Text = "Replace with Dragged Geometry";
			buttonplacegeo.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatorviews
			// 
			seperatorviews.Margin = new Padding(6, 0, 6, 0);
			seperatorviews.Name = "seperatorviews";
			seperatorviews.Size = new Size(6, 25);
			// 
			// buttontogglecomments
			// 
			buttontogglecomments.Checked = true;
			buttontogglecomments.CheckState = CheckState.Checked;
			buttontogglecomments.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttontogglecomments.Image = global::CodeImp.DoomBuilder.Properties.Resources.Comment;
			buttontogglecomments.ImageTransparentColor = Color.Magenta;
			buttontogglecomments.Name = "buttontogglecomments";
			buttontogglecomments.Size = new Size(23, 22);
			buttontogglecomments.Tag = "builder_togglecomments";
			buttontogglecomments.Text = "Show Comments";
			buttontogglecomments.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttontogglefixedthingsscale
			// 
			buttontogglefixedthingsscale.Checked = true;
			buttontogglefixedthingsscale.CheckState = CheckState.Checked;
			buttontogglefixedthingsscale.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttontogglefixedthingsscale.Image = global::CodeImp.DoomBuilder.Properties.Resources.FixedThingsScale;
			buttontogglefixedthingsscale.ImageTransparentColor = Color.Magenta;
			buttontogglefixedthingsscale.Name = "buttontogglefixedthingsscale";
			buttontogglefixedthingsscale.Size = new Size(23, 22);
			buttontogglefixedthingsscale.Tag = "builder_togglefixedthingsscale";
			buttontogglefixedthingsscale.Text = "Fixed Things Scale";
			buttontogglefixedthingsscale.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonsnaptogrid
			// 
			buttonsnaptogrid.Checked = true;
			buttonsnaptogrid.CheckState = CheckState.Checked;
			buttonsnaptogrid.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonsnaptogrid.Image = global::CodeImp.DoomBuilder.Properties.Resources.Grid4;
			buttonsnaptogrid.ImageTransparentColor = Color.Magenta;
			buttonsnaptogrid.Name = "buttonsnaptogrid";
			buttonsnaptogrid.Size = new Size(23, 22);
			buttonsnaptogrid.Tag = "builder_togglesnap";
			buttonsnaptogrid.Text = "Snap to Grid";
			buttonsnaptogrid.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonautomerge
			// 
			buttonautomerge.Checked = true;
			buttonautomerge.CheckState = CheckState.Checked;
			buttonautomerge.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonautomerge.Image = global::CodeImp.DoomBuilder.Properties.Resources.mergegeometry2;
			buttonautomerge.ImageTransparentColor = Color.Magenta;
			buttonautomerge.Name = "buttonautomerge";
			buttonautomerge.Size = new Size(23, 22);
			buttonautomerge.Tag = "builder_toggleautomerge";
			buttonautomerge.Text = "Snap to Geometry";
			buttonautomerge.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonsplitjoinedsectors
			// 
			buttonsplitjoinedsectors.Checked = true;
			buttonsplitjoinedsectors.CheckState = CheckState.Checked;
			buttonsplitjoinedsectors.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonsplitjoinedsectors.Image = global::CodeImp.DoomBuilder.Properties.Resources.SplitSectors;
			buttonsplitjoinedsectors.ImageTransparentColor = Color.Magenta;
			buttonsplitjoinedsectors.Name = "buttonsplitjoinedsectors";
			buttonsplitjoinedsectors.Size = new Size(23, 22);
			buttonsplitjoinedsectors.Tag = "builder_togglejoinedsectorssplitting";
			buttonsplitjoinedsectors.Text = "Split Joined Sectors when Drawing Lines";
			buttonsplitjoinedsectors.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttonautoclearsidetextures
			// 
			buttonautoclearsidetextures.Checked = true;
			buttonautoclearsidetextures.CheckState = CheckState.Checked;
			buttonautoclearsidetextures.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttonautoclearsidetextures.Image = global::CodeImp.DoomBuilder.Properties.Resources.ClearTextures;
			buttonautoclearsidetextures.ImageTransparentColor = Color.Magenta;
			buttonautoclearsidetextures.Name = "buttonautoclearsidetextures";
			buttonautoclearsidetextures.Size = new Size(23, 22);
			buttonautoclearsidetextures.Tag = "builder_toggleautoclearsidetextures";
			buttonautoclearsidetextures.Text = "Auto Clear Sidedef Textures";
			buttonautoclearsidetextures.Click += new EventHandler(InvokeTaggedAction);
			// 
			// seperatorgeometry
			// 
			seperatorgeometry.Margin = new Padding(6, 0, 6, 0);
			seperatorgeometry.Name = "seperatorgeometry";
			seperatorgeometry.Size = new Size(6, 25);
			// 
			// sightsdontshow
			// 
			sightsdontshow.CheckOnClick = true;
			sightsdontshow.Image = global::CodeImp.DoomBuilder.Properties.Resources.LightDisabled;
			sightsdontshow.Name = "sightsdontshow";
			sightsdontshow.Size = new Size(237, 22);
			sightsdontshow.Tag = 0;
			sightsdontshow.Text = "Don\'t show dynamic lights";
			sightsdontshow.Click += new EventHandler(ChangeLightRenderingMode);
			// 
			// lightsshow
			// 
			lightsshow.CheckOnClick = true;
			lightsshow.Image = global::CodeImp.DoomBuilder.Properties.Resources.Light;
			lightsshow.Name = "lightsshow";
			lightsshow.Size = new Size(237, 22);
			lightsshow.Tag = 1;
			lightsshow.Text = "Show dynamic lights";
			lightsshow.Click += new EventHandler(ChangeLightRenderingMode);
			// 
			// lightsshowanimated
			// 
			lightsshowanimated.CheckOnClick = true;
			lightsshowanimated.Image = global::CodeImp.DoomBuilder.Properties.Resources.Light_animate;
			lightsshowanimated.Name = "lightsshowanimated";
			lightsshowanimated.Size = new Size(237, 22);
			lightsshowanimated.Tag = 2;
			lightsshowanimated.Text = "Show animated dynamic lights";
			lightsshowanimated.Click += new EventHandler(ChangeLightRenderingMode);
			// 
			// modelsdontshow
			// 
			modelsdontshow.CheckOnClick = true;
			modelsdontshow.Image = global::CodeImp.DoomBuilder.Properties.Resources.ModelDisabled;
			modelsdontshow.Name = "modelsdontshow";
			modelsdontshow.Size = new Size(293, 22);
			modelsdontshow.Tag = 0;
			modelsdontshow.Text = "Don\'t show models";
			modelsdontshow.Click += new EventHandler(ChangeModelRenderingMode);
			// 
			// modelsshowselection
			// 
			modelsshowselection.CheckOnClick = true;
			modelsshowselection.Image = global::CodeImp.DoomBuilder.Properties.Resources.Model_selected;
			modelsshowselection.Name = "modelsshowselection";
			modelsshowselection.Size = new Size(293, 22);
			modelsshowselection.Tag = 1;
			modelsshowselection.Text = "Show models for selected things only";
			modelsshowselection.Click += new EventHandler(ChangeModelRenderingMode);
			// 
			// modelsshowfiltered
			// 
			modelsshowfiltered.CheckOnClick = true;
			modelsshowfiltered.Image = global::CodeImp.DoomBuilder.Properties.Resources.ModelFiltered;
			modelsshowfiltered.Name = "modelsshowfiltered";
			modelsshowfiltered.Size = new Size(293, 22);
			modelsshowfiltered.Tag = 2;
			modelsshowfiltered.Text = "Show models for current things filter only";
			modelsshowfiltered.Click += new EventHandler(ChangeModelRenderingMode);
			// 
			// modelsshowall
			// 
			modelsshowall.CheckOnClick = true;
			modelsshowall.Image = global::CodeImp.DoomBuilder.Properties.Resources.Model;
			modelsshowall.Name = "modelsshowall";
			modelsshowall.Size = new Size(293, 22);
			modelsshowall.Tag = 3;
			modelsshowall.Text = "Always show models";
			modelsshowall.Click += new EventHandler(ChangeModelRenderingMode);
			// 
			// buttontogglefog
			// 
			buttontogglefog.CheckOnClick = true;
			buttontogglefog.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttontogglefog.Image = global::CodeImp.DoomBuilder.Properties.Resources.fog;
			buttontogglefog.ImageTransparentColor = Color.Magenta;
			buttontogglefog.Name = "buttontogglefog";
			buttontogglefog.Size = new Size(23, 20);
			buttontogglefog.Tag = "builder_gztogglefog";
			buttontogglefog.Text = "Render Fog (Visual mode)";
			buttontogglefog.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttontogglesky
			// 
			buttontogglesky.CheckOnClick = true;
			buttontogglesky.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttontogglesky.Image = global::CodeImp.DoomBuilder.Properties.Resources.Sky;
			buttontogglesky.ImageTransparentColor = Color.Magenta;
			buttontogglesky.Name = "buttontogglesky";
			buttontogglesky.Size = new Size(23, 20);
			buttontogglesky.Tag = "builder_gztogglesky";
			buttontogglesky.Text = "Render Sky (Visual mode)";
			buttontogglesky.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttontoggleeventlines
			// 
			buttontoggleeventlines.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttontoggleeventlines.Image = global::CodeImp.DoomBuilder.Properties.Resources.InfoLine;
			buttontoggleeventlines.ImageTransparentColor = Color.Magenta;
			buttontoggleeventlines.Name = "buttontoggleeventlines";
			buttontoggleeventlines.Size = new Size(23, 20);
			buttontoggleeventlines.Tag = "builder_gztoggleeventlines";
			buttontoggleeventlines.Text = "Show Event Lines";
			buttontoggleeventlines.Click += new EventHandler(InvokeTaggedAction);
			// 
			// buttontogglevisualvertices
			// 
			buttontogglevisualvertices.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttontogglevisualvertices.Image = global::CodeImp.DoomBuilder.Properties.Resources.VisualVertices;
			buttontogglevisualvertices.ImageTransparentColor = Color.Magenta;
			buttontogglevisualvertices.Name = "buttontogglevisualvertices";
			buttontogglevisualvertices.Size = new Size(23, 20);
			buttontogglevisualvertices.Tag = "builder_gztogglevisualvertices";
			buttontogglevisualvertices.Text = "Show Editable Vertices (Visual mode)";
			buttontogglevisualvertices.Click += new EventHandler(InvokeTaggedAction);
			// 
			// separatorgzmodes
			// 
			separatorgzmodes.Margin = new Padding(6, 0, 6, 0);
			separatorgzmodes.Name = "separatorgzmodes";
			separatorgzmodes.Size = new Size(6, 25);
			// 
			// buttontest
			// 
			buttontest.DisplayStyle = ToolStripItemDisplayStyle.Image;
			buttontest.Image = global::CodeImp.DoomBuilder.Properties.Resources.Test;
			buttontest.ImageTransparentColor = Color.Magenta;
			buttontest.Name = "buttontest";
			buttontest.Size = new Size(32, 20);
			buttontest.Tag = "builder_testmap";
			buttontest.Text = "Test Map";
			buttontest.ButtonClick += new EventHandler(InvokeTaggedAction);
			// 
			// seperatortesting
			// 
			seperatortesting.Margin = new Padding(6, 0, 6, 0);
			seperatortesting.Name = "seperatortesting";
			seperatortesting.Size = new Size(6, 25);
			// 
			// statuslabel
			// 
			statuslabel.Image = global::CodeImp.DoomBuilder.Properties.Resources.Status2;
			statuslabel.ImageAlign = ContentAlignment.MiddleLeft;
			statuslabel.Margin = new Padding(2, 3, 0, 2);
			statuslabel.Name = "statuslabel";
			statuslabel.Size = new Size(340, 18);
			statuslabel.Spring = true;
			statuslabel.Text = "Initializing user interface...";
			statuslabel.TextAlign = ContentAlignment.MiddleLeft;
			// 
			// configlabel
			// 
			configlabel.AutoSize = false;
			configlabel.Font = new Font("Verdana", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			configlabel.Name = "configlabel";
			configlabel.Size = new Size(280, 18);
			configlabel.Text = "ZDoom (Doom in Hexen Format)";
			configlabel.TextAlign = ContentAlignment.MiddleRight;
			configlabel.ToolTipText = "Current Game Configuration";
			// 
			// gridlabel
			// 
			gridlabel.AutoSize = false;
			gridlabel.AutoToolTip = true;
			gridlabel.Name = "gridlabel";
			gridlabel.Size = new Size(64, 18);
			gridlabel.Text = "32 mp";
			gridlabel.TextAlign = ContentAlignment.MiddleRight;
			gridlabel.TextImageRelation = TextImageRelation.Overlay;
			gridlabel.ToolTipText = "Grid size";
			// 
			// itemgrid1024
			// 
			itemgrid1024.Name = "itemgrid1024";
			itemgrid1024.Size = new Size(153, 22);
			itemgrid1024.Tag = "1024";
			itemgrid1024.Text = "1024 mp";
			itemgrid1024.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid512
			// 
			itemgrid512.Name = "itemgrid512";
			itemgrid512.Size = new Size(153, 22);
			itemgrid512.Tag = "512";
			itemgrid512.Text = "512 mp";
			itemgrid512.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid256
			// 
			itemgrid256.Name = "itemgrid256";
			itemgrid256.Size = new Size(153, 22);
			itemgrid256.Tag = "256";
			itemgrid256.Text = "256 mp";
			itemgrid256.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid128
			// 
			itemgrid128.Name = "itemgrid128";
			itemgrid128.Size = new Size(153, 22);
			itemgrid128.Tag = "128";
			itemgrid128.Text = "128 mp";
			itemgrid128.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid64
			// 
			itemgrid64.Name = "itemgrid64";
			itemgrid64.Size = new Size(153, 22);
			itemgrid64.Tag = "64";
			itemgrid64.Text = "64 mp";
			itemgrid64.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid32
			// 
			itemgrid32.Name = "itemgrid32";
			itemgrid32.Size = new Size(153, 22);
			itemgrid32.Tag = "32";
			itemgrid32.Text = "32 mp";
			itemgrid32.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid16
			// 
			itemgrid16.Name = "itemgrid16";
			itemgrid16.Size = new Size(153, 22);
			itemgrid16.Tag = "16";
			itemgrid16.Text = "16 mp";
			itemgrid16.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid8
			// 
			itemgrid8.Name = "itemgrid8";
			itemgrid8.Size = new Size(153, 22);
			itemgrid8.Tag = "8";
			itemgrid8.Text = "8 mp";
			itemgrid8.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid4
			// 
			itemgrid4.Name = "itemgrid4";
			itemgrid4.Size = new Size(153, 22);
			itemgrid4.Tag = "4";
			itemgrid4.Text = "4 mp";
			itemgrid4.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid1
			// 
			itemgrid1.Name = "itemgrid1";
			itemgrid1.Size = new Size(153, 22);
			itemgrid1.Tag = "1";
			itemgrid1.Text = "1 mp";
			itemgrid1.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid05
			// 
			itemgrid05.Name = "itemgrid05";
			itemgrid05.Size = new Size(153, 22);
			itemgrid05.Tag = "0.5";
			itemgrid05.Text = "0.5 mp";
			itemgrid05.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid025
			// 
			itemgrid025.Name = "itemgrid025";
			itemgrid025.Size = new Size(153, 22);
			itemgrid025.Tag = "0.25";
			itemgrid025.Text = "0.25 mp";
			itemgrid025.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgrid0125
			// 
			itemgrid0125.Name = "itemgrid0125";
			itemgrid0125.Size = new Size(153, 22);
			itemgrid0125.Tag = "0.125";
			itemgrid0125.Text = "0.125 mp";
			itemgrid0125.Click += new EventHandler(itemgridsize_Click);
			// 
			// itemgridcustom
			// 
			itemgridcustom.Name = "itemgridcustom";
			itemgridcustom.Size = new Size(153, 22);
			itemgridcustom.Text = "Customize...";
			itemgridcustom.Click += new EventHandler(itemgridcustom_Click);
			// 
			// zoomlabel
			// 
			zoomlabel.AutoSize = false;
			zoomlabel.AutoToolTip = true;
			zoomlabel.Name = "zoomlabel";
			zoomlabel.Size = new Size(54, 18);
			zoomlabel.Text = "50%";
			zoomlabel.TextAlign = ContentAlignment.MiddleRight;
			zoomlabel.TextImageRelation = TextImageRelation.Overlay;
			zoomlabel.ToolTipText = "Zoom level";
			// 
			// itemzoom800
			// 
			itemzoom800.Name = "itemzoom800";
			itemzoom800.Size = new Size(156, 22);
			itemzoom800.Tag = "800";
			itemzoom800.Text = "800%";
			itemzoom800.Click += new EventHandler(itemzoomto_Click);
			// 
			// itemzoom400
			// 
			itemzoom400.Name = "itemzoom400";
			itemzoom400.Size = new Size(156, 22);
			itemzoom400.Tag = "400";
			itemzoom400.Text = "400%";
			itemzoom400.Click += new EventHandler(itemzoomto_Click);
			// 
			// itemzoom200
			// 
			itemzoom200.Name = "itemzoom200";
			itemzoom200.Size = new Size(156, 22);
			itemzoom200.Tag = "200";
			itemzoom200.Text = "200%";
			itemzoom200.Click += new EventHandler(itemzoomto_Click);
			// 
			// itemzoom100
			// 
			itemzoom100.Name = "itemzoom100";
			itemzoom100.Size = new Size(156, 22);
			itemzoom100.Tag = "100";
			itemzoom100.Text = "100%";
			itemzoom100.Click += new EventHandler(itemzoomto_Click);
			// 
			// itemzoom50
			// 
			itemzoom50.Name = "itemzoom50";
			itemzoom50.Size = new Size(156, 22);
			itemzoom50.Tag = "50";
			itemzoom50.Text = "50%";
			itemzoom50.Click += new EventHandler(itemzoomto_Click);
			// 
			// itemzoom25
			// 
			itemzoom25.Name = "itemzoom25";
			itemzoom25.Size = new Size(156, 22);
			itemzoom25.Tag = "25";
			itemzoom25.Text = "25%";
			itemzoom25.Click += new EventHandler(itemzoomto_Click);
			// 
			// itemzoom10
			// 
			itemzoom10.Name = "itemzoom10";
			itemzoom10.Size = new Size(156, 22);
			itemzoom10.Tag = "10";
			itemzoom10.Text = "10%";
			itemzoom10.Click += new EventHandler(itemzoomto_Click);
			// 
			// itemzoom5
			// 
			itemzoom5.Name = "itemzoom5";
			itemzoom5.Size = new Size(156, 22);
			itemzoom5.Tag = "5";
			itemzoom5.Text = "5%";
			itemzoom5.Click += new EventHandler(itemzoomto_Click);
			// 
			// itemzoomfittoscreen
			// 
			itemzoomfittoscreen.Name = "itemzoomfittoscreen";
			itemzoomfittoscreen.Size = new Size(156, 22);
			itemzoomfittoscreen.Text = "Fit to screen";
			itemzoomfittoscreen.Click += new EventHandler(itemzoomfittoscreen_Click);
			// 
			// xposlabel
			// 
			xposlabel.AutoSize = false;
			xposlabel.Name = "xposlabel";
			xposlabel.Size = new Size(50, 18);
			xposlabel.Tag = "builder_centeroncoordinates";
			xposlabel.Text = "0";
			xposlabel.TextAlign = ContentAlignment.MiddleRight;
			xposlabel.ToolTipText = "Current X, Y coordinates on map.\r\nClick to set specific coordinates.";
			xposlabel.Click += new EventHandler(InvokeTaggedAction);
			// 
			// yposlabel
			// 
			yposlabel.AutoSize = false;
			yposlabel.Name = "yposlabel";
			yposlabel.Size = new Size(50, 18);
			yposlabel.Tag = "builder_centeroncoordinates";
			yposlabel.Text = "0";
			yposlabel.TextAlign = ContentAlignment.MiddleLeft;
			yposlabel.ToolTipText = "Current X, Y coordinates on map.\r\nClick to set specific coordinates.";
			yposlabel.Click += new EventHandler(InvokeTaggedAction);
			// 
			// warnsLabel
			// 
			warnsLabel.AutoSize = false;
			warnsLabel.Font = new Font("Verdana", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			warnsLabel.Image = global::CodeImp.DoomBuilder.Properties.Resources.WarningOff;
			warnsLabel.ImageAlign = ContentAlignment.MiddleRight;
			warnsLabel.Name = "warnsLabel";
			warnsLabel.Size = new Size(44, 18);
			warnsLabel.Text = "0";
			warnsLabel.TextAlign = ContentAlignment.MiddleRight;
			warnsLabel.TextImageRelation = TextImageRelation.TextBeforeImage;
			warnsLabel.Click += new EventHandler(warnsLabel_Click);

			// 
			// statistics
			// 
			statistics.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
			statistics.ForeColor = SystemColors.GrayText;
			statistics.Location = new Point(849, 2);
			statistics.Name = "statistics";
			statistics.Size = new Size(138, 102);
			statistics.TabIndex = 9;
			statistics.Visible = false;
			// 
			// heightpanel1
			// 
			heightpanel1.BackColor = Color.Navy;
			heightpanel1.ForeColor = SystemColors.ControlText;
			heightpanel1.Location = new Point(0, 0);
			heightpanel1.Name = "heightpanel1";
			heightpanel1.Size = new Size(29, 106);
			heightpanel1.TabIndex = 7;
			heightpanel1.Visible = false;
			// 
			// labelcollapsedinfo
			// 
			labelcollapsedinfo.AutoSize = true;
			labelcollapsedinfo.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
			labelcollapsedinfo.Location = new Point(2, 2);
			labelcollapsedinfo.Name = "labelcollapsedinfo";
			labelcollapsedinfo.Size = new Size(155, 13);
			labelcollapsedinfo.TabIndex = 6;
			labelcollapsedinfo.Text = "Collapsed Descriptions";
			labelcollapsedinfo.Visible = false;
			// 
			// modename
			// 
			modename.AutoSize = true;
			modename.Font = new Font("Verdana", 36F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
			modename.ForeColor = SystemColors.GrayText;
			modename.Location = new Point(12, 20);
			modename.Name = "modename";
			modename.Size = new Size(476, 59);
			modename.TabIndex = 8;
			modename.Text = "Hi. I missed you.";
			modename.TextAlign = ContentAlignment.MiddleLeft;
			modename.UseMnemonic = false;
			modename.Visible = false;
			// 
			// buttontoggleinfo
			// 
			buttontoggleinfo.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
			buttontoggleinfo.FlatStyle = FlatStyle.Popup;
			buttontoggleinfo.Image = global::CodeImp.DoomBuilder.Properties.Resources.InfoPanelCollapse;
			buttontoggleinfo.Location = new Point(988, 1);
			buttontoggleinfo.Name = "buttontoggleinfo";
			buttontoggleinfo.Size = new Size(22, 19);
			buttontoggleinfo.TabIndex = 5;
			buttontoggleinfo.TabStop = false;
			buttontoggleinfo.Tag = "builder_toggleinfopanel";
			buttontoggleinfo.UseVisualStyleBackColor = true;
			buttontoggleinfo.Click += new EventHandler(InvokeTaggedAction);
			buttontoggleinfo.MouseUp += new MouseEventHandler(buttontoggleinfo_MouseUp);
			// 
			// console
			// 
			console.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
						| AnchorStyles.Left)
						| AnchorStyles.Right)));
			console.Location = new Point(3, 3);
			console.Name = "console";
			console.Size = new Size(851, 98);
			console.TabIndex = 10;
			// 
			// vertexinfo
			// 
			vertexinfo.Location = new Point(0, 0);
			vertexinfo.MaximumSize = new Size(10000, 100);
			vertexinfo.MinimumSize = new Size(100, 100);
			vertexinfo.Name = "vertexinfo";
			vertexinfo.Size = new Size(310, 100);
			vertexinfo.TabIndex = 1;
			vertexinfo.Visible = false;
			// 
			// linedefinfo
			// 
			linedefinfo.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left)
						| AnchorStyles.Right)));
			linedefinfo.Location = new Point(3, 3);
			linedefinfo.MaximumSize = new Size(10000, 100);
			linedefinfo.MinimumSize = new Size(100, 100);
			linedefinfo.Name = "linedefinfo";
			linedefinfo.Size = new Size(1006, 100);
			linedefinfo.TabIndex = 0;
			linedefinfo.Visible = false;
			// 
			// thinginfo
			// 
			thinginfo.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left)
						| AnchorStyles.Right)));
			thinginfo.Location = new Point(3, 3);
			thinginfo.MaximumSize = new Size(10000, 100);
			thinginfo.MinimumSize = new Size(100, 100);
			thinginfo.Name = "thinginfo";
			thinginfo.Size = new Size(1006, 100);
			thinginfo.TabIndex = 3;
			thinginfo.Visible = false;
			// 
			// sectorinfo
			// 
			sectorinfo.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left)
						| AnchorStyles.Right)));
			sectorinfo.Location = new Point(3, 3);
			sectorinfo.MaximumSize = new Size(10000, 100);
			sectorinfo.MinimumSize = new Size(100, 100);
			sectorinfo.Name = "sectorinfo";
			sectorinfo.Size = new Size(1006, 100);
			sectorinfo.TabIndex = 2;
			sectorinfo.Visible = false;
			// 
			// redrawtimer
			// 
			redrawtimer.Interval = 1;
			redrawtimer.Tick += new EventHandler(redrawtimer_Tick);
			// 
			// display
			// 
			display.BackColor = SystemColors.ControlDarkDark;
			display.BackgroundImageLayout = ImageLayout.Center;
			display.BorderStyle = BorderStyle.Fixed3D;
			display.CausesValidation = false;
			display.Location = new Point(373, 141);
			display.Name = "display";
			display.Size = new Size(542, 307);
			display.TabIndex = 5;
			display.MouseUp += new MouseEventHandler(display_MouseUp);
			display.MouseLeave += new EventHandler(display_MouseLeave);
			display.Paint += new PaintEventHandler(display_Paint);
			display.PreviewKeyDown += new PreviewKeyDownEventHandler(display_PreviewKeyDown);
			display.MouseMove += new MouseEventHandler(display_MouseMove);
			display.MouseDoubleClick += new MouseEventHandler(display_MouseDoubleClick);
			display.MouseClick += new MouseEventHandler(display_MouseClick);
			display.MouseDown += new MouseEventHandler(display_MouseDown);
			display.Resize += new EventHandler(display_Resize);
			display.MouseEnter += new EventHandler(display_MouseEnter);
			// 
			// processor
			// 
			processor.Interval = 10;
			processor.Tick += new EventHandler(processor_Tick);
			// 
			// statusflasher
			// 
			statusflasher.Tick += new EventHandler(statusflasher_Tick);
			// 
			// statusresetter
			// 
			statusresetter.Tick += new EventHandler(statusresetter_Tick);
			// 
			// dockersspace
			// 
			dockersspace.Dock = DockStyle.Left;
			dockersspace.Location = new Point(0, 49);
			dockersspace.Name = "dockersspace";
			dockersspace.Size = new Size(26, 515);
			dockersspace.TabIndex = 6;
			// 
			// modestoolbar
			// 
			modestoolbar.AutoSize = false;
			modestoolbar.ImageScalingSize = MainForm.ScaledIconSize;
			modestoolbar.Dock = DockStyle.Left;
			modestoolbar.Location = new Point(0, 49);
			modestoolbar.Name = "modestoolbar";
			modestoolbar.Padding = new Padding(2, 0, 2, 0);
			modestoolbar.Size = new Size(30, 515);
			modestoolbar.TabIndex = 8;
			modestoolbar.Text = "Editing Modes";
			// 
			// dockerspanel
			// 
			dockerspanel.Location = new Point(62, 67);
			dockerspanel.Name = "dockerspanel";
			dockerspanel.Size = new Size(236, 467);
			dockerspanel.TabIndex = 7;
			dockerspanel.TabStop = false;
			dockerspanel.UserResize += new EventHandler(dockerspanel_UserResize);
			dockerspanel.Collapsed += new EventHandler(LoseFocus);
			dockerspanel.MouseContainerEnter += new EventHandler(dockerspanel_MouseContainerEnter);
			// 
			// dockerscollapser
			// 
			dockerscollapser.Interval = 200;
			dockerscollapser.Tick += new EventHandler(dockerscollapser_Tick);
			// 
			// modecontrolstoolbar
			// 
			modecontrolstoolbar.Dock = DockStyle.Top;
			modecontrolstoolbar.Location = new Point(328, 0);
			modecontrolstoolbar.Name = "modecontrolstoolbar";
			modecontrolstoolbar.Size = new Size(43, 24);
			modecontrolstoolbar.ImageScalingSize = MainForm.ScaledIconSize;
			modecontrolstoolbar.TabIndex = 1;
			modecontrolstoolbar.Text = "toolStrip1";
			modecontrolstoolbar.Visible = false;
			// 
			// itemtogglecomments
			// 
			itemtogglecomments.Checked = true;
			itemtogglecomments.CheckOnClick = true;
			itemtogglecomments.CheckState = CheckState.Checked;
			itemtogglecomments.Image = global::CodeImp.DoomBuilder.Properties.Resources.Comment;
			itemtogglecomments.Name = "itemtogglecomments";
			itemtogglecomments.Size = new Size(215, 22);
			itemtogglecomments.Tag = "builder_togglecomments";
			itemtogglecomments.Text = "Show Comments";
			itemtogglecomments.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemtogglefixedthingsscale
			// 
			itemtogglefixedthingsscale.Checked = true;
			itemtogglefixedthingsscale.CheckOnClick = true;
			itemtogglefixedthingsscale.CheckState = CheckState.Checked;
			itemtogglefixedthingsscale.Image = global::CodeImp.DoomBuilder.Properties.Resources.FixedThingsScale;
			itemtogglefixedthingsscale.Name = "itemtogglefixedthingsscale";
			itemtogglefixedthingsscale.Size = new Size(215, 22);
			itemtogglefixedthingsscale.Tag = "builder_togglefixedthingsscale";
			itemtogglefixedthingsscale.Text = "Fixed Things Scale";
			itemtogglefixedthingsscale.Click += new EventHandler(InvokeTaggedAction);
			// 
			// itemdynamicgridsize
			// 
			itemdynamicgridsize.Checked = true;
			itemdynamicgridsize.CheckOnClick = true;
			itemdynamicgridsize.CheckState = CheckState.Checked;
			itemdynamicgridsize.Image = global::CodeImp.DoomBuilder.Properties.Resources.GridDynamic;
			itemdynamicgridsize.Name = "itemdynamicgridsize";
			itemdynamicgridsize.Size = new Size(219, 22);
			itemdynamicgridsize.Tag = "builder_toggledynamicgrid";
			itemdynamicgridsize.Text = "Dynamic Grid Size";
			itemdynamicgridsize.Click += new EventHandler(InvokeTaggedAction);


			menumain.ResumeLayout(false);
			menumain.PerformLayout();
			toolbar.ResumeLayout(false);
			toolbar.PerformLayout();
			toolbarContextMenu.ResumeLayout(false);
			statusbar.ResumeLayout(false);
			statusbar.PerformLayout();
			panelinfo.ResumeLayout(false);
			panelinfo.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

	}
}