﻿using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using Fomm.PackageManager;
using Fomm.Games.Fallout3.Script;
using System.Text;
using System.Drawing;
using Fomm.Games.Fallout3.Tools.CriticalRecords;
using Fomm.PackageManager.ModInstallLog;
using Fomm.PackageManager.XmlConfiguredInstall;
using Fomm.Games.Fallout3.Script.XmlConfiguredInstall;
using Fomm.PackageManager.XmlConfiguredInstall.Parsers;
using Fomm.Games.Fallout3.Script.XmlConfiguredInstall.Parsers;
using Fomm.Games.Fallout3.Tools.TESsnip;
using Fomm.Controls;
using Fomm.Games.Fallout3.Settings;

namespace Fomm.Games.Fallout3
{
	/// <summary>
	/// Provides information required for the programme to manage Fallout 3 plugins.
	/// </summary>
	public class Fallout3GameMode : GameMode
	{
		public static class SettingsFile
		{
			public static readonly string FOIniPath = "FOIniPath";
			public static readonly string FOPrefsIniPath = "FOPrefsIniPath";
			public static readonly string GeckIniPath = "GeckIniPath";
			public static readonly string GeckPrefsIniPath = "GeckPrefsIniPath";
		}

		private readonly string m_strSavesPath = null;
		private Dictionary<string, string> m_dicAdditionalPaths = new Dictionary<string, string>();
		private Dictionary<string, string> m_dicSettingsFiles = new Dictionary<string, string>();
		private List<GameTool> m_lstTools = new List<GameTool>();
		private List<GameTool> m_lstGameSettingsTools = new List<GameTool>();
		private List<GameTool> m_lstRightClickTools = new List<GameTool>();
		private List<GameTool> m_lstLoadOrderTools = new List<GameTool>();
		private List<GameTool> m_lstGameLaunchCommands = new List<GameTool>();
		private List<SettingsPage> m_lstSettingsPages = new List<SettingsPage>();
		private Fallout3PluginManager m_pmgPluginManager = new Fallout3PluginManager();

		#region Properties

		/// <summary>
		/// Gets the modDirectory of the GameMode.
		/// </summary>
		/// <value>The modDirectory of the GameMode.</value>
		public override string ModDirectory
		{
			get
			{
				string strModDirectory = Properties.Settings.Default.fallout3ModDirectory;
				if (String.IsNullOrEmpty(strModDirectory))
					throw new Exception("The Mod Directory for Fallout 3 Mods has not been set.");
				if (!Directory.Exists(strModDirectory))
					Directory.CreateDirectory(strModDirectory);
				return strModDirectory;
			}
		}

		/// <summary>
		/// Gets the modInfoCacheDirectory of the GameMode.
		/// </summary>
		/// <value>The modInfoCacheDirectory of the GameMode.</value>
		public override string ModInfoCacheDirectory
		{
			get
			{
				string strCache = Path.Combine(ModDirectory, "cache");
				if (!Directory.Exists(strCache))
					Directory.CreateDirectory(strCache);
				return strCache;
			}
		}

		/// <summary>
		/// Gets the game launch command.
		/// </summary>
		/// <value>The game launch command.</value>
		public override GameTool LaunchCommand
		{
			get
			{
				if (String.IsNullOrEmpty(Properties.Settings.Default.fallout3LaunchCommand) && File.Exists("fose_loader.exe"))
					return new GameTool("Launch FOSE", "Launches Fallout 3 using FOSE.", LaunchGame);
				return new GameTool("Launch Fallout 3", "Launches Fallout 3 using FOSE.", LaunchGame);
			}
		}

		/// <summary>
		/// Gets the icon used for the plugin file type.
		/// </summary>
		/// <value>The icon used for the plugin file type.</value>
		public override Icon PluginFileIcon
		{
			get
			{
				string strFalloutEsm = Path.Combine(PluginsPath, "fallout3.esm");
				return System.Drawing.Icon.ExtractAssociatedIcon(strFalloutEsm);
			}
		}

		/// <summary>
		/// Gets the path to the game directory were pluings are to be installed.
		/// </summary>
		/// <value>The path to the game directory were pluings are to be installed.</value>
		public override string PluginsPath
		{
			get
			{
				return Path.Combine(Environment.CurrentDirectory, "Data");
			}
		}

		/// <summary>
		/// Gets the path to the plugins.txt file.
		/// </summary>
		/// <remarks>
		/// plugins.txt is a Fallout 3 file that tracks active plugins.
		/// </remarks>
		/// <value>The path to the plugins.txt file.</value>
		public string PluginsFilePath
		{
			get
			{
				return m_dicAdditionalPaths["PluginsFile"];
			}
		}

		/// <summary>
		/// Gets the directory where installation information is stored for this game mode.
		/// </summary>
		/// <remarks>
		/// This is where install logs, overwrites, and the like are stored.
		/// </remarks>
		/// <value>The directory where installation information is stored for this game mode.</value>
		public override string InstallInfoDirectory
		{
			get
			{
				string strDirectory = Properties.Settings.Default.fallout3InstallInfoDirectory;
				if (String.IsNullOrEmpty(strDirectory))
					throw new Exception("The InstallInfoDirectory for Fallout 3 Mods has not been set.");
				if (!Directory.Exists(strDirectory))
					Directory.CreateDirectory(strDirectory);
				return strDirectory;
			}
		}

		/// <summary>
		/// Gets the settings files used in the game mode.
		/// </summary>
		/// <value>The settings files used in the game mode.</value>
		public override IDictionary<string, string> SettingsFiles
		{
			get
			{
				return m_dicSettingsFiles;
			}
		}

		/// <summary>
		/// Gets any other paths used in the game mode.
		/// </summary>
		/// <value>Any other paths used in the game mode.</value>
		public override IDictionary<string, string> AdditionalPaths
		{
			get
			{
				return m_dicAdditionalPaths;
			}
		}

		/// <summary>
		/// Gets the path to the game's save game files.
		/// </summary>
		/// <value>The path to the game's save game files.</value>
		public override string SavesPath
		{
			get
			{
				return m_strSavesPath;
			}
		}

