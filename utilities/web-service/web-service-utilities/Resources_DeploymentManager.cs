/// Copyright 2022- Burak Kara, All rights reserved.

namespace WebServiceUtilities
{
    public class Resources_DeploymentManager
    {
        private static Resources_DeploymentManager Instance = null;
        private Resources_DeploymentManager() { }
        public static Resources_DeploymentManager Get()
        {
            if (Instance == null)
            {
                Instance = new Resources_DeploymentManager();
            }
            return Instance;
        }

        private string DeploymentBranchName, DeploymentBranchNameEscapedLoweredWithDash, DeploymentBranchNameEscapedLoweredWithUnderscore;
        private string DeploymentBuildNumber;
        public void SetDeploymentBranchNameAndBuildNumber(string _DeploymentBranchName, string _DeploymentBuildNumber)
        {
            DeploymentBranchName = _DeploymentBranchName;
            DeploymentBranchNameEscapedLoweredWithDash = _DeploymentBranchName.Replace('/', '-').Replace('_', '-').ToLower();
            DeploymentBranchNameEscapedLoweredWithUnderscore = _DeploymentBranchName.Replace('/', '_');
            DeploymentBuildNumber = _DeploymentBuildNumber;
        }
        public string GetDeploymentBranchName() { return DeploymentBranchName; }
        public string GetDeploymentBranchNameEscapedLoweredWithDash() { return DeploymentBranchNameEscapedLoweredWithDash; }
        public string GetDeploymentBranchNameEscapedLoweredWithUnderscore() { return DeploymentBranchNameEscapedLoweredWithUnderscore; }
        public string GetDeploymentBuildNumber() { return DeploymentBuildNumber; }
    }
}
