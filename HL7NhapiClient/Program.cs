using NHapi.Base.Parser;
using HL7NhapiClient.CustomSegment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHapiTools.Base.Net;
using NHapi.Model.V25.Message;
using NHapi.Base.Model;

namespace HL7NhapiClient
{
    class Program
    {
        private static int PORT_NUMBER = 1080;
        static void Main(string[] args)
        {
            
            try
            {

                //ParserCustomMessageHL7();
                //ParserMessageHL7();
                // SendMessageHL7();
                // CreateMessagesParser(adtMessage);

            }
            catch (Exception e)
            {
                LogToDebugConsole($"Error occured while creating HL7 message {e.Message}");
            }
        }

        private static void ParserCustomMessageHL7()
        {
            const string customSegmentBasedHl7Message = "MSH|^~\\&|SUNS1|OVI02|AZIS|CMD|200606221348||ADT^A01|1049691900|P|2.3\r"
                                                        + "EVN|A01|200803051509||||200803031508\r"
                                                        + "PID|||5520255^^^PK^PK~ZZZZZZ83M64Z148R^^^CF^CF~ZZZZZZ83M64Z148R^^^SSN^SSN^^20070103^99991231~^^^^TEAM||ZZZ^ZZZ||19830824|F||||||||||||||||||||||N\r"
                                                        + "ZPV|Some Custom Notes|Additional custom description of the visit goes here";
            var parserPipe = new PipeParser();
            var parsedMessage = parserPipe.Parse(customSegmentBasedHl7Message, "2.3.CustomZSegments");

            LogToDebugConsole("Type: " + parsedMessage.GetType());

            var zpvA01 = (CustomSegment.ADT_A01)parsedMessage;

            if (zpvA01 != null)
            {
                LogToDebugConsole(zpvA01.ZPV.CustomNotes.Value);
                LogToDebugConsole(zpvA01.ZPV.CustomDescription.Value);
            }
        }

        private static void ParserMessageHL7()
        {
            const string messageString = "MSH|^~\\&|SENDING_APPLICATION|SENDING_FACILITY|RECEIVING_APPLICATION|RECEIVING_FACILITY|20110614075841||ACK|1407511|P|2.5||||||\r\n" +
                                        "MSA|AA|1407511|Success||";
            var pipeParser = new PipeParser();
            // parse the string format message into a message object HL7 Estandar
            var hl7Message = pipeParser.Parse(messageString);

            //cast to ACK message to get access to ACK message data
            var ackMessage = (ACK)hl7Message;

            if (ackMessage != null)
            {
                var mshSegment = ackMessage.MSH;

                LogToDebugConsole("Message Type is " + mshSegment.MessageType.MessageCode.Value);
                LogToDebugConsole("Message Control Id  " + mshSegment.MessageControlID.Value);
                LogToDebugConsole("Message Timestamp is " + mshSegment.DateTimeOfMessage.Time.GetAsDate());
                LogToDebugConsole("Sending Facility is " + mshSegment.SendingFacility.NamespaceID.Value);

                ackMessage.MSA.AcknowledgmentCode.Value = "AR";
            }

            // Display the updated HL7 message using Pipe delimited format
            LogToDebugConsole("HL7 Pipe Delimited Message Output:");
            LogToDebugConsole(pipeParser.Encode(hl7Message));

            // instantiate an XML parser that NHAPI provides 
            var xmlParser = new DefaultXMLParser();

            // convert from default encoded message into XML format, and send it to standard out for display
            LogToDebugConsole("HL7 XML Formatted Message Output:");
            LogToDebugConsole(xmlParser.Encode(hl7Message));
        }

        private static void SendMessageHL7()
        {
            // create the HL7 message
            // this AdtMessageFactory class is not from NHAPI but my own wrapper
            LogToDebugConsole("Creating ADT A01 message...");
            var adtMessage = AdtMessageFactory.CreateMessage("A01");

            // create a new MLLP client over the specified port (note this class is from NHAPI Tools)
            //Note that using higher level encodings such as UTF-16 is not recommended due to conflict with
            //MLLP wrapping characters
            var connection = new SimpleMLLPClient("localhost", PORT_NUMBER, Encoding.UTF8);

            var parser = new PipeParser();
            LogToDebugConsole("Sending message:" + "\n" + parser.Encode(adtMessage));
            var response = connection.SendHL7Message(adtMessage);

            var responseString = parser.Encode(response);
            LogToDebugConsole("Received response:\n" + responseString);
        }

        private static void CreateMessagesParser(IMessage adtMessage)
        {
            // create these parsers for the file encoding operations
            var pipeParser = new PipeParser();
            var xmlParser = new DefaultXMLParser();

            // print out the message that we constructed
            LogToDebugConsole("Message was constructed successfully..." + "\n");

            // serialize the message to pipe delimited output file
            WriteMessageFile(pipeParser, adtMessage, "N:\\HL7TestOutputs", "testPipeDelimitedOutputFile.txt");

            // serialize the message to XML format output file
            WriteMessageFile(xmlParser, adtMessage, "N:\\HL7TestOutputs", "testXmlOutputFile.xml");
        }

        private static void WriteMessageFile(ParserBase parser, NHapi.Base.Model.IMessage hl7Message, string outputDirectory, string outputFileName)
        {
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            var fileName = Path.Combine(outputDirectory, outputFileName);

            LogToDebugConsole("Writing data to file...");

            if (File.Exists(fileName))
                File.Delete(fileName);
            File.WriteAllText(fileName, parser.Encode(hl7Message));
            LogToDebugConsole($"Wrote data to file {fileName} successfully...");
        }

        private static void LogToDebugConsole(string informationToLog)
        {
            Debug.WriteLine(informationToLog);
        }
    }
}