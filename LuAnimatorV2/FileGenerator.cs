﻿using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Linq;

using Silverfeelin.StarboundDrawables;
using DrawablesGeneratorTool;

using FormCollection = System.Collections.ObjectModel.ObservableCollection<LuAnimatorV2.modeNode>;
using AnimationCollection = System.Collections.ObjectModel.ObservableCollection<System.Collections.ObjectModel.ObservableCollection<LuAnimatorV2.modeNode>>;

namespace LuAnimatorV2
{
    /// <summary>
    /// Saves and loads the luanimation projects
    /// </summary>
    class FileGenerator
    {
        /// <summary>
        /// Generate the starbound single texture directive by the given frame
        /// </summary>
        /// <param name="bsource">A frame to create directives from</param>
        /// <returns>The directive string</returns>
        protected static string GenerateDirective(BitmapSource bsource)
        {
            Bitmap bmp = BitmapConverter.BitmapSourceToBitmap(bsource);
            DrawablesGenerator generator = new DrawablesGenerator(BitmapConverter.BitmapSourceToBitmap(bsource));
            bmp.Dispose();
            generator = DrawableUtilities.SetUpGenerator(generator, 0, 0);
            generator.ReplaceBlank = true;
            generator.ReplaceWhite = true;

            DrawablesOutput output = generator.Generate();

            return DrawableUtilities.GenerateSingleTextureDirectives(output, 64);

        }

