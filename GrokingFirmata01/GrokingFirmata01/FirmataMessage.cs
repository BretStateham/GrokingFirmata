using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrokingFirmata01
{
  class FirmataMessage
  {
    public string Message { get; set; }
    public DateTime Received { get; set; }

    public FirmataMessage() : this("")
    {    }

    public FirmataMessage(string Message)
    {
      this.Message = Message;
      Received = DateTime.Now;
    }

    public override string ToString()
    {
      return String.Format("{0:HH:mm:ss:fff} - {1}", Received, Message);
    }
  }
}
