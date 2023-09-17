/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Newtonsoft.Json.Linq;
using CommonUtilities;
using System.Linq;
using System.Net.NetworkInformation;
using NetworkInterface = Google.Apis.Compute.v1.Data.NetworkInterface;

namespace CloudServiceUtilities.VMServices
{
    public class VMServiceGC : IVMServiceInterface
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly string ProjectID;
        private readonly string ZoneName;

        private readonly ServiceAccountCredential Credential;

        private readonly string ProgramUniqueID;

        /// <summary>
        /// 
        /// <para>VMServiceGC: Parametered Constructor for Managed Service by Google</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ProgramUniqueID"/>           Program Unique ID</para>
        /// <para><paramref name="_ProjectID"/>                 GC Project ID</para>
        /// <para><paramref name="_ZoneName"/>                  GC Compute Engine Zone Name</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public VMServiceGC(
            string _ProgramUniqueID,
            string _ProjectID,
            string _ZoneName,
            Action<string> _ErrorMessageAction = null)
        {
            ProgramUniqueID = _ProgramUniqueID;
            ProjectID = _ProjectID;
            ZoneName = _ZoneName;
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                string ApplicationCredentialsPlain = Environment.GetEnvironmentVariable("GOOGLE_PLAIN_CREDENTIALS");
                string ApplicationCredentialsBase64 = Environment.GetEnvironmentVariable("GOOGLE_BASE64_CREDENTIALS");
                if (ApplicationCredentials == null && ApplicationCredentialsPlain == null && ApplicationCredentialsBase64 == null)
                {
                    _ErrorMessageAction?.Invoke("VMServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS or GOOGLE_BASE64_CREDENTIALS) environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    if (ApplicationCredentials == null)
                    {
                        if (ApplicationCredentialsPlain != null && !Utility.HexDecode(out ApplicationCredentialsPlain, ApplicationCredentialsPlain, _ErrorMessageAction))
                        {
                            throw new Exception("Hex decode operation for application credentials plain has failed.");
                        }
                        else if (!Utility.Base64Decode(out ApplicationCredentialsPlain, ApplicationCredentialsBase64, _ErrorMessageAction))
                        {
                            throw new Exception("Base64 decode operation for application credentials plain has failed.");
                        }
                        Credential = GoogleCredential.FromJson(ApplicationCredentialsPlain)
                                         .CreateScoped(
                                            new string[]
                                            {
                                                ComputeService.Scope.Compute,
                                                ComputeService.Scope.CloudPlatform
                                            })
                                         .UnderlyingCredential as ServiceAccountCredential;
                    }
                    else
                    {
                        using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                        {
                            Credential = GoogleCredential.FromStream(Stream)
                                         .CreateScoped(
                                            new string[]
                                            {
                                                ComputeService.Scope.Compute,
                                                ComputeService.Scope.CloudPlatform
                                            })
                                         .UnderlyingCredential as ServiceAccountCredential;
                        }
                    }

                    if (Credential != null)
                    {
                        bInitializationSucceed = true;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"VMServiceGC->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        private ComputeService GetService()
        {
            return new ComputeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential,
                ApplicationName = ProgramUniqueID
            });
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="ITracingServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        public bool CreateInstance(
            string _UniqueInstanceName,
            string _Description,
            string _MachineType,
            long _DiskSizeGB,
            int _GpuCount,
            string _GpuName,
            string _OSSourceImageURL,
            EVMDiskType _DiskType,
            EVMOSType _OSType,
            IDictionary<string, string> _Labels,
            VMNetworkFirewall _FirewallSettings,
            string _OptionalStartupScript,
            out int _ErrorCode,
            Action<string> _ErrorMessageAction = null)
        {
            _ErrorCode = 400;

            if (!Utility.CalculateStringMD5(Utility.RandomString(32, true), out string RandomFirewallTag, _ErrorMessageAction))
            {
                _ErrorMessageAction?.Invoke("VMServiceGC->CreateInstance: Firewall tag MD5 generation has failed.");
                return false;
            }

            RandomFirewallTag = $"a-{RandomFirewallTag}";

            try
            {
                using (var Service = GetService())
                {
                    var NewInstance = new Instance()
                    {
                        Name = _UniqueInstanceName,
                        Description = _Description,
                        DeletionProtection = false,
                        Zone = $"projects/{ProjectID}/zones/{ZoneName}",
                        Labels = _Labels,
                        MachineType = $"projects/{ ProjectID}/zones/{ZoneName}/machineTypes/{_MachineType}",
                        Disks = new List<AttachedDisk>()
                        {
                            new AttachedDisk()
                            {
                                AutoDelete = true,
                                Boot = true,
                                Kind = "compute#attachedDisk",
                                DeviceName = _UniqueInstanceName,
                                Mode = "READ_WRITE",
                                InitializeParams = new AttachedDiskInitializeParams()
                                {
                                    SourceImage = _OSSourceImageURL,
                                    DiskType = $"projects/{ ProjectID}/zones/{ZoneName}/diskTypes/{(_DiskType == EVMDiskType.SSD ? "pd-ssd" : "pd-standard")}",
                                    DiskSizeGb = _DiskSizeGB
                                },
                                Type = "PERSISTENT"
                            }
                        },
                        NetworkInterfaces = new List<NetworkInterface>()
                        {
                            new NetworkInterface()
                            {
                                AccessConfigs = new List<AccessConfig>()
                                {
                                    new AccessConfig()
                                    {
                                        Kind = "compute#accessConfig",
                                        Name = "External NAT",
                                        NetworkTier = "PREMIUM",
                                        Type = "ONE_TO_ONE_NAT"
                                    }
                                },
                                Kind = "compute#networkInterface",
                                Name = "nic0",
                                Network = $"projects/{ ProjectID}/global/networks/default",
                                Subnetwork = $"projects/{ ProjectID}/regions/{ZoneName.Substring(0, ZoneName.LastIndexOf('-'))}/subnetworks/default"
                            }
                        },
                        Tags = new Tags()
                        {
                            Items = new List<string>()
                            {
                                RandomFirewallTag
                            }
                        },
                        Metadata = new Metadata()
                        {
                            Kind = "compute#metadata",
                            Items = new List<Metadata.ItemsData>()
                        },
                        ShieldedInstanceConfig = new ShieldedInstanceConfig() 
                        { 
                            EnableVtpm = true,
                            EnableSecureBoot = false,
                            EnableIntegrityMonitoring = true
                        },
                        Scheduling = new Scheduling()
                        {
                            AutomaticRestart = true,
                            Preemptible = false,
                            OnHostMaintenance = "TERMINATE"
                        }
                    };

                    if (_OptionalStartupScript != null)
                    {
                        NewInstance.Metadata.Items.Add(new Metadata.ItemsData()
                        {
                            Key = _OSType == EVMOSType.Linux ? "startup-script" : "windows-startup-script-ps1",
                            Value = _OptionalStartupScript
                        });
                    }
                    
                    if (_GpuCount > 0)
                    {
                        if (NewInstance.GuestAccelerators == null)
                        {
                            NewInstance.GuestAccelerators = new List<AcceleratorConfig>();
                        }
                        NewInstance.GuestAccelerators.Add(
                            new AcceleratorConfig()
                            {
                                AcceleratorCount = _GpuCount,
                                AcceleratorType = $"projects/{ ProjectID}/zones/{ZoneName}/acceleratorTypes/{_GpuName}"
                            });
                    }

                    if (_OSType == EVMOSType.Windows)
                    {
                        if (NewInstance.Disks[0].GuestOsFeatures == null)
                        {
                            NewInstance.Disks[0].GuestOsFeatures = new List<GuestOsFeature>();
                        }

                        if (!NewInstance.Disks[0].GuestOsFeatures.Any(Item => Item.Type == "VIRTIO_SCSI_MULTIQUEUE"))
                            NewInstance.Disks[0].GuestOsFeatures.Add(new GuestOsFeature() { Type = "VIRTIO_SCSI_MULTIQUEUE" });

                        if (!NewInstance.Disks[0].GuestOsFeatures.Any(Item => Item.Type == "WINDOWS"))
                            NewInstance.Disks[0].GuestOsFeatures.Add(new GuestOsFeature() { Type = "WINDOWS" });

                        if (!NewInstance.Disks[0].GuestOsFeatures.Any(Item => Item.Type == "MULTI_IP_SUBNET"))
                            NewInstance.Disks[0].GuestOsFeatures.Add(new GuestOsFeature() { Type = "MULTI_IP_SUBNET" });

                        if (!NewInstance.Disks[0].GuestOsFeatures.Any(Item => Item.Type == "UEFI_COMPATIBLE"))
                            NewInstance.Disks[0].GuestOsFeatures.Add(new GuestOsFeature() { Type = "UEFI_COMPATIBLE" });
                    }

                    var NewFirewall = new Firewall()
                    {
                        Kind = "compute#firewall",
                        Name = RandomFirewallTag,
                        Priority = 1000,
                        Direction = "INGRESS",
                        SelfLink = $"projects/{ ProjectID}/global/firewalls/{RandomFirewallTag}",
                        Network = $"projects/{ ProjectID}/global/networks/default",
                        SourceRanges = new List<string>(),
                        TargetTags = new List<string>()
                        {
                            RandomFirewallTag
                        },
                        Allowed = new List<Firewall.AllowedData>()
                    };
                    if (_FirewallSettings.bOpenAll)
                    {
                        NewFirewall.Allowed.Add(new Firewall.AllowedData()
                        {
                            IPProtocol = "tcp"
                        });
                        NewFirewall.Allowed.Add(new Firewall.AllowedData()
                        {
                            IPProtocol = "udp"
                        });
                    }
                    else
                    {
                        foreach (var Current in _FirewallSettings.OpenPorts)
                        {
                            string[] OpenFor;
                            if (Current.OpenFor == VMNetworkFirewall.EVMNetworkFirewallPortProtocol.TCP)
                                OpenFor = new string[] { "tcp" };
                            else if (Current.OpenFor == VMNetworkFirewall.EVMNetworkFirewallPortProtocol.UDP)
                                OpenFor = new string[] { "udp" };
                            else
                                OpenFor = new string[] { "tcp", "udp" };

                            var PortList = new List<string>()
                            {
                                $"{Current.FromPortInclusive}-{Current.ToPortInclusive}"
                            };
                            foreach (var OFor in OpenFor)
                            {
                                NewFirewall.Allowed.Add(new Firewall.AllowedData()
                                {
                                    IPProtocol = OFor,
                                    Ports = PortList
                                });
                            }
                        }
                    }

                    var FirewallCreationResult = Service.Firewalls.Insert(NewFirewall, ProjectID).Execute();
                    if (FirewallCreationResult == null || (FirewallCreationResult.HttpErrorStatusCode.HasValue && FirewallCreationResult.HttpErrorStatusCode.Value >= 400))
                    {
                        _ErrorMessageAction?.Invoke($"VMServiceGC->CreateInstance: Firewall creation has failed: {(FirewallCreationResult == null ? "Result is null." : FirewallCreationResult.HttpErrorMessage)}");
                        _ErrorCode = FirewallCreationResult.HttpErrorStatusCode.Value;
                        return false;
                    }

                    var VMCreationResult = Service.Instances.Insert(NewInstance, ProjectID, ZoneName).Execute();
                    if (VMCreationResult == null || (VMCreationResult.HttpErrorStatusCode.HasValue && VMCreationResult.HttpErrorStatusCode.Value >= 400))
                    {
                        _ErrorMessageAction?.Invoke($"VMServiceGC->CreateInstance: VM creation has failed: {(VMCreationResult == null ? "Result is null." : VMCreationResult.HttpErrorMessage)}");
                        _ErrorCode = VMCreationResult.HttpErrorStatusCode.Value;
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"VMServiceGC->CreateInstance: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        private InstanceList GetInstanceList(Action<string> _ErrorMessageAction = null)
        {
            InstanceList RequestedList = null;
            try
            {
                using (var Service = GetService())
                {
                    var Request = Service.Instances.List(ProjectID, ZoneName);
                    RequestedList = Request.Execute();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"VMServiceGC->GetInstanceList: {e.Message}, Trace: {e.StackTrace}");
                return null;
            }
            return RequestedList;
        }

        private Instance FindInstanceByUniqueName(string _UniqueName, Action<string> _ErrorMessageAction = null)
        {
            InstanceList RequestedList = GetInstanceList(_ErrorMessageAction);
            if (RequestedList != null && RequestedList.Items != null && RequestedList.Items.Count > 0)
            {
                foreach (var Current in RequestedList.Items)
                {
                    if (Current != null && Current.Name == _UniqueName)
                    {
                        return Current;
                    }
                }
            }
            return null;
        }

        public JObject ListInstances(
            Action<string> _ErrorMessageAction = null)
        {
            JObject Result = new JObject();

            InstanceList RequestedList = GetInstanceList(_ErrorMessageAction);
            if (RequestedList != null && RequestedList.Items != null && RequestedList.Items.Count > 0)
            {
                foreach (var Current in RequestedList.Items)
                {
                    if (Current != null)
                    {
                        Result[Current.Name] = new JObject()
                        {
                            ["UniqueID"] = Current.Id.HasValue ? Current.Id.Value.ToString() : "",
                            ["Status"] = Current.Status,
                            ["Zone"] = Current.Zone,
                            ["Kind"] = Current.Kind,
                            ["bDeletionProtection"] = (Current.DeletionProtection ?? false),
                            ["CreationTimestamp"] = Current.CreationTimestamp,
                            ["Description"] = Current.Description ?? "",
                            ["ETag"] = Current.ETag ?? ""
                        };

                        Result[Current.Name]["Disks"] = new JArray();
                        if (Current.Disks != null)
                        {
                            foreach (var Disk in Current.Disks)
                            {
                                if (Disk != null)
                                {
                                    JObject DiskObject = new JObject()
                                    {
                                        ["bAutoDelete"] = Disk.AutoDelete ?? false,
                                        ["Kind"] = Disk.Kind,
                                        ["DeviceName"] = Disk.DeviceName,
                                        ["bIsBootType"] = Disk.Boot ?? false,
                                        ["bReadOnly"] = Disk.Mode == "READ_ONLY",
                                        ["ETag"] = Disk.ETag ?? ""
                                    };
                                    (Result[Current.Name]["Disks"] as JArray).Add(DiskObject);
                                }
                            }
                        }

                        Result[Current.Name]["Labels"] = new JObject();
                        if (Current.Labels != null)
                        {
                            foreach (var Label in Current.Labels)
                            {
                                (Result[Current.Name]["Labels"] as JObject)[Label.Key] = Label.Value;
                            }
                        }

                        Result[Current.Name]["Tags"] = new JArray();
                        if (Current.Tags != null)
                        {
                            foreach (var Tag in Current.Tags.Items)
                            {
                                (Result[Current.Name]["Tags"] as JArray).Add(Tag);
                            }
                        }

                        Result[Current.Name]["NetworkInterfaces"] = new JArray();
                        if (Current.NetworkInterfaces != null)
                        {
                            foreach (var NetworkInterface in Current.NetworkInterfaces)
                            {
                                if (NetworkInterface != null)
                                {
                                    JObject NetworkInterfaceObject = new JObject()
                                    {
                                        ["UniqueName"] = NetworkInterface.Name,
                                        ["Kind"] = NetworkInterface.Kind,
                                        ["Network"] = NetworkInterface.Network,
                                        ["NetworkIP"] = NetworkInterface.NetworkIP,
                                        ["Subnetwork"] = NetworkInterface.Subnetwork,
                                        ["ETag"] = NetworkInterface.ETag ?? ""
                                    };
                                    if (NetworkInterface.AccessConfigs != null && NetworkInterface.AccessConfigs.Count > 0)
                                    {
                                        string ExternalIP = NetworkInterface.AccessConfigs[0].NatIP;
                                        if (ExternalIP != null)
                                        {
                                            NetworkInterfaceObject["ExternalIP"] = ExternalIP;
                                        }
                                    }
                                    (Result[Current.Name]["NetworkInterfaces"] as JArray).Add(NetworkInterfaceObject);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (RequestedList != null) return Result;
                else
                {
                    _ErrorMessageAction?.Invoke("ComputeEngine->ListInstances: Result is null.");
                }
            }
            return Result;
        }

        public EVMInstanceStatus GetStatusFromString(string _Status)
        {
            if (_Status == "RUNNING")
            {
                return EVMInstanceStatus.Running;
            }
            else if (_Status == "STOPPED" || _Status == "TERMINATED" || _Status == "SUSPENDED")
            {
                return EVMInstanceStatus.Stopped;
            }
            else if (_Status == "PROVISIONING" || _Status == "STAGING")
            {
                return EVMInstanceStatus.PreparingToRun;
            }
            else if (_Status == "STOPPING" || _Status == "SUSPENDING")
            {
                return EVMInstanceStatus.Stopping;
            }
            return EVMInstanceStatus.None;
        }

        public bool GetInstanceStatus(
            string _UniqueInstanceName,
            out EVMInstanceStatus _Status,
            Action<string> _ErrorMessageAction = null)
        {
            _Status = EVMInstanceStatus.None;

            Instance FoundInstance = FindInstanceByUniqueName(_UniqueInstanceName, _ErrorMessageAction);
            if (FoundInstance != null)
            {
                _Status = GetStatusFromString(FoundInstance.Status);
                if (_Status == EVMInstanceStatus.None)
                {
                    _Status = EVMInstanceStatus.None;
                    _ErrorMessageAction?.Invoke($"VMServiceGC->GetInstanceStatus: Unexpected instance status: { FoundInstance.Status}");
                    return false;
                }
                return true;
            }
            return false;
        }

        private int CurrentActionIndex = 0;

        private readonly Dictionary<int, Stack<object>> ProgressStacks = new Dictionary<int, Stack<object>>();
        private readonly object ProgressStacks_Lock = new object();

        //EVMInstanceStatus is the condition in here
        private int PerformActionOnInstances(
            Tuple<string, EVMInstanceAction, EVMInstanceStatus>[] _Operations,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            int ProgressStackIx = Interlocked.Increment(ref CurrentActionIndex);

            var ProgressStack = new Stack<object>();

            if (_Operations != null && _Operations.Length > 0)
            {
                lock (ProgressStacks_Lock)
                {
                    ProgressStacks.Add(ProgressStackIx, ProgressStack);
                }

                var Service = GetService(); //Will be disposed in async methods
                var Request = new BatchRequest(Service);

                foreach (var _Operation in _Operations)
                {
                    var FoundInstance = FindInstanceByUniqueName(_Operation.Item1, _ErrorMessageAction);
                    if (FoundInstance != null)
                    {
                        if (GetStatusFromString(FoundInstance.Status) == _Operation.Item3)
                        {
                            IClientServiceRequest RequestAction = null;
                            if (_Operation.Item2 == EVMInstanceAction.Start)
                            {
                                RequestAction = Service.Instances.Start(ProjectID, ZoneName, FoundInstance.Name);
                            }
                            else if (_Operation.Item2 == EVMInstanceAction.Stop)
                            {
                                RequestAction = Service.Instances.Stop(ProjectID, ZoneName, FoundInstance.Name);
                            }
                            else if (_Operation.Item2 == EVMInstanceAction.Restart)
                            {
                                RequestAction = Service.Instances.Reset(ProjectID, ZoneName, FoundInstance.Name);
                            }

                            if (RequestAction != null)
                            {
                                Request.Queue<Instance>(RequestAction,
                                (Content, Error, i, Message) =>
                                {
                                    lock (ProgressStacks_Lock)
                                    {
                                        if (ProgressStacks.TryGetValue(ProgressStackIx, out Stack<object> FoundStack) && FoundStack.Count > 0)
                                        {
                                            if (Error != null)
                                            {
                                                _ErrorMessageAction?.Invoke($"VMServiceGC->PerformActionOnInstances->Error: { Error.Message}");
                                                FoundStack.Clear();
                                                _OnFailure?.Invoke();
                                            }
                                            else
                                            {
                                                FoundStack.Pop();
                                                if (FoundStack.Count == 0)
                                                {
                                                    ProgressStacks.Remove(ProgressStackIx);
                                                    _OnCompleted?.Invoke();
                                                }
                                            }
                                        }
                                    }
                                });
                                ProgressStack.Push(new object());
                            }
                        }
                    }
                }
                if (ProgressStack.Count > 0)
                {
                    TaskWrapper.Run(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        try
                        {
                            using (var CreatedTask = Request.ExecuteAsync())
                            {
                                CreatedTask.Wait();
                            }
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke($"VMServiceGC->PerformActionOnInstances->Exception: {e.Message}");
                            _OnFailure?.Invoke();
                        }
                        Service?.Dispose();
                    });
                }
                else
                {
                    lock (ProgressStacks_Lock)
                    {
                        ProgressStacks.Remove(ProgressStackIx);
                    }
                    Service?.Dispose();
                }
            }
            return ProgressStack.Count;
        }

        private bool StartStopRestartInstances(
            string[] _UniqueInstanceNames,
            EVMInstanceAction _Action,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            if (_UniqueInstanceNames != null && _UniqueInstanceNames.Length > 0)
            {
                var Actions = new Tuple<string, EVMInstanceAction, EVMInstanceStatus>[_UniqueInstanceNames.Length];

                EVMInstanceStatus ConditionStatus = EVMInstanceStatus.None;
                switch (_Action)
                {
                    case EVMInstanceAction.Start:
                        ConditionStatus = EVMInstanceStatus.Stopped;
                        break;
                    case EVMInstanceAction.Stop:
                    case EVMInstanceAction.Restart:
                        ConditionStatus = EVMInstanceStatus.Running;
                        break;
                }

                int i = 0;
                foreach (var _Name in _UniqueInstanceNames)
                {
                    Actions[i++] = new Tuple<string, EVMInstanceAction, EVMInstanceStatus>(
                        _Name,
                        _Action,
                        ConditionStatus);
                }
                return PerformActionOnInstances(Actions, _OnCompleted, _OnFailure, _ErrorMessageAction) > 0;
            }
            return false;
        }

        public bool StartInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            return StartStopRestartInstances(_UniqueInstanceNames, EVMInstanceAction.Start, _OnCompleted, _OnFailure, _ErrorMessageAction);
        }

        public bool StopInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            return StartStopRestartInstances(_UniqueInstanceNames, EVMInstanceAction.Stop, _OnCompleted, _OnFailure, _ErrorMessageAction);
        }

        public bool RestartInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            return StartStopRestartInstances(_UniqueInstanceNames, EVMInstanceAction.Restart, _OnCompleted, _OnFailure, _ErrorMessageAction);
        }

        public bool WaitUntilInstanceStatus(
            string _UniqueInstanceName,
            EVMInstanceStatus[] _OrStatus,
            Action<string> _ErrorMessageAction = null)
        {
            EVMInstanceStatus CurrentInstanceStatus = EVMInstanceStatus.None;

            List<EVMInstanceStatus> Conditions = new List<EVMInstanceStatus>(_OrStatus);

            int LocalErrorRetryCount = 0;
            do
            {
                var FoundInstance = FindInstanceByUniqueName(_UniqueInstanceName, _ErrorMessageAction);
                if (FoundInstance != null)
                {
                    if (GetInstanceStatus(_UniqueInstanceName, out CurrentInstanceStatus, _ErrorMessageAction))
                    {
                        if (Conditions.Contains(CurrentInstanceStatus)) return true;
                    }
                    else
                    {
                        if (++LocalErrorRetryCount < 5 && ThreadSleep(2000)) continue;
                        return false;
                    }
                }
                else
                {
                    if (++LocalErrorRetryCount < 5 && ThreadSleep(2000)) continue;
                    return false;
                }
            } while (!Conditions.Contains(CurrentInstanceStatus) && ThreadSleep(2000));

            return true;
        }
        private bool ThreadSleep(int _MS) { Thread.Sleep(_MS); return true; }

        public bool RunCommand(string[] _UniqueInstanceNames, EVMOSType _VMOperationSystemType, string[] _Commands, Action _OnCompleted, Action _OnFailure, Action<string> _ErrorMessageAction = null)
        {
            throw new NotImplementedException();
        }
    }
}