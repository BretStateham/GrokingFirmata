using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Microsoft.Maker.Serial;
using Microsoft.Maker.Firmata;
using Microsoft.Maker.RemoteWiring;
using System.Diagnostics;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

// Using techniques described here:
// https://github.com/ms-iot/remote-wiring/blob/develop/advanced.md 

namespace GrokingFirmata01
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MainPage : Page
  {
    ObservableCollection<FirmataMessage> Messages = new ObservableCollection<FirmataMessage>();

    // ALL_PINS_COMMAND per https://github.com/ms-iot/remote-wiring/blob/develop/advanced.md 
    const byte ALL_PINS_COMMAND = 0x42;

    //member variables
    IStream serial;
    UwpFirmata firmata;
    RemoteDevice arduino;

    public MainPage()
    {
      this.InitializeComponent();

      InitFirmata();
    }

    private void InitFirmata()
    {
      //USB\VID_2A03&PID_0043&REV_0001
      //create a serial connection
      //var devices = await UsbSerial.listAvailableDevicesAsync();
      //var devList = devices.ToList();

      serial = new UsbSerial("VID_2A03", "PID_0043");

      //construct the firmata client
      firmata = new UwpFirmata();
      firmata.FirmataConnectionReady += Firmata_FirmataConnectionReady;
      firmata.StringMessageReceived += Firmata_StringMessageReceived;

      //last, construct the RemoteWiring layer by passing in our Firmata layer.
      arduino = new RemoteDevice(firmata);
      arduino.DeviceReady += Arduino_DeviceReady;

      //if you create the firmata client yourself, don't forget to begin it!
      firmata.begin(serial);

      //you must always call 'begin' on your IStream object to connect.
      //these parameters do not matter for bluetooth, as they depend on the device. However, these are the best params to use for USB, so they are illustrated here
      serial.begin(57600, SerialConfig.SERIAL_8N1);

    }

    /// <summary>
    /// This is just used for some brute force debugging.  I have added various 
    ///
    ///  Firmata.sendMessage("...."); 
    /// 
    /// statements to the sketch deployed to the arduino to get feedback when
    ///  things are happening on the arduino.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="argv"></param>
    private async void Firmata_StringMessageReceived(UwpFirmata caller, StringCallbackEventArgs argv)
    {
      string message = argv.getString();
      FirmataMessage firmataMessage = new FirmataMessage(message);
      Debug.WriteLine(firmataMessage);
      await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
        //Add the message to the top of the list so that most recent messages appear at the top
        Messages.Insert(0, firmataMessage);
      }));            
    }

    private async void Arduino_DeviceReady()
    {
      for (byte pin = 0; pin <= 13; pin++)
      {
        arduino.pinMode(pin, PinMode.OUTPUT);
      }

      await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() => {
        OnButton.IsEnabled = true;
        OffButton.IsEnabled = true;
      }));

    }

    private void Firmata_FirmataConnectionReady()
    {
      ////Enable the buttons
      //OnButton.IsEnabled = true;
      //OffButton.IsEnabled = true;
    }

    void toggleAllDigitalPinsIndividually(bool setPinsHigh)
    {
      for (byte pin = 0; pin <= 13; pin++)
      {
        PinState state = (setPinsHigh) ? PinState.HIGH : PinState.LOW;
        arduino.digitalWrite(pin, state);
      }
    }

    private void OnButton_Click(object sender, RoutedEventArgs e)
    {
      toggleAllDigitalPinsIndividually(true);
      //toggleAllDigitalPins(true);
      //toggleAllDigitalPinsWithPorts(true);
    }

    private void OffButton_Click(object sender, RoutedEventArgs e)
    {
      toggleAllDigitalPinsIndividually(false);
      //toggleAllDigitalPins(false);
      //toggleAllDigitalPinsWithPorts(false);
    }

    public void toggleAllDigitalPins(bool setPinsHigh)
    {
      //we're defining our own command, so we're free to encode it how we want.
      //let's send a '1' if we want the pins HIGH, and a 0 for LOW
      byte b;
      if (setPinsHigh)
      {
        b = 1;
      }
      else
      {
        b = 0;
      }

      //invoke the sendSysex command with ALL_PINS_COMMAND and our data payload as an IBuffer
      firmata.sendSysex(ALL_PINS_COMMAND, new byte[] { b }.AsBuffer());
    }

    public void toggleAllDigitalPinsWithPorts(bool setPinsHigh)
    {
      /*
       * we have 14 pins, so that is pin 0-7 in port 0, and pin 8-13 in port 1.
       */
      if (setPinsHigh)
      {
        firmata.sendDigitalPort(0, 0xFF); //all 8 pins of port 0 HIGH
        firmata.sendDigitalPort(1, 0x3F); //the first 6 pins of port 1 HIGH
      }
      else
      {
        firmata.sendDigitalPort(0, 0x00); //all pins low
        firmata.sendDigitalPort(1, 0x00); //all pins low
      }
    }
  }
}
