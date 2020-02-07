﻿using MahApps.Metro.Controls;
using Miharu.BackEnd;
using Miharu.BackEnd.Data;
using Miharu.BackEnd.Translation.WebCrawlers;
using Miharu.FrontEnd;
using Miharu.FrontEnd.Helper;
using Miharu.FrontEnd.Page;
using Miharu.FrontEnd.TextEntry;
using Miharu.Properties;
using Ookii.Dialogs.Wpf;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Miharu.BackEnd.Ripper;

namespace Miharu {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow {

		private const string _SAVE = "Save";
		private const string _DISCARD = "Discard";
		private const string _CANCEL = "Cancel";


		public Chapter LoadedChapter = null;

		private int _currentPage = 0;
		private int _previousPage = 0;

		private bool _saved = false;
		private bool _openChapterOnLoad = false;

		public string CurrentSaveFile = null;
		private string _currSavedScript = null;
		private string _currSavedJPScript = null;
		private string _currSavedCompleteScript = null;

		private double _dpiX;
		private double _dpiY;




		private bool Saved {
			get => _saved;
			set {
				_saved = value;
				string title = "";
				if (LoadedChapter != null) {
					if (CurrentSaveFile != null) {
						int slashIndex = CurrentSaveFile.LastIndexOf("\\") + 1;
						int extensionIndex = CurrentSaveFile.LastIndexOf(".");
						title += CurrentSaveFile.Substring(slashIndex, extensionIndex - slashIndex);
					}
					else
						title += "untitled";
					if (!_saved)
						title += "*";
					title += " - Miharu Scan Helper";
				}
				else
					title += "Miharu Scan Helper";
				Title = title;
			}
		}

		private bool ExistsInPath (string fileName) {
			bool res = false;

			string path = Environment.GetEnvironmentVariable("PATH");
			string [] values = path.Split(Path.PathSeparator);
			for (int i = 0; i < values.Length && !res; i++) {
				var fullPath = Path.Combine(values[i], fileName);
				res = File.Exists(fullPath);
			}

			return res;
		}

		private void CheckForTesseract() {
			if (!File.Exists((string)Settings.Default["TesseractPath"])) {
				if (ExistsInPath("tesseract.exe")) {
					if ((string) Settings.Default ["TesseractPath"] == "tesseract.exe")
						return;
					Settings.Default ["TesseractPath"] = "tesseract.exe";
					Settings.Default.Save();
					return;
				}
				TaskDialog dialog = new TaskDialog();
				dialog.WindowTitle = "Warning Tesseract Not Found";
				dialog.MainIcon = TaskDialogIcon.Warning;
				dialog.MainInstruction = "Tesseract executable could not be located.";
				dialog.Content = @"Miharu requires Tesseract to function.
Would you like to locate the Tesseract exectutable manually?";

				TaskDialogButton okButton = new TaskDialogButton(ButtonType.Yes);
				dialog.Buttons.Add(okButton);
				TaskDialogButton cancelButton = new TaskDialogButton(ButtonType.No);
				dialog.Buttons.Add(cancelButton);
				TaskDialogButton button = dialog.ShowDialog(this);
				if (button.ButtonType == ButtonType.No) {
					Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
					Application.Current.Shutdown(0);
					return;
				}

				VistaOpenFileDialog fileDialog = new VistaOpenFileDialog();
				fileDialog.AddExtension = true;
				fileDialog.CheckFileExists = true;
				fileDialog.CheckPathExists = true;
				fileDialog.DefaultExt = ".exe";
				fileDialog.Filter = "Tesseract (tesseract.exe)|tesseract.exe";
				fileDialog.Multiselect = false;
				fileDialog.Title = "Select Tesseract Executable";
				bool? res = fileDialog.ShowDialog(this);
				if (res ?? false) {
					Settings.Default ["TesseractPath"] = fileDialog.FileName;
					Settings.Default.Save();
				}
				else {
					Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
					Application.Current.Shutdown(0);
					return;
				}
			}
		}

		

		public MainWindow () {
			InitializeComponent();

			

			Graphics g = Graphics.FromHwnd(IntPtr.Zero);
			_dpiX = g.DpiX;
			_dpiY = g.DpiY;

			ChangePage();

			

			string [] args = Environment.GetCommandLineArgs();


			System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory.ToString());
			CheckForTesseract();
			
