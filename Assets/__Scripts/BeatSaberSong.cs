using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class BeatSaberSong {

    public static readonly Color DEFAULT_LEFTCOLOR = Color.red;
    public static readonly Color DEFAULT_RIGHTCOLOR = new Color(0, 0.282353f, 1, 1);
    public static readonly Color DEFAULT_LEFTNOTE = new Color(0.7352942f, 0, 0);
    public static readonly Color DEFAULT_RIGHTNOTE = new Color(0, 0.3701827f, 0.7352942f);

    [Serializable]
    public class DifficultyBeatmap
    {
        public string difficulty = "Easy";
        public int difficultyRank = 1;
        public string beatmapFilename = "Easy.dat";
        public float noteJumpMovementSpeed = 16;
        public float noteJumpStartBeatOffset = 0;
        public Color colorLeft = DEFAULT_LEFTNOTE;
        public Color colorRight = DEFAULT_RIGHTNOTE;
        public Color envColorLeft = DEFAULT_LEFTCOLOR;
        public Color envColorRight = DEFAULT_RIGHTCOLOR;
        public Color obstacleColor = DEFAULT_LEFTCOLOR;
        public JSONNode customData;
        [NonSerialized] public DifficultyBeatmapSet parentBeatmapSet;

        public DifficultyBeatmap (DifficultyBeatmapSet beatmapSet)
        {
            parentBeatmapSet = beatmapSet;
        }

        public void UpdateParent(DifficultyBeatmapSet newParentSet)
        {
            parentBeatmapSet = newParentSet;
        }

        public void UpdateName(string fileName = null)
        {
            if (fileName is null) beatmapFilename = $"{difficulty}{parentBeatmapSet.beatmapCharacteristicName}.dat";
            else beatmapFilename = fileName;
        }
    }

    [Serializable]
    public class DifficultyBeatmapSet
    {
        public string beatmapCharacteristicName = "Standard";
        public List<DifficultyBeatmap> difficultyBeatmaps = new List<DifficultyBeatmap>();

        public DifficultyBeatmapSet() {
            beatmapCharacteristicName = "Standard";
        }
        public DifficultyBeatmapSet(string CharacteristicName)
        {
            beatmapCharacteristicName = CharacteristicName;
        }
    }

    public string songName = "New Song";
    
    public string directory;
    public JSONNode json;

    public string version = "2.0.0";
    public string songSubName = "";
    public string songAuthorName = "";
    public string levelAuthorName = "";
    public float beatsPerMinute = 100;
    public float songTimeOffset = 0;
    public float previewStartTime = 12;
    public float previewDuration = 10;
    public float shuffle = 0;
    public float shufflePeriod = 0.5f;
    public string songFilename = "song.ogg"; // .egg file extension is a problem solely beat saver deals with, work with .ogg for the mapper
    public string coverImageFilename = "cover.png";
    public string environmentName = "DefaultEnvironment";
    public JSONNode customData;

    private bool isWIPMap = false;

    public List<DifficultyBeatmapSet> difficultyBeatmapSets = new List<DifficultyBeatmapSet>();

    public List<string> warnings = new List<string>();
    public List<string> suggestions = new List<string>();
    public List<string> requirements = new List<string>();

    public BeatSaberSong(string directory, JSONNode json) {
        this.directory = directory;
        this.json = json;
    }

    public BeatSaberSong(bool wipmap)
    {
        directory = null;
        json = null;
        isWIPMap = wipmap;
    }

    public void SaveSong() {
        try {
            if (string.IsNullOrEmpty(directory))
                directory = $"{(isWIPMap ? Settings.Instance.CustomWIPSongsFolder : Settings.Instance.CustomSongsFolder)}/{songName}";
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (json == null) json = new JSONObject();
            if (customData == null) customData = new JSONObject();

            //Just in case, i'm moving them up here
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            json["_version"] = version;
            json["_songName"] = songName;
            json["_songSubName"] = songSubName;
            json["_songAuthorName"] = songAuthorName;
            json["_levelAuthorName"] = levelAuthorName;

            json["_beatsPerMinute"] = beatsPerMinute;
            json["_previewStartTime"] = previewStartTime;
            json["_previewDuration"] = previewDuration;
            json["_songTimeOffset"] = songTimeOffset;

            json["_shuffle"] = shuffle;
            json["_shufflePeriod"] = shufflePeriod;

            json["_coverImageFilename"] = coverImageFilename;
            json["_songFilename"] = songFilename;

            json["_environmentName"] = environmentName;
            json["_customData"] = customData;

            //BeatSaver schema changes, see below comment.
            if (string.IsNullOrEmpty(customData["_contributors"])) json["_customData"].Remove("_contributors");
            if (string.IsNullOrEmpty(customData["_customEnvironment"])) json["_customData"].Remove("_customEnvironment");
            if (string.IsNullOrEmpty(customData["_customEnvironmentHash"])) json["_customData"].Remove("_customEnvironmentHash");
            if (json["_customData"].Linq.Count() <= 0) json.Remove("_customData");

            JSONArray sets = new JSONArray();
            foreach (DifficultyBeatmapSet set in difficultyBeatmapSets)
            {
                JSONNode setNode = new JSONObject();
                setNode["_beatmapCharacteristicName"] = set.beatmapCharacteristicName;
                JSONArray diffs = new JSONArray();
                foreach(DifficultyBeatmap diff in set.difficultyBeatmaps)
                {
                    JSONNode subNode = new JSONObject();
                    subNode["_difficulty"] = diff.difficulty;
                    subNode["_difficultyRank"] = diff.difficultyRank;
                    subNode["_beatmapFilename"] = diff.beatmapFilename;
                    subNode["_noteJumpMovementSpeed"] = diff.noteJumpMovementSpeed;
                    subNode["_noteJumpStartBeatOffset"] = diff.noteJumpStartBeatOffset;
                    subNode["_customData"] = diff.customData;

                    if (diff.colorLeft != DEFAULT_LEFTNOTE)
                        subNode["_customData"]["_colorLeft"] = GetJSONNodeFromColor(diff.colorLeft);
                    if (diff.colorRight != DEFAULT_RIGHTNOTE)
                        subNode["_customData"]["_colorRight"] = GetJSONNodeFromColor(diff.colorRight);
                    if (diff.envColorLeft != DEFAULT_LEFTCOLOR && diff.envColorLeft != diff.colorLeft)
                        subNode["_customData"]["_envColorLeft"] = GetJSONNodeFromColor(diff.envColorLeft);
                    if (diff.envColorRight != DEFAULT_RIGHTCOLOR && diff.envColorRight != diff.colorRight)
                        subNode["_customData"]["_envColorRight"] = GetJSONNodeFromColor(diff.envColorRight);
                    if (diff.obstacleColor != DEFAULT_LEFTCOLOR)
                        subNode["_customData"]["_obstacleColor"] = GetJSONNodeFromColor(diff.obstacleColor);


                    /*
                     * More BeatSaver Schema changes, yayyyyy! (fuck)
                     * If any additional non-required fields are present, they cannot be empty.
                     * 
                     * So ChroMapper is just gonna yeet anything that is null or empty, then keep going down the list.
                     * If customData is empty, then we just yeet that.
                     */
                    if (string.IsNullOrEmpty(diff.customData["_difficultyLabel"])) subNode["_customData"].Remove("_difficultyLabel");
                    if (diff.customData["_editorOldOffset"] != null && diff.customData["_editorOldOffset"].AsFloat <= 0)
                        subNode["_customData"].Remove("_editorOldOffset"); //For some reason these are used by MM but not by CM
                    if (diff.customData["_editorOffset"] != null && diff.customData["_editorOffset"].AsFloat <= 0)
                        subNode["_customData"].Remove("_editorOffset"); //So we're just gonna yeet them. Sorry squanksers.
                    if (diff.customData["_warnings"] != null && diff.customData["_warnings"].AsArray.Count <= 0)
                        subNode["_customData"].Remove("_warnings");
                    if (diff.customData["_information"] != null && diff.customData["_information"].AsArray.Count <= 0)
                        subNode["_customData"].Remove("_information");
                    if (diff.customData["_suggestions"] != null && diff.customData["_suggestions"].AsArray.Count <= 0)
                        subNode["_customData"].Remove("_suggestions");
                    if (diff.customData["_requirements"] != null && diff.customData["_requirements"].AsArray.Count <= 0)
                        subNode["_customData"].Remove("_requirements");
                    if (subNode["_customData"].Linq.Count() <= 0) subNode.Remove("_customData");

                    diffs.Add(subNode);
                }
                setNode["_difficultyBeatmaps"] = diffs;
                sets.Add(setNode);
            }

            json["_difficultyBeatmapSets"] = sets;

            using (StreamWriter writer = new StreamWriter(directory + "/info.dat", false))
                writer.Write(json.ToString(2));

            Debug.Log("Saved song info.dat for " + songName);

        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    public static BeatSaberSong GetSongFromFolder(string directory) {

        try {

            JSONNode mainNode = GetNodeFromFile(directory + "/info.dat");
            if (mainNode == null) return null;

            BeatSaberSong song = new BeatSaberSong(directory, mainNode);

            JSONNode.Enumerator nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext()) {
                string key = nodeEnum.Current.Key;
                JSONNode node = nodeEnum.Current.Value;

                switch (key) {
                    case "_songName": song.songName = node.Value; break;
                    case "_songSubName": song.songSubName = node.Value; break;
                    case "_songAuthorName": song.songAuthorName = node.Value; break;
                    case "_levelAuthorName": song.levelAuthorName = node.Value; break;

                    case "_beatsPerMinute": song.beatsPerMinute = node.AsFloat; break;
                    case "_songTimeOffset": song.songTimeOffset = node.AsFloat; break;
                    case "_previewStartTime": song.previewStartTime = node.AsFloat; break;
                    case "_previewDuration": song.previewDuration = node.AsFloat; break;
                        
                    case "_shuffle": song.shuffle = node.AsFloat; break;
                    case "_shufflePeriod": song.shufflePeriod = node.AsFloat; break;

                    case "_coverImageFilename": song.coverImageFilename = node.Value; break;
                    case "_songFilename": song.songFilename = node.Value; break;
                    case "_environmentName": song.environmentName = node.Value; break;

                    case "_customData": song.customData = node; break;

                    case "_difficultyBeatmapSets":
                        foreach (JSONNode n in node) {
                            DifficultyBeatmapSet set = new DifficultyBeatmapSet();
                            set.beatmapCharacteristicName = n["_beatmapCharacteristicName"];
                            foreach (JSONNode d in n["_difficultyBeatmaps"])
                            {
                                DifficultyBeatmap beatmap = new DifficultyBeatmap(set)
                                {
                                    difficulty = d["_difficulty"].Value,
                                    difficultyRank = d["_difficultyRank"].AsInt,
                                    noteJumpMovementSpeed = d["_noteJumpMovementSpeed"].AsFloat,
                                    noteJumpStartBeatOffset = d["_noteJumpStartBeatOffset"].AsFloat,
                                    customData = d["_customData"],
                                };
                                if (d["_customData"]["_colorLeft"] != null)
                                    beatmap.colorLeft = GetColorFromJSONNode(d["_customData"]["_colorLeft"]);
                                if (d["_customData"]["_colorRight"] != null)
                                    beatmap.colorRight = GetColorFromJSONNode(d["_customData"]["_colorRight"]);
                                if (d["_customData"]["_envColorLeft"] != null)
                                    beatmap.envColorLeft = GetColorFromJSONNode(d["_customData"]["_envColorLeft"]);
                                else if (d["_customData"]["_colorLeft"] != null) beatmap.envColorLeft = beatmap.colorLeft;
                                if (d["_customData"]["_envColorRight"] != null)
                                    beatmap.envColorRight = GetColorFromJSONNode(d["_customData"]["_envColorRight"]);
                                else if (d["_customData"]["_colorRight"] != null) beatmap.envColorRight = beatmap.colorRight;
                                if (d["_customData"]["_obstacleColor"] != null)
                                    beatmap.obstacleColor = GetColorFromJSONNode(d["_customData"]["_obstacleColor"]);
                                beatmap.UpdateName(d["_beatmapFilename"]);
                                set.difficultyBeatmaps.Add(beatmap);
                            }
                            song.difficultyBeatmapSets.Add(set);
                        }
                        break;
                }
            }
            return song;
        } catch (Exception e) {
            Debug.LogError(e);
            return null;
        }
    }

    public BeatSaberMap GetMapFromDifficultyBeatmap(DifficultyBeatmap data) {

        JSONNode mainNode = GetNodeFromFile(directory + "/" + data.beatmapFilename);
        if (mainNode == null) {
            Debug.LogWarning("Failed to get difficulty json file "+(directory + "/" + data.beatmapFilename));
            return null;
        }

        return BeatSaberMap.GetBeatSaberMapFromJSON(mainNode, directory + "/" + data.beatmapFilename);
    }

    private static Color GetColorFromJSONNode(JSONNode node)
    {
        return new Color(node["r"].AsFloat, node["g"].AsFloat, node["b"].AsFloat);
    }

    private JSONNode GetJSONNodeFromColor(Color color)
    {
        JSONObject obj = new JSONObject();
        obj["r"] = color.r;
        obj["g"] = color.g;
        obj["b"] = color.b;
        return obj;
    }

    private static JSONNode GetNodeFromFile(string file) {
        if (!File.Exists(file)) return null;
        try {
            using (StreamReader reader = new StreamReader(file)) {
                JSONNode node = JSON.Parse(reader.ReadToEnd());
                return node;
            }
        } catch (Exception e) {
            Debug.LogError(e);
        }
        return null;
    }

}
