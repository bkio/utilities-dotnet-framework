﻿/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using CloudServiceUtilities;
using CommonUtilities;

namespace CloudServiceUtilitiesTest.Tests
{
    public class FileServiceTest
    {
        private readonly IFileServiceInterface SelectedFileService;

        private readonly string BucketName;
        private readonly string FileKey;
        private readonly string FileLocalPath;

        private readonly Action<string> PrintAction;

        public FileServiceTest(IFileServiceInterface _FileService, string _BucketName, string _FileKey, string _FileLocalPath, Action<string> _PrintAction)
        {
            SelectedFileService = _FileService;

            BucketName = _BucketName;
            FileKey = _FileKey;
            FileLocalPath = _FileLocalPath;

            PrintAction = _PrintAction;
        }

        public bool Start()
        {
            PrintAction?.Invoke($"DatabaseServicesTest->Info-> Test is starting.");

            if (SelectedFileService == null)
            {
                PrintAction?.Invoke($"FileServicesTest->Error-> Given SelectedFileService is null.");
                return false;
            }

            if (BucketName == null || BucketName.Length == 0)
            {
                PrintAction?.Invoke($"FileServicesTest->Error-> Given BucketName is null or empty.");
                return false;
            }

            if (FileKey == null || FileKey.Length == 0)
            {
                PrintAction?.Invoke($"FileServicesTest->Error-> Given FileKey is null or empty.");
                return false;
            }

            if (FileLocalPath == null || 
                FileLocalPath.Length == 0 || 
                !Utility.DoesFileExist(FileLocalPath, out bool bFileExists, (string Message) => PrintAction?.Invoke($"FileServicesTest->Error-> {Message}")) ||
                !bFileExists)
            {
                PrintAction?.Invoke($"FileServicesTest->Error-> Given FileLocalPath is null or empty or does not exist.");
                return false;
            }

            if (!SelectedFileService.HasInitializationSucceed())
            {
                PrintAction?.Invoke($"FileServicesTest->Error-> Initialization failed.");
                return false;
            }
            PrintAction?.Invoke($"FileServicesTest->Log-> Initialization succeed.");

            PreCleanup();

            if (!TestUploadFile()) return false;

            if (!TestCreateSignedDownloadUrl()) return false;

            if (!TestGetChecksum()) return false;

            if (!TestListFiles(1)) return false;

            if (!TestDeleteLocalFile()) return false;

            if (!TestDownloadFile()) return false;

            if (!TestGetFileSize()) return false;

            if (!TestGetFileTags()) return false;

            if (!TestSetFileTags()) return false;

            if (!TestGetFileTags()) return false;

            if (!TestCopyFile()) return false;

            if (!TestSetFileAccessibility()) return false;

            if (!TestDeleteFile(FileKey)) return false;

            if (!TestDeleteFile($"{FileKey}_copy")) return false;

            if (!TestCreateSignedUploadUrl()) return false;

            return true;
        }

        private void PreCleanup()
        {
            SelectedFileService.DeleteFile(BucketName, FileKey, null);
            SelectedFileService.DeleteFile(BucketName, $"{FileKey}_copy", null);
        }

        private bool TestCreateSignedUploadUrl()
        {
            bool bLocalResult = true;
            bLocalResult = SelectedFileService.CreateSignedURLForUpload(out string SignedUrl, BucketName, FileKey, "application/octet-stream", 1,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"TestCreateSignedUploadUrl->Error-> { Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"TestCreateSignedUploadUrl->Error-> CreateSignedURLForUpload failed.");
                return false;
            }
            if (SignedUrl == null || SignedUrl.Length == 0)
            {
                PrintAction?.Invoke($"TestCreateSignedUploadUrl->Error-> CreateSignedURLForUpload failed. SignedUrl: { SignedUrl}");
                return false;
            }
            PrintAction?.Invoke($"TestCreateSignedUploadUrl-> Succeed. Signed url: {SignedUrl}");
            return true;
        }

        private bool TestListFiles(int ExpectedCount)
        {
            bool bLocalResult = true;
            bLocalResult = SelectedFileService.ListAllFilesInBucket(BucketName, out List<string> FileKeys,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"TestListFiles->Error-> {Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"TestListFiles->Error-> ListAllFilesInBucket failed.");
                return false;
            }
            if (FileKeys == null || FileKeys.Count != ExpectedCount)
            {
                PrintAction?.Invoke($"TestListFiles->Error-> ListAllFilesInBucket failed. FileKeys Count: {FileKeys?.Count}");
                return false;
            }
            return true;
        }

