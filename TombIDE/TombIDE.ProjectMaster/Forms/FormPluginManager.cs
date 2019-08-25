﻿using DarkUI.Controls;
using DarkUI.Forms;
using Microsoft.VisualBasic.FileIO;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TombIDE.Shared;
using TombLib.Projects;
using TombLib.Utils;

namespace TombIDE.ProjectMaster
{
	public partial class FormPluginManager : DarkForm
	{
		private IDE _ide;

		#region Initialization

		public FormPluginManager(IDE ide)
		{
			_ide = ide;
			_ide.IDEEventRaised += OnIDEEventRaised;

			InitializeComponent();

			_ide.RefreshPluginLists();
		}

		private void OnIDEEventRaised(IIDEEvent obj)
		{
			if (obj is IDE.PluginListsUpdatedEvent)
			{
				UpdateAvailablePluginsTreeView();
				UpdateInstalledPluginsTreeView();

				ResetUIElements();
			}
		}

		#endregion Initialization

		#region Events

		private void button_OpenArchive_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dialog = new OpenFileDialog())
			{
				dialog.Title = "Select the archive of the plugin you want to install";
				dialog.Filter = "All Supported Files|*.zip;*.rar;*.7z|ZIP Archives|*.zip|RAR Archives|*.rar|7-Zip Archives|*.7z";

				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					string filePath = dialog.FileName;
					string pluginsFolderPath = Path.Combine(SharedMethods.GetProgramDirectory(), "TRNG Plugins");

					try
					{
						if (!Directory.Exists(pluginsFolderPath))
							Directory.CreateDirectory(pluginsFolderPath);

						foreach (string directory in Directory.GetDirectories(pluginsFolderPath))
						{
							if (Path.GetFileName(directory.ToLower()) == Path.GetFileNameWithoutExtension(filePath.ToLower()))
								throw new ArgumentException("Plugin already installed.");
						}

						using (IArchive archive = ArchiveFactory.Open(filePath))
						{
							if (!IsValidPluginArchive(archive))
								throw new ArgumentException("Selected archive doesn't contain a valid plugin DLL file.");

							string extractionPath = Path.Combine(pluginsFolderPath, Path.GetFileNameWithoutExtension(filePath));

							foreach (IArchiveEntry entry in archive.Entries)
							{
								if (!entry.IsDirectory)
									entry.WriteToDirectory(extractionPath, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
							}

							ValidatePluginFolder(extractionPath, true);

							Plugin plugin = Plugin.InstallPluginFolder(extractionPath);

							// Check for name duplicates
							foreach (Plugin availablePlugin in _ide.AvailablePlugins)
							{
								if (availablePlugin.Name.ToLower() == plugin.Name.ToLower())
								{
									Directory.Delete(extractionPath, true);
									throw new ArgumentException("A plugin with the same name already exists on the list.");
								}
							}

							_ide.RefreshPluginLists();
						}
					}
					catch (Exception ex)
					{
						DarkMessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void button_OpenFolder_Click(object sender, EventArgs e)
		{
			using (BrowseFolderDialog dialog = new BrowseFolderDialog())
			{
				dialog.Title = "Select the folder of the plugin you want to install";

				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					string folderPath = dialog.Folder;
					string pluginsFolderPath = Path.Combine(SharedMethods.GetProgramDirectory(), "TRNG Plugins");

					try
					{
						if (!Directory.Exists(pluginsFolderPath))
							Directory.CreateDirectory(pluginsFolderPath);

						foreach (string directory in Directory.GetDirectories(pluginsFolderPath))
						{
							if (Path.GetFileName(directory.ToLower()) == Path.GetFileName(folderPath.ToLower()))
								throw new ArgumentException("Plugin already installed.");
						}

						if (!IsValidPluginFolder(folderPath))
							throw new ArgumentException("Selected folder doesn't contain a valid plugin DLL file.");

						string extractionPath = Path.Combine(pluginsFolderPath, Path.GetFileName(folderPath));

						Directory.CreateDirectory(extractionPath);

						// Create all of the subdirectories
						foreach (string dirPath in Directory.GetDirectories(folderPath, "*", System.IO.SearchOption.AllDirectories))
							Directory.CreateDirectory(dirPath.Replace(folderPath, extractionPath));

						// Copy all the files into the folder
						foreach (string newPath in Directory.GetFiles(folderPath, "*.*", System.IO.SearchOption.AllDirectories))
							File.Copy(newPath, newPath.Replace(folderPath, extractionPath));

						ValidatePluginFolder(extractionPath, true);

						Plugin plugin = Plugin.InstallPluginFolder(extractionPath);

						// Check for name duplicates
						foreach (Plugin availablePlugin in _ide.AvailablePlugins)
						{
							if (availablePlugin.Name.ToLower() == plugin.Name.ToLower())
							{
								Directory.Delete(extractionPath, true);
								throw new ArgumentException("A plugin with the same name already exists on the list.");
							}
						}

						_ide.RefreshPluginLists();
					}
					catch (Exception ex)
					{
						DarkMessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void button_Delete_Click(object sender, EventArgs e)
		{
			Plugin affectedPlugin = (Plugin)treeView_Available.SelectedNodes[0].Tag;

			DialogResult result = DarkMessageBox.Show(this, "Are you sure you want to delete the \"" + affectedPlugin.Name + "\" plugin?\n" +
					"This will send the plugin folder with all its files into the recycle bin.", "Are you sure?",
					MessageBoxButtons.YesNo, MessageBoxIcon.Question);

			if (result == DialogResult.Yes)
				FileSystem.DeleteDirectory(Path.GetDirectoryName(affectedPlugin.InternalDllPath), UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

			// The lists will refresh themself, because ProjectMaster.cs is watching the plugin folders
		}

		private void button_Refresh_Click(object sender, EventArgs e) => _ide.RefreshPluginLists();

		private void button_Install_Click(object sender, EventArgs e)
		{
			foreach (DarkTreeNode node in treeView_Available.SelectedNodes)
			{
				try
				{
					string dllFilePath = ((Plugin)node.Tag).InternalDllPath;
					string destPath = Path.Combine(_ide.Project.ProjectPath, Path.GetFileName(dllFilePath));

					File.Copy(dllFilePath, destPath, true);

					// The lists will refresh themself, because ProjectMaster.cs is watching the plugin folders
				}
				catch (Exception ex)
				{
					DarkMessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void button_Uninstall_Click(object sender, EventArgs e)
		{
			foreach (DarkTreeNode node in treeView_Installed.SelectedNodes)
			{
				try
				{
					string dllFilePath = ((Plugin)node.Tag).InternalDllPath;

					if (string.IsNullOrEmpty(dllFilePath))
					{
						DialogResult result = DarkMessageBox.Show(this, "The \"" + ((Plugin)node.Tag).Name + "\" plugin is not installed in TombIDE.\n" +
							"Would you like to move the DLL file into the recycle bin instead?", "Are you sure?",
							MessageBoxButtons.YesNo, MessageBoxIcon.Question);

						if (result == DialogResult.Yes)
							FileSystem.DeleteFile(Path.Combine(_ide.Project.ProjectPath, ((Plugin)node.Tag).Name), UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
					}
					else
					{
						string dllProjectPath = Path.Combine(_ide.Project.ProjectPath, Path.GetFileName(dllFilePath));

						if (File.Exists(dllProjectPath))
							File.Delete(dllProjectPath);
					}

					// The lists will refresh themself, because ProjectMaster.cs is watching the plugin folders
				}
				catch (Exception ex)
				{
					DarkMessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void button_Download_Click(object sender, EventArgs e) => Process.Start("https://www.tombraiderforums.com/showpost.php?p=7636390");

		private void button_OpenInExplorer_Click(object sender, EventArgs e)
		{
			string pluginFolderPath = string.Empty;

			if (treeView_Installed.SelectedNodes.Count == 1)
				pluginFolderPath = Path.GetDirectoryName(((Plugin)treeView_Installed.SelectedNodes[0].Tag).InternalDllPath);
			else if (treeView_Available.SelectedNodes.Count == 1)
				pluginFolderPath = Path.GetDirectoryName(((Plugin)treeView_Available.SelectedNodes[0].Tag).InternalDllPath);

			SharedMethods.OpenFolderInExplorer(pluginFolderPath);
		}

		private void treeView_Available_SelectedNodesChanged(object sender, EventArgs e)
		{
			if (treeView_Available.SelectedNodes.Count == 0)
				return;

			treeView_Installed.SelectedNodes.Clear();
			treeView_Installed.Invalidate();

			button_Install.Enabled = true;
			button_Uninstall.Enabled = false;

			button_Delete.Enabled = treeView_Available.SelectedNodes.Count == 1;
			button_OpenInExplorer.Enabled = treeView_Available.SelectedNodes.Count == 1;
		}

		private void treeView_Installed_SelectedNodesChanged(object sender, EventArgs e)
		{
			if (treeView_Installed.SelectedNodes.Count == 0)
				return;

			treeView_Available.SelectedNodes.Clear();
			treeView_Available.Invalidate();

			button_Install.Enabled = false;
			button_Uninstall.Enabled = true;

			button_Delete.Enabled = false;
			button_OpenInExplorer.Enabled = treeView_Installed.SelectedNodes.Count == 1;
		}

		#endregion Events

		#region Methods

		private void UpdateAvailablePluginsTreeView()
		{
			treeView_Available.Nodes.Clear();

			foreach (Plugin availablePlugin in _ide.AvailablePlugins)
			{
				bool isPluginInstalled = false;

				foreach (Plugin installedPlugin in _ide.Project.InstalledPlugins)
				{
					if (availablePlugin.InternalDllPath == installedPlugin.InternalDllPath)
					{
						isPluginInstalled = true;
						break;
					}
				}

				if (isPluginInstalled)
					continue;

				DarkTreeNode node = new DarkTreeNode(availablePlugin.Name)
				{
					Tag = availablePlugin
				};

				treeView_Available.Nodes.Add(node);
			}

			treeView_Available.Invalidate();
		}

		private void UpdateInstalledPluginsTreeView()
		{
			treeView_Installed.Nodes.Clear();

			foreach (Plugin plugin in _ide.Project.InstalledPlugins)
			{
				DarkTreeNode node = new DarkTreeNode(plugin.Name)
				{
					Tag = plugin
				};

				treeView_Installed.Nodes.Add(node);
			}

			treeView_Installed.Invalidate();
		}

		private void ResetUIElements()
		{
			treeView_Installed.SelectedNodes.Clear();
			treeView_Installed.Invalidate();

			treeView_Available.SelectedNodes.Clear();
			treeView_Available.Invalidate();

			button_Delete.Enabled = false;

			button_Install.Enabled = false;
			button_Uninstall.Enabled = false;

			button_OpenInExplorer.Enabled = false;
		}

		private void ValidatePluginFolder(string pluginFolderPath, bool extractedFromArchive = false)
		{
			if (Directory.GetFileSystemEntries(pluginFolderPath).Length == 1)
			{
				string nextFolderPath = Path.Combine(pluginFolderPath, Path.GetFileName(Directory.GetFileSystemEntries(pluginFolderPath).First()));
				string cachedFolderPath = nextFolderPath;

				while (Directory.GetFileSystemEntries(nextFolderPath).Length == 1)
				{
					string nextEntry = Path.Combine(nextFolderPath, Path.GetFileName(Directory.GetFileSystemEntries(nextFolderPath).First()));

					if ((File.GetAttributes(nextEntry) & FileAttributes.Directory) == FileAttributes.Directory)
					{
						nextFolderPath = nextEntry;
						continue;
					}
					else if (!Path.GetFileName(nextEntry).ToLower().StartsWith("plugin_") || !Path.GetFileName(nextEntry).ToLower().EndsWith(".dll"))
					{
						Directory.Delete(pluginFolderPath, true);
						throw new ArgumentException(string.Format("Selected {0} doesn't contain a valid plugin DLL file.",
							extractedFromArchive ? "archive" : "folder"));
					}

					break;
				}

				if (!IsValidPluginFolder(nextFolderPath))
				{
					Directory.Delete(pluginFolderPath, true);
					throw new ArgumentException(string.Format("Selected {0} doesn't contain a valid plugin DLL file.",
						extractedFromArchive ? "archive" : "folder"));
				}

				foreach (string file in Directory.GetFiles(nextFolderPath))
					File.Move(file, Path.Combine(pluginFolderPath, Path.GetFileName(file)));

				foreach (string directory in Directory.GetDirectories(nextFolderPath))
					Directory.Move(directory, Path.Combine(pluginFolderPath, Path.GetFileName(directory)));

				Directory.Delete(cachedFolderPath, true);
			}

			if (Directory.GetFiles(pluginFolderPath, "plugin*.dll").Length == 0)
			{
				Directory.Delete(pluginFolderPath, true);
				throw new ArgumentException(string.Format("Couldn't find a valid plugin DLL file in the first directory of the {0}.",
					extractedFromArchive ? "archive" : "folder"));
			}

			// Check for DLL duplicates
			foreach (Plugin plugin in _ide.AvailablePlugins)
			{
				string existingDLLFileName = Path.GetFileName(plugin.InternalDllPath.ToLower());
				string installedDLLFileName = Path.GetFileName(Directory.GetFiles(pluginFolderPath, "plugin_*.dll").First().ToLower());

				if (existingDLLFileName == installedDLLFileName)
				{
					Directory.Delete(pluginFolderPath, true);
					throw new ArgumentException(string.Format("Selected {0} has the same DLL file as the\n" +
						"\"{1}\" plugin.", extractedFromArchive ? "archive" : "folder", plugin.Name));
				}
			}
		}

		private bool IsValidPluginArchive(IArchive archive)
		{
			foreach (IArchiveEntry entry in archive.Entries)
			{
				if (Path.GetFileName(entry.Key).ToLower().StartsWith("plugin_") && Path.GetFileName(entry.Key).ToLower().EndsWith(".dll"))
					return true;
			}

			return false;
		}

		private bool IsValidPluginFolder(string path)
		{
			return Directory.GetFiles(path, "plugin_*.dll", System.IO.SearchOption.AllDirectories).Length > 0;
		}

		#endregion Methods
	}
}
