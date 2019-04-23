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
            CreateEvnSegment(currentDateTimeString);
            CreatePidSegment();
            CreatePv1Segment();

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
            mshSegment.MessageType.TriggerEvent.Value = "A01";
            mshSegment.VersionID.VersionID.Value = "2.5";
            mshSegment.ProcessingID.ProcessingID.Value = "P";
            
        }

        private void CreateEvnSegment(string currentDateTimeString)
        {
            var evnSegment = _adtMessage.EVN;

            evnSegment.EventTypeCode.Value = "A01";
            evnSegment.RecordedDateTime.Time.Value = currentDateTimeString;
        }

        private void CreatePidSegment()
        {
            var pidSegment = _adtMessage.PID;
            var patientName = pidSegment.GetPatientName(0);  
            patientName.GivenName.Value = "Mouse";
            pidSegment.SetIDPID.Value = "378785433211";
            var patientAddress = pidSegment.GetPatientAddress(0);
            patientAddress.StreetAddress.StreetName.Value = "123 Main Street";
            patientAddress.City.Value = "Lake Buena Vista";
            patientAddress.StateOrProvince.Value = "FL";
            patientAddress.Country.Value = "USA";
        }

        private void CreatePv1Segment()
        {
            var pv1Segment = _adtMessage.PV1;
            pv1Segment.PatientClass.Value = "O";
            var assignedPatientLocation = pv1Segment.AssignedPatientLocation;
            assignedPatientLocation.Facility.NamespaceID.Value = "Some Treatment Facility";
            assignedPatientLocation.PointOfCare.Value = "Some Treatment Facility";
            pv1Segment.AdmissionType.Value = "ALERT";
            var referringDoctor = pv1Segment.GetReferringDoctor(0);
            referringDoctor.IDNumber.Value = "99999999";            
            referringDoctor.GivenName.Value = "Jack";
            referringDoctor.IdentifierTypeCode.Value = "456789";
            pv1Segment.AdmitDateTime.Time.Value = GetCurrentTimeStamp();
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
