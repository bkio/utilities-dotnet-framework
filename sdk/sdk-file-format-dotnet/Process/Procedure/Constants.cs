/// Copyright 2022- Burak Kara, All rights reserved.

using System.Collections.Generic;

namespace SDKFileFormat.Process.Procedure
{
    public enum EProcessedFileType
    {
        NONE_OR_RAW,
        HIERARCHY_RAF,
        HIERARCHY_CF,
        GEOMETRY_RAF,
        GEOMETRY_CF,
        METADATA_RAF,
        METADATA_CF,
        UNREAL_HGM,
        UNREAL_HG,
        UNREAL_H,
        UNREAL_G
    }
    public class Constants
    {
        public enum EProcessStage : int
        {
            NotUploaded = 0,
            Uploaded_Processing = 1,
            Uploaded_ProcessFailed = 2,
            Uploaded_Processed = 3
        }

        public static readonly Dictionary<EProcessedFileType, string> ProcessedFileType_Extension_Map = new Dictionary<EProcessedFileType, string>()
        {
            [EProcessedFileType.HIERARCHY_RAF] = "hraf",
            [EProcessedFileType.HIERARCHY_CF] = "hcf",

            [EProcessedFileType.GEOMETRY_RAF] = "graf",
            [EProcessedFileType.GEOMETRY_CF] = "gcf",

            [EProcessedFileType.METADATA_RAF] = "mraf",
            [EProcessedFileType.METADATA_CF] = "mcf",

            [EProcessedFileType.UNREAL_HGM] = "hgm",
            [EProcessedFileType.UNREAL_HG] = "hg",
            [EProcessedFileType.UNREAL_H] = "h",
            [EProcessedFileType.UNREAL_G] = "gs"
        };

        public static readonly Dictionary<string, EProcessedFileType> ProcessedFileType_Enum_Map = new Dictionary<string, EProcessedFileType>()
        {
            ["hraf"] = EProcessedFileType.HIERARCHY_RAF,
            ["hcf"] = EProcessedFileType.HIERARCHY_CF,

            ["graf"] = EProcessedFileType.GEOMETRY_RAF,
            ["gcf"] = EProcessedFileType.GEOMETRY_CF,

            ["mraf"] = EProcessedFileType.METADATA_RAF,
            ["mcf"] = EProcessedFileType.METADATA_CF,

            ["hgm"] = EProcessedFileType.UNREAL_HGM,
            ["hg"] = EProcessedFileType.UNREAL_HG,
            ["h"] = EProcessedFileType.UNREAL_H,
            ["gs"] = EProcessedFileType.UNREAL_G,
            ["-1"] = EProcessedFileType.NONE_OR_RAW,
            ["1"] = EProcessedFileType.NONE_OR_RAW,
            ["2"] = EProcessedFileType.NONE_OR_RAW,
            ["3"] = EProcessedFileType.NONE_OR_RAW,
            ["4"] = EProcessedFileType.NONE_OR_RAW,
            ["5"] = EProcessedFileType.NONE_OR_RAW,
            ["6"] = EProcessedFileType.NONE_OR_RAW,
            ["7"] = EProcessedFileType.NONE_OR_RAW
        };

        public static readonly Dictionary<EProcessedFileType, string> ProcessedFileType_FolderPrefix_Map = new Dictionary<EProcessedFileType, string>()
        {
            [EProcessedFileType.HIERARCHY_RAF] = "hierarchy_raf/",
            [EProcessedFileType.HIERARCHY_CF] = "hierarchy_cf/",

            [EProcessedFileType.GEOMETRY_RAF] = "geometry_raf/",
            [EProcessedFileType.GEOMETRY_CF] = "geometry_cf/",

            [EProcessedFileType.METADATA_RAF] = "metadata_raf/",
            [EProcessedFileType.METADATA_CF] = "metadata_cf/",

            [EProcessedFileType.UNREAL_HGM] = "unreal_hgm/",
            [EProcessedFileType.UNREAL_HG] = "unreal_hg/",
            [EProcessedFileType.UNREAL_H] = "unreal_h/",
            [EProcessedFileType.UNREAL_G] = "unreal_g/"
        };
    }
}