        private bool TestCreateSignedDownloadUrl()
        {
            bool bLocalResult = true;
            bLocalResult = SelectedFileService.CreateSignedURLForDownload(out string SignedUrl, BucketName, FileKey, 1,
                (string Message) =>
                {
                    bLocalResult = false;
                    PrintAction?.Invoke($"TestCreateSignedDownloadUrl->Error-> {Message}");
                });
            if (!bLocalResult)
            {
                PrintAction?.Invoke($"TestCreateSignedDownloadUrl->Error-> CreateSignedURLForDownload failed.");
                return false;
            }
            if (SignedUrl == null || SignedUrl.Length == 0)
            {
                PrintAction?.Invoke($"TestCreateSignedDownloadUrl->Error-> CreateSignedURLForDownload failed. SignedUrl: {SignedUrl}");
                return false;
            }
            PrintAction?.Invoke($"TestCreateSignedDownloadUrl-> Succeed. Signed url: {SignedUrl}");
            return true;
        }

        private bool TestDeleteFile(string _FileKeyInCloud)
        {
            bool bLocalFailure = false;

            //Test delete file
            PrintAction?.Invoke($"TestDeleteFile->Log-> Testing DeleteFile...");
            SelectedFileService.DeleteFile(BucketName, _FileKeyInCloud, (string Message) =>
            {
                PrintAction?.Invoke($"TestDeleteFile->Error-> {Message}");
                bLocalFailure = true;
            });
            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestDeleteFile->Error-> DeleteFile failed.");
                return false;
            }
            PrintAction?.Invoke($"TestDeleteFile->Log-> DeleteFile succeed.");
            return true;
        }