			if (CrashHandler.LastSessionCrashed) {
				TaskDialog dialog = new TaskDialog();
				dialog.WindowTitle = "Warning";
				dialog.MainIcon = TaskDialogIcon.Warning;
				dialog.MainInstruction = "It seems like a file can be recovered from last session.";
				dialog.Content = "Would you like to attempt to recover the file?";
				TaskDialogButton saveButton = new TaskDialogButton(ButtonType.Yes);
				saveButton.Text = "Yes";
				dialog.Buttons.Add(saveButton);
				TaskDialogButton noSaveButton = new TaskDialogButton(ButtonType.No);
				noSaveButton.Text = "No";
				dialog.Buttons.Add(noSaveButton);
				TaskDialogButton button = dialog.ShowDialog(this);
				string temp = CrashHandler.RecoverLastSessionFile();
				if (button.ButtonType == ButtonType.Yes) {
					CurrentSaveFile = temp;
					_openChapterOnLoad = true;
				}
			}
			else if (args.Length > 1 && System.IO.File.Exists(args [1])) {
				CurrentSaveFile = args [1];
				_openChapterOnLoad = true;
			}

			
			Task.Run(() => {
				WebDriverManager.Init();
				if (!WebDriverManager.IsAlive) {
					
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Warning";
					dialog.MainIcon = TaskDialogIcon.Warning;
					dialog.MainInstruction = "Could not load Firefox Web Driver.";
					dialog.Content = "To access more and better translation sources, Firefox Web Browser must be installed in your system.";

					TaskDialogButton okButton = new TaskDialogButton("Ok");
					okButton.ButtonType = ButtonType.Ok;
					dialog.Buttons.Add(okButton);
					Application.Current.Dispatcher.Invoke((Action)delegate{
						TaskDialogButton button = dialog.ShowDialog(this);
					});
					
				}
			});

		}


		#region TopMenu

		private void SetTopMenuItems () {
			bool set = LoadedChapter != null;

			SaveChapterMenuItem.IsEnabled = set;
			SaveAsChapterMenuItem.IsEnabled = set;

			CloseChapterMenuItem.IsEnabled = set;

			ExportTSScriptMenuItem.IsEnabled = set;
			ExportAsTSScriptMenuItem.IsEnabled = set;

			ExportJPScriptMenuItem.IsEnabled = set;
			ExportAsJPScriptMenuItem.IsEnabled = set;

			ExportRScriptMenuItem.IsEnabled = set;
			ExportAsRScriptMenuItem.IsEnabled = set;

			EditPagesMenuItem.IsEnabled = set;

		}

