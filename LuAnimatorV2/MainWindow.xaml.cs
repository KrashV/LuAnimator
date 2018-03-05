using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Input;
using System.Linq;
using System.Windows.Media.Imaging;
using DrawablesGeneratorTool;
using Microsoft.Win32;

using Newtonsoft.Json;
using System.IO;
using System.Windows.Controls.Primitives;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Windows.Controls;

using FormCollection = System.Collections.ObjectModel.ObservableCollection<LuAnimatorV2.modeNode>;
using AnimationCollection = System.Collections.ObjectModel.ObservableCollection<System.Collections.ObjectModel.ObservableCollection<LuAnimatorV2.modeNode>>;
using System.Deployment.Application;

namespace LuAnimatorV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>



    public partial class MainWindow : Window
    {
        AnimationCollection currentForms = new AnimationCollection();

        private static int currentForm = 0;
        private static int currentFrame = 0;

        private System.Windows.Controls.Image currentImage;

        private static string previousMode = "Idle";
        private static string previousEmote = "idle";

        private static readonly int
            PREVIEW_MARGIN_LEFT = 202,
            PREVIEW_MARGIN_TOP = 300,
            PREVIEW_MARGIN_RIGHT = 192,
            PREVIEW_MARGIN_BOTTOM = 73;

        private static int animationSpeed = 9;

        private static DispatcherTimer _timer;

        private static bool isSaved = true;

        public MainWindow()
        {
            InitializeComponent();

            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            InstallUpdateSyncWithInfo();

            InitializeImage(imgPreview);
            InitializeImage(imgPreviewF);


            currentFrame = 0;
            currentImage = imgPreview;

            ListBoxFrames.Focus();
            _timer = new DispatcherTimer();
            TimeSpan span = TimeSpan.FromMilliseconds(animationSpeed * 1000.0 / 60.0);
            _timer.Interval = span;
            _timer.Tick += new EventHandler(AnimatingCycle);
            _timer.Start();
        }

        private void InitializeImage(System.Windows.Controls.Image img)
        {
            Thickness t = img.Margin;
            t.Bottom = PREVIEW_MARGIN_BOTTOM;
            t.Left = PREVIEW_MARGIN_LEFT;
            t.Top = PREVIEW_MARGIN_TOP;
            t.Right = PREVIEW_MARGIN_RIGHT;

            img.Margin = t;
            img.Height = img.Width = 0;
        }

        #region Animating

        /// <summary>
        /// /Handles the animation
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void AnimatingCycle(Object source, EventArgs e)
        {
            int foreFrames = 0, backFrames = 0;
            if (!ListBoxFrames.Items.IsEmpty)
            {
                BitmapSource[] il = new BitmapSource[ListBoxFrames.Items.Count];
                ListBoxFrames.Items.CopyTo(il, 0);
                foreFrames = ListBoxFrames.Items.Count;
                ImageChanger(il, foreFrames, currentImage);
            }
            else
            {
                currentImage.Source = null;
            }

            System.Windows.Controls.Image backImage = currentImage == imgPreview ? imgPreviewF : imgPreview;

            if (currentForms.Count != 0)
            { 
                modeNode mode = currentForms[currentForm].FirstOrDefault(form => form.modeName == previousMode);

                if (mode != null)
                {
                    emoteNode emote = mode.emotes.FirstOrDefault(em => em.name == previousEmote);

                    if (emote != null)
                    {
                        BitmapSource[] il = backImage == imgPreview ? emote.frames : emote.fullbrightFrames;

                        if (il != null)
                        {
                            backFrames = il.Length;
                            ImageChanger(il, backFrames, backImage);
                        }
                        else
                        {
                            backImage.Source = null;
                        }
                    }
                    else
                    {
                        backImage.Source = null;
                    }
                }
                else
                {
                    backImage.Source = null;
                }
            }

            currentFrame = currentFrame + 1 >= Math.Max(foreFrames, backFrames) ? 0 : currentFrame + 1;
        }

        /// <summary>
        /// Changes the images to the current image
        /// </summary>
        /// <param name="il">list of frames</param>
        /// <param name="count">number of frames</param>
        /// <param name="img">image to change</param>
        private void ImageChanger(BitmapSource[] il, int count, System.Windows.Controls.Image img)
        {
            if (currentFrame < count)
            {
                BitmapSource bi = il[currentFrame];

                img.Source = bi;

                double scale;
                if (tbxframeSize.Value == null)
                    scale = 0;
                else
                    scale = (double)tbxframeSize.Value;

                img.Width = bi.PixelWidth * 2 * scale;
                img.Height = bi.PixelHeight * 2 * scale;
                ModifyPosition();
            }
        }

        #endregion

        private void CleanUP()
        {
            foreach (FormCollection form in currentForms)
            {
                form.Clear();
            }
            currentForms = new AnimationCollection();
        }

        private void PopulateListBox(string[] files)
        {
            ListBoxFrames.Items.Clear();
            foreach (string path in files)
            {
                if (DrawableUtilities.IsValidImage(path))
                {
                    BitmapSource p = new BitmapImage(new Uri(path));
                    ListBoxFrames.Items.Add(p);
                }
                else if (Path.GetExtension(path) == ".gif")
                {
                    PopulateListBox(ExtractGif(path));
                }
                else {
                    MessageBox.Show("Please choose valid images!");
                    return;
                }

            }
        }

        private BitmapSource[] ExtractGif(string sourceGifPath)
        {
            // Get frames from GIF
            System.Drawing.Image gif = System.Drawing.Image.FromFile(sourceGifPath);
            FrameDimension dimension = new FrameDimension(gif.FrameDimensionsList[0]);

            int frameCount = gif.GetFrameCount(dimension);
            int digits = frameCount.ToString().Length;

            System.Drawing.Image[] frames = new System.Drawing.Image[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                gif.SelectActiveFrame(dimension, i);
                frames[i] = ((System.Drawing.Image)gif.Clone());
            }

            BitmapSource[] list = new BitmapSource[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                Bitmap t = new Bitmap(frames[i]);
                list[i] = BitmapConverter.loadBitmap(t);
                t.Dispose();
            }

            return list;
        }

        private void PopulateListBox(BitmapSource[] frames)
        {
            ListBoxFrames.Items.Clear();
            foreach (BitmapSource p in frames)
            {
                ListBoxFrames.Items.Add(p);
            }
        }

        #region Themes

        /// <summary>
        /// Changes the background of the preview to a dark image.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ThemeBlack_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            ChangeTheme("DarkSmall.png");
        }

        /// <summary>
        /// Changes the background of the preview to a light image.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ThemeWhite_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            ChangeTheme("GreenWall.png");
        }

        /// <summary>
        /// Changes the background of the preview to a natural image.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ThemeGreen_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            ChangeTheme("City.png");
        }

        /// <summary>
        /// Changes the background of the preview.
        /// </summary>
        /// <param name="resourcePath">Path to an image, relative to Project/Resources/. Do not start with a slash.</param>
        private void ChangeTheme(string resourcePath)
        {
            if (resourcePath.IndexOf("/") == 0)
                resourcePath = resourcePath.Substring(1);

            imgPreviewBackground.Source = new BitmapImage(new Uri(@"Resources/" + resourcePath, UriKind.Relative));
        }

        #endregion

        #region Drag on Preview

        private int xtranslation = 0;
        private int ytranslation = 0;

        private void SetImage(object sender, System.Windows.Point pos)
        {
            xtranslation = (int)pos.X - PREVIEW_MARGIN_LEFT;
            ytranslation = PREVIEW_MARGIN_TOP - (int)pos.Y;

            tbxXPos.Text = xtranslation.ToString();
            tbxYPos.Text = ytranslation.ToString();
        }
        /// <summary>
        /// Starts capturing the mouse for the preview window, to update the position of the image in the Preview_MouseMove event.
        /// Also calls Preview_MouseMove, to update the preview even when the mouse isn't moved.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Preview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            imgPreview.CaptureMouse();
            this.Preview_MouseMove(sender, e);
        }

        /// <summary>
        /// Adjusts the hand position textboxes by clicking (and dragging the mouse) on the preview window. 
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Preview_MouseMove(object sender, MouseEventArgs e)
        {
            if (imgPreview.IsMouseCaptured)
            {
                SetImage(sender, e.GetPosition(brdPreview));
            }
        }

        private void ModifyPosition()
        {
            ModifyPosition(imgPreview);
            ModifyPosition(imgPreviewF);
        }

        private void ModifyPosition(System.Windows.Controls.Image img)
        {
            Thickness t = img.Margin;
            t.Left = PREVIEW_MARGIN_LEFT - img.Width + xtranslation;
            t.Top = PREVIEW_MARGIN_TOP - img.Height - ytranslation;
            t.Right = PREVIEW_MARGIN_RIGHT - img.Width - xtranslation;
            t.Bottom = PREVIEW_MARGIN_BOTTOM - img.Height + ytranslation;
            img.Margin = t;
        }

        /// <summary>
        /// Stops capturing the mouse for the preview window.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Preview_MouseUp(object sender, MouseButtonEventArgs e)
        {
            imgPreview.ReleaseMouseCapture();
        }



        private void Position_Changed(object sender, EventArgs e)
        {
            if (tbxXPos.Value != null)
                xtranslation = (int)tbxXPos.Value;
            else
            {
                xtranslation = 0;
                tbxXPos.Value = 0;
            }

            if (tbxYPos.Value != null)
                ytranslation = (int)tbxYPos.Value;
            else
            {
                ytranslation = 0;
                tbxYPos.Value = 0;
            }

            ModifyPosition();
        }

        #endregion

        #region Save and Load
        private void Save_Mode(modeNode mode, emoteNode emote)
        {
            emote.looping = chkLoop != null ? (bool)chkLoop.IsChecked : true;
            mode.invisible = chkInvisible != null ? (bool)chkInvisible.IsChecked : true;
            mode.xtranslation = xtranslation;
            mode.ytranslation = ytranslation;
            emote.speed = animationSpeed;
            mode.framescale = tbxframeSize != null ? (double)tbxframeSize.Value : 1;
            emote.sound = soundName != null ? soundName.Text.Replace("\\", "/").Split() : null;
            emote.soundLoop = chkSoundLoop != null ? (bool)chkSoundLoop.IsChecked : true;

            emote.soundInterval = tbxSoundInterval != null ? (double)tbxSoundInterval.Value : 1;
            emote.soundVolume = tbxSoundVolume != null ? (double)tbxSoundVolume.Value : 1;
            emote.soundPitch = tbxSoundPitch != null ? (double)tbxSoundPitch.Value : 1;

            if (ListBoxFrames != null && !ListBoxFrames.Items.IsEmpty)
            {
                if (currentImage == imgPreview)
                {
                    emote.frames = new BitmapSource[ListBoxFrames.Items.Count];
                    ListBoxFrames.Items.CopyTo(emote.frames, 0);
                }
                else
                {
                    emote.fullbrightFrames = new BitmapSource[ListBoxFrames.Items.Count];
                    ListBoxFrames.Items.CopyTo(emote.fullbrightFrames, 0);
                }
                isSaved = false;
            }
        }

        private void Advanced_Save()
        {
            if (currentForms.ElementAtOrDefault(currentForm) == null)
            {
                FormCollection f = new FormCollection();
                currentForms.Add(f);
            }
            if (ListBoxFrames != null && !ListBoxFrames.Items.IsEmpty)
            {
                modeNode mode = currentForms[currentForm].FirstOrDefault(form => form.modeName == previousMode);
                if (mode == null)
                {
                    mode = new modeNode();
                    mode.modeName = previousMode;
                    mode.emotes = new System.Collections.Generic.List<emoteNode>();
                    currentForms[currentForm].Add(mode);
                }
                emoteNode emote = mode.emotes.FirstOrDefault(em => em.name == previousEmote);
                if (emote == null)
                {
                    emote = new emoteNode();
                    emote.name = previousEmote;

                    mode.emotes.Add(emote);
                }

                Save_Mode(mode, emote);
            }
        }

        private void Advanced_Load()
        {
            if (currentForms.ElementAtOrDefault(currentForm) == null)
                currentForms.Add(new FormCollection());

            ComboBoxItem CBIM = (ComboBoxItem)cbxGenerateType.SelectedValue;
            string state = CBIM.Content.ToString();

            ComboBoxItem CBIE = (ComboBoxItem)cbxGenerateEmote.SelectedValue;
            string emotestate = CBIE.Content.ToString();


            modeNode oldmode = currentForms[currentForm].FirstOrDefault(form => form.modeName == state);

            if (oldmode != null)
            {
                emoteNode emote = oldmode.emotes.FirstOrDefault(form => form.name == emotestate);

                tbxXPos.Text = oldmode.xtranslation.ToString();
                tbxYPos.Text = oldmode.ytranslation.ToString();
                tbxframeSize.Value = oldmode.framescale;
                chkInvisible.IsChecked = oldmode.invisible;
                xtranslation = oldmode.xtranslation;
                ytranslation = oldmode.ytranslation;


                if (emote != null)
                {
                    chkLoop.IsChecked = emote.looping;
                    chkSoundLoop.IsChecked = emote.soundLoop;

                    animationSpeed = emote.speed;
                    tbxAnimSpeed.Value = emote.speed;
                    tbxSoundInterval.Value = emote.soundInterval;
                    tbxSoundPitch.Value = emote.soundPitch;
                    tbxSoundVolume.Value = emote.soundVolume;
                    soundName.Text = String.Join(" ", emote.sound);

                    if (currentImage == imgPreview)
                    {
                        if (emote.frames != null)
                            PopulateListBox(emote.frames);
                    }
                    else
                    {
                        if (emote.fullbrightFrames != null)
                            PopulateListBox(emote.fullbrightFrames);
                    }
                }
            }
            else
                soundName.Text = "";
        }

