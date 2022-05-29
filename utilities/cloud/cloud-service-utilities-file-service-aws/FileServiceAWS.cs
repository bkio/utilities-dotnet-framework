/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CommonUtilities;

namespace CloudServiceUtilities.FileServices
{
    public class FileServiceAWS : IFileServiceInterface
    {
        /// <summary>AWS S3 Client that is responsible to serve to this object</summary>
        private readonly AmazonS3Client S3Client;

        /// <summary>AWS Transfer Utility helps with dealing large files</summary>
        private readonly TransferUtility TransferUtil;

        /// <summary>Holds initialization success</summary>
        private readonly bool bInitializationSucceed;

        /// <summary>
        ///
        /// <para>FileServiceAWS: Parametered Constructor:</para>
        ///
        /// <para><paramref name="_AccessKey"/>              AWS Access Key</para>
        /// <para><paramref name="_SecretKey"/>              AWS Secret Key</para>
        /// <para><paramref name="_Region"/>                 AWS Region that DynamoDB Client will connect to (I.E. eu-west-1) </para>
        /// <para><paramref name="_ErrorMessageAction"/>     Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public FileServiceAWS(
            string _AccessKey,
            string _SecretKey,
            string _Region,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                S3Client = new AmazonS3Client(new Amazon.Runtime.BasicAWSCredentials(_AccessKey, _SecretKey), Amazon.RegionEndpoint.GetBySystemName(_Region));

                TransferUtilityConfig TransferUtilConfig = new TransferUtilityConfig
                {
                    ConcurrentServiceRequests = 10,
                };
                TransferUtil = new TransferUtility(S3Client, TransferUtilConfig);

                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->Constructor: {e.Message}, Trace: {e.StackTrace}");