		/// <summary>
		/// Gets the path to the directory where Windows live install the DLCs.
		/// </summary>
		/// <value>The path to the directory where Windows live install the DLCs.</value>
		protected string DLCDirectory
		{
			get
			{
				return m_dicAdditionalPaths["DLCDir"];
			}
		}

		#region Tool Injection

		/// <summary>
		/// Gets the list of tools to add to the tools menu.
		/// </summary>
		/// <value>The list of tools to add to the tools menu.</value>
		public override IList<GameTool> Tools
		{
			get
			{
				return m_lstTools;
			}
		}

		/// <summary>
		/// Gets the list of tools to add to the game settings menu.
		/// </summary>
		/// <value>The list of tools to add to the game settings menu.</value>
		public override IList<GameTool> GameSettingsTools
		{
			get
			{
				return m_lstGameSettingsTools;
			}
		}

		/// <summary>
		/// Gets the list of tools to add to the right-click menu.
		/// </summary>
		/// <value>The list of tools to add to the right-click menu.</value>
		public override IList<GameTool> RightClickTools
		{
			get
			{
				return m_lstRightClickTools;
			}
		}

		/// <summary>
		/// Gets the list of tools to add to the load order menu.
		/// </summary>
		/// <value>The list of tools to add to the load order menu.</value>
		public override IList<GameTool> LoadOrderTools
		{
			get
			{
				return m_lstLoadOrderTools;
			}
		}

		/// <summary>
		/// Gets the list of game launch commands.
		/// </summary>
		/// <value>The list of game launch commands.</value>
		public override IList<GameTool> GameLaunchCommands
		{
			get
			{
				return m_lstGameLaunchCommands;
			}
		}

		#endregion

		/// <summary>
		/// Gets the settings pages that privode management of game mode-specific settings.
		/// </summary>
		/// <value>The settings pages that privode management of game mode-specific settings.</value>
		public override IList<SettingsPage> SettingsPages
		{
			get
			{
				return m_lstSettingsPages;
			}
		}

		/// <summary>
		/// Gets the plugin manager for this game mode.
		/// </summary>
		/// <value>The plugin manager for this game mode.</value>
		public override PluginManager PluginManager
		{
			get
			{
				return m_pmgPluginManager;
			}
		}
		
		/// <summary>
		/// Gets the Fallout 3 rederer info file.
		/// </summary>
		/// <value>The Fallout 3 rederer info file.</value>
		public string FORendererFile
		{
			get
			{
				return m_dicAdditionalPaths["FORendererFile"];
			}
		}

