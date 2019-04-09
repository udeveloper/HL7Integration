using NHapi.Model.V25.Message;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7NhapiClient.Builders
{
    internal class AdtA01MessageBuilder
    {
        private ADT_A01 _adtMessage;


        /*You can pass in a domain or data transfer object as a parameter
            when integrating with data from your application here
        */

        public ADT_A01 Build()
        {
            var currentDateTimeString = GetCurrentTimeStamp();
            _adtMessage = new ADT_A01();

            CreateMshSegment(currentDateTimeString);

            return _adtMessage;
        }

        private void CreateMshSegment(string currentDateTimeString)
        {
            var mshSegment = _adtMessage.MSH;

            mshSegment.FieldSeparator.Value = "|";
            mshSegment.EncodingCharacters.Value = "^~\\&";
            mshSegment.SendingApplication.NamespaceID.Value = "SCSE";
            mshSegment.SendingFacility.NamespaceID.Value = "FCI SCSE";
            mshSegment.ReceivingApplication.NamespaceID.Value = "SYSTEM ARMADO CUENTAS";
            mshSegment.ReceivingFacility.NamespaceID.Value = "RED NORTE";
            mshSegment.DateTimeOfMessage.Time.Value = currentDateTimeString;
            mshSegment.MessageControlID.Value = GetSequenceNumber();
            mshSegment.MessageType.MessageCode.Value = "ADT";
            mshSegment.MessageType.TriggerEvent.Value = "001";
            mshSegment.VersionID.VersionID.Value = "2.3";
            mshSegment.ProcessingID.ProcessingID.Value = "P";
            
        }

        private static string GetCurrentTimeStamp()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }

        private static string GetSequenceNumber()
        {
            const string facilityNumberPrefix = "1234"; // some arbitrary prefix for the facility
            return facilityNumberPrefix + GetCurrentTimeStamp();
        }
    }
}
