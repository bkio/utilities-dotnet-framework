/// Copyright 2022- Burak Kara, All rights reserved.

namespace WebServiceUtilities.Common
{
    public enum EVMStatus : int
    {
        Available = 0,
        Busy = 1,
        NotResponding = 2,
        Stopped = 3
    }

    public enum EProcessStage : int
    {
        Stage0_FileUpload = 0,
        Stage1_PullingData = 1,
        Stage2_ExtractingGeometryInfo = 2,
        Stage3_OptimizingModel = 3,
        Stage4_FilteringModel = 4,
        Stage5_CustomPlatformConvertion = 5,
        Stage6_UnrealEngineConvertion = 6
    }
    public enum EProcessStatus : int
    {
        Idle = 0,
        Processing = 1,
        Canceled = 2,
        Failed = 3,
        Completed = 4
    }
    public enum EFileProcessStatus : int
    {
        NotUploaded = 0,
        Processing = 1,
        ProcessFailed = 2,
        ProcessCanceled = 3,
        Processed = 4,
        Queued = 5
    }
    public enum EInternalProcessStage
    {
        Queued = 0,
        Processing = 1,
        ProcessFailed = 2,
        ProcessComplete = 3,
        ProcessCanceled = 4
    }
    public enum EOptimizationPreset : int
    {
        Default = 0,
        LargeFiles_WideOpenFactory_WithUniformCompactness = 1,
        MediumFiles_WideOpenFactory_WithUniformCompactness = 2,
        WideArea_WithCompactModels = 3,
        VeryDenseVessel = 4,
        TopsideOilRig = 5
    }
    public enum EProcessMode : int
    {
        Undefined = 0,
        Kubernetes = 1,
        VirtualMachine = 2
    }
    public enum EGetClearance
    {
        Yes,
        No
    }
}
