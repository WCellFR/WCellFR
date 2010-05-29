/*************************************************************************
 *
 *   file		: Program.cs
 *   copyright		: (C) The WCell Team
 *   email		: info@wcell.org
 *   last changed	: $LastChangedDate: 2008-06-06 16:12:13 +0800 (Fri, 06 Jun 2008) $
 *   last author	: $LastChangedBy: domiii $
 *   revision		: $Rev: 455 $
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using MpqReader;
using WCell.Constants;
using WCell.MPQTool.DBC.Compare;
using WCell.Util;

namespace WCell.MPQTool
{
	public class DBCTool
	{
		//static string DBCOutputDir = string.Format(@"{0}\Content\dbc", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
		public static readonly string DBCDir = new DirectoryInfo(string.Format(@"../Content/dbc")).FullName;
		public static readonly string DefaultDBCOutputDir = DBCDir + WCellInfo.RequiredVersion.BasicString + "/";
		public static string DBCOutputDir = DefaultDBCOutputDir;
		public static DirectoryInfo DumpDir = new DirectoryInfo(string.Format("Output"));

		static string m_wowDir;

		public static string WowDir
		{
			get
			{
				return m_wowDir;
			}
		}

		#region Export
		/// <summary>
		/// Returns all MPQ Files that need processing for the DBC Files
		/// </summary>
		/// <param name="strDataFolder">The Folder in which the locale is</param>
		/// <returns>List of ALL MPQ Files that will have DBC Files in</returns>
		public static List<string> GetMPQFiles(string strDataFolder)
		{
			//Get all the MPQ Files inside the Locale Folder
			List<string> lstAllMPQs = GetFiles(string.Format(@"{0}\", strDataFolder));
			var lstFinalMPQs = new List<string>();

			var varFinalMPQs = from a in lstAllMPQs
			                   where a.Contains("locale-") || a.Contains("patch-")
			                   orderby a descending
			                   select a;

			foreach (var strFile in varFinalMPQs)
			{
				lstFinalMPQs.Add(strFile);
			}

			return lstFinalMPQs;
		}

		/// <summary>
		/// Find all files inside a folder
		/// </summary>
		/// <param name="strParentFolder">The folder to search inside</param>
		/// <returns>List of files inside this folder</returns>
		public static List<string> GetFiles(string strParentFolder)
		{
			string[] arrFiles = Directory.GetFiles(strParentFolder, "*.MPQ");

			return new List<string>(arrFiles);
		}

		public static void ProcessMPQ(List<string> lstAllMPQFiles)
		{
			// Create a folder to dump all this into
			Directory.CreateDirectory(DBCOutputDir);

			// Go through all the files, getting all DBCs
			for (int i = 0; i < lstAllMPQFiles.Count; i++)
			{
				using (var oArchive = new MpqArchive(lstAllMPQFiles[i]))
				{
					var dbcsFiles = from a in oArchive.Files
					                                    where a.Name.EndsWith(".dbc")
					                                    select a.Name;

					foreach (var strFileName in dbcsFiles)
					{
						var strLocalFilePath = string.Format(@"{0}\{1}", DBCOutputDir, Path.GetFileName(strFileName));

						// Does it already exist? If it does then it'll be one from a previous package, so let's leave it
						if (!File.Exists(strLocalFilePath))
						{
							using (Stream stmOutput = new FileStream(strLocalFilePath, FileMode.Create))
							{
								using (Stream stmInput = oArchive.OpenFile(strFileName))
								{
									// Writing...
									Console.Write(string.Format("Writing File {0}....", Path.GetFileName(strFileName)));

									// Create an 8kb buffer
									var byFileContents = new byte[8192];

									// Loop until we're out of data
									while (true)
									{
										// Read from the MPQ
										int intBytesRead = stmInput.Read(byFileContents, 0, byFileContents.Length);

										// Was there anything to read?
										if (intBytesRead == 0)
											break;

										// Write to the file
										stmOutput.Write(byFileContents, 0, intBytesRead);
									}
								}

								// Close the File
								stmOutput.Close();

								Console.WriteLine("Done");
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets the locale.
		/// </summary>
		/// <param name="strDataFolder">The Wow data folder.</param>
		/// <returns></returns>
		private static string GetLocale(string strDataFolder)
		{
			string strLocale = "";

			//Let's try and find a Locale folder...use the first one that has 4 characeters
			string[] arrFoundFolders = Directory.GetDirectories(strDataFolder);

			//Loop through all found
			for (int intFolder = 0; intFolder < arrFoundFolders.Length; intFolder++)
			{
				//Get this folders info
				var dirInfo = new DirectoryInfo(arrFoundFolders[intFolder]);

				//4 Characters long, could be enGB for example
				if (dirInfo.Name.Length == 4)
				{
					//Let's just use this one.
					strLocale = dirInfo.Name;
					//No point still looping
					break;
				}
			}

			return strLocale;
		}
		#endregion

		public static string FindWowDir()
		{
			return FindWowDir(null);
		}

		/// <summary>
		/// Looks for and returns the Wow dir
		/// </summary>
		/// <exception cref="Exception">If dir could not be found</exception>
		public static string FindWowDir(string wowDir)
		{
			if (wowDir != null)
			{
				return wowDir;
			}
			if (LookLocally() || LookInRegistry())
			{
				Console.WriteLine("Found WoW in: " + m_wowDir);
				return Path.GetFullPath(m_wowDir);
			}
			throw new Exception("Could not find WoW directory.");
		}

		public static void Dump()
		{
			Dump(null, true, true);
		}

		/// <summary>
		/// Looks up the wow dir (if not specified) and dumps the DBC files from there)
		/// </summary>
		/// <param name="wowDir"></param>
		/// <param name="clear">Whether to clear the DBC-dir</param>
		/// <param name="checkClient"></param>
		public static void Dump(string wowDir, bool clear, bool checkClient)
		{
			try
			{
				if (checkClient)
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("Required Client Version: " + WCellInfo.RequiredVersion);
					Console.ResetColor();
				}
				Console.WriteLine();

				m_wowDir = FindWowDir(wowDir);

				Console.WriteLine("Found WoW in: " + m_wowDir);

				string response;
				var curDir = new FileInfo(".");
				do
				{
					var outputDir = new DirectoryInfo(DBCOutputDir);
					Console.WriteLine("Output Directory: {0} ({1})", outputDir.FullName, outputDir.Exists ? "already exists" : "does not exist");
					Console.WriteLine("Do you want to export to that directory?");
					Console.WriteLine("Press y to confirm or n to re-enter destination.");
					response = Console.ReadLine();
					if (response == null)
					{
						// program shutdown
						return;
					}

					if (!response.StartsWith("y"))
					{
						Console.WriteLine("Current Directory (.): ");
						Console.WriteLine(curDir.FullName);
						Console.WriteLine("Please enter the Output Directory - You can also use a relative path.");
						DBCOutputDir = Console.ReadLine();
					}
				}
				while (!response.StartsWith("y"));


				Console.WriteLine("Exporting DBC files -");
				Console.WriteLine("To: " + DBCOutputDir);

				if (clear && Directory.Exists(DBCOutputDir))
				{
					Console.WriteLine();
					Console.Write("Clearing Ouput directory... ");
					Directory.Delete(DBCOutputDir, true);
					Console.WriteLine("Done.");
				}

				if (Export())
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine();
					Console.WriteLine("Done.");

					if (checkClient)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine();
						Console.WriteLine("Please make sure that you were exporting from Client v" + WCellInfo.RequiredVersion);
					}
					Console.ResetColor();
				}
			}

			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.Message);
				Console.WriteLine("Export failed. Make sure, you configured your build correctly.");
				Console.ResetColor();
			}
		}

		private static bool Export()
		{
			var dir = m_wowDir + @"\Data";

			// Is there even a Data folder?
			if (Directory.Exists(dir))
			{
				string strLocale = GetLocale(dir);

				// Did we find a locale?
				if (strLocale != string.Empty)
				{
					dir = string.Format(@"{0}\{1}", dir, strLocale);
				}
				else
				{
					//Nope
					throw new Exception("No Locale Folder was found");
				}
			}
			else
			{
				throw new Exception(string.Format("Invalid WoW installation. Could not find WoW in folder: {0}", dir));
			}

			// Get MPQ files
			List<string> lstAllMPQFiles = GetMPQFiles(dir);

			if (lstAllMPQFiles.Count > 0)
			{
				Console.WriteLine(string.Format("Found {0} MPQ's", lstAllMPQFiles.Count));
				ProcessMPQ(lstAllMPQFiles);
				return true;
			}
			throw new Exception("No Matching Locale Files were found");
		}

		private static bool LookLocally()
		{
			Console.Write("Checking Local folder...");

			m_wowDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

			const string wowExeName = "Wow.exe";

			// Check if we can get this file; means we're in the top 
			// directory and we have to append "Data" to the directory.

			var configInfo = new FileInfo(Path.Combine(m_wowDir, wowExeName));

			if (configInfo.Exists)
			{
				//DBCOutputDir = m_wowDir + "/DBC";
				return true;
			}

		    Console.ForegroundColor = ConsoleColor.Yellow;
		    Console.WriteLine("Local folder is not WoW folder.");
		    return false;
		}

		private static bool LookInRegistry()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Checking Install Folder...");

			// Check the Registry for an installed folder -- HKEY_LOCAL_MACHINE\SOFTWARE\Blizzard Entertainment\World of Warcraft            
			var key = Registry.LocalMachine.OpenSubKey("SOFTWARE");
			if (key == null)
			{
				return false;
			}

			if (!Environment.Is64BitOperatingSystem)
			{
				key = key.OpenSubKey("Blizzard Entertainment");
			}
			else
			{
				key = key.OpenSubKey("Wow6432Node");
				if (key == null)
				{
					return false;
				}
				key = key.OpenSubKey("Blizzard Entertainment");
			}

			// Any blizzard software?
			if (key != null)
			{
				// Just 'cos it's Blizzard, could be Diablo 2...and everyone should have that key.
				var oWoWKey = key.OpenSubKey("World of Warcraft");

				// So, did it exist?
				if (oWoWKey != null)
				{
					//Let's get the install folder...
					m_wowDir = oWoWKey.GetValue("InstallPath").ToString();
					return true;
				}

			    throw new Exception("Could not find any WoW installation.");
			}
			return false;
		}

		/// <param name="minColChangePct">The percentage of changed rows for a column to assume that it moved</param>
		/// <param name="minColMatchPct">The percentage of matching rows between 2 columns to assume that they are identical (col might have moved to that one)</param>
		public static void Compare(float minColChangePct, float minColMatchPct)
		{
			DBCFileComparer.MinColumnChangePct = minColChangePct;
			DBCFileComparer.MinColumnMatchPct = minColMatchPct;
			DumpDir.Create();
			var outputFile = Path.Combine(DumpDir.FullName, "DBCCompare.txt");
			Console.WriteLine("Writing Comparison Dump file to: \n" + outputFile);
			using (var writer = new StreamWriter(outputFile, false))
			{
				var oldDir = Path.Combine(new DirectoryInfo(DefaultDBCOutputDir).Parent.FullName, "dbc 2.3");
				var newDir = DefaultDBCOutputDir;
				var comparer = new DBCDirComparer(newDir, oldDir);
				comparer.Compare(writer);
			}
		}

		static void Main(string[] args)
		{
			Dump();
			// Compare(40f, 90f);
			//Dump(@"F:\games\wow\");
			Console.ResetColor();
			Console.WriteLine();
			Console.WriteLine("Press ANY key to continue...");
			Console.ReadKey();
		}

	}
}