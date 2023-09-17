/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Compute.Fluent;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent.Models;

namespace CloudServiceUtilities.VMServices
{
    public class VMServiceAZ : IVMServiceInterface
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        /// <summary>
        /// Azure Manager for managing Azure resources
        /// </summary>
        private readonly IAzure AzureManager;

        private readonly string ResourseGroupName;

        /// <summary>
        /// 
        /// <para>VMServiceAZ: Parametered Constructor for Managed Service by Azure</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_SubscriptionId"/>                            Azure Subscription Id</para>
        /// <para><paramref name="_ClientId"/>                                  Azure Client Id</para>
        /// <para><paramref name="_ClientSecret"/>                              Azure Client Secret</para>
        /// <para><paramref name="_TenantId"/>                                  Azure Tenant Id</para>
        /// <para><paramref name="_ResourceGroupName"/>                         Azure Resource Group Name</para>
        /// <para><paramref name="_ErrorMessageAction"/>                        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public VMServiceAZ(
            string _SubscriptionId,
            string _ClientId,
            string _ClientSecret,
            string _TenantId, 
            string _ResourceGroupName, 
            Action<string> _ErrorMessageAction)
        {
            ResourseGroupName = _ResourceGroupName;

            try
            {
                var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(_ClientId, _ClientSecret, _TenantId, AzureEnvironment.AzureGlobalCloud);

                AzureManager = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithSubscription(_SubscriptionId);

                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"VMServiceGC->Constructor: {e.Message}, Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
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
            Action<string> _ErrorMessageAction)
        {
            _ErrorCode = 400;

            return false;
        }

        private List<IVirtualMachine> GetInstanceList(Action<string> _ErrorMessageAction)
        {
            try
            {
                using (var GetVirtualMachineTask = AzureManager.VirtualMachines.ListByResourceGroupAsync(ResourseGroupName))
                {
                    GetVirtualMachineTask.Wait();
                    return GetVirtualMachineTask.Result.ToList();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"VMServiceGC->GetInstanceList: {e.Message}, Trace: {e.StackTrace}");
                return null;
            }
        }

        private IVirtualMachine FindInstanceByUniqueName(string _UniqueName, Action<string> _ErrorMessageAction)
        {
            try
            {
                using (var GetVirtualMachineTask = AzureManager.VirtualMachines.GetByResourceGroupAsync(ResourseGroupName, _UniqueName))
                {
                    GetVirtualMachineTask.Wait();
                    return GetVirtualMachineTask.Result;
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"VMServiceGC->FindInstanceByUniqueName: {e.Message}, Trace: {e.StackTrace}");
                return null;
            }
        }

        public JObject ListInstances(Action<string> _ErrorMessageAction)
        {
            JObject Result = new JObject();

            List<IVirtualMachine> RequestedList = GetInstanceList(_ErrorMessageAction);
            if (RequestedList != null && RequestedList.Count > 0)
            {
                foreach (var Current in RequestedList)
                {
                    if (Current != null)
                    {
                        Result[Current.Name] = new JObject()
                        {
                            ["UniqueID"] = Current.Id,
                            ["Status"] = Current.PowerState.ToString(),
                            ["Zone"] = Current.AvailabilityZones.ToList().ToString(),
                            ["Kind"] = Current.Type,
                            //["bDeletionProtection"] = (Current.DeletionProtection ?? false),
                            //["CreationTimestamp"] = Current.,
                            //["Description"] = Current.Description ?? "",
                            //["ETag"] = Current.ETag ?? ""
                        };

                        Result[Current.Name]["Disks"] = new JArray();
                        if (Current.DataDisks != null)
                        {
                            //foreach (var Disk in Current.DataDisks)
                            //{
                            //    if (Disk != null)
                            //    {
                            //        JObject DiskObject = new JObject()
                            //        {
                            //            ["bAutoDelete"] = Disk.AutoDelete ?? false,
                            //            ["Kind"] = Disk.Kind,
                            //            ["DeviceName"] = Disk.DeviceName,
                            //            ["bIsBootType"] = Disk.Boot ?? false,
                            //            ["bReadOnly"] = Disk.Mode == "READ_ONLY",
                            //            ["ETag"] = Disk.ETag ?? ""
                            //        };
                            //        (Result[Current.Name]["Disks"] as JArray).Add(DiskObject);
                            //    }
                            //}
                        }

                        //Result[Current.Name]["Labels"] = new JObject();
                        //if (Current.Labels != null)
                        //{
                        //    foreach (var Label in Current.Labels)
                        //    {
                        //        (Result[Current.Name]["Labels"] as JObject)[Label.Key] = Label.Value;
                        //    }
                        //}

                        Result[Current.Name]["Tags"] = new JArray();
                        if (Current.Tags != null)
                        {
                            foreach (var Tag in Current.Tags)
                            {
                                (Result[Current.Name]["Tags"] as JArray).Add(Tag);
                            }
                        }

                        Result[Current.Name]["NetworkInterfaces"] = new JArray();
                        if (Current.NetworkInterfaceIds != null)
                        {
                            foreach (var NetworkInterfaceId in Current.NetworkInterfaceIds)
                            {
                                using (var GetNetworkInterfaceTask = AzureManager.NetworkInterfaces.GetByIdAsync(NetworkInterfaceId))
                                {
                                    GetNetworkInterfaceTask.Wait();
                                    var NetworkInterface = GetNetworkInterfaceTask.Result;
                                    if (NetworkInterface != null)
                                    {
                                        //JObject NetworkInterfaceObject = new JObject()
                                        //{
                                        //    ["UniqueName"] = NetworkInterface.Name,
                                        //    ["Kind"] = NetworkInterface.Type,
                                        //    ["Network"] = NetworkInterface.,
                                        //    ["NetworkIP"] = NetworkInterface.PrimaryIPConfiguration.,
                                        //    ["Subnetwork"] = NetworkInterface.Subnetwork,
                                        //    ["ETag"] = NetworkInterface.ETag ?? ""
                                        //};
                                        //if (NetworkInterface.IPConfigurations != null && NetworkInterface.AccessConfigs.Count > 0)
                                        //{
                                        //    string ExternalIP = NetworkInterface.AccessConfigs[0].NatIP;
                                        //    if (ExternalIP != null)
                                        //    {
                                        //        NetworkInterfaceObject["ExternalIP"] = ExternalIP;
                                        //    }
                                        //}
                                        //(Result[Current.Name]["NetworkInterfaces"] as JArray).Add(NetworkInterfaceObject);
                                    }
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
            _Status = _Status.ToLower();

            if (_Status.Contains("running"))
            {
                return EVMInstanceStatus.Running;
            }
            else if (_Status.Contains("stopped") || _Status.Contains("deallocated"))
            {
                return EVMInstanceStatus.Stopped;
            }
            else if (_Status.Contains("starting"))
            {
                return EVMInstanceStatus.PreparingToRun;
            }
            else if (_Status.Contains("stopping") || _Status.Contains("deallocating"))
            {
                return EVMInstanceStatus.Stopping;
            }
            return EVMInstanceStatus.None;
        }

        public bool GetInstanceStatus(
            string _UniqueInstanceName,
            out EVMInstanceStatus _Status,
            Action<string> _ErrorMessageAction)
        {
            _Status = EVMInstanceStatus.None;

            var FoundInstance = FindInstanceByUniqueName(_UniqueInstanceName, _ErrorMessageAction);
            if (FoundInstance != null)
            {
                _Status = GetStatusFromString(FoundInstance.PowerState.ToString());
                if (_Status == EVMInstanceStatus.None)
                {
                    _Status = EVMInstanceStatus.None;
                    _ErrorMessageAction?.Invoke($"VMServiceAZ->GetInstanceStatus: Unexpected instance status: {FoundInstance.PowerState}");
                    return false;
                }
                return true;
            }
            return false;
        }

        //EVMInstanceStatus is the condition in here
        private bool PerformActionOnInstances(
            Tuple<string, EVMInstanceAction, EVMInstanceStatus>[] _Operations,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction)
        {
            if (_Operations != null && _Operations.Length > 0)
            {
                var Request = new Queue<Task>();
                var NumberOfTasks = 0;

                foreach (var _Operation in _Operations)
                {
                    var FoundInstance = FindInstanceByUniqueName(_Operation.Item1, _ErrorMessageAction);
                    if (FoundInstance != null)
                    {
                        if (GetStatusFromString(FoundInstance.PowerState.ToString()) == _Operation.Item3)
                        {
                            Task RequestAction = null;
                            if (_Operation.Item2 == EVMInstanceAction.Start)
                            {
                                RequestAction = FoundInstance.StartAsync();
                                _ErrorMessageAction?.Invoke($"[LOG:] PerformActionOnInstances: StartAsync has been called for VM: {_Operation.Item1}");
                            }
                            else if (_Operation.Item2 == EVMInstanceAction.Stop)
                            {
                                RequestAction = FoundInstance.DeallocateAsync();
                                _ErrorMessageAction?.Invoke($"[LOG:] PerformActionOnInstances: DeallocateAsync has been called for VM: {_Operation.Item1}");
                            }
                            else if ((_Operation.Item2 == EVMInstanceAction.Restart))
                            {
                                RequestAction = FoundInstance.RestartAsync();
                                _ErrorMessageAction?.Invoke($"[LOG:] PerformActionOnInstances: RestartAsync has been called for VM: {_Operation.Item1}");
                            }

                            if (RequestAction != null)
                            {
                                Request.Enqueue(RequestAction);
                                NumberOfTasks++;
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke($"PerformActionOnInstances: Found the instance with given name {_Operation.Item1}, but the status is different than condition: {GetStatusFromString(FoundInstance.PowerState.ToString())}, whereas the expected condition: {_Operation.Item3}");
                        }
                    }
                }

                for (var i = 0; i < NumberOfTasks; i++)
                {
                    try
                    {
                        if (Request.TryDequeue(out Task CreatedTask))
                        {
                            using (CreatedTask)
                            {
                                _ErrorMessageAction?.Invoke($"[LOG:] PerformActionOnInstances: Upon StartAsync/DeallocateAsync; waiting for task to complete for VM.");
                                CreatedTask.Wait();
                                _ErrorMessageAction?.Invoke($"[LOG:] PerformActionOnInstances: Upon StartAsync/DeallocateAsync; task wait has also been completed.");
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("VMServiceAZ->PerformActionOnInstances->TryDequeue error occured.");
                            _OnFailure?.Invoke();
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke($"VMServiceGC->PerformActionOnInstances: {e.Message}, Trace: {e.StackTrace}");
                        _OnFailure?.Invoke();
                        return false;
                    }
                }

                _OnCompleted?.Invoke();
                return true;
            }
            return false;
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
                return PerformActionOnInstances(Actions, _OnCompleted, _OnFailure, _ErrorMessageAction);
            }
            return false;
        }

        public bool StartInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction)
        {
            return StartStopRestartInstances(_UniqueInstanceNames, EVMInstanceAction.Start, _OnCompleted, _OnFailure, _ErrorMessageAction);
        }

        public bool StopInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction)
        {
            return StartStopRestartInstances(_UniqueInstanceNames, EVMInstanceAction.Stop, _OnCompleted, _OnFailure, _ErrorMessageAction);
        }

        public bool RestartInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction)
        {
            return StartStopRestartInstances(_UniqueInstanceNames, EVMInstanceAction.Restart, _OnCompleted, _OnFailure, _ErrorMessageAction);
        }

        //EVMInstanceStatus is the condition in here
        private bool PerformRunCommandActions(
            string[] _UniqueInstanceNames,
            EVMOSType _VMOperationSystemType,
            string[] _Commands,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction)
        {
            if (_UniqueInstanceNames != null && _UniqueInstanceNames.Length > 0)
            {
                var Request = new Queue<Task>();
                var RunCommandCount = 0;

                foreach (var _InstanceName in _UniqueInstanceNames)
                {
                    var FoundInstance = FindInstanceByUniqueName(_InstanceName, _ErrorMessageAction);
                    if (FoundInstance != null)
                    {
                        if (GetStatusFromString(FoundInstance.PowerState.ToString()) == EVMInstanceStatus.Running)
                        {
                            var _CommandId = "RunPowerShellScript";
                            if (_VMOperationSystemType == EVMOSType.Linux)
                            {
                                _CommandId = "RunShellScript";
                            }

                            var _RunCommandInput = new RunCommandInput()
                            {
                                CommandId = _CommandId,
                                Script = _Commands.ToList()
                            };

                            var RequestAction = FoundInstance.RunCommandAsync(_RunCommandInput);
                            _ErrorMessageAction?.Invoke($"[LOG:] PerformRunCommandActions: RunCommandAsync has been called for VM: {_InstanceName}");

                            Request.Enqueue(RequestAction);
                            RunCommandCount++;
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("VMServiceAZ->PerformRunCommandActions->Virtual Machine is not running.");
                            _OnFailure?.Invoke();
                            return false;
                        }
                    }
                }

                for (var i = 0; i < RunCommandCount; i++)
                {
                    try
                    {
                        if (Request.TryDequeue(out Task CreatedTask))
                        {
                            using (CreatedTask)
                            {
                                _ErrorMessageAction?.Invoke($"[LOG:] PerformRunCommandActions: Upon RunCommandAsync; waiting for task to complete for VM.");
                                CreatedTask.Wait();
                                _ErrorMessageAction?.Invoke($"[LOG:] PerformRunCommandActions: Upon RunCommandAsync;task has been completed for VM.");
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("VMServiceAZ->PerformRunCommandActions->TryDequeue error occured.");
                            _OnFailure?.Invoke();
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke($"VMServiceGC->PerformRunCommandActions: {e.Message}, Trace: {e.StackTrace}");
                        _OnFailure?.Invoke();
                        return false;
                    }
                }

                _OnCompleted?.Invoke();
                return true;
            }
            return false;
        }

        public bool RunCommand(
            string[] _UniqueInstanceNames,
            EVMOSType _VMOperationSystemType,
            string[] _Commands,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction)
        {
            if (_UniqueInstanceNames != null && _UniqueInstanceNames.Length > 0)
            {
                return PerformRunCommandActions(_UniqueInstanceNames, _VMOperationSystemType, _Commands, _OnCompleted, _OnFailure, _ErrorMessageAction);
            }
            return false;
        }

        public bool WaitUntilInstanceStatus(
            string _UniqueInstanceName,
            EVMInstanceStatus[] _OrStatus,
            Action<string> _ErrorMessageAction)
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
    }
}