                bInitializationSucceed = false;
            }
        }

        ~FileServiceAWS()
        {
            S3Client?.Dispose();
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IFileServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        ///
        /// <para>CopyFile:</para>
        ///
        /// <para>Copy a file from a bucket and relative location to another in File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.CopyFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CopyFile(
            string _SourceBucketName, 
            string _SourceKeyInBucket, 
            string _DestinationBucketName, 
            string _DestinationKeyInBucket, 
            ERemoteFileReadPublicity _RemoteFileReadAccess = ERemoteFileReadPublicity.AuthenticatedRead, 
            Action<string> _ErrorMessageAction = null)
        {
            if (S3Client == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->CopyFile: S3Client is null.");
                return false;
            }

            CopyObjectRequest Request = new CopyObjectRequest
            {
                SourceBucket = _SourceBucketName,
                SourceKey = _SourceKeyInBucket,
                DestinationBucket = _DestinationBucketName,
                DestinationKey = _DestinationKeyInBucket
            };

            if (_RemoteFileReadAccess == ERemoteFileReadPublicity.PublicRead)
            {
                Request.CannedACL = S3CannedACL.PublicRead;
            }
            else if (_RemoteFileReadAccess == ERemoteFileReadPublicity.ProjectWideProtectedRead)
            {
                Request.CannedACL = S3CannedACL.AuthenticatedRead;
            }
            else
            {
                Request.CannedACL = S3CannedACL.AuthenticatedRead;
            }

            try
            {
                using (var CreatedTask = S3Client.CopyObjectAsync(Request))
                {
                    CreatedTask.Wait();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->CopyFile: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>DeleteFile:</para>
        ///
        /// <para>Deletes a file from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.DeleteFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DeleteFile(
            string _BucketName, 
            string _KeyInBucket, 
            Action<string> _ErrorMessageAction = null)
        {
            if (S3Client == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->DeleteFile: S3Client is null.");
                return false;
            }

            DeleteObjectRequest Request = new DeleteObjectRequest
            {
                BucketName = _BucketName,
                Key = _KeyInBucket
            };

            try
            {
                using (var CreatedTask = S3Client.DeleteObjectAsync(Request))
                {
                    CreatedTask.Wait();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->DeleteFile: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>DeleteFolder:</para>
        ///
        /// <para>Deletes a folder from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.DeleteFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DeleteFolder(
            string _BucketName,
            string _Folder,
            Action<string> _ErrorMessageAction = null)
        {
            if (S3Client == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->DeleteFolder: S3Client is null.");
                return false;
            }

            var PrefixedObjects = new List<KeyVersion>();

            try
            {
                ListObjectsV2Request ListRequest = new ListObjectsV2Request()
                {
                    BucketName = _BucketName,
                    Prefix = _Folder
                };
                using (var ListObjectsTask = S3Client.ListObjectsV2Async(ListRequest))
                {
                    ListObjectsTask.Wait();
                    foreach (var FileObject in ListObjectsTask.Result.S3Objects)
                    {
                        if (FileObject != null)
                        {
                            PrefixedObjects.Add(new KeyVersion()
                            {
                                Key = FileObject.Key
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PrefixedObjects = null;
                _ErrorMessageAction?.Invoke($"FileServiceAWS->DeleteFolder->ListPrefixed: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }

            DeleteObjectsRequest Request = new DeleteObjectsRequest
            {
                BucketName = _BucketName,
                Objects = PrefixedObjects
            };

            try
            {
                using (var CreatedTask = S3Client.DeleteObjectsAsync(Request))
                {
                    CreatedTask.Wait();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->DeleteFolder: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>DownloadFile:</para>
        ///
        /// <para>Downloads a file from File Service and stores locally/or to stream, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.DownloadFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DownloadFile(
            string _BucketName, 
            string _KeyInBucket,
            StringOrStream _Destination,
            Action<string> _ErrorMessageAction = null,
            UInt64 _StartIndex = 0,
            UInt64 _Size = 0)
        {
            //TODO: StartIndex and Size implementation

            if (_Destination == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->DownloadFile: _Destination is null.");
                return false;
            }

            if (!CheckFileExistence(
                _BucketName,
                _KeyInBucket,
                out bool bExists,
                _ErrorMessageAction
                ))
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->DownloadFile: CheckFileExistence failed.");
                return false;
            }

            if (!bExists)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->DownloadFile: File does not exist in the File Service.");
                return false;
            }
            
            if (_Destination.Type == EStringOrStreamEnum.String)
            {
                if (TransferUtil == null)
                {
                    _ErrorMessageAction?.Invoke("FileServiceAWS->DownloadFile: TransferUtil is null.");
                    return false;
                }

                try
                {
                    TransferUtil.Download(
                        _Destination.String,
                        _BucketName,
                        _KeyInBucket
                    );
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"FileServiceAWS->DownloadFile: {e.Message}, Trace: {e.StackTrace}");
                    return false;
                }

                if (!Utility.DoesFileExist(
                    _Destination.String,
                    out bool bLocalFileExists,
                    _ErrorMessageAction))
                {
                    _ErrorMessageAction?.Invoke("FileServiceAWS->DownloadFile: DoesFileExist failed.");
                    return false;
                }

                if (!bLocalFileExists)
                {
                    _ErrorMessageAction?.Invoke("FileServiceAWS->DownloadFile: Download finished, but still file does not locally exist.");
                    return false;
                }
            }
            else
            {
                if (S3Client == null)
                {
                    _ErrorMessageAction?.Invoke("FileServiceAWS->DownloadFile: S3Client is null.");
                    return false;
                }

                if (_Destination.Stream == null)
                {
                    _ErrorMessageAction?.Invoke("FileServiceAWS->DownloadFile: Destination stream is null.");
                    return false;
                }

                GetObjectRequest GetRequest = new GetObjectRequest
                {
                    BucketName = _BucketName,
                    Key = _KeyInBucket
                };

                try
                {
                    using (var CreatedTask = S3Client.GetObjectAsync(GetRequest))
                    {
                        CreatedTask.Wait();

                        using (GetObjectResponse GetResponse = CreatedTask.Result)
                        {
                            try
                            {
                                GetResponse.ResponseStream.CopyTo(_Destination.Stream);
                            }
                            catch (Exception e)
                            {
                                _ErrorMessageAction?.Invoke($"FileServiceAWS->DownloadFile: {e.Message}, Trace: {e.StackTrace}");
                                return false;
                            }
                        }
                    }
                    
                    _Destination.Stream.Position = 0;
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke($"FileServiceAWS->DownloadFile: {e.Message}, Trace: {e.StackTrace}");
                }
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>GetFileSize:</para>
        ///
        /// <para>Gets size of a file in bytes from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.GetFileSize"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool GetFileSize(
            string _BucketName,
            string _KeyInBucket,
            out ulong _FileSize,
            Action<string> _ErrorMessageAction = null)
        {
            _FileSize = 0;

            if (S3Client == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->GetFileSize: S3Client is null.");
                return false;
            }

            GetObjectMetadataRequest MetadataRequest = new GetObjectMetadataRequest()
            {
                BucketName = _BucketName,
                Key = _KeyInBucket
            };

            try
            {
                using (var CreatedTask = S3Client.GetObjectMetadataAsync(MetadataRequest))
                {
                    CreatedTask.Wait();

                    var MetadataResponse = CreatedTask.Result;

                    if (MetadataResponse == null || MetadataResponse.Headers == null)
                    {
                        _ErrorMessageAction?.Invoke("FileServiceAWS->GetFileSize: MetadataResponse or MetadataResponse.Headers is null.");
                        return false;
                    }

                    _FileSize = (ulong)MetadataResponse.Headers.ContentLength;
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->GetFileSize: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>GetFileTags:</para>
        ///
        /// <para>Gets the tags of the file from the file service</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.GetFileTags"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool GetFileTags(
            string _BucketName,
            string _KeyInBucket,
            out List<Tuple<string, string>> _Tags,
            Action<string> _ErrorMessageAction = null)
        {
            _Tags = new List<Tuple<string, string>>();

            if (S3Client == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->GetFileTags: S3Client is null.");
                return false;
            }

            GetObjectTaggingRequest TaggingRequest = new GetObjectTaggingRequest
            {
                BucketName = _BucketName,
                Key = _KeyInBucket
            };

            try
            {
                using (var CreatedTask = S3Client.GetObjectTaggingAsync(TaggingRequest))
                {
                    CreatedTask.Wait();

                    var TaggingResponse = CreatedTask.Result;

                    if (TaggingResponse == null || TaggingResponse.Tagging == null)
                    {
                        _ErrorMessageAction?.Invoke("FileServiceAWS->GetFileTags: TaggingResponse or TaggingResponse.Tagging is null.");
                        return false;
                    }

                    foreach (var CurrentTag in TaggingResponse.Tagging)
                    {
                        if (CurrentTag != null)
                        {
                            _Tags.Add(new Tuple<string, string>(CurrentTag.Key, CurrentTag.Value));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->GetFileTags: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>SetFileTags:</para>
        ///
        /// <para>Sets the tags of the file in the file service, existing tags for the file in the cloud will be deleted</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.SetFileTags"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetFileTags(
            string _BucketName,
            string _KeyInBucket,
            Tuple<string, string>[] _Tags,
            Action<string> _ErrorMessageAction = null)
        {
            if (S3Client == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->SetFileTags: S3Client is null.");
                return false;
            }

            if (_Tags == null || _Tags.Length == 0)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->SetFileTags: Tags parameter is null or empty.");
                return false;
            }

            List<Tag> NewTags = new List<Tag>();
            foreach (Tuple<string, string> CurrentTag in _Tags)
            {
                Tag NewTag = new Tag()
                {
                    Key = CurrentTag.Item1,
                    Value = CurrentTag.Item2
                };
                NewTags.Add(NewTag);
            }
            Tagging NewTagSet = new Tagging();
            NewTagSet.TagSet = NewTags;

            PutObjectTaggingRequest TaggingRequest = new PutObjectTaggingRequest
            {
                BucketName = _BucketName,
                Key = _KeyInBucket,
                Tagging = NewTagSet
            };

            try
            {
                using (var CreatedTask = S3Client.PutObjectTaggingAsync(TaggingRequest))
                {
                    CreatedTask.Wait();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->SetFileTags: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>CheckFileExistence:</para>
        ///
        /// <para>Checks existence of an object in File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.CheckFileExistence"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CheckFileExistence(
            string _BucketName,
            string _KeyInBucket,
            out bool _bExists,
            Action<string> _ErrorMessageAction = null)
        {
            _bExists = false;

            if (S3Client == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->CheckFileExistence: S3Client is null.");
                return false;
            }

            GetObjectMetadataRequest MetadataRequest = new GetObjectMetadataRequest()
            {
                BucketName = _BucketName,
                Key = _KeyInBucket
            };

            try
            {
                using (var CreatedTask = S3Client.GetObjectMetadataAsync(MetadataRequest))
                {
                    CreatedTask.Wait();
                }
            }
            catch (AmazonS3Exception e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return true;
                }

                _ErrorMessageAction?.Invoke($"FileServiceAWS->CheckFileExistence: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->CheckFileExistence: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }

            _bExists = true;

            return true;
        }

        /// <summary>
        ///
        /// <para>UploadFile:</para>
        ///
        /// <para>Uploads a local file to File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.UploadFile"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool UploadFile(
            StringOrStream _LocalFileOrStream, 
            string _BucketName, 
            string _KeyInBucket, 
            ERemoteFileReadPublicity _RemoteFileReadAccess = ERemoteFileReadPublicity.AuthenticatedRead,
            Tuple<string, string>[] _FileTags = null,
            Action<string> _ErrorMessageAction = null)
        {
            if (TransferUtil == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->UploadFile: TransferUtil is null.");
                return false;
            }

            TransferUtilityUploadRequest UploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = _BucketName,
                PartSize = 16,
                Key = _KeyInBucket
            };
            if (_LocalFileOrStream.Type == EStringOrStreamEnum.String)
            {
                if (!Utility.DoesFileExist(
                _LocalFileOrStream.String,
                out bool bLocalFileExists,
                _ErrorMessageAction))
                {
                    _ErrorMessageAction?.Invoke("FileServiceAWS->UploadFile: DoesFileExist failed.");
                    return false;
                }

                if (!bLocalFileExists)
                {
                    _ErrorMessageAction?.Invoke("FileServiceAWS->UploadFile: Local file does not exist.");
                    return false;
                }

                UploadRequest.FilePath = _LocalFileOrStream.String;
            }
            else
            {
                UploadRequest.InputStream = _LocalFileOrStream.Stream;
            }

            if (_RemoteFileReadAccess == ERemoteFileReadPublicity.PublicRead)
            {
                UploadRequest.CannedACL = S3CannedACL.PublicRead;
            }
            else if (_RemoteFileReadAccess == ERemoteFileReadPublicity.ProjectWideProtectedRead)
            {
                UploadRequest.CannedACL = S3CannedACL.AuthenticatedRead;
            }
            else
            {
                UploadRequest.CannedACL = S3CannedACL.AuthenticatedRead;
            }

            if (_FileTags != null && _FileTags.Length > 0)
            {
                List<Tag> FileTags = new List<Tag>();
                foreach (Tuple<string, string> CurrentTag in _FileTags)
                {
                    Tag NewTag = new Tag
                    {
                        Key = CurrentTag.Item1,
                        Value = CurrentTag.Item2
                    };
                    FileTags.Add(NewTag);
                }

                UploadRequest.TagSet = FileTags;
            }

            try
            {
                TransferUtil.Upload(UploadRequest);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->UploadFile: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }

            return true;
        }

        /// <summary>
        ///
        /// <para>SetFileAccessibility:</para>
        ///
        /// <para>Changes accessibility of a file in the File Service</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.SetFileAccessibility"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetFileAccessibility(
            string _BucketName,
            string _KeyInBucket,
            ERemoteFileReadPublicity _RemoteFileReadAccess = ERemoteFileReadPublicity.AuthenticatedRead,
            Action<string> _ErrorMessageAction = null)
        {
            if (S3Client == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->SetFileAccessibility: S3Client is null.");
                return false;
            }

            PutACLRequest Request = new PutACLRequest
            {
                BucketName = _BucketName,
                Key = _KeyInBucket
            };

            if (_RemoteFileReadAccess == ERemoteFileReadPublicity.PublicRead)
            {
                Request.CannedACL = S3CannedACL.PublicRead;
            }
            else if (_RemoteFileReadAccess == ERemoteFileReadPublicity.ProjectWideProtectedRead)
            {
                Request.CannedACL = S3CannedACL.AuthenticatedRead;
            }
            else
            {
                Request.CannedACL = S3CannedACL.AuthenticatedRead;
            }

            try
            {
                using (var CreatedTask = S3Client.PutACLAsync(Request))
                {
                    CreatedTask.Wait();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->SetFileAccessibility: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>CreateSignedURLForUpload:</para>
        ///
        /// <para>Creates signed url for uploading a file</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.CreateSignedURLForUpload"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool CreateSignedURLForUpload(
            out string _SignedUrl,
            string _BucketName,
            string _KeyInBucket,
            string _ContentType,
            int _URLValidForMinutes = 60,
            Action<string> _ErrorMessageAction = null)
        {
            var PreSignedRequest = new GetPreSignedUrlRequest()
            {
                BucketName = _BucketName,
                Key = _KeyInBucket,
                ContentType = _ContentType,
                Expires = DateTime.UtcNow.AddMinutes(_URLValidForMinutes),
                Verb = HttpVerb.PUT,
                Protocol = Protocol.HTTPS
            };

            try
            {
                _SignedUrl = S3Client.GetPreSignedURL(PreSignedRequest);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->CreateSignedURLForUpload: {e.Message}, Trace: {e.StackTrace}");
                _SignedUrl = null;
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>CreateSignedURLForDownload:</para>
        ///
        /// <para>Creates signed url for downloading a file</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.CreateSignedURLForDownload"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool CreateSignedURLForDownload(
            out string _SignedUrl,
            string _BucketName, 
            string _KeyInBucket, 
            int _URLValidForMinutes = 1, 
            Action<string> _ErrorMessageAction = null)
        {
            var PreSignedRequest = new GetPreSignedUrlRequest()
            {
                BucketName = _BucketName,
                Key = _KeyInBucket,
                Expires = DateTime.UtcNow.AddMinutes(_URLValidForMinutes),
                Verb = HttpVerb.GET,
                Protocol = Protocol.HTTPS
            };

            try
            {
                _SignedUrl = S3Client.GetPreSignedURL(PreSignedRequest);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->CreateSignedURLForDownload: {e.Message}, Trace: {e.StackTrace}");
                _SignedUrl = null;
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>ListAllFilesInBucket:</para>
        ///
        /// <para>Lists keys of all files</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.ListAllFilesInBucket"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool ListAllFilesInBucket(string _BucketName, out List<string> _FileKeys, Action<string> _ErrorMessageAction = null)
        {
            _FileKeys = null;

            var AllObjects = new List<string>();

            try
            {
                using (var ListObjectsTask = S3Client.ListObjectsAsync(_BucketName))
                {
                    ListObjectsTask.Wait();
                    foreach (var FileObject in ListObjectsTask.Result.S3Objects)
                    {
                        if (FileObject != null)
                        {
                            AllObjects.Add(FileObject.Key);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AllObjects = null;
                _ErrorMessageAction?.Invoke($"FileServiceAWS->ListAllFilesInBucket: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }

            if (AllObjects.Count > 0)
            {
                _FileKeys = AllObjects;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>GetFileChecksum:</para>
        ///
        /// <para>Gets MD5 checksum of a file from File Service, caller thread will be blocked before it is done</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.GetFileChecksum"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool GetFileChecksum(
            string _BucketName,
            string _KeyInBucket,
            out string _Checksum,
            Action<string> _ErrorMessageAction = null)
        {
            var MetadataRequest = new GetObjectMetadataRequest()
            {
                BucketName = _BucketName,
                Key = _KeyInBucket
            };

            _Checksum = null;

            try
            {
                using (var CreatedTask = S3Client.GetObjectMetadataAsync(MetadataRequest))
                {
                    CreatedTask.Wait();

                    var MetadataResponse = CreatedTask.Result;

                    if (MetadataResponse == null || MetadataResponse.ETag == null)
                    {
                        _ErrorMessageAction?.Invoke("FileServiceAWS->GetFileChecksum: MetadataResponse or MetadataResponse.ETag is null.");
                        return false;
                    }

                    _Checksum = MetadataResponse.ETag.Trim('"').ToLower();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->GetFileChecksum: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///
        /// <para>CreateFilePubSubNotification:</para>
        ///
        /// <para>Creates file based pub/sub notification</para>
        ///
        /// <para>Check <seealso cref="IFileServiceInterface.CreateFilePubSubNotification"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CreateFilePubSubNotification(string _BucketName, string _TopicName, string _PathPrefixToListen, List<EFilePubSubNotificationEventType> _EventsToListen, Action<string> _ErrorMessageAction = null)
        {
            if (S3Client == null)
            {
                _ErrorMessageAction?.Invoke("FileServiceAWS->CreateFilePubSubNotification: S3Client is null.");
                return false;
            }

            var EventTypes = new List<EventType>();
            if (_EventsToListen.Contains(EFilePubSubNotificationEventType.Uploaded))
                EventTypes.Add("ObjectCreatedAll");
            if (_EventsToListen.Contains(EFilePubSubNotificationEventType.Deleted))
                EventTypes.Add("ObjectRemovedAll");

            PutBucketNotificationRequest Request = new PutBucketNotificationRequest
            {
                BucketName = _BucketName,
                TopicConfigurations = new List<TopicConfiguration>()
                {
                    new TopicConfiguration()
                    {
                        Topic = _TopicName,
                        Filter = new Filter()
                        {
                            S3KeyFilter = new S3KeyFilter()
                            {
                                FilterRules = new List<FilterRule>()
                                {
                                    new FilterRule("prefix", _PathPrefixToListen)
                                }
                            }
                        },
                        Events = EventTypes
                    }
                }
            };

            try
            {
                using (var CreatedTask = S3Client.PutBucketNotificationAsync(Request))
                {
                    CreatedTask.Wait();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"FileServiceAWS->CreateFilePubSubNotification: {e.Message}, Trace: {e.StackTrace}");
                return false;
            }
            return true;
        }
    }
}