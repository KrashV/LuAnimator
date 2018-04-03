using System;
using System.Windows.Threading;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Input;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Deployment.Application;

using FormCollection = System.Collections.ObjectModel.ObservableCollection<LuAnimatorV2.modeNode>;
using AnimationCollection = System.Collections.ObjectModel.ObservableCollection<System.Collections.ObjectModel.ObservableCollection<LuAnimatorV2.modeNode>>;


namespace LuAnimatorV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {
        private readonly string changelog = "Finally fixed the loading issue";
        AnimationCollection animationCollection = new AnimationCollection();

        private static int currentForm = 0,
                           currentFrame = 0,
                           animationSpeed = 9;

        private System.Windows.Controls.Image currentImage;

        private static string previousModeName = "Idle",
                              previousEmoteName = "idle";

        private static readonly Thickness DEFAULT_MARGIN = new Thickness(202, 300, 192, 73);

        private static DispatcherTimer _timer;

        private static bool isSaved = true;
        private static string fileName = "New animation";

        public MainWindow()
        {
            InitializeComponent();

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            InstallUpdateSyncWithInfo(false);

            AssigneKeyBindings();

            InitializeImage(imgPreview);
            InitializeImage(imgPreviewF);
            currentImage = imgPreview;

            _timer = new DispatcherTimer();
            TimeSpan span = TimeSpan.FromMilliseconds(animationSpeed * 1000.0 / 60.0);
            _timer.Interval = span;
            _timer.Tick += new EventHandler(AnimatingCycle);
            _timer.Start();

            ((System.Collections.Specialized.INotifyCollectionChanged)ListBoxFrames.Items).CollectionChanged += (sender, e) =>
            {
                SetTitleAsSaved(false);
            };
        }

        private void AssigneKeyBindings()
        {
            var cmd = new RoutedCommand();
        }

        private void InitializeImage(System.Windows.Controls.Image img)
        {
            img.SetCurrentValue(MarginProperty, DEFAULT_MARGIN);
            img.SetCurrentValue(HeightProperty, (double)0);
            img.SetCurrentValue(WidthProperty, (double)0);
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
                currentImage.SetCurrentValue(System.Windows.Controls.Image.SourceProperty, null);
            }

            System.Windows.Controls.Image backImage = currentImage == imgPreview ? imgPreviewF : imgPreview;

