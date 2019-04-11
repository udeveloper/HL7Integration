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
using ADT_A01 = NHapi.Model.V23.Message.ADT_A01;
using NHapi.Base.Util;
using HL7NhapiClient.Helpers;

namespace HL7NhapiClient
{
    class Program
    {
        private static int PORT_NUMBER = 1080;
        
        static void Main(string[] args)
        {
            
            try
            {
                // create a new MLLP client over the specified port (note this class is from NHAPI Tools)
                //Note that using higher level encodings such as UTF-16 is not recommended due to conflict with
                //MLLP wrapping characters

                //var connection = new SimpleMLLPClient("localhost", PORT_NUMBER, Encoding.UTF8);

                for (int i=0; i<=4;i++)
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    System.Threading.Thread.Sleep(3000);
                    SendBinaryDataB64InMessageHL7(new SimpleMLLPClient("localhost", PORT_NUMBER, Encoding.UTF8));
                    watch.Stop();
                    Console.WriteLine($"Tiempo Transcurrido Mensaje # {i} + " + watch.Elapsed.TotalSeconds);
                }

                

               // connection.Disconnect();
                
                //TerserMessageHL7();
                //ParserCustomMessageHL7();
                //ParserMessageHL7();
                //SendMessageHL7();
                //CreateMessagesParser(adtMessage);

            }
            catch (Exception e)
            {
                LogToDebugConsole($"Error occured while creating HL7 message {e.Message}");
            }
        }

        private static void SendBinaryDataB64InMessageHL7(SimpleMLLPClient connection)
        {
            // create the HL7 message
            // this OruMessageFactory class is not from NHAPI but my own wrapper class
            var oruMessage = AdtMessageFactory.CreateMessage("R01");
            
            // send the previously created HL7 message over the connection established
            var pipeParser = new PipeParser();
            LogToDebugConsole("Sending ORU R01 message:" + "\n" + pipeParser.Encode(oruMessage));
            var responseMessage = connection.SendHL7Message(oruMessage);

            // display the message response received from the remote party
            var responseString = pipeParser.Encode(responseMessage);
            LogToDebugConsole("Received response:\n" + responseString);

           connection.Disconnect();
           
        }

        private static void TerserMessageHL7()
        {
            var stringMessage = File.ReadAllText(
                                @"N:\hl7-master\hl7-master\Test HL7 Message Files\FileWithObservationResultMessage.txt");

            // instantiate a PipeParser, which handles the normal HL7 encoding
            var pipeParser = new PipeParser();

            // parse the message string into a message object
            var orderResultHl7Message = pipeParser.Parse(stringMessage);

            // create a terser object instance by wrapping it around the message object
            var terser = new Terser(orderResultHl7Message);

            var terserHelper = new TerserHelper(terser);

            var terserExpression = "/.MSH-6";
            var dataRetrieved = terserHelper.GetData(terserExpression);
            LogToDebugConsole($"Field 6 of MSH segment using expression '{terserExpression}' was '{dataRetrieved}'");

            terserExpression = "/.PID-5-2"; // notice the /. to indicate relative position to root node
            dataRetrieved = terserHelper.GetData(terserExpression);
            LogToDebugConsole($"Field 5 and Component 2 of the PID segment using expression '{terserExpression}' was {dataRetrieved}'");

            terserExpression = "/.*ID-5-2";
            dataRetrieved = terserHelper.GetData(terserExpression);
            LogToDebugConsole($"Field 5 and Component 2 of the PID segment using wildcard-based expression '{terserExpression}' was '{dataRetrieved}'");

            terserExpression = "/.P?D-5-2";
            dataRetrieved = terserHelper.GetData(terserExpression);
            LogToDebugConsole($"Field 5 and Component 2 of the PID segment using another wildcard-based expression '{terserExpression}' was '{dataRetrieved}'");


            terserExpression = "/.PV1-9(1)"; // note: field repetitions are zero-indexed
            dataRetrieved = terserHelper.GetData(terserExpression);
            LogToDebugConsole($"2nd repetition of Field 9 and Component 1 for it in the PV1 segment using expression '{terserExpression}' was '{dataRetrieved}'");

            terserExpression = "/RESPONSE/PATIENT/PID-5-1";
            dataRetrieved = terserHelper.GetData(terserExpression);
            LogToDebugConsole($"Terser expression  '{terserExpression}' yielded '{dataRetrieved}'");

            terserExpression = "/RESPONSE/PATIENT/VISIT/PV1-9-3";
            dataRetrieved = terserHelper.GetData(terserExpression);
            LogToDebugConsole($"Terser expression '{terserExpression}' yielded '{dataRetrieved}'");

            terserExpression = "/RESPONSE/ORDER_OBSERVATION(0)/ORC-12-3";
            dataRetrieved = terserHelper.GetData(terserExpression);
            LogToDebugConsole($"Terser expression '{terserExpression}' yielded '{dataRetrieved}'");

            //let us now try a set operation using the terser
            terserExpression = "/.OBSERVATION(0)/NTE-3";
            terserHelper.SetData(terserExpression, "This is our override value using the setter");
            LogToDebugConsole("Set the data for second repetition of the NTE segment and its Third field..");

            LogToDebugConsole("\nWill display our modified message below \n");
            LogToDebugConsole(pipeParser.Encode(orderResultHl7Message));
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
            string messageString ="MSH|^~\\&|SENDING_APPLICATION|SENDING_FACILITY|RECEIVING_APPLICATION|RECEIVING_FACILITY|20110614075841||ACK|1407511|P|2.5||||||\r\n" +
                                        "MSA|AA|1407511|Success||";

            messageString= File.ReadAllText(@"N:\hl7-master\hl7-master\Test HL7 Message Files\FileWithNonConformingAdtA01Message.txt");

            var pipeParser = new PipeParser();
            // parse the string format message into a message object HL7 Estandar
            var hl7Message = pipeParser.Parse(messageString);

            //cast to ACK message to get access to ACK message data
            var ackMessage = (ADT_A01)hl7Message;

            if (ackMessage != null)
            {
                var mshSegment = ackMessage.MSH;

                LogToDebugConsole("Message Type is " + mshSegment.MessageType.MessageType.Value);
                LogToDebugConsole("Message Control Id  " + mshSegment.MessageControlID.Value);
                LogToDebugConsole("Message Timestamp is " + mshSegment.DateTimeOfMessage.TimeOfAnEvent.GetAsDate());
                LogToDebugConsole("Sending Facility is " + mshSegment.SendingFacility.NamespaceID.Value);

                //ackMessage.MSA.AcknowledgmentCode.Value = "AR";
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