		/// <summary>
		/// Gets the version of the installed game.
		/// </summary>
		/// <value>The version of the installed game.</value>
		public override Version GameVersion
		{
			get
			{
				if (File.Exists("Fallout3.exe"))
					return new Version(System.Diagnostics.FileVersionInfo.GetVersionInfo("Fallout3.exe").FileVersion.Replace(", ", "."));
				if (File.Exists("Fallout3ng.exe"))
					return new Version(System.Diagnostics.FileVersionInfo.GetVersionInfo("Fallout3ng.exe").FileVersion.Replace(", ", "."));
				return null;
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// The default constructor.
		/// </summary>
		public Fallout3GameMode()
		{
			string strUserGameDataPath = Path.Combine(Program.PersonalDirectory, "My games\\Fallout3");

			m_dicSettingsFiles[SettingsFile.FOIniPath] = Path.Combine(strUserGameDataPath, "Fallout.ini");
			m_dicSettingsFiles[SettingsFile.FOPrefsIniPath] = Path.Combine(strUserGameDataPath, "FalloutPrefs.ini");
			m_dicSettingsFiles[SettingsFile.GeckIniPath] = Path.Combine(strUserGameDataPath, "GECKCustom.ini");
			m_dicSettingsFiles[SettingsFile.GeckPrefsIniPath] = Path.Combine(strUserGameDataPath, "GECKPrefs.ini");

			m_dicAdditionalPaths["FORendererFile"] = Path.Combine(strUserGameDataPath, "RendererInfo.txt");
			m_dicAdditionalPaths["PluginsFile"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fallout3/plugins.txt");
			m_dicAdditionalPaths["DLCDir"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\xlive\\DLC");

			m_strSavesPath = Path.Combine(strUserGameDataPath, NativeMethods.GetPrivateProfileString("General", "SLocalSavePath", "Games", m_dicSettingsFiles["FOIniPath"]));

			m_lstSettingsPages.Add(new GeneralSettingsPage());

			m_lstTools.Add(new GameTool("BSA Tool", "Creates and unpacks BSA files.", LaunchBSATool));
			m_lstTools.Add(new GameTool("TESsnip", "An ESP/ESM editor.", LaunchTESsnipTool));
			m_lstTools.Add(new GameTool("Shader Editor", "A shader (SDP) editor.", LaunchShaderEditTool));
			m_lstTools.Add(new GameTool("CREditor", "Edits critical records in an ESP/ESM.", LaunchCREditorTool));
			m_lstTools.Add(new GameTool("Archive Invalidation", "Toggles Archive Invalidation.", ToggleArchiveInvalidation));
			m_lstTools.Add(new GameTool("Install Tweaker", "Advanced Fallout 3 tweaking.", LaunchInstallTweakerTool));
			m_lstTools.Add(new GameTool("Conflict Detector", "Checks for conflicts with mod-author specified critical records.", LaunchConflictDetector));
			m_lstTools.Add(new GameTool("Save Games", "Save game info viewer.", LaunchSaveGamesViewer));

			m_lstGameSettingsTools.Add(new GameTool("Graphics Settings", "Changes the graphics settings.", LaunchGraphicsSettingsTool));

			m_lstRightClickTools.Add(new GameTool("Open in TESsnip...", "Open the selected plugins in TESsnip.", LaunchTESsnipToolWithSelectedPlugins));
			m_lstRightClickTools.Add(new GameTool("Open in CREditor...", "Open the selected plugins in TESsnip.", LaunchCREditorToolWithSelectedPlugins));

			m_lstLoadOrderTools.Add(new GameTool("Load Order Report...", "Generates a report on the current load order, as compared to the BOSS recomendation.", LaunchLoadOrderReport));
			m_lstLoadOrderTools.Add(new GameTool("BOSS Auto Sort", "Auto-sorts the plugins using BOSS's masterlist.", LaunchSortPlugins));

			m_lstGameLaunchCommands.Add(new GameTool("Launch Fallout 3", "Launches plain Fallout 3.", LaunchFallout3Plain));
			m_lstGameLaunchCommands.Add(new GameTool("Launch FOSE", "Launches Fallout 3 with FOSE.", LaunchFallout3FOSE));
			m_lstGameLaunchCommands.Add(new GameTool("Launch Custom Fallout 3", "Launches Fallout 3 with custom command.", LaunchFallout3Custom));
		}

		#endregion

		#region Tool Launch Methods

		#region Game Launch

		/// <summary>
		/// Launches the game with a custom command.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchFallout3Custom(MainForm p_frmMainForm)
		{
			if (p_frmMainForm.HasOpenUtilityWindows)
			{
				MessageBox.Show("Please close all utility windows before launching fallout");
				return;
			}
			string command = Properties.Settings.Default.fallout3LaunchCommand;
			string args = Properties.Settings.Default.fallout3LaunchCommandArgs;
			if (String.IsNullOrEmpty(command))
			{
				MessageBox.Show("No custom launch command has been set", "Error");
				return;
			}
			try
			{
				System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
				psi.Arguments = args;
				psi.FileName = command;
				psi.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(command));
				if (System.Diagnostics.Process.Start(psi) == null)
				{
					MessageBox.Show("Failed to launch '" + command + "'");
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to launch '" + command + "'\n" + ex.Message);
				return;
			}
			p_frmMainForm.Close();
		}

		/// <summary>
		/// Launches the game, with FOSE.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchFallout3FOSE(MainForm p_frmMainForm)
		{
			if (!File.Exists("fose_loader.exe"))
			{
				MessageBox.Show("fose does not appear to be installed");
				return;
			}
			if (p_frmMainForm.HasOpenUtilityWindows)
			{
				MessageBox.Show("Please close all utility windows before launching fallout");
				return;
			}
			try
			{
				System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
				psi.FileName = "fose_loader.exe";
				psi.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath("fose_loader.exe"));
				if (System.Diagnostics.Process.Start(psi) == null)
				{
					MessageBox.Show("Failed to launch 'fose_loader.exe'");
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to launch 'fose_loader.exe'\n" + ex.Message);
				return;
			}
			p_frmMainForm.Close();
		}

		/// <summary>
		/// Launches the game, without FOSE.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchFallout3Plain(MainForm p_frmMainForm)
		{
			if (p_frmMainForm.HasOpenUtilityWindows)
			{
				MessageBox.Show("Please close all utility windows before launching fallout");
				return;
			}
			string command;
			if (File.Exists("fallout3.exe"))
				command = "fallout3.exe";
			else
				command = "fallout3ng.exe";
			try
			{
				System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
				psi.FileName = command;
				psi.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(command));
				if (System.Diagnostics.Process.Start(psi) == null)
				{
					MessageBox.Show("Failed to launch '" + command + "'");
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to launch '" + command + "'\n" + ex.Message);
				return;
			}
			p_frmMainForm.Close();
		}

		/// <summary>
		/// Launches the game, using FOSE if present.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchGame(MainForm p_frmMainForm)
		{
			string command = Properties.Settings.Default.fallout3LaunchCommand;
			string args = Properties.Settings.Default.fallout3LaunchCommandArgs;
			if (String.IsNullOrEmpty(command))
			{
				if (File.Exists("fose_loader.exe"))
					command = "fose_loader.exe";
				else if (File.Exists("fallout3.exe"))
					command = "fallout3.exe";
				else
					command = "fallout3ng.exe";
				args = null;
			}
			try
			{
				System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
				psi.Arguments = args;
				psi.FileName = command;
				psi.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(command));
				if (System.Diagnostics.Process.Start(psi) == null)
				{
					MessageBox.Show("Failed to launch '" + command + "'");
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to launch '" + command + "'\n" + ex.Message);
				return;
			}
		}

		#endregion

		#region Load Order Menu

		/// <summary>
		/// Auto-sorts the plugins using BOSS's masterlist.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchSortPlugins(MainForm p_frmMainForm)
		{
			if (MessageBox.Show(p_frmMainForm, "This is currently a beta feature, and the load order template may not be optimal.\n" +
				"Ensure you have a backup of your load order before running this tool.\n" +
				"War you sure you wish to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
			string[] plugins = new string[p_frmMainForm.PluginsListViewItems.Count];
			for (int i = 0; i < plugins.Length; i++)
				plugins[i] = p_frmMainForm.PluginsListViewItems[i].Text;
			Fomm.Games.Fallout3.Tools.AutoSorter.LoadOrderSorter.SortList(plugins);
			for (int i = 0; i < plugins.Length; i++)
				PluginManager.SetLoadOrder(Path.Combine(PluginsPath, plugins[i]), i);
			p_frmMainForm.RefreshPluginList();
		}

		/// <summary>
		/// Generates a report on the current load order, as compared to the BOSS recomendation.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchLoadOrderReport(MainForm p_frmMainForm)
		{
			string[] plugins = new string[p_frmMainForm.PluginsListViewItems.Count];
			bool[] active = new bool[plugins.Length];
			bool[] corrupt = new bool[plugins.Length];
			string[][] masters = new string[plugins.Length][];
			Plugin p;
			List<string> mlist = new List<string>();
			for (int i = 0; i < plugins.Length; i++)
			{
				plugins[i] = p_frmMainForm.PluginsListViewItems[i].Text;
				active[i] = p_frmMainForm.PluginsListViewItems[i].Checked;
				try
				{
					p = new Plugin(Path.Combine(PluginsPath, plugins[i]), true);
				}
				catch
				{
					p = null;
					corrupt[i] = true;
				}
				if (p != null)
				{
					foreach (SubRecord sr in ((Record)p.Records[0]).SubRecords)
					{
						if (sr.Name != "MAST") continue;
						mlist.Add(sr.GetStrData().ToLowerInvariant());
					}
					if (mlist.Count > 0)
					{
						masters[i] = mlist.ToArray();
						mlist.Clear();
					}
				}
			}
			string s = Fomm.Games.Fallout3.Tools.AutoSorter.LoadOrderSorter.GenerateReport(plugins, active, corrupt, masters);
			PackageManager.TextEditor.ShowEditor(s, Fomm.PackageManager.TextEditorType.Text, false);
		}

		#endregion

		#region Right Click Menu

		/// <summary>
		/// Launches the TESsnip tool, passing it the given plugins.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchTESsnipToolWithSelectedPlugins(MainForm p_frmMainForm)
		{
			if (p_frmMainForm.SelectedPlugins.Count == 0)
				return;
			List<string> lstPlugins = new List<string>();
			foreach (string strPluginName in p_frmMainForm.SelectedPlugins)
				lstPlugins.Add(Path.Combine(Program.GameMode.PluginsPath, strPluginName));
			Tools.TESsnip.TESsnip tes = new Tools.TESsnip.TESsnip(lstPlugins.ToArray());
			tes.FormClosed += delegate(object sender2, FormClosedEventArgs e2)
			{
				p_frmMainForm.RefreshPluginList();
				GC.Collect();
			};
			tes.Show();
		}

		/// <summary>
		/// Launches the CREditor tool, passing it the given plugins.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchCREditorToolWithSelectedPlugins(MainForm p_frmMainForm)
		{
			if (p_frmMainForm.SelectedPlugins.Count == 0)
				return;
			List<string> lstPlugins = new List<string>();
			foreach (string strPluginName in p_frmMainForm.SelectedPlugins)
				lstPlugins.Add(Path.Combine(Program.GameMode.PluginsPath, strPluginName));
			Tools.CriticalRecords.CriticalRecordsForm crfEditor = new Tools.CriticalRecords.CriticalRecordsForm(lstPlugins.ToArray());
			crfEditor.Show();
		}

		#endregion

		#region Game Settings Menu

		/// <summary>
		/// Launches the Graphics Settings tool.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchGraphicsSettingsTool(MainForm p_frmMainForm)
		{
			Tools.GraphicsSettings.GraphicsSettings gsfGraphicsSettingsForm = new Tools.GraphicsSettings.GraphicsSettings();
			gsfGraphicsSettingsForm.ShowDialog();
		}

		#endregion

		#region Tools Menu

		/// <summary>
		/// Launches the save games viewer.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchSaveGamesViewer(MainForm p_frmMainForm)
		{
			List<string> lstActive = new List<string>();
			//the original implementation populated the inactive list with all plugins
			// we only populate it with inactive plugins - hopefully that's OK
			List<string> lstInactive = new List<string>();

			foreach (ListViewItem lviPlugin in p_frmMainForm.PluginsListViewItems)
			{
				if (lviPlugin.Checked)
					lstActive.Add(lviPlugin.Text);
				else
					lstInactive.Add(lviPlugin.Text);
			}
			(new Tools.SaveForm(lstActive.ToArray(), lstInactive.ToArray())).Show();
		}

		/// <summary>
		/// Launches the conflict detector tool.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchConflictDetector(MainForm p_frmMainForm)
		{
			string strMessage = "This is an experimental feature that relies on fomod authors specifying which parts of their plugins are critical." + Environment.NewLine + "Using this feature will not hurt anything, but it is not guaranteed to find any or all conflicts.";
			if (MessageBox.Show(p_frmMainForm, strMessage, "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.Cancel)
				return;
			Tools.PluginConflictDetector pcdDetector = new Tools.PluginConflictDetector(p_frmMainForm);
			pcdDetector.CheckForConflicts();
			p_frmMainForm.LoadPluginInfo();
		}

		/// <summary>
		/// Launches the BSA tool.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchBSATool(MainForm p_frmMainForm)
		{
			new Tools.BSA.BSABrowser().Show();
		}

		/// <summary>
		/// Launches the Install Tweaker tool.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchInstallTweakerTool(MainForm p_frmMainForm)
		{
			if (p_frmMainForm.IsPackageManagerOpen)
			{
				MessageBox.Show(p_frmMainForm, "Please close the Package Manager before running the install tweaker.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			(new Tools.InstallTweaker.InstallationTweaker()).ShowDialog();
		}

		/// <summary>
		/// Launches the TESsnip tool.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchTESsnipTool(MainForm p_frmMainForm)
		{
			Tools.TESsnip.TESsnip tes = new Tools.TESsnip.TESsnip();
			tes.FormClosed += delegate(object sender2, FormClosedEventArgs e2)
			{
				p_frmMainForm.RefreshPluginList();
				GC.Collect();
			};
			tes.Show();
		}

		/// <summary>
		/// Launches the Shader Edit tool.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchShaderEditTool(MainForm p_frmMainForm)
		{
			new Tools.ShaderEdit.MainForm().Show();
		}

		/// <summary>
		/// Launches the CREditor tool.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void LaunchCREditorTool(MainForm p_frmMainForm)
		{
			Tools.CriticalRecords.CriticalRecordsForm crfEditor = new Tools.CriticalRecords.CriticalRecordsForm();
			crfEditor.Show();
			GC.Collect();
		}


		/// <summary>
		/// Toggles archive invalidation.
		/// </summary>
		/// <param name="p_frmMainForm">The main mod management form.</param>
		public void ToggleArchiveInvalidation(MainForm p_frmMainForm)
		{
			Fomm.Games.Fallout3.Tools.ArchiveInvalidation.Update();
		}

		#endregion

		#endregion

		#region Scripts

		/// <summary>
		/// Gets the default script for a mod.
		/// </summary>
		/// <value>The default script for a mod.</value>
		public virtual string DefaultCSharpScript
		{
			get
			{
				return @"using System;
using fomm.Scripting;

class Script : Fallout3BaseScript {
	public static bool OnActivate() {
        //Install all files from the fomod and activate any esps
        PerformBasicInstall();
		return true;
	}
}
";
			}
		}

		/// <summary>
		/// Creates a mod install script for the given <see cref="fomod"/>.
		/// </summary>
		/// <param name="p_fomodMod">The mod for which to create an installer script.</param>
		/// <param name="p_mibInstaller">The installer for which the script is being created.</param>
		/// <returns>A mod install script for the given <see cref="fomod"/>.</returns>
		public override ModInstallScript CreateInstallScript(fomod p_fomodMod, ModInstallerBase p_mibInstaller)
		{
			return new Fallout3ModInstallScript(p_fomodMod, p_mibInstaller);
		}

		/// <summary>
		/// Creates a mod upgrade script for the given <see cref="fomod"/>.
		/// </summary>
		/// <param name="p_fomodMod">The mod for which to create an installer script.</param>
		/// <param name="p_mibInstaller">The installer for which the script is being created.</param>
		/// <returns>A mod upgrade script for the given <see cref="fomod"/>.</returns>
		public override ModInstallScript CreateUpgradeScript(fomod p_fomodMod, ModInstallerBase p_mibInstaller)
		{
			return new Fallout3ModUpgradeScript(p_fomodMod, p_mibInstaller);
		}

		/// <summary>
		/// Creates a <see cref="DependencyStateManager"/> for the given <see cref="ModInstallScript"/>.
		/// </summary>
		/// <param name="p_misInstallScript">The <see cref="ModInstallScript"/> for which the
		/// <see cref="DependencyStateManager"/> is being created.</param>
		/// <returns>A <see cref="DependencyStateManager"/> for the given <see cref="ModInstallScript"/>.</returns>
		public override DependencyStateManager CreateDependencyStateManager(ModInstallScript p_misInstallScript)
		{
			return new Fallout3DependencyStateManager(p_misInstallScript);
		}

		/// <summary>
		/// The factory method that creates the appropriate parser extension for the specified configuration file version.
		/// </summary>
		/// <param name="p_strVersion">The XML configuration file version for which to return a parser extension.</param>
		/// <returns>The appropriate parser extension for the specified configuration file version, or
		/// <lang cref="null"/> if no extension is available.</returns>
		public override ParserExtension CreateParserExtension(string p_strVersion)
		{
			switch (p_strVersion)
			{
				case "1.0":
					return new Fallout3Parser10Extension();
				case "2.0":
				case "3.0":
				case "4.0":
				case "5.0":
					return new Fallout3Parser20Extension();
				default:
					return null;
			}
		}

		/// <summary>
		/// Gets the path to the schema file for the specified configuration file version.
		/// </summary>
		/// <param name="p_strVersion">The XML configuration file version for which to return a parser extension.</param>
		/// <returns>The path to the schema file for the specified configuration file version, or
		/// <lang cref="null"/> if there is no game-specific schema for the specified configuration
		/// file version.</returns>
		public override string GetGameSpecificXMLConfigSchemaPath(string p_strVersion)
		{
			return Path.Combine(Program.ProgrammeInfoDirectory, String.Format(@"Fallout3\ModConfig{1}.xsd", p_strVersion));
		}

		#endregion

		#region Command Line Arguments

		/// <summary>
		/// Return command line help for the arguments provided by the game mode.
		/// </summary>
		/// <remarks>
		/// This method should only return the text required to describe the arguments. All header,
		/// footer, and context text is already provided.
		/// </remarks>
		/// <returns>Command line help for the arguments provided by the game mode.</returns>
		public override string GetCommandLineHelp()
		{
			StringBuilder stbHelp = new StringBuilder();
			stbHelp.AppendLine("*.dat, *.bsa, *.esm, *.esp, *.sdp");
			stbHelp.AppendLine("Open the specified file in the relevent utility");
			stbHelp.AppendLine();
			stbHelp.AppendLine("-setup, -bsa-unpacker, -bsa-creator, -tessnip, -sdp-editor, -install-tweaker");
			stbHelp.AppendLine("Open the specified utility window, without opening the main form where appropriate");
			return stbHelp.ToString();
		}

		/// <summary>
		/// Handles the command line arguments that run outside of an instance of FOMM.
		/// </summary>
		/// <param name="p_strArgs">The command line arguments that were passed to the programme.</param>
		/// <returns><lang cref="true"/> if at least one of the arguments were handled;
		/// <lang cref="false"/> otherwise.</returns>
		public override bool HandleStandaloneArguments(string[] p_strArgs)
		{
			if (!p_strArgs[0].StartsWith("-") && File.Exists(p_strArgs[0]))
			{
				switch (Path.GetExtension(p_strArgs[0]).ToLowerInvariant())
				{
					case ".dat":
					case ".bsa":
						Application.Run(new Tools.BSA.BSABrowser(p_strArgs[0]));
						return true;
					case ".sdp":
						Application.Run(new Tools.ShaderEdit.MainForm(p_strArgs[0]));
						return true;
					case ".esp":
					case ".esm":
						Application.Run(new Tools.TESsnip.TESsnip(new string[] { p_strArgs[0] }));
						return true;
				}
			}
			else
			{
				switch (p_strArgs[0])
				{
					case "-setup":
						bool booNewMutex = false;
						Mutex mutex = new Mutex(true, "fommMainMutex", out booNewMutex);
						if (!booNewMutex)
						{
							MessageBox.Show("fomm is already running", "Error");
							mutex.Close();
							return true;
						}
						Application.Run(new SettingsForm());
						mutex.Close();
						return true;
					case "-bsa-unpacker":
						Application.Run(new Tools.BSA.BSABrowser());
						return true;
					case "-bsa-creator":
						Application.Run(new Tools.BSA.BSACreator());
						return true;
					case "-tessnip":
						Application.Run(new Tools.TESsnip.TESsnip());
						return true;
					case "-sdp-editor":
						Application.Run(new Tools.ShaderEdit.MainForm());
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Handles the command line arguments that affect an instance of FOMM.
		/// </summary>
		/// <param name="p_strArgs">The command line arguments that were passed to the programme.</param>
		/// <returns><lang cref="true"/> if at least one of the arguments were handled;
		/// <lang cref="false"/> otherwise.</returns>
		public override bool HandleInAppArguments(string[] p_strArgs)
		{
			if (Array.IndexOf<string>(p_strArgs, "-install-tweaker") != -1)
			{
				Application.Run(new Tools.InstallTweaker.InstallationTweaker());
				return true;
			}
			return false;
		}

		#endregion

		/// <summary>
		/// Verifies that the given path is a valid working directory for the game mode.
		/// </summary>
		/// <param name="p_strPath">The path to validate as a working directory.</param>
		/// <returns><lang cref="true"/> if the path is a vlid working directory;
		/// <lang cref="false"/> otherwise.</returns>
		public static bool VerifyWorkingDirectory(string p_strPath)
		{
			if (String.IsNullOrEmpty(p_strPath))
				return false;

			string[] strExes = new string[] { Path.Combine(p_strPath, "fallout3.exe"),
												Path.Combine(p_strPath, "fallout3ng.exe") };
			bool booFound = false;
			foreach (string strExe in strExes)
				if (File.Exists(strExe))
				{
					booFound = true;
					break;
				}
			return booFound;
		}


		/// <summary>
		/// Sets the working directory for the programme.
		/// </summary>
		/// <remarks>
		/// This sets the working directory to the Fallout 3 install folder.
		/// </remarks>
		/// <param name="p_strErrorMessage">The out parameter that is set to the error message, if an error occurred.</param>
		/// <returns><lang cref="true"/> if the working directory was successfully set;
		/// <lang cref="false"/> otherwise.</returns>
		public override bool SetWorkingDirectory(out string p_strErrorMessage)
		{
#if TRACE
			Trace.WriteLine("Looking for Fallout 3.");
			Trace.Indent();
#endif
			string strWorkingDirectory = Properties.Settings.Default.fallout3WorkingDirectory;

			if (String.IsNullOrEmpty(strWorkingDirectory) || !Directory.Exists(strWorkingDirectory))
			{
				try
				{
					strWorkingDirectory = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Bethesda Softworks\Fallout3", "Installed Path", null) as string;
				}
				catch
				{
					strWorkingDirectory = null;
				}
			}

			using (WorkingDirectorySelectionForm wdfForm = new WorkingDirectorySelectionForm(
					"Could not find Fallout 3 directory." + Environment.NewLine +
					"Fallout's registry entry appears to be missing or incorrect." + Environment.NewLine +
					"Please enter the path to your Fallout 3 game file, or click \"Auto Detect\" to search" +
					" for the install directory. Note that Auto Detection can take several minutes.",
					"Fallout 3 Game Directory:",
					new string[] { "fallout3.exe", "fallout3ng.exe" }))
			{
				while (!VerifyWorkingDirectory(strWorkingDirectory))
				{
					if (wdfForm.ShowDialog() == DialogResult.Cancel)
					{
						p_strErrorMessage = "Could not find Fallout 3 directory.";
						return false;
					}
					strWorkingDirectory = wdfForm.WorkingDirectory;
				}
			}
			Directory.SetCurrentDirectory(strWorkingDirectory);
			Properties.Settings.Default.fallout3WorkingDirectory = strWorkingDirectory;
			Properties.Settings.Default.Save();
#if TRACE
				Trace.WriteLine("Found: " + Path.GetFullPath("."));
#endif
			p_strErrorMessage = null;
			return true;
		}

		/// <summary>
		/// This initializes the game mode.
		/// </summary>
		/// <remarks>
		/// This gets the user to specify the directories where the programme will store info
		/// such as install logs, if the directories have not already been setup.
		/// 
		/// This method also checks for DLCs, and cleans up any missing FOMods.
		/// </remarks>
		/// <returns><lang cref="true"/> if the game mode was able to initialize;
		/// <lang cref="false"/> otherwise.</returns>
		public override bool Init()
		{
			if (!Properties.Settings.Default.fallout3DoneSetup)
			{
				SetupForm sfmSetup = new SetupForm();
				if (sfmSetup.ShowDialog() == DialogResult.Cancel)
					return false;
				Properties.Settings.Default.fallout3DoneSetup = true;
				Properties.Settings.Default.Save();
			}

			CheckForDLCs();
			ScanForReadonlyPlugins();

			FOMMMigrator m = new FOMMMigrator();
			if (!m.Migrate())
				return false;

			return true;
		}

		/// <summary>
		/// This checks for DLCs isntall by Windows Live, and optionally moves them so
		/// they are compatible with FOSE.
		/// </summary>
		protected void CheckForDLCs()
		{
#if TRACE
			Trace.WriteLine("Checking DLC location.");
			Trace.Indent();
#endif

			if (Directory.Exists(DLCDirectory) && !Properties.Settings.Default.fallout3IgnoreDLC)
			{
#if TRACE
				Trace.Write("Anchorage...");
#endif
				if (Program.GetFiles(DLCDirectory, "Anchorage.esm", SearchOption.AllDirectories).Length == 1)
				{
					if (!File.Exists("data\\Anchorage.esm") && !File.Exists("data\\Anchorage - Main.bsa") && !File.Exists("data\\Anchorage - Sounds.bsa"))
					{
						string[] f1 = Directory.GetFiles(DLCDirectory, "Anchorage.esm", SearchOption.AllDirectories);
						string[] f2 = Directory.GetFiles(DLCDirectory, "Anchorage - Main.bsa", SearchOption.AllDirectories);
						string[] f3 = Directory.GetFiles(DLCDirectory, "Anchorage - Sounds.bsa", SearchOption.AllDirectories);
						if (f1.Length == 1 && f2.Length == 1 && f3.Length == 1)
						{
							switch (MessageBox.Show("You seem to have bought the DLC Anchorage.\n" +
								"Would you like to move it to fallout's data directory to allow for offline use and fose compatibility?\n" +
								"Note that this may cause issues with any save games created after it was purchased but before it was moved.\n" +
								"Click yes to move, cancel to ignore, and no if you don't want fomm to offer to move any DLC for you again.",
								"Question", MessageBoxButtons.YesNoCancel))
							{
								case DialogResult.Yes:
									File.Move(f1[0], "data\\Anchorage.esm");
									File.Move(f2[0], "data\\Anchorage - Main.bsa");
									File.Move(f3[0], "data\\Anchorage - Sounds.bsa");
									break;
								case DialogResult.No:
									Properties.Settings.Default.fallout3IgnoreDLC = true;
									Properties.Settings.Default.Save();
									break;
							}
						}
					}
				}
#if TRACE
				Trace.WriteLine("Done");
				Trace.Write("The Pitt...");
#endif
				if (Program.GetFiles(DLCDirectory, "ThePitt.esm", SearchOption.AllDirectories).Length == 1)
				{
					if (!File.Exists("data\\ThePitt.esm") && !File.Exists("data\\ThePitt - Main.bsa") && !File.Exists("data\\ThePitt - Sounds.bsa"))
					{
						string[] f1 = Directory.GetFiles(DLCDirectory, "ThePitt.esm", SearchOption.AllDirectories);
						string[] f2 = Directory.GetFiles(DLCDirectory, "ThePitt - Main.bsa", SearchOption.AllDirectories);
						string[] f3 = Directory.GetFiles(DLCDirectory, "ThePitt - Sounds.bsa", SearchOption.AllDirectories);
						if (f1.Length == 1 && f2.Length == 1 && f3.Length == 1)
						{
							switch (MessageBox.Show("You seem to have bought the DLC The Pitt.\n" +
								"Would you like to move it to fallout's data directory to allow for offline use and fose compatibility?\n" +
								"Note that this may cause issues with any save games created after it was purchased but before it was moved.\n" +
								"Click yes to move, cancel to ignore, and no if you don't want fomm to offer to move any DLC for you again.",
								"Question", MessageBoxButtons.YesNoCancel))
							{
								case DialogResult.Yes:
									File.Move(f1[0], "data\\ThePitt.esm");
									File.Move(f2[0], "data\\ThePitt - Main.bsa");
									File.Move(f3[0], "data\\ThePitt - Sounds.bsa");
									break;
								case DialogResult.No:
									Properties.Settings.Default.fallout3IgnoreDLC = true;
									Properties.Settings.Default.Save();
									break;
							}
						}
					}
				}
#if TRACE
				Trace.WriteLine("Done.");
				Trace.Write("Broken Steel...");
#endif
				if (Program.GetFiles(DLCDirectory, "BrokenSteel.esm", SearchOption.AllDirectories).Length == 1)
				{
					if (!File.Exists("Data\\BrokenSteel.esm"))
					{
						string[][] files = new string[8][];
						files[0] = Directory.GetFiles(DLCDirectory, "BrokenSteel.esm", SearchOption.AllDirectories);
						files[1] = Directory.GetFiles(DLCDirectory, "BrokenSteel - Main.bsa", SearchOption.AllDirectories);
						files[2] = Directory.GetFiles(DLCDirectory, "BrokenSteel - Sounds.bsa", SearchOption.AllDirectories);
						files[3] = Directory.GetFiles(DLCDirectory, "2 weeks later.bik", SearchOption.AllDirectories);
						files[4] = Directory.GetFiles(DLCDirectory, "B09.bik", SearchOption.AllDirectories);
						files[5] = Directory.GetFiles(DLCDirectory, "B27.bik", SearchOption.AllDirectories);
						files[6] = Directory.GetFiles(DLCDirectory, "B28.bik", SearchOption.AllDirectories);
						files[7] = Directory.GetFiles(DLCDirectory, "B29.bik", SearchOption.AllDirectories);
						bool missing = false;
						for (int i = 0; i < 8; i++)
						{
							if (files[i].Length != 1)
							{
								missing = true;
								break;
							}
							if ((i < 3 && File.Exists(Path.Combine(PluginsPath, Path.GetFileName(files[i][0])))) ||
							(i > 4 && File.Exists(Path.Combine(PluginsPath, Path.Combine("Video", Path.GetFileName(files[i][0]))))))
							{
								missing = true;
								break;
							}
						}
						if (!missing)
						{
							switch (MessageBox.Show("You seem to have bought the DLC Broken Steel.\n" +
								"Would you like to move it to fallout's data directory to allow for offline use and fose compatibility?\n" +
								"Note that this may cause issues with any save games created after it was purchased but before it was moved.\n" +
								"Click yes to move, cancel to ignore, and no if you don't want fomm to offer to move any DLC for you again.",
								"Question", MessageBoxButtons.YesNoCancel))
							{
								case DialogResult.Yes:
									if (File.Exists("data\\video\\2 weeks later.bik"))
									{
										File.Move("data\\video\\2 weeks later.bik", "data\\Video\\2 weeks later.bik.old");
									}
									if (File.Exists("data\\video\\b09.bik"))
									{
										File.Move("data\\video\\b09.bik", "data\\Video\\b09.bik.old");
									}
									for (int i = 0; i < 3; i++)
									{
										File.Move(files[i][0], Path.Combine(PluginsPath, Path.GetFileName(files[i][0])));
									}
									for (int i = 3; i < 8; i++)
									{
										File.Move(files[i][0], Path.Combine(PluginsPath, Path.Combine("Video", Path.GetFileName(files[i][0]))));
									}
									break;
								case DialogResult.No:
									Properties.Settings.Default.fallout3IgnoreDLC = true;
									Properties.Settings.Default.Save();
									break;
							}
						}
					}
				}
#if TRACE
				Trace.WriteLine("Done.");
				Trace.Write("Point Lookout...");
#endif
				if (Program.GetFiles(DLCDirectory, "PointLookout.esm ", SearchOption.AllDirectories).Length == 1)
				{
					if (!File.Exists("data\\PointLookout.esm ") && !File.Exists("data\\PointLookout - Main.bsa") && !File.Exists("data\\PointLookout - Sounds.bsa"))
					{
						string[] f1 = Directory.GetFiles(DLCDirectory, "PointLookout.esm", SearchOption.AllDirectories);
						string[] f2 = Directory.GetFiles(DLCDirectory, "PointLookout - Main.bsa", SearchOption.AllDirectories);
						string[] f3 = Directory.GetFiles(DLCDirectory, "PointLookout - Sounds.bsa", SearchOption.AllDirectories);
						if (f1.Length == 1 && f2.Length == 1 && f3.Length == 1)
						{
							switch (MessageBox.Show("You seem to have bought the DLC Point lookout.\n" +
								"Would you like to move it to fallout's data directory to allow for offline use and fose compatibility?\n" +
								"Note that this may cause issues with any save games created after it was purchased but before it was moved.\n" +
								"Click yes to move, cancel to ignore, and no if you don't want fomm to offer to move any DLC for you again.",
								"Question", MessageBoxButtons.YesNoCancel))
							{
								case DialogResult.Yes:
									File.Move(f1[0], "data\\PointLookout.esm");
									File.Move(f2[0], "data\\PointLookout - Main.bsa");
									File.Move(f3[0], "data\\PointLookout - Sounds.bsa");
									break;
								case DialogResult.No:
									Properties.Settings.Default.fallout3IgnoreDLC = true;
									Properties.Settings.Default.Save();
									break;
							}
						}
					}
				}
#if TRACE
				Trace.WriteLine("Done.");
				Trace.Write("Zeta...");
#endif
				if (Program.GetFiles(DLCDirectory, "Zeta.esm ", SearchOption.AllDirectories).Length == 1)
				{
					if (!File.Exists("data\\Zeta.esm ") && !File.Exists("data\\Zeta - Main.bsa") && !File.Exists("data\\Zeta - Sounds.bsa"))
					{
						string[] f1 = Directory.GetFiles(DLCDirectory, "Zeta.esm", SearchOption.AllDirectories);
						string[] f2 = Directory.GetFiles(DLCDirectory, "Zeta - Main.bsa", SearchOption.AllDirectories);
						string[] f3 = Directory.GetFiles(DLCDirectory, "Zeta - Sounds.bsa", SearchOption.AllDirectories);
						if (f1.Length == 1 && f2.Length == 1 && f3.Length == 1)
						{
							switch (MessageBox.Show("You seem to have bought the DLC Mothership Zeta.\n" +
								"Would you like to move it to fallout's data directory to allow for offline use and fose compatibility?\n" +
								"Note that this may cause issues with any save games created after it was purchased but before it was moved.\n" +
								"Click yes to move, cancel to ignore, and no if you don't want fomm to offer to move any DLC for you again.",
								"Question", MessageBoxButtons.YesNoCancel))
							{
								case DialogResult.Yes:
									File.Move(f1[0], "data\\Zeta.esm");
									File.Move(f2[0], "data\\Zeta - Main.bsa");
									File.Move(f3[0], "data\\Zeta - Sounds.bsa");
									break;
								case DialogResult.No:
									Properties.Settings.Default.fallout3IgnoreDLC = true;
									Properties.Settings.Default.Save();
									break;
							}
						}
					}
				}
#if TRACE
				Trace.WriteLine("Done.");
#endif
			}
#if TRACE
			Trace.Unindent();
#endif
		}

		/// <summary>
		/// This chaecks for any FOMods that have been manually deleted since the programme last ran.
		/// </summary>
		protected void ScanForReadonlyPlugins()
		{
			DirectoryInfo difPluginsDirectory = new DirectoryInfo(Program.GameMode.PluginsPath);
			List<FileInfo> lstPlugins = new List<FileInfo>(Program.GetFiles(difPluginsDirectory, "*.esp"));
			lstPlugins.AddRange(Program.GetFiles(difPluginsDirectory, "*.esm"));

			for (Int32 i = 0; i < lstPlugins.Count; i++)
			{
				FileInfo fifPlugin = lstPlugins[i];
				if ((fifPlugin.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				{
					if (MessageBox.Show(null, String.Format("'{0}' is read-only, so its load order cannot be changed. Would you like to make it not read-only?", fifPlugin.Name), "Read Only", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
						fifPlugin.Attributes &= ~FileAttributes.ReadOnly;
				}
			}
		}

		/// <summary>
		/// Determines if the specified file is a plugin for the game mode.
		/// </summary>
		/// <param name="p_strPath">The path to the file for which it is to be determined if it is a plugin file.</param>
		/// <returns><lang cref="true"/> if the specified file is a plugin file in the game mode;
		/// <lang cref="false"/> otherwise.</returns>
		public override bool IsPluginFile(string p_strPath)
		{
			string strExt = Path.GetExtension(p_strPath).ToLowerInvariant();
			return (strExt == ".esp" || strExt == ".esm");
		}

		/// <summary>
		/// hecks for any updates that are available for any game-specific components.
		/// </summary>
		/// <remarks><lang cref="true"/> if updates were available; otherwise <lang cref="false"/>.</remarks>
		public override bool CheckForUpdates()
		{
			//check for new load order tepmlate
			Int32 intLOVersion = Fomm.Games.Fallout3.Tools.AutoSorter.BOSSUpdater.GetMasterlistVersion();
			if (intLOVersion > Fomm.Games.Fallout3.Tools.AutoSorter.LoadOrderSorter.GetFileVersion())
			{
				if (MessageBox.Show("A new version of the load order template is available: Release " + intLOVersion +
					"\nDo you wish to download?", "Message", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					Fomm.Games.Fallout3.Tools.AutoSorter.BOSSUpdater.UpdateMasterlist(Fomm.Games.Fallout3.Tools.AutoSorter.LoadOrderSorter.LoadOrderTemplatePath);
					MessageBox.Show("The load order template was updated.", "Update Complete.", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				return true;
			}
			return false;
		}
	}
}