            if (animationCollection.Count != 0)
            {
                modeNode mode = animationCollection[currentForm].FirstOrDefault(form => form.modeName == previousModeName);

                if (mode != null)
                {
                    emoteNode emote = mode.emotes.FirstOrDefault(em => em.name == previousEmoteName);

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
                            backImage.SetCurrentValue(System.Windows.Controls.Image.SourceProperty, null);
                        }
                    }
                    else
                    {
                        backImage.SetCurrentValue(System.Windows.Controls.Image.SourceProperty, null);
                    }
                }
                else
                {
                    backImage.SetCurrentValue(System.Windows.Controls.Image.SourceProperty, null);
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

                img.SetCurrentValue(System.Windows.Controls.Image.SourceProperty, bi);

                double scale;
                if (tbxframeSize.Value == null)
                    scale = 0;
                else
                    scale = (double)tbxframeSize.Value;

                img.SetCurrentValue(WidthProperty, bi.PixelWidth * 2 * scale);
                img.SetCurrentValue(HeightProperty, bi.PixelHeight * 2 * scale);
                ModifyPosition();
            }
        }

        #endregion

        /// <summary>
        /// Reset the animation container
        /// </summary>
        private void CleanUP()
        {
            foreach (FormCollection form in animationCollection)
            {
                form.Clear();
            }
            animationCollection = new AnimationCollection();
        }

        /// <summary>
        /// Populate the ListBox
        /// </summary>
        /// <param name="files">Files to check and put on success</param>
        private void PopulateListBox(string[] files)
        {
            foreach (string path in files)
            {
                if (IsValidImage(path))
                {
                    BitmapSource p = new BitmapImage(new Uri(path));
                    ListBoxFrames.Items.Add(p);
                }
                else if (Path.GetExtension(path) == ".gif")
                {
                    PopulateListBox(ExtractGif(path));
                }
                else
                {
                    MessageBox.Show("Please choose valid images!");
                    return;
                }

            }
        }

        /// <summary>
        /// Extract the frames from the gif
        /// </summary>
        /// <param name="sourceGifPath">The path to the gif</param>
        /// <returns>Array of frames</returns>
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
                list[i] = BitmapConverter.BitmapToBitmapSource(t);
                t.Dispose();
            }

            return list;
        }

        /// <summary>
        /// Populate the ListBox
        /// </summary>
        /// <param name="frames">Frames to put in the ListBox</param>
        private void PopulateListBox(BitmapSource[] frames)
        {
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

            imgPreviewBackground.SetCurrentValue(System.Windows.Controls.Image.SourceProperty, new BitmapImage(new Uri(@"Resources/BackgroundPreview/" + resourcePath, UriKind.Relative)));
        }

        #endregion

        #region Drag on Preview

        private int xtranslation = 0;
        private int ytranslation = 0;

        private void SetImage(object sender, System.Windows.Point pos)
        {
            xtranslation = (int)pos.X - (int)DEFAULT_MARGIN.Left;
            ytranslation = (int)DEFAULT_MARGIN.Top - (int)pos.Y;

            tbxXPos.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.InputBase.TextProperty, xtranslation.ToString());
            tbxYPos.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.InputBase.TextProperty, ytranslation.ToString());
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
            img.SetCurrentValue(MarginProperty, new Thickness(
                (int)DEFAULT_MARGIN.Left - img.Width + xtranslation,
                (int)DEFAULT_MARGIN.Top - img.Height - ytranslation,
                (int)DEFAULT_MARGIN.Right - img.Width - xtranslation,
                (int)DEFAULT_MARGIN.Bottom - img.Height + ytranslation
                ));
        }

        /// <summary>
        /// Stops capturing the mouse for the preview window.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Preview_MouseUp(object sender, MouseButtonEventArgs e)
        {
            imgPreview.ReleaseMouseCapture();

            if (currentImage.Source != null)
                SetTitleAsSaved(false);
        }



        private void Position_Changed(object sender, EventArgs e)
        {
            if (tbxXPos.Value != null)
                xtranslation = (int)tbxXPos.Value;
            else
            {
                xtranslation = 0;
                tbxXPos.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.UpDownBase<int?>.ValueProperty, 0);
            }

            if (tbxYPos.Value != null)
                ytranslation = (int)tbxYPos.Value;
            else
            {
                ytranslation = 0;
                tbxYPos.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.UpDownBase<int?>.ValueProperty, 0);
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
            }
        }

        private void Advanced_Save()
        {
            if (animationCollection.ElementAtOrDefault(currentForm) == null)
            {
                FormCollection f = new FormCollection();
                f.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler((obj, target) =>
                {
                    SetTitleAsSaved(false);
                });
                animationCollection.Add(f);
            }
            if (ListBoxFrames != null && !ListBoxFrames.Items.IsEmpty)
            {
                modeNode mode = animationCollection[currentForm].FirstOrDefault(form => form.modeName == previousModeName);
                if (mode == null)
                {
                    mode = new modeNode();
                    mode.modeName = previousModeName;
                    mode.emotes = new System.Collections.Generic.List<emoteNode>();
                    animationCollection[currentForm].Add(mode);
                }
                emoteNode emote = mode.emotes.FirstOrDefault(em => em.name == previousEmoteName);
                if (emote == null)
                {
                    emote = new emoteNode();
                    emote.name = previousEmoteName;

                    mode.emotes.Add(emote);
                }

                Save_Mode(mode, emote);
            }
        }

        private void Advanced_Load()
        {
            if (animationCollection.ElementAtOrDefault(currentForm) == null)
            {
                animationCollection.Add(new FormCollection());
                soundName.SetCurrentValue(TextBox.TextProperty, "");
            }

            ComboBoxItem CBIM = (ComboBoxItem)cbxGenerateType.SelectedValue;
            string state = CBIM.Content.ToString();

            ComboBoxItem CBIE = (ComboBoxItem)cbxGenerateEmote.SelectedValue;
            string emotestate = CBIE.Content.ToString();


            modeNode oldmode = animationCollection[currentForm].FirstOrDefault(form => form.modeName == state);

            if (oldmode != null)
            {
                emoteNode emote = oldmode.emotes.FirstOrDefault(form => form.name == emotestate);

                tbxXPos.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.InputBase.TextProperty, oldmode.xtranslation.ToString());
                tbxYPos.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.InputBase.TextProperty, oldmode.ytranslation.ToString());
                tbxframeSize.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.UpDownBase<double?>.ValueProperty, oldmode.framescale);
                chkInvisible.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, oldmode.invisible);
                xtranslation = oldmode.xtranslation;
                ytranslation = oldmode.ytranslation;
                
                if (emote != null)
                {
                    chkLoop.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, emote.looping);
                    chkSoundLoop.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, emote.soundLoop);

                    animationSpeed = emote.speed;
                    tbxAnimSpeed.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.UpDownBase<int?>.ValueProperty, emote.speed);
                    tbxSoundInterval.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.UpDownBase<double?>.ValueProperty, emote.soundInterval);
                    tbxSoundPitch.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.UpDownBase<double?>.ValueProperty, emote.soundPitch);
                    tbxSoundVolume.SetCurrentValue(Xceed.Wpf.Toolkit.Primitives.UpDownBase<double?>.ValueProperty, emote.soundVolume);
                    soundName.SetCurrentValue(TextBox.TextProperty, emote.sound != null ? String.Join(" ", emote.sound) : "");

                    ListBoxFrames.Items.Clear();
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
                soundName.SetCurrentValue(TextBox.TextProperty, "");
        }

        #endregion

        #region Sound Button

        /// <summary>
        /// Select the vanilla sounds from the folder with unpacked assets
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_Sound(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "OGG files (*.ogg)|*.ogg";

            if (openFileDialog.ShowDialog() == true)
            {
                soundName.SetCurrentValue(TextBox.TextProperty, ConvertPath(openFileDialog.FileNames));
                SetTitleAsSaved(false);
            }
        }

        /// <summary>
        /// Clear the sound selection
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_Remove_Sound(object sender, RoutedEventArgs e)
        {
            if (soundName.Text.Length > 0)
            {
                ComboBoxItem CBIM = (ComboBoxItem)cbxGenerateType.SelectedValue;
                string state = CBIM.Content.ToString();

                ComboBoxItem CBIE = (ComboBoxItem)cbxGenerateEmote.SelectedValue;
                string emotestate = CBIE.Content.ToString();

                modeNode mode = animationCollection[currentForm].FirstOrDefault(form => form.modeName == state);

                if (mode != null)
                {
                    emoteNode emote = mode.emotes.FirstOrDefault(form => form.name == emotestate);
                    if (emote != null)
                    {
                        emote.sound = null;
                    }
                }
                soundName.SetCurrentValue(TextBox.TextProperty, null);
                SetTitleAsSaved(false);
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

        /// <summary>
        /// ListBox scrollbar handler
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="MouseWheelEventArgs"/> instance containing the event data.</param>
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

        /// <summary>
        /// Make the character semi-transparent if it will be invisible during the animations
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CheckBox_Invisible_Checked(object sender, EventArgs e)
        {
            if (chkInvisible.IsLoaded)
            {
                imgPreviewCharacter.SetCurrentValue(OpacityProperty, 0.5);
                SetTitleAsSaved(false);
            }
        }

        /// <summary>
        /// Make the character fullbright if it will be visible during the animations
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CheckBox_Invisible_Unchecked(object sender, EventArgs e)
        {
            if (chkInvisible.IsLoaded)
            {
                imgPreviewCharacter.SetCurrentValue(OpacityProperty, (double)1);
                SetTitleAsSaved(false);
            }
        }

        /// <summary>
        /// Populate the ListBox with dragged images
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                PopulateListBox(files);
                SetImage(sender, e.GetPosition(brdPreview));
            }
        }

        /// <summary>
        /// Clear the ListBox
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_Clear_List(object sender, RoutedEventArgs e)
        {
            ComboBoxItem CBIM = (ComboBoxItem)cbxGenerateType.SelectedValue;
            string state = CBIM.Content.ToString();

            ComboBoxItem CBIE = (ComboBoxItem)cbxGenerateEmote.SelectedValue;
            string emotestate = CBIE.Content.ToString();

            ListBoxFrames.Items.Clear();

            if (animationCollection.ElementAtOrDefault(currentForm) == null)
                return;

            modeNode mode = animationCollection[currentForm].FirstOrDefault(form => form.modeName == state);

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

        /// <summary>
        /// (Un-)Pause the animation cycle
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_PauseAnimation(object sender, RoutedEventArgs e)
        {
            var mode = Toggle_Animation.Content.ToString();

            if (mode == "Pause")
            {
                Toggle_Animation.SetCurrentValue(ContentProperty, "Play");
                _timer.Stop();
            }
            else
            {
                Toggle_Animation.SetCurrentValue(ContentProperty, "Pause");
                _timer.Start();
            }

        }

        /// <summary>
        /// Change the animation speed according to user's request
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void animationSpeedChanged(object sender, RoutedEventArgs e)
        {
            if (tbxAnimSpeed.Value.HasValue)
                animationSpeed = (int)tbxAnimSpeed.Value;
            TimeSpan span = TimeSpan.FromMilliseconds(animationSpeed * 1000.0 / 60.0);
            _timer.Interval = span;

            SetTitleAsSaved(false);
        }

        /// <summary>
        /// Change the current mode and emoting according to user's request
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ComboBox_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxFrames == null || chkLoop == null || chkInvisible == null || cbxGenerateEmote == null || cbxGenerateType == null || cbxGenerateType.SelectedIndex == -1 || cbxGenerateEmote.SelectedIndex == -1)
                return;

            bool wasSaved = isSaved;
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
                chkLoop.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true);
                chkLoop.SetCurrentValue(IsEnabledProperty, false);
            }
            else
                chkLoop.SetCurrentValue(IsEnabledProperty, true);

            string path;
            switch (state)
            {
                case "Sitting_Down":
                case "Sit":
                    path = @"Resources/CharacterPreview/sit.png";
                    break;

                case "Crouch":
                    path = @"Resources/CharacterPreview/duck.png";
                    break;

                default:
                    path = @"Resources/CharacterPreview/stand.png";
                    break;
            }
            imgPreviewCharacter.SetCurrentValue(System.Windows.Controls.Image.SourceProperty, new BitmapImage(new Uri(path, UriKind.Relative)));

            previousModeName = state;
            previousEmoteName = emotestate;

            ModifyPosition();
            SetTitleAsSaved(wasSaved);
        }


        /// <summary>
        /// Change the current layer according to user's request
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Layer_Changed(object sender, EventArgs e)
        {
            if (!Animation_Layer.IsLoaded)
                return;

            Advanced_Save();
            ListBoxFrames.Items.Clear();
            if (currentImage == imgPreview)
            {
                currentImage = imgPreviewF;
                imgPreview.SetCurrentValue(OpacityProperty, 0.5);
                imgPreviewF.SetCurrentValue(OpacityProperty, (double)1);
            }
            else
            {
                currentImage = imgPreview;
                imgPreviewF.SetCurrentValue(OpacityProperty, 0.5);
                imgPreview.SetCurrentValue(OpacityProperty, (double)1);
            }
            Advanced_Load();
        }

        /// <summary>
        /// Frame scale value changed
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void Scale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tbxframeSize.IsLoaded && tbxframeSize.Value.HasValue)
            {
                SetTitleAsSaved(false);
            }
        }

        /// <summary>
        /// Delete the selectede frame from the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (ListBoxFrames.SelectedItem != null)
            {
                ListBoxFrames.Items.Remove(ListBoxFrames.SelectedItem);
            };
        }

        /// <summary>
        /// Keyboard event handler: move the image around or execute standart shortcuts
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.S:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        SaveCommand_Executed(sender, null);
                    }
                    break;
                case Key.O:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        OpenCommand_Executed(sender, null);
                    }
                    break;
                case Key.N:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        NewCommand_Executed(sender, null);
                    }
                    break;
                case Key.Delete:
                    DeleteCommand_Executed(sender, null);
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

        /// <summary>
        /// Next form button handler
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_Form_Left(object sender, RoutedEventArgs e)
        {
            SetForm(Math.Max(currentForm - 1, 0));
        }

        /// <summary>
        /// Previous form button handler
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
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
            bool wasSaved = isSaved;
            // Saving mode
            Advanced_Save();

            currentForm = formNumber;


            tbxCurrentForm.SetCurrentValue(TextBox.TextProperty, "Form " + (currentForm + 1));

            btnLeftForm.SetCurrentValue(IsEnabledProperty, (currentForm != 0));

            previousModeName = "Idle";
            previousEmoteName = "idle";

            ListBoxFrames.Items.Clear();

            cbxGenerateType.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedIndexProperty, 0);
            cbxGenerateEmote.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedIndexProperty, 0);

            // Loading mode
            Advanced_Load();
            SetTitleAsSaved(wasSaved);
        }

        #endregion

        #region Menu Buttons

        /// <summary>
        /// A handler for New File menu option
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Advanced_Save();
            if (AskUserToSave())
            {
                CleanUP();
                previousEmoteName = "idle";
                previousModeName = "Idle";
                currentForm = 0;
                ListBoxFrames.Items.Clear();
                SetForm(0);
                fileName = "New animation";
                SetTitleAsSaved(true);
            }
        }

        /// <summary>
        /// Save the project to the current file
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save file to...";
            sfd.Filter = "JSON file|*.json";
            sfd.FileName = "luanimation.json";

            if (sfd.ShowDialog() == true)
            {
                Advanced_Save();

                Thread awaiting = OpenProgressBarWindow("Saving...");

                File.WriteAllText(sfd.FileName, FileGenerator.ToJson(animationCollection));

                awaiting.Abort();

                fileName = sfd.FileName;
                SetTitleAsSaved(true);

            }
        }

        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// Save the current project
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (File.Exists(fileName))
            {
                Advanced_Save();
                Thread awaiting = OpenProgressBarWindow("Saving...");
                File.WriteAllText(fileName, FileGenerator.ToJson(animationCollection));
                awaiting.Abort();
                SetTitleAsSaved(true);
            }
            else
                SaveAsCommand_Executed(sender, null);
        }

        /// <summary>
        /// A handler for Open File menu option
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Advanced_Save();
            if (AskUserToSave())
            {

                OpenFileDialog ofd = new OpenFileDialog();


                ofd.Filter = "JSON Files |*.json";
                ofd.Title = "Open luanimation.json";

                if (ofd.ShowDialog() == true)
                {
                    Thread awaiting = OpenProgressBarWindow("Loading " + Path.GetFileName(ofd.FileName) + "\nThis will take a while");
                    AnimationCollection temp = FileGenerator.ToAnimationCollection(ofd.FileName);
                    awaiting.Abort();

                    if (temp == null)
                    {
                        MessageBox.Show("Couldn't load the file");
                    }
                    else
                    {
                        Button_Remove_Sound(sender, null);
                        CleanUP();
                        currentForm = 0;
                        ListBoxFrames.Items.Clear();
                        animationCollection = temp;
                        SetForm(0);

                        fileName = ofd.FileName;
                        // we've just opened the project
                        SetTitleAsSaved(true);
                    }
                }
            }
        }

        /// <summary>
        /// A handler for Exit menu option: closes the application
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Advanced_Save();
            this.Close();
        }

        /// <summary>
        /// A handler for Guid menu option: redirects user to the guide on iLoveBacon's showcase
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Guide_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/KrashV/LuAnimator/wiki");
        }

        /// <summary>
        /// A handler for Donate menu option : redirects user to the PayPal.Me page
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.paypal.me/degranon");
        }

        /// <summary>
        /// Show the information about the program
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AboutWindow a = new AboutWindow();
            a.Show();
        }

        /// <summary>
        /// A handler for Update menu option: checks for availabe updates.
        /// Application does this on its own in the beginning
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            InstallUpdateSyncWithInfo(true);
        }

        #endregion

        /// <summary>
        /// Opens a progress bar box, so user could see the process running
        /// </summary>
        /// <param name="title">Title of the box</param>
        /// <returns>Thread running the bar</returns>
        private Thread OpenProgressBarWindow(string title)
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



        /// <summary>
        /// Returns whether the given path is a valid image
        /// </summary>
        /// <param name="path">The path to check for validity</param>
        /// <returns>True if the given string is an image.</returns>
        public static bool IsValidImage(string path)
        {
            return Convert.ToBoolean(path.ToLower().IndexOf(".png") + path.ToLower().IndexOf(".jpg") + path.ToLower().IndexOf(".jpeg") + 3); // Tricky way to convert -3 (if not an image) to zero.
        }


        /// <summary>
        /// Ask user if they want to save their project before exititing / starting a new one
        /// </summary>
        /// <returns>false if user cancelled his action; true otherwise</returns>
        private bool AskUserToSave()
        {
            if (!isSaved)
            {
                MessageBoxResult mbr = MessageBox.Show("The project is unsaved. Do you want to save it first?", "Warning", MessageBoxButton.YesNoCancel);
                if (mbr == MessageBoxResult.Yes)
                {
                    SaveCommand_Executed(null, null);
                }
                else if (mbr == MessageBoxResult.Cancel)
                    return false;
            }
            return true;
        }


        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            if (chkLoop.IsLoaded && chkLoop.IsChecked.HasValue)
            {
                SetTitleAsSaved(false);
            }
        }

        private void SoundLoop_Clicked(object sender, RoutedEventArgs e)
        {
            if (chkSoundLoop.IsLoaded && chkSoundLoop.IsChecked.HasValue)
            {
                SetTitleAsSaved(false);
            }
        }

        private void SoundInterval_Changed(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tbxSoundInterval.IsLoaded && tbxSoundInterval.Value.HasValue)
            {
                SetTitleAsSaved(false);
            }
        }

        private void SoundVolume_Changed(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tbxSoundVolume.IsLoaded && tbxSoundVolume.Value.HasValue)
            {
                SetTitleAsSaved(false);
            }
        }

        private void SoundPitch_Changed(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tbxSoundPitch.IsLoaded && tbxSoundPitch.Value.HasValue)
            {
                SetTitleAsSaved(false);
            }
        }


        /// <summary>
        /// Ask user to save his project before exiting the application
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        protected void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Advanced_Save();
            if (!AskUserToSave())
                e.Cancel = true;
        }

        /// <summary>
        /// Check the application for updates and update if selected
        /// </summary>
        /// <param name="isRequested">Checking whether the user asked for the update or it was done at the start.</param>
        private void InstallUpdateSyncWithInfo(bool isRequested)
        {
            UpdateCheckInfo info = null;

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                if (ad.IsFirstRun)
                {
                    MessageBox.Show("Changelog:\n" + ad.CurrentVersion.ToString() + ": " + changelog);
                }

                try
                {
                    info = ad.CheckForDetailedUpdate();

                }
                catch (DeploymentDownloadException dde)
                {
                    if (isRequested)
                        MessageBox.Show("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message, "Error");
                    return;
                }
                catch (InvalidDeploymentException ide)
                {
                    MessageBox.Show("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message, "Error");
                    return;
                }
                catch (InvalidOperationException ioe)
                {
                    MessageBox.Show("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message, "Error");
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
                            MessageBox.Show("The application has been upgraded, you may now restart.");
                            ad.Update();
                            this.Close();
                        }
                        catch (DeploymentDownloadException dde)
                        {
                            MessageBox.Show("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
                            return;
                        }
                    }
                }
                else if (isRequested)
                {
                    MessageBox.Show("You have the latest version of LuAnimator", "No update available");
                }
                
            }
            else if (isRequested)
            {
                MessageBox.Show("You have the latest version of LuAnimator", "No update available");
            }
        }

        private void SetTitleAsSaved(bool saved)
        {
            if (!saved)
            {
                SetCurrentValue(TitleProperty, "*" + fileName + " - LuAnimator");
            }
            else
            {
                SetCurrentValue(TitleProperty, fileName + " - LuAnimator");
            }
            isSaved = saved;
        }
    }

    public static class Commands
    {
        public static readonly RoutedUICommand New = new RoutedUICommand
        (
            "New Project...",
            "New",
            typeof(Commands),
            new InputGestureCollection()
            {
                            new KeyGesture(Key.N, ModifierKeys.Control)
            }

        );

        public static readonly RoutedUICommand Open = new RoutedUICommand
        (
            "Open Project...",
            "Open",
            typeof(Commands),
            new InputGestureCollection()
            {
                            new KeyGesture(Key.O, ModifierKeys.Control)
            }

        );

        public static readonly RoutedUICommand Save = new RoutedUICommand
            (
                "Save Project...",
                "Save",
                typeof(Commands),
                new InputGestureCollection()
                {
                        new KeyGesture(Key.S, ModifierKeys.Control)
                }
            );

        public static readonly RoutedUICommand SaveAs = new RoutedUICommand
        (
            "Save Project As...",
            "SaveAs",
            typeof(Commands),
            new InputGestureCollection()
            {
                            new KeyGesture(Key.S, (ModifierKeys.Control | ModifierKeys.Shift))
            }

        );

        public static readonly RoutedUICommand About = new RoutedUICommand
        (
            "About...",
            "About",
            typeof(Commands),
            new InputGestureCollection()
            {
                            new KeyGesture(Key.F1)
            }

        );

        public static readonly RoutedCommand DeleteFrame = new RoutedCommand
        (
            "Delete",
            typeof(Commands),
            new InputGestureCollection()
            {
                                    new KeyGesture(Key.Delete)
            }

        );
    }
}