		private void MainWindow1_KeyDown (object sender, KeyEventArgs e) {
			if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control && !Saved && LoadedChapter != null)
				SaveChapterMenuItem_Click(sender, e);
		}

		private void OpenChapter (string file) {
			try {
				Mouse.SetCursor(Cursors.Wait);
				int page = 0;
				string finalFile = file;
				LoadedChapter = Chapter.Load(file, out page, out finalFile);
				if (LoadedChapter == null)
					throw new Exception("Failed to open chapter from file: " + file);
				LoadChapter(page);
				foreach (Page p in LoadedChapter.Pages) {
					p.PageChanged += OnItemChange;
					foreach (Text t in p.TextEntries)
						t.TextChanged += OnItemChange;
				}
				CurrentSaveFile = finalFile;
				Saved = true;
				GC.Collect();
				Mouse.SetCursor(Cursors.Arrow);
			}
			catch (Exception ex) {
				Mouse.SetCursor(Cursors.Arrow);
				LoadedChapter = null;
				_currentPage = 0;
				TaskDialog dialog = new TaskDialog();
				dialog.WindowTitle = "Error";
				dialog.MainIcon = TaskDialogIcon.Error;
				dialog.MainInstruction = "Loading error.";
				dialog.Content = ex.Message;
				TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
				dialog.Buttons.Add(okButton);
				TaskDialogButton button = dialog.ShowDialog(this);

			}
		}

		private void LoadChapter (int page = 0) {

			AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(PreviewIMG);
			if (_pageRectangles? [_previousPage] != null)
				adornerLayer.Remove(_pageRectangles [_previousPage]);
			_pageRectangles? [_currentPage].InvalidateVisual();

			_pageRectangles = new RectangleAdorner [LoadedChapter.Pages.Count];

			_previousPage = page;
			_currentPage = page;
			ChangePage();

			CurrentSaveFile = null;
			Saved = false;
			_currSavedScript = null;

			SetTopMenuItems();
		}

		private string WarnNotSaved () {
			TaskDialog dialog = new TaskDialog();
			dialog.WindowTitle = "Warning";
			dialog.MainIcon = TaskDialogIcon.Warning;
			dialog.MainInstruction = "There are unsaved changes.";
			dialog.Content = "Would you like to save the current chapter?";

			TaskDialogButton saveButton = new TaskDialogButton(_SAVE);
			dialog.Buttons.Add(saveButton);
			TaskDialogButton noSaveButton = new TaskDialogButton(_DISCARD);
			dialog.Buttons.Add(noSaveButton);
			TaskDialogButton cancelButton = new TaskDialogButton(_CANCEL);
			dialog.Buttons.Add(cancelButton);
			TaskDialogButton button = dialog.ShowDialog(this);

			return button.Text;
		}



		private void NewChapterFolderMenuItem_Click (object sender, RoutedEventArgs e) {
			if (LoadedChapter != null && !Saved) {
				string warnRes = WarnNotSaved();
				if (warnRes == _SAVE)
					SaveChapterMenuItem_Click(sender, e);
				else if (warnRes == _CANCEL)
					return;
			}


			VistaFolderBrowserDialog folderDialog = new VistaFolderBrowserDialog();
			bool? res = folderDialog.ShowDialog(this);
			if (res ?? false) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter = new Chapter(folderDialog.SelectedPath);
					LoadChapter();
					foreach (Page p in LoadedChapter.Pages)
						p.PageChanged += OnItemChange;
					GC.Collect();
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					LoadedChapter = null;
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "No images found.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);

				}
			}
		}

		private void NewChapterFilesMenuItem_Click (object sender, RoutedEventArgs e) {
			if (LoadedChapter != null && !Saved) {
				string warnRes = WarnNotSaved();
				if (warnRes == _SAVE)
					SaveChapterMenuItem_Click(sender, e);
				else if (warnRes == _CANCEL)
					return;
			}

			VistaOpenFileDialog filesDialog = new VistaOpenFileDialog();
			filesDialog.Multiselect = true;
			filesDialog.Title = "Select Images";
			filesDialog.AddExtension = true;
			filesDialog.CheckFileExists = true;
			filesDialog.CheckPathExists = true;
			filesDialog.DefaultExt = ".png";
			filesDialog.Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";
			bool? res = filesDialog.ShowDialog(this);
			if (res ?? false) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter = new Chapter(filesDialog.FileNames);
					LoadChapter();
					foreach (Page p in LoadedChapter.Pages)
						p.PageChanged += OnItemChange;
					GC.Collect();
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					LoadedChapter = null;
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "No images found.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);

				}
			}
		}

		private void OpenChapterMenuItem_Click (object sender, RoutedEventArgs e) {
			if (LoadedChapter != null && !Saved) {
				string warnRes = WarnNotSaved();
				if (warnRes == _SAVE)
					SaveChapterMenuItem_Click(sender, e);
				else if (warnRes == _CANCEL)
					return;
			}

			VistaOpenFileDialog fileDialog = new VistaOpenFileDialog();
			fileDialog.AddExtension = true;
			fileDialog.CheckFileExists = true;
			fileDialog.CheckPathExists = true;
			fileDialog.DefaultExt = ".scan";
			fileDialog.Filter = "Scans files (*.scan)|*.scan";
			fileDialog.Multiselect = false;
			fileDialog.Title = "Open Chapter";
			bool? res = fileDialog.ShowDialog(this);
			if (res ?? false) {
				OpenChapter(fileDialog.FileName);
			}
		}

		private void CloseChapterMenuItem_Click (object sender, RoutedEventArgs e) {
			if (!Saved) {
				string res = WarnNotSaved();
				if (res == _SAVE)
					SaveChapterMenuItem_Click(sender, e);
				else if (res == _CANCEL)
					return;
			}

			Mouse.SetCursor(Cursors.Wait);
			AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(PreviewIMG);
			if (_pageRectangles [_previousPage] != null)
				adornerLayer.Remove(_pageRectangles [_previousPage]);
			_pageRectangles [_currentPage].InvalidateVisual();
			PreviewIMG.Source = null;
			PreviewIMG.InvalidateVisual();

			TextEntriesStackPanel.Children.Clear();
			TextEntriesStackPanel.InvalidateVisual();

			_selectedTextEntry = null;
			TextEntryGrid.Children.Clear();
			TextEntryGrid.InvalidateVisual();

			LoadedChapter = null;
			_pageRectangles = null;
			_currentPage = 0;
			ChangePage();

			CurrentSaveFile = null;
			Saved = false;

			SetTopMenuItems();

			GC.Collect();
			Mouse.SetCursor(Cursors.Arrow);
		}


		private void SaveChapterMenuItem_Click (object sender, RoutedEventArgs e) {
			if (CurrentSaveFile != null) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter.Save(CurrentSaveFile, _currentPage);
					Saved = true;
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					Saved = false;
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "There was an error saving the chapter.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);
				}
			}
			else
				SaveAsChapterMenuItem_Click(sender, e);
		}

		private void SaveAsChapterMenuItem_Click (object sender, RoutedEventArgs e) {
			string previousSavedFile = CurrentSaveFile;
			VistaSaveFileDialog fileDialog = new VistaSaveFileDialog();
			fileDialog.AddExtension = true;
			fileDialog.DefaultExt = ".scan";
			fileDialog.Filter = "Scans files (*.scan)|*.scan";
			fileDialog.OverwritePrompt = true;
			fileDialog.Title = "Save Chapter";
			bool? res = fileDialog.ShowDialog(this);
			if (res ?? false) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter.Save(fileDialog.FileName, _currentPage);
					CurrentSaveFile = fileDialog.FileName;
					Saved = true;
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					CurrentSaveFile = previousSavedFile;
					Saved = false;
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "There was an error saving the chapter.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);
				}
			}
		}


		private void ExportScriptMenuItem_Click (object sender, RoutedEventArgs e) {
			if (_currSavedScript != null) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter.ExportScript(_currSavedScript);
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "There was an error exporting the script.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);
				}
			}
			else
				ExportAsScriptMenuItem_Click(sender, e);
		}

		private void ExportAsScriptMenuItem_Click (object sender, RoutedEventArgs e) {
			string previousSavedFile = _currSavedScript;
			VistaSaveFileDialog fileDialog = new VistaSaveFileDialog();
			fileDialog.AddExtension = true;
			fileDialog.DefaultExt = ".txt";
			fileDialog.Filter = "Script files (*.txt)|*.txt";
			fileDialog.OverwritePrompt = true;
			fileDialog.Title = "Export Script";
			bool? res = fileDialog.ShowDialog(this);
			if (res ?? false) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter.ExportScript(fileDialog.FileName);
					_currSavedScript = fileDialog.FileName;
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					_currSavedScript = previousSavedFile;
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "There was an error exporting the script.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);
				}
			}
		}

		private void ExportJPScriptMenuItem_Click (object sender, RoutedEventArgs e) {
			if (_currSavedJPScript != null) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter.ExportJPScript(_currSavedJPScript);
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "There was an error exporting the transcription.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);
				}
			}
			else
				ExportAsJPScriptMenuItem_Click(sender, e);
		}

		private void ExportAsJPScriptMenuItem_Click (object sender, RoutedEventArgs e) {
			string previousSavedFile = _currSavedJPScript;
			VistaSaveFileDialog fileDialog = new VistaSaveFileDialog();
			fileDialog.AddExtension = true;
			fileDialog.DefaultExt = ".txt";
			fileDialog.Filter = "Transcription files (*.txt)|*.txt";
			fileDialog.OverwritePrompt = true;
			fileDialog.Title = "Export Transcription";
			bool? res = fileDialog.ShowDialog(this);
			if (res ?? false) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter.ExportJPScript(fileDialog.FileName);
					_currSavedJPScript = fileDialog.FileName;
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					_currSavedJPScript = previousSavedFile;
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "There was an error exporting the transcription.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);
				}
			}
		}

		private void ExportCompleteMenuItem_Click (object sender, RoutedEventArgs e) {
			if (_currSavedCompleteScript != null) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter.ExportCompleteScript(_currSavedCompleteScript);
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "There was an error exporting the complete script.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);
				}
			}
			else
				ExportAsCompleteMenuItem_Click(sender, e);
		}

		private void ExportAsCompleteMenuItem_Click (object sender, RoutedEventArgs e) {
			string previousSavedFile = _currSavedCompleteScript;
			VistaSaveFileDialog fileDialog = new VistaSaveFileDialog();
			fileDialog.AddExtension = true;
			fileDialog.DefaultExt = ".txt";
			fileDialog.Filter = "Script files (*.txt)|*.txt";
			fileDialog.OverwritePrompt = true;
			fileDialog.Title = "Export Complete Script";
			bool? res = fileDialog.ShowDialog(this);
			if (res ?? false) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter.ExportCompleteScript(fileDialog.FileName);
					_currSavedCompleteScript = fileDialog.FileName;
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					_currSavedCompleteScript = previousSavedFile;
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "There was an error exporting the complete script.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);
				}
			}
		}


		private void ExitMenuItem_Click (object sender, RoutedEventArgs e) {

			if (LoadedChapter != null && !Saved) {
				string res = WarnNotSaved();
				if (res == _SAVE)
					SaveChapterMenuItem_Click(sender, new RoutedEventArgs());
				else if (res == _CANCEL) {
					return;
				}
			}
			Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
			Application.Current.Shutdown(0);
		}



		private void EditPagesMenuItem_Click (object sender, RoutedEventArgs e) {
			if (!LoadedChapter.AllPagesReady) {
				Mouse.SetCursor(Cursors.Wait);
				LoadedChapter.ChapterWaitHandle.WaitOne();
				Mouse.SetCursor(Cursors.Arrow);
			}
			EditChapterWindow editPagesDialog = new EditChapterWindow(LoadedChapter, _currentPage);
			editPagesDialog.Owner = this;
			editPagesDialog.ShowDialog();
			Mouse.SetCursor(Cursors.Wait);
			LoadChapter(editPagesDialog.SelectedIndex);
			GC.Collect();
			Mouse.SetCursor(Cursors.Arrow);
		}


		private void PreferencesMenuItem_Click (object sender, RoutedEventArgs e) {
			PreferencesDialog pd = new PreferencesDialog();
			pd.Owner = this;
			pd.ShowDialog();
		}




		private void RipMenuItem_Click (object sender, RoutedEventArgs e) {
			if (LoadedChapter != null && !Saved) {
				string res = WarnNotSaved();
				if (res == _SAVE)
					SaveChapterMenuItem_Click(sender, e);
				else if (res == _CANCEL)
					return;
			}
			RipDialog ripDialog = new RipDialog();
			ripDialog.Owner = this;
			ripDialog.ShowDialog();
			if (ripDialog.Success) {
				try {
					Mouse.SetCursor(Cursors.Wait);
					string dest = Ripper.FileRip(ripDialog.File, ripDialog.DestinationPath);
					LoadedChapter = new Chapter(dest);
					LoadChapter();
					foreach (Page p in LoadedChapter.Pages)
						p.PageChanged += OnItemChange;
					GC.Collect();
					Mouse.SetCursor(Cursors.Arrow);
				}
				catch (RipperException ex) {
					Mouse.SetCursor(Cursors.Arrow);
					LoadedChapter = null;
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "There was an error while ripping.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);
				}
				catch (Exception ex) {
					Mouse.SetCursor(Cursors.Arrow);
					LoadedChapter = null;
					TaskDialog dialog = new TaskDialog();
					dialog.WindowTitle = "Error";
					dialog.MainIcon = TaskDialogIcon.Error;
					dialog.MainInstruction = "No images found.";
					dialog.Content = ex.Message;
					TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
					dialog.Buttons.Add(okButton);
					TaskDialogButton button = dialog.ShowDialog(this);

				}
			}
		}

		private void CrashSimulatorItemItem_Click(object sender, RoutedEventArgs e)
		{
			throw new Exception("Simulated Crash");
		}




		private void AboutMenuItem_Click (object sender, RoutedEventArgs e) {
			AboutDialog aboutDialog = new AboutDialog();
			aboutDialog.Owner = this;
			aboutDialog.ShowDialog();
		}

		#endregion



		#region PagePanel
		private string _previousCurrPageTBText;

		private BitmapSource ChangeImageDPI (BitmapImage src) {
			int width = src.PixelWidth;
			int height = src.PixelHeight;
			PixelFormat pxFormat = src.Format;

			int stride = width * 4; // 4 bytes per pixel
			byte [] pixelData = new byte [stride * height];
			src.CopyPixels(pixelData, stride, 0);

			BitmapSource result = BitmapSource.Create(width, height, _dpiX, _dpiY, pxFormat, null, pixelData, stride);

			return result;
		}

		private void ChangePage () {
			if (CurrPageTextBox.IsEnabled = (LoadedChapter != null)) {

				BitmapImage imgSrc = new BitmapImage();
				imgSrc.BeginInit();
				imgSrc.UriSource = new Uri(LoadedChapter.Pages[_currentPage].Path, UriKind.Relative);
				imgSrc.CacheOption = BitmapCacheOption.OnLoad;
				imgSrc.EndInit();

				if (imgSrc.DpiX != _dpiX || imgSrc.DpiY != _dpiY)
					PreviewIMG.Source = ChangeImageDPI(imgSrc);
				else
					PreviewIMG.Source = imgSrc;

				PreviewIMGScroll.ScrollToTop();
				PreviewIMGScroll.ScrollToRightEnd();

				if (_pageRectangles[_currentPage] == null) {
					_pageRectangles[_currentPage] = new RectangleAdorner(PreviewIMG, LoadedChapter.Pages[_currentPage].TextEntries);
					_pageRectangles [_currentPage].IsHitTestVisible = false;
				}

				AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(PreviewIMG);
				if (_pageRectangles [_previousPage] != null)
					adornerLayer.Remove(_pageRectangles [_previousPage]);
				adornerLayer.Add(_pageRectangles [_currentPage]);
				_pageRectangles [_currentPage].InvalidateVisual();

				//_imageProcessing = new ImageProcessing (_loadedChapter.Pages[_currentPage].Path, rects);

				if (!LoadedChapter.Pages [_currentPage].Ready) {
					Mouse.SetCursor(Cursors.Wait);
					LoadedChapter.Pages [_currentPage].PageWaitHandle.WaitOne();
					Mouse.SetCursor(Cursors.Arrow);
				}

				TextEntryGrid.Children.Clear();
				if (_pageRectangles[_currentPage].SelectedRect != -1)
					TextEntryGrid.Children.Add(new TextEntryControl(LoadedChapter.Pages[_currentPage].TextEntries[_pageRectangles[_currentPage].SelectedRect]));
				TextEntriesStackPanel.Children.Clear();
				for (int i = 0; i < LoadedChapter.Pages[_currentPage].TextEntries.Count; i++) {
					TextEntriesStackPanel.Children.Add(new TextEntryListView(LoadedChapter.Pages[_currentPage].TextEntries[i],
																	this));
				}
			}
			else {
				_currentPage = 0;
			}
			int totalPages = 0;
			if (LoadedChapter != null) {
				totalPages = LoadedChapter.TotalPages;
				CurrPageLabel.Content = "Page: " + (LoadedChapter.Pages[_currentPage].Name);
			}
			else {
				CurrPageLabel.Content = "";
			}
			CurrPageTextBox.Text = (_currentPage + 1) + " / " + totalPages;
			_previousCurrPageTBText = CurrPageTextBox.Text;
			PrevPageButton.IsEnabled = _currentPage > 0;
			NextPageButton.IsEnabled = _currentPage < totalPages - 1;

			_previousPage = _currentPage;
			//Translator.Cancel();
		}


		private void NextPageButton_Click (object sender, RoutedEventArgs e) {
			if (_currentPage < LoadedChapter.TotalPages - 1)
				_currentPage++;
			ChangePage();
		}

		private void PrevPageButton_Click (object sender, RoutedEventArgs e) {
			if (_currentPage > 0)
				_currentPage--;
			ChangePage();
		}

		private void CurrPageTextBox_LostFocus (object sender, RoutedEventArgs e) {
			CurrPageTextBox.Text = _previousCurrPageTBText;
			Keyboard.ClearFocus();
		}

		private void CurrPageTextBox_PreviewKeyDown (object sender, KeyEventArgs e) {
			if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return) {
				e.Handled = true;
			}
		}

		private void CurrPageTextBox_PreviewKeyUp (object sender, KeyEventArgs e) {
			if (_previousCurrPageTBText != CurrPageTextBox.Text && (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)) {
				ParseCurrPageTextBox();
				Keyboard.ClearFocus();
				e.Handled = true;
			}
			else if (e.Key == System.Windows.Input.Key.Escape) {
				Keyboard.ClearFocus();
				CurrPageTextBox.Text = _previousCurrPageTBText;
				e.Handled = true;
			}

		}



		private void ParseCurrPageTextBox () {
			string significant = CurrPageTextBox.Text;
			if (significant.Contains("/"))
				significant = significant.Substring(0, significant.IndexOf('/'));
			int result = _currentPage;
			if (Int32.TryParse(significant, out result) && result > 0 && result <= LoadedChapter.Pages.Count) {
				_currentPage = result -1;
				ChangePage();
			}
			else {
				CurrPageTextBox.Text = _previousCurrPageTBText;
				TaskDialog dialog = new TaskDialog();
				dialog.WindowTitle = "Error";
				dialog.MainIcon = TaskDialogIcon.Error;
				dialog.MainInstruction = "Invalid input.";
				dialog.Content = "The input entered was not a recognizable number.";
				TaskDialogButton okButton = new TaskDialogButton(ButtonType.Ok);
				dialog.Buttons.Add(okButton);
				TaskDialogButton button = dialog.ShowDialog(this);
			}


		}

		private void CurrPageTextBox_GotMouseCapture (object sender, MouseEventArgs e) {
			CurrPageTextBox.SelectAll();
		}



		#endregion



		#region ImageCropping

		//private ImageProcessing _imageProcessing;

		private void PreviewIMGScroll_PreviewMouseWheel (object sender, MouseWheelEventArgs e) {
			if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
				PreviewIMGScroll.ScrollToHorizontalOffset(PreviewIMGScroll.HorizontalOffset - e.Delta);
				e.Handled = true;
			}
		}

		private System.Windows.Point _startingPoint;
		private RectangleAdorner [] _pageRectangles = null;
		bool _previousMouseState = false;


		private int NextRectangle (System.Windows.Point mousePos) {
			int index = -1;
			int firstIndex = -1;

			bool next = false;
			bool done = false;
			if (LoadedChapter != null) {
				for (int i = 0; i < LoadedChapter.Pages[_currentPage].TextEntries.Count && !done; i++) {
					if (LoadedChapter.Pages[_currentPage].TextEntries[i].Rectangle.Contains(mousePos)) {
						if (index < 0) {
							index = i;
							firstIndex = i;
						}
						else if (next) {
							index = i;
							done = true;
						}
						next = i == _pageRectangles[_currentPage].SelectedRect;
					}
				}
			}

			if (!done)
				index = firstIndex;

			return index;
		}

		private void PreviewIMG_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			System.Windows.Point mousePos = e.GetPosition(PreviewIMG);

			int index = NextRectangle(mousePos);
			if (index >= 0) {
				SelectTextEntry(index);
				_pageRectangles[_currentPage].InvalidateVisual();
			}


		}


		private void PreviewIMG_PreviewMouseRightButtonDown (object sender, MouseButtonEventArgs e) {
			if (LoadedChapter != null) {
				_startingPoint = e.GetPosition(PreviewIMG);

				_startingPoint = e.GetPosition(PreviewIMG);
				Rect rect = new Rect(_startingPoint, _startingPoint);
				_pageRectangles[_currentPage].DragRect = rect;
				_pageRectangles [_currentPage].InvalidateVisual();

				_previousMouseState = true;
			}

		}

		private void PreviewIMG_MouseMove (object sender, MouseEventArgs e) {
			if (LoadedChapter != null) {
				System.Windows.Point mousePos = e.GetPosition(PreviewIMG);

				int index = NextRectangle(mousePos);
				_pageRectangles[_currentPage].MouseOverRect = index;
				_pageRectangles[_currentPage].InvalidateVisual();


				if (e.RightButton == MouseButtonState.Pressed && _previousMouseState) {
					Rect rect = new Rect(_startingPoint, e.GetPosition(PreviewIMG));
					_pageRectangles [_currentPage].DragRect = rect;
					_pageRectangles [_currentPage].InvalidateVisual();

				}
				else if (_previousMouseState) {
					Mouse.SetCursor(Cursors.Wait);
					_previousMouseState = false;
					Rect rect = new Rect (_pageRectangles [_currentPage].DragRect.Value.X,
											_pageRectangles [_currentPage].DragRect.Value.Y,
											_pageRectangles [_currentPage].DragRect.Value.Width,
											_pageRectangles [_currentPage].DragRect.Value.Height);
					_pageRectangles [_currentPage].DragRect = null;

					if (rect .Width == 0 || rect.Height == 0) {
						Mouse.SetCursor(Cursors.Arrow);
						_pageRectangles [_currentPage].InvalidateVisual();
						return;
					}
					Text txt = LoadedChapter.Pages[_currentPage].AddTextEntry(rect);
					txt.TextChanged += OnItemChange;
					TextEntryListView te = new TextEntryListView(txt, this);
					TextEntriesStackPanel.Children.Add(te);
					SelectTextEntry(LoadedChapter.Pages[_currentPage].TextEntries.Count-1).ForceTranslation();
					_pageRectangles [_currentPage].InvalidateVisual();

					Mouse.SetCursor(Cursors.Arrow);


				}
			}
		}



		#endregion



		#region TextEntries

		private TextEntryListView _selectedTextEntry = null;

		public TextEntryControl SelectTextEntry (int index) {
			_pageRectangles [_currentPage].SelectedRect = index;
			TextEntryGrid.Children.Clear();
			TextEntryControl tec = new TextEntryControl(LoadedChapter.Pages[_currentPage].TextEntries[index]);
			TextEntryGrid.Children.Add(tec);


			if (_selectedTextEntry != null)
				_selectedTextEntry.Selected = false;
			_selectedTextEntry = (TextEntryListView)TextEntriesStackPanel.Children[index];
			_selectedTextEntry.Selected = true;

			return tec;
		}

		public void SelectTextEntry (TextEntryListView entry) {
			int index = TextEntriesStackPanel.Children.IndexOf(entry);
			SelectTextEntry(index);
			_pageRectangles[_currentPage].InvalidateVisual();
		}

		public void RemoveTextEntry (Text target) {
			int index = LoadedChapter.Pages[_currentPage].TextEntries.IndexOf(target);
			TextEntriesStackPanel.Children.RemoveAt(index);
			LoadedChapter.Pages[_currentPage].RemoveTextEntry(index);
			if (_pageRectangles [_currentPage].SelectedRect == index)
				_pageRectangles [_currentPage].SelectedRect = -1;
			if (_pageRectangles [_currentPage].MouseOverRect == index)
				_pageRectangles [_currentPage].MouseOverRect = -1;
			_pageRectangles [_currentPage].InvalidateVisual();
		}

		public void MoveTextEntry (Text target, bool up) {
			int offset = up ? -1 : 1;
			int index = LoadedChapter.Pages[_currentPage].TextEntries.IndexOf(target);
			if ((up && index > 0) || (!up && index < LoadedChapter.Pages[_currentPage].TextEntries.Count-1)) {
				LoadedChapter.Pages [_currentPage].MoveTextEntry(index, index + offset);

				if (_pageRectangles [_currentPage].SelectedRect == index)
					_pageRectangles [_currentPage].SelectedRect+= offset;
				if (_pageRectangles [_currentPage].MouseOverRect == index)
					_pageRectangles [_currentPage].MouseOverRect+= offset;

				var tmp2 = TextEntriesStackPanel.Children[index];
				TextEntriesStackPanel.Children.RemoveAt(index);
				TextEntriesStackPanel.Children.Insert(index + offset, tmp2);

				_pageRectangles [_currentPage].InvalidateVisual();

			}

		}



		#endregion



		#region WindowEventManagement
		public void OnItemChange (object sender, EventArgs args) {
			Saved = false;
		}

		private void MainWindow1_Closing (object sender, System.ComponentModel.CancelEventArgs e) {
			if (LoadedChapter != null && !Saved) {
				string res = WarnNotSaved();
				if (res == _SAVE)
					SaveChapterMenuItem_Click(sender, new RoutedEventArgs());
				else if (res == _CANCEL) {
					e.Cancel = true;
					return;
				}
			}
		}



		private void OnImageLoaded (object sender, RoutedEventArgs e) {

			if (_openChapterOnLoad)
				OpenChapter(CurrentSaveFile);
		}


		

		#endregion


	}
}