        private bool TestSetFileAccessibility()
        {
            bool bLocalFailure = false;

            //Test set file accessibility
            PrintAction?.Invoke($"TestSetFileAccessibility->Log-> Testing SetFileAccessibility...");

            try
            {
                SelectedFileService.SetFileAccessibility(BucketName, FileKey, ERemoteFileReadPublicity.PublicRead, (string Message) =>
                {
                    PrintAction?.Invoke($"TestSetFileAccessibility->Error-> { Message}");
                    bLocalFailure = true;
                });
            }
            catch(NotSupportedException)
            {
                bLocalFailure = false;
            }


            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestSetFileAccessibility->Error-> SetFileAccessibility failed.");
                return false;
            }
            PrintAction?.Invoke($"TestSetFileAccessibility->Log-> SetFileAccessibility succeed.");
            return true;
        }

        private bool TestSetFileTags()
        {
            bool bLocalFailure = false;

            //Test set file tags
            PrintAction?.Invoke($"TestSetFileTags->Log-> Testing SetFileTags...");
            SelectedFileService.SetFileTags(BucketName, FileKey, new Tuple<string, string>[]
            {
                new Tuple<string, string>("TestTag_1", "TestValue_1"),
                new Tuple<string, string>("TestTag_2", "TestValue_2")
            }, 
            (string Message) =>
            {
                PrintAction?.Invoke($"TestSetFileTags->Error-> { Message}");
                bLocalFailure = true;
            });
            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestSetFileTags->Error-> SetFileTags failed.");
                return false;
            }
            PrintAction?.Invoke($"TestSetFileTags->Log-> SetFileTags succeed.");
            return true;
        }

        private bool TestGetFileTags()
        {
            bool bLocalFailure = false;

            //Test get file tags
            PrintAction?.Invoke($"TestGetFileTags->Log-> Testing GetFileTags...");
            SelectedFileService.GetFileTags(BucketName, FileKey, out List<Tuple<string, string>> ResultTags,
            (string Message) =>
            {
                PrintAction?.Invoke($"TestGetFileTags->Error-> { Message}");
                bLocalFailure = true;
            });
            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestGetFileTags->Error-> GetFileTags failed.");
                return false;
            }
            string AsStr = "";
            foreach (var Current in ResultTags)
            {
                AsStr += $"{Current.Item1} = {Current.Item2}\n";
            }
            PrintAction?.Invoke($"TestGetFileTags->Log-> GetFileTags succeed, tags count: { ResultTags.Count}");
            return true;
        }

        private bool TestGetChecksum()
        {
            bool bLocalFailure = false;

            //Test get file size
            PrintAction?.Invoke($"TestGetChecksum->Log-> Testing TestGetChecksum...");
            SelectedFileService.GetFileChecksum(BucketName, FileKey, out string ResultChecksum,
            (string Message) =>
            {
                PrintAction?.Invoke($"TestGetChecksum->Error-> { Message}");
                bLocalFailure = true;
            });
            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestGetChecksum->Error-> TestGetChecksum failed.");
                return false;
            }
            PrintAction?.Invoke($"TestGetChecksum->Log-> TestGetChecksum succeed, checksum: { ResultChecksum}");
            return true;
        }

        private bool TestGetFileSize()
        {
            bool bLocalFailure = false;

            //Test get file size
            PrintAction?.Invoke($"TestGetFileSize->Log-> Testing GetFileSize...");
            SelectedFileService.GetFileSize(BucketName, FileKey, out ulong ResultSize,
            (string Message) =>
            {
                PrintAction?.Invoke($"TestGetFileSize->Error-> { Message}");
                bLocalFailure = true;
            });
            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestGetFileSize->Error-> GetFileSize failed.");
                return false;
            }
            PrintAction?.Invoke($"TestGetFileSize->Log-> GetFileSize succeed, size: { ResultSize}");
            return true;
        }

        private bool TestDownloadFile()
        {
            bool bLocalFailure = false;

            //Test download file
            PrintAction?.Invoke($"TestDownloadFile->Log-> Testing DownloadFile...");
            SelectedFileService.DownloadFile(BucketName, FileKey, new StringOrStream(FileLocalPath),
            (string Message) =>
            {
                PrintAction?.Invoke($"TestDownloadFile->Error-> { Message}");
                bLocalFailure = true;
            });
            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestDownloadFile->Error-> DownloadFile failed.");
                return false;
            }
            PrintAction?.Invoke($"TestDownloadFile->Log-> DownloadFile succeed.");
            return true;
        }

        private bool TestDeleteLocalFile()
        {
            bool bLocalFailure = false;

            //Test delete local file
            PrintAction?.Invoke($"TestDeleteLocalFile->Log-> Testing DeleteLocalFile...");
            Utility.DeleteFile(FileLocalPath, (string Message) =>
            {
                PrintAction?.Invoke($"TestDeleteLocalFile->Error-> { Message}");
                bLocalFailure = true;
            });
            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestDeleteLocalFile->Error-> DeleteLocalFile failed.");
                return false;
            }
            PrintAction?.Invoke($"TestDeleteLocalFile->Log-> DeleteLocalFile succeed.");
            return true;
        }

        private bool TestCopyFile()
        {
            bool bLocalFailure = false;

            //Test copy file
            PrintAction?.Invoke($"TestCopyFile->Log-> Testing CopyFile...");
            SelectedFileService.CopyFile(BucketName, FileKey, BucketName, $"{FileKey}_copy", ERemoteFileReadPublicity.PublicRead,
            (string Message) =>
            {
                PrintAction?.Invoke($"TestCopyFile->Error-> { Message}");
                bLocalFailure = true;
            });
            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestCopyFile->Error-> CopyFile failed.");
                return false;
            }
            PrintAction?.Invoke($"TestCopyFile->Log-> CopyFile succeed.");
            return true;
        }

        private bool TestUploadFile()
        {
            bool bLocalFailure = false;

            //Test upload file
            PrintAction?.Invoke($"TestUploadFile->Log-> Testing UploadFile...");
            SelectedFileService.UploadFile(new StringOrStream(FileLocalPath), BucketName, FileKey, ERemoteFileReadPublicity.AuthenticatedRead, new Tuple<string, string>[]
            {
                new Tuple<string, string>("TestTag_Default", "TestValue_Default")
            },
            (string Message) =>
            {
                PrintAction?.Invoke($"TestUploadFile->Error-> { Message}");
                bLocalFailure = true;
            });
            if (bLocalFailure)
            {
                PrintAction?.Invoke($"TestUploadFile->Error-> UploadFile failed.");
                return false;
            }
            PrintAction?.Invoke($"TestUploadFile->Log-> UploadFile succeed.");
            return true;
        }
    }
}