        /// <summary>
        /// Create Json object by the animation collection
        /// </summary>
        /// <param name="animationCollection">Animation</param>
        /// <returns>Json containing line, ready to be saved</returns>
        public static string ToJson(AnimationCollection animationCollection)
        {
            JArray animationJSON = new JArray();

            foreach (FormCollection form in animationCollection)
            {
                JObject jsonJSON = new JObject();
                foreach (modeNode mode in form)
                {
                    jsonJSON[mode.modeName] = new JObject();
                    JObject json = (JObject)jsonJSON[mode.modeName];

                    json["emotes"] = new JObject();
                    JObject jsonEmoteList = (JObject)json["emotes"];

                    int xposition = mode.xtranslation;
                    int yposition = mode.ytranslation;

                    foreach (emoteNode emote in mode.emotes)
                    {
                        jsonEmoteList[emote.name] = new JObject();
                        JObject jsonEmote = (JObject)jsonEmoteList[emote.name];


                        JObject jsonFrames = new JObject();
                        JObject jsonFFrames = new JObject();

                        int i = 1;
                        if (emote.frames != null)
                        {
                            foreach (BitmapSource imglst in emote.frames)
                            {
                                jsonFrames[i.ToString()] = GenerateDirective(imglst);
                                i += emote.speed;
                            }
                            jsonEmote["frames"] = jsonFrames;
                        }
                        int j = 1;
                        if (emote.fullbrightFrames != null)
                        {
                            foreach (BitmapSource imglst in emote.fullbrightFrames)
                            {
                                jsonFFrames[j.ToString()] = GenerateDirective(imglst);
                                j += emote.speed;
                            }
                            jsonEmote["fullbrightFrames"] = jsonFFrames;
                        }

                        if (emote.looping)
                            jsonEmote["limit"] = Math.Max(i, j);
                        else
                            jsonEmote["limit"] = -1;

                        jsonEmote["speed"] = emote.speed;

                        if (emote.sound != null)
                        {
                            jsonEmote["sound"] = JArray.FromObject(emote.sound);
                            jsonEmote["soundLoop"] = emote.soundLoop;
                            jsonEmote["soundInterval"] = Math.Ceiling(emote.soundInterval * 60);
                            jsonEmote["soundPitch"] = emote.soundPitch;
                            jsonEmote["soundVolume"] = emote.soundVolume;
                        }
                    }
                    json["properties"] = new JObject();
                    json["properties"]["isInvisible"] = mode.invisible;
                    json["properties"]["frameScale"] = mode.framescale;


                    JObject translation = JObject.Parse("{'translation':[0,0]}");
                    translation["translation"][0] = mode.xtranslation * 0.0625;
                    translation["translation"][1] = mode.ytranslation * 0.0625;
                    json["properties"]["translation"] = translation["translation"];


                    if (mode.modeName == "Activate" || mode.modeName == "Deactivate" || mode.modeName == "Sitting_Down" || mode.modeName == "Standing_Up" || mode.modeName == "Transform_Next" || mode.modeName == "Transform_Previous" || mode.modeName == "Primary_Fire" || mode.modeName == "Alt_Fire")
                        json["properties"]["playOnce"] = true;
                }
                if (jsonJSON.ToString() != "{}")
                    animationJSON.Add(jsonJSON);
            }

            return animationJSON.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Generates the 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static AnimationCollection ToAnimationCollection(string path)
        {
            AnimationCollection animation = new AnimationCollection();

            JToken settings = JToken.Parse(File.ReadAllText(path));
            int currentForm = -1;

            try
            {
                foreach (JToken jForm in settings)
                {
                    currentForm++;
                    if (animation.ElementAtOrDefault(currentForm) == null)
                        animation.Add(new FormCollection());

                    foreach (JProperty jState in jForm)
                    {
                        string stateName = jState.Name;

                        modeNode mode = animation[currentForm].FirstOrDefault(form => form.modeName == stateName);
                        if (mode == null)
                        {
                            mode = new modeNode();
                            mode.modeName = stateName;
                            mode.emotes = new System.Collections.Generic.List<emoteNode>();
                            animation[currentForm].Add(mode);
                        }

                        mode.invisible = jState.Value["properties"]["isInvisible"].ToObject<bool>();
                        mode.xtranslation = (int)(jState.Value["properties"]["translation"].ToObject<double[]>()[0] / 0.0625);
                        mode.ytranslation = (int)(jState.Value["properties"]["translation"].ToObject<double[]>()[1] / 0.0625);
                        mode.framescale = jState.Value["properties"]["frameScale"].ToObject<int>();


                        foreach (JProperty jEmote in jState.Value["emotes"])
                        {
                            string emoteName = jEmote.Name;

                            emoteNode emote = mode.emotes.FirstOrDefault(em => em.name == emoteName);
                            if (emote == null)
                            {
                                emote = new emoteNode();
                                emote.name = emoteName;

                                mode.emotes.Add(emote);
                            }

                            emote.soundPitch = jEmote.Value["soundPitch"].ToObject<double>();
                            emote.soundVolume = jEmote.Value["soundVolume"].ToObject<double>();
                            emote.soundInterval = jEmote.Value["soundInterval"].ToObject<double>() / 60;
                            emote.soundLoop = jEmote.Value["soundLoop"].ToObject<bool>();
                            emote.sound = jEmote.Value["sound"].ToObject<string[]>();
                            emote.looping = (jEmote.Value["limit"].ToObject<int>() > 0);

                            emote.speed = jEmote.Value["speed"].ToObject<int>();

                            if (jEmote.Value["frames"] != null)
                            {
                                BitmapSource[] frames = new BitmapSource[jEmote.Value["frames"].Count()];
                                int i = 0;
                                foreach (string directive in jEmote.Value["frames"])
                                {
                                    Bitmap bmp = new Bitmap(64, 64);
                                    StarCheatReloaded.GUI.Directive.ApplyDirectives(ref bmp, directive);

                                    frames[i++] = BitmapConverter.BitmapToBitmapSource(bmp);
                                    bmp.Dispose();
                                }

                                emote.frames = frames;
                            }

                            if (jEmote.Value["fullbrightFrames"] != null)
                            {
                                BitmapSource[] fullbrightFrames = new BitmapSource[jEmote.Value["frames"].Count()];
                                int i = 0;
                                foreach (string directive in jEmote.Value["frames"])
                                {
                                    Bitmap bmp = new Bitmap(64, 64);
                                    StarCheatReloaded.GUI.Directive.ApplyDirectives(ref bmp, directive);

                                    fullbrightFrames[i++] = BitmapConverter.BitmapToBitmapSource(bmp);
                                    bmp.Dispose();
                                }

                                emote.fullbrightFrames = fullbrightFrames;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return animation;
        }
    }
}
