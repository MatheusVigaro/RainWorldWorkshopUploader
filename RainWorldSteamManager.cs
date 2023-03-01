using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using RainWorldWorkshopUploader;
using Steamworks;

internal class RainWorldSteamManager
{
    public RainWorldSteamManager()
    {
        if (SteamManager.Initialized)
        {
            string personaName = SteamFriends.GetPersonaName();
            Console.WriteLine("Logged in as steam account: " + personaName);
            RainWorldSteamManager.ownerUserID = SteamUser.GetSteamID().m_SteamID;
            Console.WriteLine("SteamUserStats.RequestCurrentStats() - " + SteamUserStats.RequestCurrentStats().ToString());
            this.currentlyDownloading = new List<PublishedFileId_t>();
            this.createItemCallback = CallResult<CreateItemResult_t>.Create(new CallResult<CreateItemResult_t>.APIDispatchDelegate(this.OnCreateItemResult));
            this.updateItemCallback = CallResult<SubmitItemUpdateResult_t>.Create(new CallResult<SubmitItemUpdateResult_t>.APIDispatchDelegate(this.OnUpdateItemResult));
            this.queryCallback = CallResult<SteamUGCQueryCompleted_t>.Create(new CallResult<SteamUGCQueryCompleted_t>.APIDispatchDelegate(this.OnQueryResult));
            this.downloadCallback = Callback<DownloadItemResult_t>.Create(new Callback<DownloadItemResult_t>.DispatchDelegate(this.OnDownloadItemResult));
            return;
        }
        Console.WriteLine("Steam login failed");
        this.ShutDown();
    }

    public void Update()
    {
        if (!SteamManager.Initialized)
        {
            return;
        }
        if (this.isCurrentlyUploading)
        {
            SteamUGC.GetItemUpdateProgress(this.updateHandle, out this.bytesProcessed, out this.bytesTotal);
        }
        SteamAPI.RunCallbacks();
    }

    public void ShutDown()
    {
        Console.WriteLine("Shutting down steam manager");
        this.shutdown = true;
    }

    public static string ValidateWorkshopModForProblems(Mod mod)
    {
        string text = mod.path + Path.DirectorySeparatorChar.ToString() + "thumbnail.png";
        if (File.Exists(text))
        {
            try
            {
                if (new FileInfo(text).Length >= 1000000L)
                {
                    return "Mod's thumbnail image must be less than 1 MB in size.";
                }
                Bitmap img = new Bitmap(text);
                double num = (double)img.Height / (double)img.Width;
                if (num < 0.5616 || num > 0.5634)
                {
                    return "Mod's thumbnail image should have a 16:9 aspect ratio.";
                }
            }
            catch
            {
            }
        }
        if (!ValidateModApplicableForWorkshopUpload(mod))
        {
            return "This mod cannot be uploaded to the Workshop.";
        }
        return null;
    }

    public static bool ValidateModApplicableForWorkshopUpload(Mod mod)
    {
        return !(mod.WorkshopData.ID == "moreslugcats") && !(mod.WorkshopData.ID == "rwremix") && !(mod.WorkshopData.ID == "expedition") && !(mod.WorkshopData.ID == "jollycoop") && !(mod.WorkshopData.ID == "devtools") && !(mod.WorkshopData.ID == "RainWorld_BaseGame") && !(mod.id == "_TestDummy_");
    }

