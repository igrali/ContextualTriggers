using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace ContextualTriggers
{
    public class ActivityStateTrigger : StateTriggerBase
    {
        // Common class ID for activity sensors
        //Guid ActivitySensorClassId = new Guid("9D9E0118-1807-4F2E-96E4-2CE57142E196");

        private ActivitySensor activitySensor;
        private DeviceAccessInformation deviceAccessInformation;

        public ActivityType ActivityType { get; set; } = ActivityType.Unknown;

        public ActivitySensorReadingConfidence ActivityReadingConfidence { get; set; } = ActivitySensorReadingConfidence.Low;

        public ActivityStateTrigger()
        {
            var watcher = DeviceInformation.CreateWatcher(ActivitySensor.GetDeviceSelector());

            watcher.Added += OnActivitySensorAddedAsync;
            watcher.Removed += OnActivitySensorRemoved;
            watcher.Start();
        }

        /// <summary>
        /// Invoked when ActivitySensor reading changed event gets raised.
        /// </summary>
        ///<param name="sender"></param>
        ///<param name="args"></param>
        private async void ActivitySensor_ReadingChangedAsync(ActivitySensor sender, ActivitySensorReadingChangedEventArgs args)
        {
            var isActive = false;
            var reading = args.Reading;

            if (reading != null)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    isActive = (reading.Activity == this.ActivityType) && (reading.Confidence >= this.ActivityReadingConfidence);
                });
            }

            SetActive(isActive);
        }


        /// <summary>
        /// Invoked when ActivitySensor device access gets changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void DeviceAccessInfo_AccessChangedAsync(DeviceAccessInformation sender, DeviceAccessChangedEventArgs args)
        {
            var status = args.Status;

            if (status == DeviceAccessStatus.Allowed)
            {
                await InitializeAsync();
            }
            else
            {
                Release();
            }
        }

        /// <summary>
        /// Subscribes to reading changed event handlers and initializes the state trigger value.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeAsync()
        {
            if (this.activitySensor != null)
            {
                var reading = await this.activitySensor.GetCurrentReadingAsync();

                if (reading != null)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SetActive((reading.Activity == this.ActivityType) && (reading.Confidence >= this.ActivityReadingConfidence));
                    });
                }

                this.activitySensor.ReadingChanged += ActivitySensor_ReadingChangedAsync;
            }
        }

        /// <summary>
        /// Invoked when the device watcher detects that the activity sensor was added.
        /// </summary>
        /// <param name="sender">The device watcher.</param>
        /// <param name="device">The device that was added.</param>
        private async void OnActivitySensorAddedAsync(DeviceWatcher sender, DeviceInformation device)
        {
            if (this.activitySensor == null)
            {
                var addedSensor = await ActivitySensor.FromIdAsync(device.Id);

                if (addedSensor != null)
                {
                    this.activitySensor = addedSensor;

                    this.deviceAccessInformation = DeviceAccessInformation.CreateFromId(this.activitySensor.DeviceId);
                    this.deviceAccessInformation.AccessChanged += DeviceAccessInfo_AccessChangedAsync;
                }
            }
        }

        /// <summary>
        /// Invoked when the device watcher detects that the activity sensor was removed.
        /// </summary>
        /// <param name="sender">The device watcher.</param>
        /// <param name="device">The device that was removed.</param>
        private void OnActivitySensorRemoved(DeviceWatcher sender, DeviceInformationUpdate device)
        {
            if ((this.activitySensor != null) && (this.activitySensor.DeviceId == device.Id))
            {
                this.activitySensor.ReadingChanged -= ActivitySensor_ReadingChangedAsync;
                this.deviceAccessInformation.AccessChanged -= DeviceAccessInfo_AccessChangedAsync;
                this.activitySensor = null;

                SetActive(false);
            }
        }

        /// <summary>
        /// Releases reading changed event handlers.
        /// </summary>
        private void Release()
        {
            if (this.activitySensor != null)
            {
                this.activitySensor.ReadingChanged -= ActivitySensor_ReadingChangedAsync;
            }

            SetActive(false);
        }
    }
}
