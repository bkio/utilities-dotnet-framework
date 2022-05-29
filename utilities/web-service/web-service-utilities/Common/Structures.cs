/// Copyright 2022- Burak Kara, All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebServiceUtilities.Common
{
    public class FileConversionProgressData
    {
        public const string MODEL_NAME_PROPERTY = "modelName";
        public const string MODEL_REVISION_PROPERTY = "modelRevision";
        public const string GLOBAL_CURRENT_STAGE_PROPERTY = "globalCurrentStage";
        public const string NAVISWORKS_TRIANGLE_COUNT_PROPERTY = "navisworksTriangleCount";
        public const string NAVISWORKS_TARGET_CHUNK_COUNT_PROPERTY = "navisworksTargetChunkCount";
        public const string NAVISWORKS_CHUNKS_COMPLETE_PROPERTY = "navisworksChunksComplete";
        public const string NAVISWORKS_CHUNK_TRIANGLES_PROPERTY = "navisworksChunkTriangles";

        public const string PIXYZ_CHUNKS_PROCESSED_PROPERTY = "pixyzChunksProcessed";
        public const string PIXYZ_LODS_PROPERTY = "pixyzLODs";
        public const string PIXYZ_LOD_TRIANGLES_PROPERTY = "pixyzLODTriangles";
        public const string PIXYZ_LOD_PROCESSING_TIME_PROPERTY = "pixyzLODProcessingTime";

        [JsonProperty(MODEL_NAME_PROPERTY)]
        public string ModelName;

        [JsonProperty(MODEL_REVISION_PROPERTY)]
        public int ModelRevision;

        [JsonProperty(GLOBAL_CURRENT_STAGE_PROPERTY)]
        public int GlobalCurrentStage;

        [JsonProperty(NAVISWORKS_TRIANGLE_COUNT_PROPERTY)]
        public int NavisworksTriangleCount;

        [JsonProperty(NAVISWORKS_TARGET_CHUNK_COUNT_PROPERTY)]
        public int NavisworksTargetChunkCount;

        [JsonProperty(NAVISWORKS_CHUNKS_COMPLETE_PROPERTY)]
        public int NavisworksChunksComplete;

        [JsonProperty(NAVISWORKS_CHUNK_TRIANGLES_PROPERTY)]
        public List<int> NavisworksChunkTriangles;

        [JsonProperty(PIXYZ_CHUNKS_PROCESSED_PROPERTY)]
        public int PixyzChunksProcessed;

        [JsonProperty(PIXYZ_LODS_PROPERTY)]
        public int PixyzLods;

        [JsonProperty(PIXYZ_LOD_TRIANGLES_PROPERTY)]
        public List<int> PixyzLodTriangles = new List<int>();

        [JsonProperty(PIXYZ_LOD_PROCESSING_TIME_PROPERTY)]
        public List<int> PixyzLodProcessingTime = new List<int>();
    }

    public class ConversionProgressInfo
    {
        public const string MODEL_ID_PROPERTY = "modelId";
        public const string VM_ID_PROPERTY = "vmId";
        public const string PROCESS_STATUS_PROPERTY = "processStatus";
        public const string INFO_PROPERTY = "info";
        public const string ERROR_PROPERTY = "error";
        public const string PROCESS_FAILED_PROPERTY = "processFailed";
        public const string PROGRESS_DETAILS_PROPERTY = "progressDetails";

        [JsonProperty(MODEL_ID_PROPERTY)]
        public string ModelId;

        [JsonProperty(VM_ID_PROPERTY)]
        public string VMId;

        [JsonProperty(PROCESS_STATUS_PROPERTY)]
        public int ProcessStatus = (int)EProcessStatus.Idle;

        [JsonProperty(INFO_PROPERTY)]
        public string Info;

        [JsonProperty(ERROR_PROPERTY)]
        public string Error;

        [JsonProperty(PROCESS_FAILED_PROPERTY)]
        public bool ProcessFailed;

        [JsonProperty(PROGRESS_DETAILS_PROPERTY)]
        public FileConversionProgressData ProgressDetails = new FileConversionProgressData();
    }

    public class ModelProcessTask
    {
        public const string MODEL_ID_PROPERTY = "modelId";
        public const string MODEL_NAME_PROPERTY = "modelName";
        public const string MODEL_REVISION_PROPERTY = "modelRevision";

        public const string STAGE_DOWNLOAD_URL_PROPERTY = "stageDownloadUrl";
        public const string LOG_DOWNLOAD_URL_PROPERTY = "logDownloadUrl";

        public const string PROCESS_STEP_PROPERTY = "processStep";

        public const string ZIP_MAIN_ASSEMBLY_FILE_NAME_IF_ANY_PROPERTY = "zipMainAssemblyFileNameIfAny";

        public const string GLOBAL_SCALE_PROPERTY = "globalScale";
        public const string GLOBAL_X_OFFSET_PROPERTY = "globalXOffset";
        public const string GLOBAL_Y_OFFSET_PROPERTY = "globalYOffset";
        public const string GLOBAL_Z_OFFSET_PROPERTY = "globalZOffset";
        public const string GLOBAL_X_ROTATION_PROPERTY = "globalXRotation";
        public const string GLOBAL_Y_ROTATION_PROPERTY = "globalYRotation";
        public const string GLOBAL_Z_ROTATION_PROPERTY = "globalZRotation";

        public const string CUSTOM_PYTHON_SCRIPT_PROPERTY = "customPythonScript";

        public const string LOD_PARAMETERS_PROPERTY = "lodParameters";

        public const string LEVEL_THRESHOLDS_PROPERTY = "levelThresholds";
        public const string CULLING_THRESHOLDS_PROPERTY = "cullingThresholds";

        public const string MERGING_PARTS_PROPERTY = "mergingParts";
        public const string ADVANCED_PARAMETERS_PROPERTY = "advancedParameters";

        public const string MERGE_FINAL_LEVEL_PROPERTY = "mergeFinalLevel";
        public const string DELETE_DUPLICATES_PROPERTY = "deleteDuplicates";

        [JsonProperty(MODEL_ID_PROPERTY)]
        public string ModelId;

        [JsonProperty(MODEL_NAME_PROPERTY)]
        public string ModelName;

        [JsonProperty(MODEL_REVISION_PROPERTY)]
        public int ModelRevision;

        [JsonProperty(STAGE_DOWNLOAD_URL_PROPERTY)]
        public string StageDownloadUrl;

        [JsonProperty(LOG_DOWNLOAD_URL_PROPERTY)]
        public string LogDownloadUrl;

        [JsonProperty(PROCESS_STEP_PROPERTY)]
        public int ProcessStep = (int)EProcessStage.Stage0_FileUpload;

        [JsonProperty(ZIP_MAIN_ASSEMBLY_FILE_NAME_IF_ANY_PROPERTY)]
        public string ZipMainAssemblyFileNameIfAny;

        [JsonProperty(GLOBAL_SCALE_PROPERTY)]
        public float GlobalScale;

        [JsonProperty(GLOBAL_X_OFFSET_PROPERTY)]
        public float GlobalXOffset;

        [JsonProperty(GLOBAL_Y_OFFSET_PROPERTY)]
        public float GlobalYOffset;

        [JsonProperty(GLOBAL_Z_OFFSET_PROPERTY)]
        public float GlobalZOffset;

        [JsonProperty(GLOBAL_X_ROTATION_PROPERTY)]
        public float GlobalXRotation;

        [JsonProperty(GLOBAL_Y_ROTATION_PROPERTY)]
        public float GlobalYRotation;

        [JsonProperty(GLOBAL_Z_ROTATION_PROPERTY)]
        public float GlobalZRotation;

        [JsonProperty(CUSTOM_PYTHON_SCRIPT_PROPERTY)]
        public string CustomPythonScript;

        [JsonProperty(LEVEL_THRESHOLDS_PROPERTY)]
        public string LevelThresholds;

        [JsonProperty(CULLING_THRESHOLDS_PROPERTY)]
        public string CullingThresholds;

        [JsonProperty(LOD_PARAMETERS_PROPERTY)]
        public string LODParameters;

        [JsonProperty(MERGING_PARTS_PROPERTY)]
        public string MergingParts;

        [JsonProperty(ADVANCED_PARAMETERS_PROPERTY)]
        public string AdvancedParameters;

        [JsonProperty(MERGE_FINAL_LEVEL_PROPERTY)]
        public bool bMergeFinalLevel;

        [JsonProperty(DELETE_DUPLICATES_PROPERTY)]
        public bool bDeleteDuplicates;
    }
}