    private bool InitCreate()
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.isCurrentlyCreating = false;
            this.lastCreateFail = "Steam manager not initialized";
            this.ShutDown();
            return false;
        }
        this.lastCreateFail = "";
        this.needsLegalAgreement = false;
        this.isCurrentlyCreating = true;
        return true;
    }

    public void CreateWorkshopMod(Mod mod)
    {
        MainWindow.Instance.SetTitle("Creating new Workshop item");
        if (!this.InitCreate())
        {
            return;
        }
        this.creatingMod = mod;
        SteamAPICall_t hAPICall = SteamUGC.CreateItem(new AppId_t(RainWorldSteamManager.APP_ID), EWorkshopFileType.k_EWorkshopFileTypeFirst);
        this.createItemCallback.Set(hAPICall, null);
    }

    private bool InitUpload()
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.isCurrentlyUploading = false;
            this.lastUploadFail = "Steam manager not initialized";
            this.ShutDown();
            return false;
        }
        this.lastUploadFail = "";
        this.bytesTotal = 0UL;
        this.needsLegalAgreement = false;
        this.isCurrentlyUploading = true;
        return true;
    }

    public bool UploadWorkshopMod(Mod mod)
    {
        MainWindow.Instance.SetTitle("Updating Workshop item");
        if (!this.InitUpload())
        {
            return false;
        }
        if (mod.WorkshopData.WorkshopID == 0UL)
        {
            MainWindow.Instance.Error("Cannot update mod with no pre-existing workshop ID.");
            this.isCurrentlyUploading = false;
            this.lastUploadFail = "Cannot update mod with no pre-existing workshop ID.";
            return false;
        }
        this.uploadingMod = mod;
        this.updateHandle = SteamUGC.StartItemUpdate(new AppId_t(RainWorldSteamManager.APP_ID), new PublishedFileId_t(mod.WorkshopData.WorkshopID));


        SteamUGC.AddItemKeyValueTag(this.updateHandle, "id", mod.WorkshopData.ID);
        SteamUGC.AddItemKeyValueTag(this.updateHandle, "version", mod.WorkshopData.Version);
        SteamUGC.SetItemContent(this.updateHandle, mod.path);

        if (mod.WorkshopData.UploadFilesOnly.Value == false)
        {
            SteamUGC.SetItemTitle(this.updateHandle, mod.WorkshopData.Title);
            SteamUGC.SetItemVisibility(this.updateHandle, WorkshopDataClass.VisibilityFromText(mod.WorkshopData.Visibility));
            SteamUGC.SetItemDescription(this.updateHandle, mod.WorkshopData.Description);
            SteamUGC.AddItemKeyValueTag(this.updateHandle, "targetGameVersion", mod.WorkshopData.TargetGameVersion);
            SteamUGC.AddItemKeyValueTag(this.updateHandle, "authors", mod.WorkshopData.Authors);
            SteamUGC.AddItemKeyValueTag(this.updateHandle, "requirements", mod.WorkshopData.Requirements);
            SteamUGC.AddItemKeyValueTag(this.updateHandle, "requirementNames", mod.WorkshopData.RequirementNames);
            SteamUGC.SetItemTags(this.updateHandle, mod.WorkshopData.Tags);
            string text = mod.path + Path.DirectorySeparatorChar.ToString() + "thumbnail.png";
            if (File.Exists(text))
            {
                try
                {
                    if (new FileInfo(text).Length < 1000000L)
                    {
                        SteamUGC.SetItemPreview(this.updateHandle, text);
                    }
                }
                catch
                {
                }
            }
            if (mod.trailerID != null && mod.trailerID != "")
            {
                SteamUGC.AddItemPreviewVideo(this.updateHandle, mod.trailerID);
            }
        }
        SteamAPICall_t hAPICall = SteamUGC.SubmitItemUpdate(this.updateHandle, "");
        this.updateItemCallback.Set(hAPICall, null);
        return true;
    }

    private bool InitQuery()
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.isCurrentlyQuerying = false;
            this.lastQueryFail = "Steam manager not initialized";
            this.ShutDown();
            return false;
        }
        this.lastQueryCount = -1;
        this.lastQueryFail = "";
        this.isCurrentlyQuerying = true;
        return true;
    }

    public void FindWorkshopItemsWithKeyValue(string key, string value)
    {
        MainWindow.Instance.SetTitle("Searching for the Workshop item");

        if (!this.InitQuery())
        {
            return;
        }
        AppId_t appId_t = new AppId_t(RainWorldSteamManager.APP_ID);
        this.lastQueryHandle = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_RankedByPublicationDate, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse, appId_t, appId_t, null);
        SteamUGC.AddRequiredKeyValueTag(this.lastQueryHandle, key, value);
        SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(this.lastQueryHandle);
        this.queryCallback.Set(hAPICall, null);
    }

    public void ShowLegalAgreement()
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.ShutDown();
            return;
        }
        Console.WriteLine("SHOWING LEGAL AGREEMENT");
        SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/sharedfiles/workshoplegalagreement", EActivateGameOverlayToWebPageMode.k_EActivateGameOverlayToWebPageMode_Default);
    }

    public void ShowWorkshopDetails(ulong workshopId)
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.ShutDown();
            return;
        }
        Console.WriteLine("SHOWING WORKSHOP DETAILS FOR: " + workshopId.ToString());
        SteamFriends.ActivateGameOverlayToWebPage("steam://url/CommunityFilePage/" + workshopId.ToString(), EActivateGameOverlayToWebPageMode.k_EActivateGameOverlayToWebPageMode_Default);
    }

    public void ShowStorePage()
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.ShutDown();
            return;
        }
        SteamFriends.ActivateGameOverlayToStore(new AppId_t(RainWorldSteamManager.APP_ID), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
    }

    public void ShowDownpourStorePage()
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.ShutDown();
            return;
        }
        SteamFriends.ActivateGameOverlayToStore(new AppId_t(RainWorldSteamManager.DOWNPOUR_APP_ID), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
    }

    public PublishedFileId_t[] GetSubscribedItems()
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.ShutDown();
            return new PublishedFileId_t[0];
        }
        uint numSubscribedItems = SteamUGC.GetNumSubscribedItems();
        PublishedFileId_t[] array = new PublishedFileId_t[numSubscribedItems];
        if (numSubscribedItems > 0U)
        {
            SteamUGC.GetSubscribedItems(array, numSubscribedItems);
        }
        return array;
    }

    public bool DownloadWorkshopMod(PublishedFileId_t fileid)
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.ShutDown();
            return false;
        }
        this.currentlyDownloading.Add(fileid);
        this.numberItemsAddedForDownloading++;
        bool flag = SteamUGC.DownloadItem(fileid, true);
        if (!flag)
        {
            this.currentlyDownloading.Remove(fileid);
            this.numberItemsAddedForDownloading--;
        }
        return flag;
    }

    public void ResetDownloadBatch()
    {
        this.numberItemsAddedForDownloading = 0;
    }

    public double GetWorkshopDownloadProgress()
    {
        if (!SteamManager.Initialized)
        {
            Console.WriteLine("Steam manager not initialized");
            this.ShutDown();
            return 0.0;
        }
        double num = 1.0 / (double)this.numberItemsAddedForDownloading;
        double num2 = 0.0;
        int num3 = this.numberItemsAddedForDownloading - this.currentlyDownloading.Count;
        num2 += (double)num3 * num;
        for (int i = 0; i < this.currentlyDownloading.Count; i++)
        {
            ulong punBytesDownloaded;
            ulong punBytesTotal;
            if (SteamUGC.GetItemDownloadInfo(this.currentlyDownloading[i], out punBytesDownloaded, out punBytesTotal) && punBytesTotal > 0UL)
            {
                num2 += punBytesDownloaded / punBytesTotal * num;
            }
        }
        return num2;
    }

    private void OnCreateItemResult(CreateItemResult_t callback, bool ioFailure)
    {
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            PublishedFileId_t nPublishedFileId = callback.m_nPublishedFileId;
            this.creatingMod.WorkshopData.WorkshopID = nPublishedFileId.m_PublishedFileId;
            if (callback.m_bUserNeedsToAcceptWorkshopLegalAgreement)
            {
                Console.WriteLine("NEEDS TO ACCEPT LEGAL AGREEMENT");
                this.needsLegalAgreement = true;
            }
            else
            {
                Console.WriteLine("SUCCESS");
                MainWindow.Instance.SaveWorkshopData(true);
                UploadWorkshopMod(MainWindow.SelectedMod);
            }
        }
        else
        {
            Console.WriteLine("CREATE WORKSHOP ITEM FAILED: " + callback.m_eResult.ToString());
            if (callback.m_eResult == EResult.k_EResultBanned || callback.m_eResult == EResult.k_EResultInsufficientPrivilege)
            {
                this.lastCreateFail = "Your account is locked or has a community or VAC ban.";
            }
            else if (callback.m_eResult == EResult.k_EResultTimeout)
            {
                this.lastCreateFail = "The operation timed out. Try again later.";
            }
            else if (callback.m_eResult == EResult.k_EResultNotLoggedOn)
            {
                this.lastCreateFail = "You are not currently logged into Steam.";
            }
            else if (callback.m_eResult == EResult.k_EResultServiceUnavailable)
            {
                this.lastCreateFail = "The workshop service is unavailable. Try again later.";
            }
            else if (callback.m_eResult == EResult.k_EResultInvalidParam)
            {
                this.lastCreateFail = "Some of the metadata for this mod was invalid and cannot be accepted.";
            }
            else if (callback.m_eResult == EResult.k_EResultAccessDenied)
            {
                this.lastCreateFail = "Access was denied. Do you own the game on Steam?";
            }
            else if (callback.m_eResult == EResult.k_EResultLimitExceeded)
            {
                this.lastCreateFail = "Limit exceeded. You may need to reduce the filesize or remove prior workshop content.";
            }
            else if (callback.m_eResult == EResult.k_EResultFileNotFound)
            {
                this.lastCreateFail = "Could not find the files to upload.";
            }
            else if (callback.m_eResult == EResult.k_EResultDuplicateRequest)
            {
                this.lastCreateFail = "This item already exists on the workshop.";
            }
            else if (callback.m_eResult == EResult.k_EResultDuplicateName)
            {
                this.lastCreateFail = "One of your workshop mods already has the same name as this one.";
            }
            else if (callback.m_eResult == EResult.k_EResultServiceReadOnly)
            {
                this.lastCreateFail = "Your account is in read-only mode due to a recent password or email change.";
            }
            else
            {
                this.lastCreateFail = callback.m_eResult.ToString();
            }
            MainWindow.Instance.Error(lastCreateFail);

        }
        this.isCurrentlyCreating = false;
    }

    private void OnUpdateItemResult(SubmitItemUpdateResult_t callback, bool ioFailure)
    {
        this.isCurrentlyUploading = false;
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            if (callback.m_bUserNeedsToAcceptWorkshopLegalAgreement)
            {
                MainWindow.Instance.Error("NEEDS TO ACCEPT LEGAL AGREEMENT");
                this.needsLegalAgreement = true;
            }
            else
            {
                MainWindow.Instance.IsEnabled = true;
                MessageBox.Show("Upload complete!");
                Console.WriteLine("SUCCESS");
            }
        }
        else
        {
            Console.WriteLine("UPLOAD WORKSHOP ITEM FAILED: " + callback.m_eResult.ToString());
            if (callback.m_eResult == EResult.k_EResultAccessDenied)
            {
                this.lastUploadFail = "Access was denied. Do you own the game on Steam?";
            }
            else if (callback.m_eResult == EResult.k_EResultInvalidParam)
            {
                this.lastUploadFail = "Some of the metadata for this mod was invalid and cannot be accepted.";
            }
            else if (callback.m_eResult == EResult.k_EResultFileNotFound)
            {
                this.lastUploadFail = "Could not find the files to upload.";
            }
            else if (callback.m_eResult == EResult.k_EResultLimitExceeded)
            {
                this.lastUploadFail = "Limit exceeded. You may need to reduce the filesize or remove prior workshop content.";
            }
            else if (callback.m_eResult == EResult.k_EResultLockingFailed)
            {
                this.lastUploadFail = "Failed to acquire User Generated Content lock. Try again later.";
            }
            else if (callback.m_eResult == EResult.k_EResultFail)
            {
                this.lastUploadFail = "Generic failure.";
            }
            else
            {
                this.lastUploadFail = callback.m_eResult.ToString();
            }
            MainWindow.Instance.Error(lastUploadFail);
        }

        MainWindow.Instance.SetTitle("");
        this.isCurrentlyUploading = false;
    }

    private void OnQueryResult(SteamUGCQueryCompleted_t callback, bool ioFailure)
    {
        this.lastQueryOwners = new List<ulong>();
        this.lastQueryFiles = new List<PublishedFileId_t>();
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            Console.WriteLine("GOT " + callback.m_unTotalMatchingResults.ToString() + " MATCHING RESULTS FROM QUERY");
            this.lastQueryCount = (int)callback.m_unTotalMatchingResults;
            for (int i = 0; i < this.lastQueryCount; i++)
            {
                SteamUGCDetails_t pDetails;
                if (SteamUGC.GetQueryUGCResult(this.lastQueryHandle, (uint)i, out pDetails))
                {
                    this.lastQueryOwners.Add(pDetails.m_ulSteamIDOwner);
                    this.lastQueryFiles.Add(pDetails.m_nPublishedFileId);
                }
            }

            for (int i = 0; i < lastQueryFiles.Count; i++)
            {
                if (lastQueryOwners[i] == RainWorldSteamManager.ownerUserID)
                {
                    MainWindow.SelectedMod.WorkshopData.WorkshopID = lastQueryFiles[i].m_PublishedFileId;
                    break;
                }
            }
            if (MainWindow.SelectedMod.WorkshopData.WorkshopID > 0UL)
            {
                MainWindow.Instance.SaveWorkshopData(true);
                UploadWorkshopMod(MainWindow.SelectedMod);
            }
            else
            {
                CreateWorkshopMod(MainWindow.SelectedMod);
            }
        }
        else
        {
            this.lastQueryFail = callback.m_eResult.ToString();
            this.lastQueryCount = -1;
            MainWindow.Instance.Error("QUERY WORKSHOP ITEMS FAILED: " + callback.m_eResult.ToString());
        }
        this.isCurrentlyQuerying = false;
    }

    private void OnDownloadItemResult(DownloadItemResult_t callback)
    {
        if (callback.m_unAppID.m_AppId == RainWorldSteamManager.APP_ID)
        {
            PublishedFileId_t nPublishedFileId = callback.m_nPublishedFileId;
            if (callback.m_eResult != EResult.k_EResultOK)
            {
                Console.WriteLine("DOWNLOAD WORKSHOP ITEM (" + nPublishedFileId.m_PublishedFileId.ToString() + ") FAILED: " + callback.m_eResult.ToString());
            }
            if (this.currentlyDownloading.Contains(nPublishedFileId))
            {
                this.currentlyDownloading.Remove(nPublishedFileId);
            }
        }
    }

    public static uint APP_ID = 312520U;

    public static uint DOWNPOUR_APP_ID = 1933390U;

    public bool shutdown;

    public static ulong ownerUserID;

    public Mod creatingMod;

    public bool isCurrentlyCreating;

    public string lastCreateFail = "";

    public bool needsLegalAgreement;

    public Mod uploadingMod;

    public bool isCurrentlyUploading;

    public string lastUploadFail = "";

    public ulong bytesProcessed;

    public ulong bytesTotal;

    public bool isCurrentlyQuerying;

    public int lastQueryCount;

    public string lastQueryFail = "";

    public UGCQueryHandle_t lastQueryHandle;

    public List<ulong> lastQueryOwners;

    public List<PublishedFileId_t> lastQueryFiles;

    public List<PublishedFileId_t> currentlyDownloading;

    public int numberItemsAddedForDownloading;

    private UGCUpdateHandle_t updateHandle;

    private CallResult<CreateItemResult_t> createItemCallback;

    private CallResult<SubmitItemUpdateResult_t> updateItemCallback;

    private CallResult<SteamUGCQueryCompleted_t> queryCallback;

    private Callback<DownloadItemResult_t> downloadCallback;
}
