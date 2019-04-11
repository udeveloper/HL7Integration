using HL7NhapiClient.Helpers;
using NHapi.Model.V25.Datatype;
using NHapi.Model.V25.Message;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7NhapiClient.Builders
{
    public class OruR01MessageBuilder
    {
        private ORU_R01 _oruR01Message;
        private readonly Base64Helper _Base64Helper = new Base64Helper();
        private  string _pdfFilePath = string.Empty;
        public ORU_R01 Build(string filePath)
        {
            _pdfFilePath = filePath;
            var currentDateTimeString = GetCurrentTimeStamp();
            _oruR01Message = new ORU_R01();
            CreateMshSegment(currentDateTimeString);
            CreatePidSegment();
            CreatePv1Segment();
            CreateObrSegment();
            CreateObxSegment();

            return _oruR01Message;
        }

        private void CreateMshSegment(string currentDateTimeString)
        {
            var mshSegment = _oruR01Message.MSH;
            mshSegment.FieldSeparator.Value = "|";
            mshSegment.EncodingCharacters.Value = "^~\\&";
            mshSegment.SendingApplication.NamespaceID.Value = "Our System";
            mshSegment.SendingFacility.NamespaceID.Value = "Our Facility";
            mshSegment.ReceivingApplication.NamespaceID.Value = "Their Remote System";
            mshSegment.ReceivingFacility.NamespaceID.Value = "Their Remote Facility";
            mshSegment.DateTimeOfMessage.Time.Value = currentDateTimeString;
            mshSegment.MessageControlID.Value = GetSequenceNumber();
            mshSegment.MessageType.MessageCode.Value = "ORU";
            mshSegment.MessageType.TriggerEvent.Value = "R01";
            mshSegment.VersionID.VersionID.Value = "2.5";
            mshSegment.ProcessingID.ProcessingID.Value = "P";
        }

        private void CreatePidSegment()
        {
            var pidSegment = _oruR01Message.GetPATIENT_RESULT().PATIENT.PID;

            var patientName = pidSegment.GetPatientName(0);
            patientName.FamilyName.Surname.Value = "Mouse";
            patientName.GivenName.Value = "Mickey";
            pidSegment.PatientID.IDNumber.Value = "378785433211";
            var patientAddress = pidSegment.GetPatientAddress(0);
            patientAddress.StreetAddress.StreetName.Value = "123 Main Street";
            patientAddress.City.Value = "Lake Buena Vista";
            patientAddress.StateOrProvince.Value = "FL";
            patientAddress.Country.Value = "USA";
        }

        private void CreatePv1Segment()
        {
            var patientInformation = _oruR01Message.GetPATIENT_RESULT().PATIENT;
            var visitInformation = patientInformation.VISIT;
            var pv1Segment = visitInformation.PV1;
            pv1Segment.PatientClass.Value = "O"; // to represent an 'Outpatient'
            var assignedPatientLocation = pv1Segment.AssignedPatientLocation;
            assignedPatientLocation.Facility.NamespaceID.Value = "Some Treatment Facility";
            assignedPatientLocation.PointOfCare.Value = "Some Point of Care";
            pv1Segment.AdmissionType.Value = "ALERT";
            var referringDoctor = pv1Segment.GetReferringDoctor(0);
            referringDoctor.IDNumber.Value = "99999999";
            referringDoctor.FamilyName.Surname.Value = "Smith";
            referringDoctor.GivenName.Value = "Jack";
            referringDoctor.IdentifierTypeCode.Value = "456789";
            pv1Segment.AdmitDateTime.Time.Value = GetCurrentTimeStamp();
        }

        private void CreateObrSegment()
        {
            var ourOrderObservation = _oruR01Message.GetPATIENT_RESULT().GetORDER_OBSERVATION();
            var obrSegment = ourOrderObservation.OBR;
            obrSegment.FillerOrderNumber.UniversalID.Value = "123456";
            obrSegment.UniversalServiceIdentifier.Text.Value = "Document";
            obrSegment.ObservationEndDateTime.Time.SetLongDate(DateTime.Now);
            obrSegment.ResultStatus.Value = "F";
        }

        private void CreateObxSegment()
        {
            var ourOrderObservation = _oruR01Message.GetPATIENT_RESULT().GetORDER_OBSERVATION();
            var observationSegment = ourOrderObservation.GetOBSERVATION(0);
            var obxSegment = observationSegment.OBX;

            obxSegment.SetIDOBX.Value = "0";
            //see HL7 table for list of permitted values here. We will use "Encapsulated Data" here
            obxSegment.ValueType.Value = "ED";
            obxSegment.ObservationIdentifier.Identifier.Value = "Report";

            //"Varies" is a NHAPI class to handle data where the appropriate 
            //data type is not known until run-time (e.g. OBX-5)
            var varies = obxSegment.GetObservationValue(0);
            var encapsulatedData = new ED(_oruR01Message, "PDF Report Content");

            encapsulatedData.SourceApplication.NamespaceID.Value = "Our .NET Application";
            encapsulatedData.TypeOfData.Value = "AP"; //see HL7 table 0191: Type of referenced data
            encapsulatedData.DataSubtype.Value = "PDF";
            encapsulatedData.Encoding.Value = "Base64";

            var base64EncodedStringOfPdfReport = _Base64Helper.ConvertToBase64String(new FileInfo(_pdfFilePath));
            encapsulatedData.Data.Value = base64EncodedStringOfPdfReport;
            
            varies.Data = encapsulatedData;
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
