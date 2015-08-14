using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace ContextualTriggers
{
    public class ProximityStateTrigger : StateTriggerBase
    {
        private ProximitySensor proximitySensor;

        public int MinimumDistanceInMillimeters { get; set; } = Int32.MinValue;
        public bool ObjectDetected { get; set; } = true;

        public ProximityStateTrigger()
        {
            var watcher = DeviceInformation.CreateWatcher(ProximitySensor.GetDeviceSelector());

            watcher.Added += OnProximitySensorAddedAsync;
            watcher.Removed += OnProximitySensorRemoved;
            watcher.Start();
        }

        /// <summary>
        /// Invoked when the device watcher detects that the proximity sensor was added.
        /// </summary>
        /// <param name="sender">The device watcher.</param>
        /// <param name="device">The device that was added.</param>
        private async void OnProximitySensorAddedAsync(DeviceWatcher sender, DeviceInformation device)
        {
            if (this.proximitySensor == null)
            {
                var addedSensor = ProximitySensor.FromId(device.Id);

                if (addedSensor != null)
                {
                    var minimumDistanceSatisfied = true;

                    //if we care about minimum distance
                    if (this.MinimumDistanceInMillimeters > Int32.MinValue)
                    {
                        if ((this.MinimumDistanceInMillimeters > addedSensor.MaxDistanceInMillimeters) ||
                            (this.MinimumDistanceInMillimeters < addedSensor.MinDistanceInMillimeters))
                        {
                            minimumDistanceSatisfied = false;
                        }
                    }

                    if (minimumDistanceSatisfied)
                    {
                        this.proximitySensor = addedSensor;

                        await SetActiveFromReadingAsync(this.proximitySensor.GetCurrentReading());

                        this.proximitySensor.ReadingChanged += ProximitySensor_ReadingChangedAsync;
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the device watcher detects that the proximity sensor was removed.
        /// </summary>
        /// <param name="sender">The device watcher.</param>
        /// <param name="device">The device that was removed.</param>
        private void OnProximitySensorRemoved(DeviceWatcher sender, DeviceInformationUpdate device)
        {
            if ((this.proximitySensor != null) && (this.proximitySensor.DeviceId == device.Id))
            {
                this.proximitySensor.ReadingChanged -= ProximitySensor_ReadingChangedAsync;
                this.proximitySensor = null;

                SetActive(false);
            }
        }

        /// <summary>
        /// Invoked when ProximitySensor reading changed event gets raised.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void ProximitySensor_ReadingChangedAsync(ProximitySensor sender, ProximitySensorReadingChangedEventArgs args)
        {
            await SetActiveFromReadingAsync(args.Reading);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reading"></param>
        private async Task SetActiveFromReadingAsync(ProximitySensorReading reading)
        {
            if (reading != null)
            {
                if (this.ObjectDetected)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var isActive = reading.IsDetected;

                        if (isActive)
                        {
                            if (reading.DistanceInMillimeters.HasValue)
                            {
                                isActive = (reading.DistanceInMillimeters >= this.MinimumDistanceInMillimeters);
                            }
                        }

                        SetActive(isActive);
                    });
                }
                else
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SetActive(!reading.IsDetected);
                    });
                }
            }
        }
    }
}