#endregion

        #region Sound Button
        private void Button_Sound(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "OGG files (*.ogg)|*.ogg";

            if (openFileDialog.ShowDialog() == true)
            {
                soundName.Text = ConvertPath(openFileDialog.FileNames);
            }
        }

        private void Button_Remove_Sound(object sender, RoutedEventArgs e)
        {
            if (soundName.Text.Length > 0)
            {
                ComboBoxItem CBIM = (ComboBoxItem)cbxGenerateType.SelectedValue;
                string state = CBIM.Content.ToString();

                ComboBoxItem CBIE = (ComboBoxItem)cbxGenerateEmote.SelectedValue;
                string emotestate = CBIE.Content.ToString();

                modeNode mode = currentForms[currentForm].FirstOrDefault(form => form.modeName == state);

                if (mode != null)
                {
                    emoteNode emote = mode.emotes.FirstOrDefault(form => form.name == emotestate);
                    if (emote != null)
                    {
                        emote.sound = null;
                    }
                }
                soundName.Text = null;
            }
        }

        /// <summary>
        /// Trips the full path to the Starbound-asset reference path
        /// </summary>
        /// <param name="oldpaths">array of full paths</param>
        /// <returns>array of stripped paths</returns>
        private string ConvertPath(string[] oldpaths)
        {
            string text = null;
            try
            {
                for (int i = 0; i < oldpaths.Count(); i++)
                {
                    string searchString = "\\sfx";
                    int startIndex = oldpaths[i].IndexOf(searchString);
                    oldpaths[i] = oldpaths[i].Substring(startIndex, oldpaths[i].Length - startIndex);
                    oldpaths[i].Replace("\\", "/");
                }
            }
            catch
            {
                MessageBox.Show("Please choose an .ogg file from the assets! (They're in the \"sfx\" folder))");
                return null;
            }
            return text = String.Join(" ", oldpaths);
        }

        #endregion

        #region Event handlers

        private void HorizontalScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer s = (ScrollViewer)sender;

            if (e.Delta == 0)
                return;

            if (e.Delta > 0)
                s.LineLeft();
            else
                s.LineRight();

            e.Handled = true;
        }

        private void ListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            ListBoxFrames.SelectedItem = null;
        }

        private void ListBox_DragLeave(object sender, DragEventArgs e)
        {
            if (ListBoxFrames.SelectedItem != null)
                ListBoxFrames.Items.Remove(ListBoxFrames.SelectedItem);
        }

        private void ListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
        }

        private void CheckBox_Invisible_Checked(object sender, EventArgs e)
        {
            imgPreviewCharacter.Opacity = 0.5;
        }

        private void CheckBox_Invisible_Unchecked(object sender, EventArgs e)
        {
            imgPreviewCharacter.Opacity = 1;
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                PopulateListBox(files);
                SetImage(sender, e.GetPosition(brdPreview));
            }
        }

        private void Button_Clear_List(object sender, RoutedEventArgs e)
        {
            ComboBoxItem CBIM = (ComboBoxItem)cbxGenerateType.SelectedValue;
            string state = CBIM.Content.ToString();

            ComboBoxItem CBIE = (ComboBoxItem)cbxGenerateEmote.SelectedValue;
            string emotestate = CBIE.Content.ToString();

            ListBoxFrames.Items.Clear();

            if (currentForms.ElementAtOrDefault(currentForm) == null)
                return;

            modeNode mode = currentForms[currentForm].FirstOrDefault(form => form.modeName == state);

            if (mode != null)
            {
                emoteNode emote = mode.emotes.FirstOrDefault(form => form.name == emotestate);
                if (emote != null)
                {
                    if (currentImage == imgPreview)
                        emote.frames = null;
                    else
                        emote.fullbrightFrames = null;
                }
            }

        }

        private void Toggle_Animation_Click(object sender, RoutedEventArgs e)
        {
            var mode = Toggle_Animation.Content.ToString();

            if (mode == "Pause")
            {
                Toggle_Animation.Content = "Play";
                _timer.Stop();
            }
            else
            {
                Toggle_Animation.Content = "Pause";
                _timer.Start();
            }

        }

        private void animationSpeedChanged(object sender, RoutedEventArgs e)
        {
            if (tbxAnimSpeed.Value.HasValue)
                animationSpeed = (int)tbxAnimSpeed.Value;
            TimeSpan span = TimeSpan.FromMilliseconds(animationSpeed * 1000.0 / 60.0);
            _timer.Interval = span;

        }

        private void ComboBox_Selection_Changed(object sender, EventArgs e)
        {
            if (ListBoxFrames == null || chkLoop == null || chkInvisible == null || cbxGenerateEmote == null || cbxGenerateType == null || cbxGenerateType.SelectedIndex == -1 || cbxGenerateEmote.SelectedIndex == -1)
                return;

            // Saving mode
            Advanced_Save();

            ListBoxFrames.Items.Clear();

            // Loading mode
            Advanced_Load();

            ComboBoxItem CBIM = (ComboBoxItem)cbxGenerateType.SelectedValue;
            string state = CBIM.Content.ToString();

            ComboBoxItem CBIE = (ComboBoxItem)cbxGenerateEmote.SelectedValue;
            string emotestate = CBIE.Content.ToString();


            if (state == "Activate" || state == "Deactivate" || state == "Sitting_Down" || state == "Standing_Up" || state == "Transform_Next" || state == "Transform_Previous" || state == "Primary_Fire" || state == "Alt_Fire")
            {
                chkLoop.IsChecked = true;
                chkLoop.IsEnabled = false;
            }
            else
                chkLoop.IsEnabled = true;

            switch (state)
            {
                case "Sitting_Down":
                case "Sit":
                    imgPreviewCharacter.Source = new BitmapImage(new Uri(@"Resources/sit.png", UriKind.Relative));
                    break;

                case "Crouch":
                    imgPreviewCharacter.Source = new BitmapImage(new Uri(@"Resources/duck.png", UriKind.Relative));
                    break;

                default:
                    imgPreviewCharacter.Source = new BitmapImage(new Uri(@"Resources/stand.png", UriKind.Relative));
                    break;
            }

            previousMode = state;
            previousEmote = emotestate;

            ModifyPosition();
        }

        private void Layer_Changed(object sender, EventArgs e)
        {
            if (!Animation_Layer.IsLoaded)
                return;

            Advanced_Save();
            ListBoxFrames.Items.Clear();
            if (currentImage == imgPreview)
            {
                currentImage = imgPreviewF;
                imgPreview.Opacity = 0.5;
                imgPreviewF.Opacity = 1;
            }
            else
            {
                currentImage = imgPreview;
                imgPreviewF.Opacity = 0.5;
                imgPreview.Opacity = 1;
            }
            Advanced_Load();
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            
            switch (e.Key)
            {
                case Key.S:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        Save_Click(sender, null);
                    }
                    break;
                case Key.O:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        Open_Click(sender, null);
                    }
                    break;
                case Key.N:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        New_Click(sender, null);
                    }
                    break;
                case Key.Delete:
                    if (ListBoxFrames.SelectedItem != null)
                    {
                        ListBoxFrames.Items.Remove(ListBoxFrames.SelectedItem);
                    };
                    break;
                case Key.Right:
                    tbxXPos.Value++;
                    break;
                case Key.Left:
                    tbxXPos.Value--;
                    break;
                case Key.Up:
                    tbxYPos.Value++;
                    break;
                case Key.Down:
                    tbxYPos.Value--;
                    break;
            }
            e.Handled = true;
        }

        #endregion

        #region Form Selector
        private void Button_Form_Left(object sender, RoutedEventArgs e)
        {
            SetForm(Math.Max(currentForm - 1, 0));
        }

        private void Button_Form_Right(object sender, RoutedEventArgs e)
        {
            SetForm(currentForm + 1);
        }

        /// <summary>
        /// Sets the current form
        /// </summary>
        /// <param name="formNumber">The number of desired form</param>
        private void SetForm(int formNumber)
        {
            // Saving mode
            Advanced_Save();

            currentForm = formNumber;
            
            tbxCurrentForm.Text = "Form " + (currentForm + 1);

            btnLeftForm.IsEnabled = (currentForm != 0);


            previousMode = "Idle";
            previousEmote = "idle";
            ListBoxFrames.Items.Clear();

            cbxGenerateType.SelectedIndex = 0;
            cbxGenerateEmote.SelectedIndex = 0;

            // Loading mode
            Advanced_Load();
        }

        #endregion

        #region Main Buttons events

        private void New_Click(object sender, RoutedEventArgs e)
        {
            Advanced_Save();
            if (AskUserToSave(sender))
            {
                CleanUP();
                previousEmote = "idle";
                previousMode = "Idle";
                currentForm = 0;
                ListBoxFrames.Items.Clear();
                SetForm(0);
                isSaved = true;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save file to...";
            sfd.Filter = "JSON file|*.json";
            sfd.FileName = "luanimation.json";

            if (sfd.ShowDialog() == true)
            {
                Advanced_Save();

                Thread awaiting = CreateWindow("Saving...");

                File.WriteAllText(sfd.FileName, FileGenerator.Save(currentForms));

                awaiting.Abort();
                isSaved = true;
            }            
        }


        public void Open_Click(object sender, RoutedEventArgs e)
        {
            Advanced_Save();
            if (AskUserToSave(sender))
            {

                OpenFileDialog ofd = new OpenFileDialog();

                ofd.Filter = "JSON Files |*.json";
                ofd.Title = "Open luanimation.json";

                if (ofd.ShowDialog() == true)
                {
                    Thread awaiting = CreateWindow("Loading " + Path.GetFileName(ofd.FileName) + "\nThis will take a while");
                    AnimationCollection temp = FileGenerator.Load(ofd.FileName);

                    if (temp == null)
                    {
                        awaiting.Abort();
                        MessageBox.Show("Couldn't load the file");
                    }
                    else
                    {
                        Button_Remove_Sound(sender, null);
                        CleanUP();
                        currentForm = 0;
                        ListBoxFrames.Items.Clear();
                        currentForms = temp;
                        SetForm(0);

                        awaiting.Abort();
                    }
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Advanced_Save();
            this.Close();
        }

        #endregion

        private void Guide_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://ilovebacons.com/showcase/luanimator-v3-1-01.228/");
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.paypal.me/degranon");
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            About a = new About();
            a.Show();
        }

        private Thread CreateWindow(string title)
        {
            Thread awaiting = new Thread(new ThreadStart(() =>
            {
                // Create and show the Window
                ProgressBarTaskOnUiThread tempWindow = new ProgressBarTaskOnUiThread(title);
                tempWindow.Closed += (s, e) =>
                    Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);

                tempWindow.Show();
                // Start the Dispatcher Processing
                Dispatcher.Run();
            }));
            // Set the apartment state
            awaiting.SetApartmentState(ApartmentState.STA);
            // Make the thread a background thread
            awaiting.IsBackground = true;
            // Start the thread
            awaiting.Start();


            return awaiting;
        }

        private bool AskUserToSave(object sender)
        {
            if (!isSaved)
            {
                MessageBoxResult mbr = MessageBox.Show("The project is unsaved. Do you want to save it first?", "Warning", MessageBoxButton.YesNoCancel);
                if (mbr == MessageBoxResult.Yes)
                {
                    Save_Click(sender, null);
                }
                else if (mbr == MessageBoxResult.Cancel)
                    return false;
            }
            return true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!AskUserToSave(sender))
                e.Cancel = true;
        }

        private void InstallUpdateSyncWithInfo()
        {
            UpdateCheckInfo info = null;

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                try
                {
                    info = ad.CheckForDetailedUpdate();

                }
                catch (DeploymentDownloadException dde)
                {
                    MessageBox.Show("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                    return;
                }
                catch (InvalidDeploymentException ide)
                {
                    MessageBox.Show("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                    return;
                }
                catch (InvalidOperationException ioe)
                {
                    MessageBox.Show("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
                    return;
                }

                if (info.UpdateAvailable)
                {
                    Boolean doUpdate = true;

                    if (!info.IsUpdateRequired)
                    {
                        MessageBoxResult dr = MessageBox.Show("An update is available. Would you like to update the application now?", "Update Available", MessageBoxButton.OKCancel);
                        if (!(MessageBoxResult.OK == dr))
                        {
                            doUpdate = false;
                        }
                    }
                    else
                    {
                        // Display a message that the app MUST reboot. Display the minimum required version.
                        MessageBox.Show("This application has detected a mandatory update from your current " +
                            "version to version " + info.MinimumRequiredVersion.ToString() +
                            ". The application will now install the update and restart.",
                            "Update Available", MessageBoxButton.OK);
                    }

                    if (doUpdate)
                    {
                        try
                        {
                            ad.Update();
                            MessageBox.Show("The application has been upgraded, and will now restart.");
                            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                            Application.Current.Shutdown();
                        }
                        catch (DeploymentDownloadException dde)
                        {
                            MessageBox.Show("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
                            return;
                        }
                    }
                }
            }
        }

    }
}