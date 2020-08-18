using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Tasks.Offline;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RedDots
{
    public partial class MainPage : ContentPage
    {
        private Map _onlineMap;
        private Map _preplannedMap;
        private Map _onDemandMap;

        private string PreplannedDataFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Offline\PreplannedMap");

        private string OnDemandDataFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Offline\OnDemandMap");

        public MainPage()
        {
            InitializeComponent();
            Startup();
        }

        private async void Startup()
        {
            await Task.WhenAll(InitializeOnlineMap(), CheckPreplannedMap());
        }

        private async Task InitializeOnlineMap()
        {
            var portal = await ArcGISPortal.CreateAsync();
            var item = await PortalItem.CreateAsync(portal, "a5283e945e69456e9a4d1d29a0e0796e");
            TheMap.Map = new Map(item);
        }

        private async void DownloadButton_Clicked(object sender, EventArgs e)
        {
            await Task.WhenAll(DownloadPreplannedMap(), DownloadOnDemandMap());
        }

        private async Task DownloadOnDemandMap()
        {
            var downloadTask = await OfflineMapTask.CreateAsync(TheMap.Map);
            var parameters = await downloadTask.CreateDefaultGenerateOfflineMapParametersAsync(TheMap.GetCurrentViewpoint(ViewpointType.BoundingGeometry).TargetGeometry);
            parameters.AttachmentSyncDirection = AttachmentSyncDirection.None;
            parameters.ReturnLayerAttachmentOption = ReturnLayerAttachmentOption.None;
            var job = downloadTask.GenerateOfflineMap(parameters, OnDemandDataFolder);
            job.ProgressChanged += OnDemandJob_ProgressChanged;
            var result = await job.GetResultAsync();
            if (!result.HasErrors)
            {
                OnDemandStatusLabel.Text = "OnDemand Map: Download complete.";
                OnDemandStatusLabel.TextColor = Color.Green;
                _onDemandMap = result.OfflineMap;
            }
            else
            {
                OnDemandStatusLabel.Text = "OnDemand Map: Completed with errors.";
                OnDemandStatusLabel.TextColor = Color.Red;
            }

            ConditionallyEnableStep2();
        }

        private void OnDemandJob_ProgressChanged(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var job = (GenerateOfflineMapJob)sender;
                OnDemandStatusLabel.Text = string.Format($"OnDemand Map: {job.Status}, {job.Progress}%");
                OnDemandStatusLabel.TextColor = Color.Black;
            });
        }

        private async Task DownloadPreplannedMap()
        {
            var downloadTask = await OfflineMapTask.CreateAsync(TheMap.Map);
            var areas = await downloadTask.GetPreplannedMapAreasAsync();
            var firstArea = areas.First();
            await firstArea.LoadAsync();
            var parameters = await downloadTask.CreateDefaultDownloadPreplannedOfflineMapParametersAsync(firstArea);
            parameters.ContinueOnErrors = false;
            parameters.IncludeBasemap = true;
            var job = downloadTask.DownloadPreplannedOfflineMap(parameters, PreplannedDataFolder);
            job.ProgressChanged += PreplannedJob_ProgressChanged;
            var result = await job.GetResultAsync();
            if (!result.HasErrors)
            {
                PreplannedStatusLabel.Text = "Preplanned Map: Download complete.";
                PreplannedStatusLabel.TextColor = Color.Green;
                _preplannedMap = result.OfflineMap;
            }
            else
            {
                PreplannedStatusLabel.Text = "Preplanned Map: Completed with errors.";
                PreplannedStatusLabel.TextColor = Color.Red;
            }

            ConditionallyEnableStep2();
        }

        private void PreplannedJob_ProgressChanged(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var job = (DownloadPreplannedOfflineMapJob)sender;
                PreplannedStatusLabel.Text = string.Format($"Preplanned Map: {job.Status}, {job.Progress}%");
                PreplannedStatusLabel.TextColor = Color.Black;
            });
        }
        private async Task CheckOnDemandMap()
        {
            if (!Directory.Exists(OnDemandDataFolder))
            {
                Directory.CreateDirectory(OnDemandDataFolder);
            }

            if (Directory.EnumerateFiles(OnDemandDataFolder).Any())
            {
                try
                {
                    var mmpk = await MobileMapPackage.OpenAsync(OnDemandDataFolder);
                    OnDemandStatusLabel.Text = "OnDemand Map: Download complete.";
                    OnDemandStatusLabel.TextColor = Color.Green;
                    _onDemandMap = mmpk.Maps.First();
                }
                catch (Exception)
                {
                    Directory.Delete(OnDemandDataFolder, true);
                    await CheckPreplannedMap();
                }
            }
        }

        private async Task CheckPreplannedMap()
        {
            if (!Directory.Exists(PreplannedDataFolder))
            {
                Directory.CreateDirectory(PreplannedDataFolder);
            }

            if (Directory.EnumerateFiles(PreplannedDataFolder).Any())
            {
                try
                {
                    var mmpk = await MobileMapPackage.OpenAsync(PreplannedDataFolder);
                    PreplannedStatusLabel.Text = "Preplanned Map: Download complete.";
                    PreplannedStatusLabel.TextColor = Color.Green;
                    _preplannedMap = mmpk.Maps.First();
                }
                catch (Exception)
                {
                    Directory.Delete(PreplannedDataFolder, true);
                    await CheckPreplannedMap();
                }
            }
        }

        private void ConditionallyEnableStep2()
        {
            if (_preplannedMap != null && _onDemandMap != null)
            {
                OnlineMapButton.IsEnabled = true;
                OnDemandMapButton.IsEnabled = true;
                PreplannedMapButton.IsEnabled = true;
                CycleActivateButton.IsEnabled = true;
            }
        }

        private void OnlineMapButton_Clicked(object sender, EventArgs e)
        {

        }

        private void OnDemandMapButton_Clicked(object sender, EventArgs e)
        {

        }

        private void PreplannedMapButton_Clicked(object sender, EventArgs e)
        {

        }

        private void CycleActivateButton_Clicked(object sender, EventArgs e)
        {

        }
    }
}
